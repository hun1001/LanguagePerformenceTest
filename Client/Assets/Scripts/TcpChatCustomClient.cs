using System.Collections;
using UnityEngine;
using System.Net.Sockets;
using MemoryPack;
using System;

public class TcpChatCustomClient
{
    private TcpClient _client = null;
    private const int _port = 7777;

    private Action<Packet> OnReceivePacket = null;
    public void AddReceivePacketListener(Action<Packet> listener) => OnReceivePacket += listener;

    public TcpChatCustomClient()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect("127.0.0.1", _port);
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
        byte[] bin;

        while (true)
        {
            if (_client.Available > 0)
            {
                bin = new byte[_client.Available];
                stream.Read(bin, 0, bin.Length);

                var packet = Packet.Deserialize(bin);
                OnReceivePacket?.Invoke(packet);
            }
            yield return null;
        }
    }
}
