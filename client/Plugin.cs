using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using BepInEx;
using Characters;
using HarmonyLib;
using Levels;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using PlatformSpecific;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace MultiplayerDropIn;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    public static Dictionary<Guid, GameObject> Players = new Dictionary<Guid, GameObject>();

    // private static ConfigEntry<Boolean> Host_Machine;
    // private static ConfigEntry<int> Port;
    // private static ConfigEntry<string> Address;

    private static PaqueretteController clientPlayer;
    private static Guid g;
    private static GameObject onlineUsers; 
    
    private static Message lastSent;
    private static Stack changes = new Stack();
    // private static bool nametags = false;
    private static bool showPlayerList = false;

    private void Awake()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        g = Guid.NewGuid();

        // TODO: These calls need to be split off to whatever respective patch is necessary to do a server creation or client creation
        // if (Host_Machine.Value)
        // {
        //     Thread thread = new Thread(new ThreadStart(ServerStart));
        //     thread.Start();
        // }
        // else
        // {
        //     Thread thread = new Thread(new ThreadStart(ClientStart));
        //     
        // }

        // TODO: Currently assumed to be server on game start for debugging reasons
        Thread thread = new Thread(() => ClientHandler.ClientStart("141.148.63.115", 25565, ref changes));
        thread.Start();

        harmony.PatchAll();
    }

    
    // On player load
    [HarmonyPatch(typeof(PaqueretteController), "Start")]
    class PlayerMove
    {
        [HarmonyPostfix]
        static void setpatch()
        {
            Console.WriteLine("AYAYAYA");
            if (clientPlayer == null)
            {
                Application.runInBackground = true;
                clientPlayer = GameManager.PaqueretteController;
                var canvas = GameObject.Find("Canvas");
                var reference = GameObject.Find("BunniesCounter");
                var start = reference.GetComponentInChildren<TextMeshProUGUI>();
                onlineUsers = new GameObject("PlayerList");
                onlineUsers.transform.parent = canvas.transform;
                onlineUsers.transform.position = new Vector3(6.5f, -1.2f, 0);
                onlineUsers.transform.localScale = Vector3.one;
                var text = onlineUsers.gameObject.AddComponent<TextMeshProUGUI>();
                text.font = start.font;
                text.fontSize = 8;
                GameManager.OnDoneLoadingLevel += SideExit;
                GameManager.PaqueretteController.OnSuccessfulMove += Move;
            }
        }
    }

    [HarmonyPatch(typeof(PaqueretteController), "Update")]
    class Update
    {
        [HarmonyPostfix]
        static void setpatch()
        {
            if (Keyboard.current[Key.Tab].isPressed && !showPlayerList && clientPlayer)
            {
                showPlayerList = true;
                GetPlayers();
            }
            if (Keyboard.current[Key.Tab].wasReleasedThisFrame && clientPlayer)
            {
                showPlayerList = false;
                ClearPlayers();
            }

            if (changes.Count > 0)
            {
                Message message = (Message)changes.Pop();
                // Console.WriteLine($"Recieved: {message}");

                if (message.Action.Equals("delete"))
                {
                    DeletePlayer(message.UserID);
                    Players.Remove(message.UserID);
                }
                else if (message.Action.Equals("move") || message.Action.Equals("transition"))
                {
                    DrawPlayer(message);

                }
                else if (message.Action.Equals("users"))
                {
                    ListUsers(message);
                }
            }
        }

    }

    [HarmonyPatch(typeof(SteamworksManager), "OnApplicationQuit")]
    class Quit
    {
        [HarmonyPrefix]
        static void setpatch()
        {
            lastSent.Action = "delete";
            ClientHandler.SendToServer(lastSent);
        }
    }


    static void DeletePlayer(Guid id)
    {
        if (!Players.ContainsKey(id))
        {
            return;
        }

        GameObject go = Players[id];
        Destroy(go);
    }
    static void DrawPlayer(Message message)
    {
        GameObject go;
        if (!Players.ContainsKey(message.UserID))
        {
            go = Instantiate(clientPlayer.gameObject);
            // var text = go.AddComponent<TextMeshProUGUI>();
            // text.text = message.UserID.ToString();
            // text.enabled = nametags;
            Players.Add(message.UserID, go);
        }
        else
        {
            go = Players[message.UserID];
        }

        var coordinates = message.PositionVec();

        try
        {
            go.transform.position = coordinates;
            go.GetComponent<PaqueretteController>().enabled = true;
            go.GetComponent<PaqueretteController>().ForceTurn(message.Facing);
            go.GetComponent<PaqueretteController>().enabled = false;

        }
        catch (FormatException)
        {
            Console.WriteLine(coordinates);
        }
    }

    static void GetPlayers()
    {
        Message message = new Message(LevelIndicatorGenerator.GetShortLevelIndicator(), g,
    clientPlayer.gameObject.transform.position.ToString());
        message.Action = "users";
        ClientHandler.SendToServer(message);
    }
    
    static void ListUsers(Message message)
    {
        var text = onlineUsers.gameObject.GetComponent<TextMeshProUGUI>();
        text.text =  message.Extra.Replace("@", Environment.NewLine);
    }

    static void ClearPlayers()
    {
        var text = onlineUsers.gameObject.GetComponent<TextMeshProUGUI>();
        text.text = "";
    }
    // static void ToggleNames()
    // {
    //     nametags = !nametags;
    //     foreach (var id in Players)
    //     {
    //         Players[id.Key].GetComponent<TextMeshProUGUI>().enabled = nametags;
    //     }
    // }

    
    
    static void SideExit()
    {
        lastSent.Action = "delete";
        ClientHandler.SendToServer(lastSent);
        foreach (var id in Players)
        {
            DeletePlayer(id.Key);
        }
        Players = new Dictionary<Guid, GameObject>();
        
        Message message = BuildMessage();
        lastSent ??= message;
        message.Facing = clientPlayer.FacedDirection;
        message.Action = "transition";

        if (!message.Equals(lastSent))
        {
            lastSent = message;
            ClientHandler.SendToServer(message);
        }

    }

    static void Move()
    {
        Message message = BuildMessage();
        lastSent ??= message;
        message.Facing = clientPlayer.FacedDirection;
        message.Action = "move";


        if (!message.Equals(lastSent))
        {
            lastSent = message;
            ClientHandler.SendToServer(message);
        }

    }

    static Message BuildMessage()
    {
       return new Message(LevelIndicatorGenerator.GetShortLevelIndicator(), g,
            clientPlayer.gameObject.transform.position.ToString());
    }
    
    
}