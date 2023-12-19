using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using System;

public class TcpChatCustomClient
{
    private readonly TcpClient _client;
    private const int Port = 7777;

    private Action<Packet> OnReceivePacket;

    public void AddReceivePacketListener(Action<Packet> listener)
    {
        OnReceivePacket += listener;
    }

    public TcpChatCustomClient()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect("127.0.0.1", Port);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Application.Quit();
        }
    }

    public void Send(Packet packet)
    {
        var bin = packet.Serialize();
        var stream = _client.GetStream();

        stream.Write(bin, 0, bin.Length);
        stream.Flush();
    }

    public IEnumerator Receive()
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