using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BepInEx;
using HarmonyLib;

namespace MultiplayerDropIn;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.ServerHandler", $"{MyPluginInfo.PLUGIN_GUID}.ClientHandler",
    MyPluginInfo.PLUGIN_VERSION)]
public class ServerHandler : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Logger.LogInfo($"ServerHandler plugin is loaded");
        harmony.PatchAll();
    }

    public static void ServerStart(int port, ref Stack changes)
    {
        Console.WriteLine($"Creating host server on {port}");
        try
        {
            while (true)
            {
                byte[] data = new byte[1024];
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);
                UdpClient newsock = new UdpClient(ipep);

                Console.WriteLine("Waiting for a client...");

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                while (true)
                {
                    data = newsock.Receive(ref sender);
                    changes.Push(sender.Address.ToString());
                    changes.Push(data);

                    Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
                    newsock.Send(data, data.Length, sender);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine($"FAILED to create server on {port}");
        }
    }
}