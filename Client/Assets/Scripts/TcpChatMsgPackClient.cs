using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using MemoryPack;
using System;

public class TcpChatMsgPackClient
{
    private TcpClient _client = null;
    private const int _port = 7777;

    private Action<MsgPackPacket> OnReceivePacket = null;
    public void AddReceivePacketListener(Action<MsgPackPacket> listener) => OnReceivePacket += listener;

    public TcpChatMsgPackClient()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect("221.140.152.102", _port);
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
        byte[] bin;

        while (true)
        {
            if(_client.Available > 0)
            {
                bin = new byte[_client.Available];
                stream.Read(bin, 0, bin.Length);

                var packet = MemoryPackSerializer.Deserialize<MsgPackPacket>(bin);
                OnReceivePacket?.Invoke(packet);
            }
            yield return null;
        }
    }
}
