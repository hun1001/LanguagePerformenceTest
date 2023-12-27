using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Collections.Generic;

public class TcpChatClient
{
    private static readonly Dictionary<ServerType, bool> FailedServerDictionary = new();
    public static bool IsFailedServer(ServerType serverType) => FailedServerDictionary.ContainsKey(serverType) && FailedServerDictionary[serverType];

    protected TcpClient _client;
    private int Port;

    private Action<Packet> OnReceivePacket;

    public void AddReceivePacketListener(Action<Packet> listener)
    {
        OnReceivePacket += listener;
    }

    public TcpChatClient()
    {
    }

    public TcpChatClient(ServerType st) => Init(st);

    public void Init(ServerType serverType)
    {
        if (FailedServerDictionary.ContainsKey(serverType) && FailedServerDictionary[serverType])
        {
            return;
        }

        Port = (int)serverType;

        try
        {
            _client = new TcpClient();
            if (_client.ConnectAsync("127.0.0.1", Port).Wait(1000))
            {
                Debug.Log($"Connected to server {serverType}");
                FailedServerDictionary[serverType] = false;
            }
            else
            {
                Debug.Log($"Failed to connect to server {serverType}");
                FailedServerDictionary[serverType] = true;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Application.Quit();
        }
    }

    public void Send(Packet packet)
    {
        if (_client is not { Connected: true })
            return;

        var bin = packet.Serialize();
        var stream = _client.GetStream();

        stream.Write(bin, 0, bin.Length);
        stream.Flush();
    }

    public virtual IEnumerator Receive()
    {
        var stream = _client.GetStream();

        while (true)
        {
            if (_client.Available > 0)
            {
                var bin = new byte[_client.Available];
                _ = stream.Read(bin, 0, bin.Length);

                var packet = Packet.Deserialize(bin);
                OnReceivePacket?.Invoke(packet);
            }

            yield return null;
        }
    }
}