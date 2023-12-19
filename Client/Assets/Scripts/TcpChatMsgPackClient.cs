using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using MemoryPack;
using System;

public class TcpChatMsgPackClient
{
    private readonly TcpClient _client;
    private const int Port = 7777;

    private Action<MsgPackPacket> OnReceivePacket;

    public void AddReceivePacketListener(Action<MsgPackPacket> listener)
    {
        OnReceivePacket += listener;
    }

    public TcpChatMsgPackClient()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect("221.140.152.102", Port);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Application.Quit();
        }
    }

    public void Send(MsgPackPacket packet)
    {
        var bin = MemoryPackSerializer.Serialize(packet);
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

                var packet = MemoryPackSerializer.Deserialize<MsgPackPacket>(bin);
                OnReceivePacket?.Invoke(packet);
            }

            yield return null;
        }
    }
}