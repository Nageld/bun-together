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
using UnityEngine;
using UnityEngine.XR.WSA.Input;

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

    private static Message lastSent;
    private static Stack changes = new Stack();

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

    static void DrawPlayer(Message message)
    {
        GameObject go;
        if (!Players.ContainsKey(message.UserID))
        {
            go = Instantiate(clientPlayer.gameObject);
            go.GetComponent<PaqueretteController>().enabled = false;
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

    [HarmonyPatch(typeof(PaqueretteController), "HandleMovementDone")]
    class PlayerMove
    {
        [HarmonyPostfix]
        static void setpatch()
        {
            if (clientPlayer == null)
            {
                Application.runInBackground = true;
                clientPlayer = GameManager.PaqueretteController;
            }
    
            Message message = new Message(LevelIndicatorGenerator.GetShortLevelIndicator(), g,
                clientPlayer.gameObject.transform.position.ToString());
            lastSent ??= message;
            message.Facing = clientPlayer.FacedDirection;
            message.Action = "move";

            
            if (!message.Equals(lastSent))
            {
                lastSent = message;
                Console.WriteLine($"Sent: {message}");
                ClientHandler.SendToServer(message);
            }

        }
    }

    [HarmonyPatch(typeof(PaqueretteController), "Update")]
    class Update
    {
        [HarmonyPostfix]
        static void setpatch()
        {
            try
            {
                string level = LevelIndicatorGenerator.GetShortLevelIndicator();
            }
            catch
            {
                Console.WriteLine("Not loaded yet");
            }

            if (changes.Count > 0)
            {
                Message message = (Message)changes.Pop();
                Console.WriteLine($"Recieved: {message}");

                if (message.Action.Equals("delete"))
                {
                    DeletePlayer(message.UserID);
                    Players.Remove(message.UserID);
                }
                else
                {
                    DrawPlayer(message);

                }
            }
        }

        static void DrawPlayer(Message message)
        {
            GameObject go;
            if (!Players.ContainsKey(message.UserID))
            {
                go = Instantiate(clientPlayer.gameObject);
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

    }
    
    
    [HarmonyPatch(typeof(UIController), "DisplayLevelIndicator")]
    class ChangeScreen
    {
        [HarmonyPostfix]
        static void setpatch()
        {

            if (clientPlayer == null)
            {
                return;
            }

            lastSent.Action = "delete";
            ClientHandler.SendToServer(lastSent);
            foreach (var id in Players)
            {
                DeletePlayer(id.Key);
            }

            Players = new Dictionary<Guid, GameObject>();


            Message message = new Message(LevelIndicatorGenerator.GetShortLevelIndicator(), g,
                clientPlayer.gameObject.transform.position.ToString());
            lastSent ??= message;
            message.Facing = clientPlayer.FacedDirection;
            message.Action = "transition";

            if (!message.Equals(lastSent))
            {
                lastSent = message;
                Console.WriteLine($"TRANSITION: {message}");
                ClientHandler.SendToServer(message);
            }

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
    
}