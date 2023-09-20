using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.XR;

namespace MultiplayerDropIn;

public struct ConnectionInfo
{
    public Boolean IsHost;
    public String Address;
    public int Port;
}

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.ClientHandler", $"{MyPluginInfo.PLUGIN_GUID}.ClientHandler", MyPluginInfo.PLUGIN_VERSION)]
public class ClientHandler : BaseUnityPlugin
{
    public static ConnectionInfo ServerInfo;
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    public static UdpClient udpClient;
    
    private void Awake()
    {
        Logger.LogInfo($"ClientHandler plugin is loaded");
        harmony.PatchAll();
    }

    public static void ClientStart(String ip, int port, ref Stack changes)
    {
        Console.WriteLine($"Connecting to {ip} on port {port}");
        
        udpClient = new UdpClient();
        try
        {
            Stack changed = changes;
            udpClient.Connect(ip, port);
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Thread thread = new Thread(() => AwaitMessages(ref udpClient, RemoteIpEndPoint, ref changed ));
            thread.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            Console.WriteLine($"FAILED to connect to {ip} on port {port}");
        }
    }


    private static void AwaitMessages(ref UdpClient udpClient, IPEndPoint RemoteIpEndPoint, ref Stack changes)
    {
        Console.WriteLine($"AWAITING");

        while (true)
        {

            Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);

            string returnData = Encoding.ASCII.GetString(receiveBytes);
            try
            {
                
                var result = JsonConvert.DeserializeObject<Message>(returnData);
                // Console.WriteLine($"Received: {result}");
                changes.Push(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    
    public static void SendToServer(Message message)
    {
        
        udpClient.Send(Encoding.ASCII.GetBytes(message.ToString()), Encoding.ASCII.GetBytes(message.ToString()).Length);
    }

}