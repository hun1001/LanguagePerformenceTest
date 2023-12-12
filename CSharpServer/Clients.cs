using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MemoryPackChatServer
{
    public class Clients
    {
        private List<TcpClient> clients;

        private Action<Packet>? _onPacketRecieved;
        public void OnPacketRecieved(Action<Packet> action) => _onPacketRecieved = action;
        
        public Clients()
        {
            clients = new List<TcpClient>();
            _onPacketRecieved = null;
        }

        public void Add(TcpClient client)
        {
            clients.Add(client);
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
            
            Task.Run(() => RecievePacket(client));
        }

        public void Remove(TcpClient client)
        {
            clients.Remove(client);
            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
        }

        private WaitCallback RecievePacket(TcpClient client)
        {
            NetworkStream stream;
            byte[] buffer;

            while (client.Connected)
            {
                if (client.Available > 0)
                {
                    stream = client.GetStream();
                    buffer = new byte[client.Available];

                    var len = stream.Read(buffer, 0, buffer.Length);

                    var packet = MemoryPack.MemoryPackSerializer.Deserialize<Packet>(buffer);

                    if (packet != null)
                    {
                        _onPacketRecieved?.Invoke(packet);
                    }
                }
            }

            return (_) => Remove(client);
        }

        public TcpClient[] GetClients() => clients.ToArray();
    }
}

namespace CustomPacket
{
    public class Clients
    {
        private List<TcpClient> clients;

        private Action<Packet>? _onPacketRecieved;
        public void OnPacketRecieved(Action<Packet> action) => _onPacketRecieved = action;

        public Clients()
        {
            clients = new List<TcpClient>();
            _onPacketRecieved = null;
        }

        public void Add(TcpClient client)
        {
            clients.Add(client);
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");

            Task.Run(() => RecievePacket(client));
        }

        public void Remove(TcpClient client)
        {
            clients.Remove(client);
            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
        }

        private WaitCallback RecievePacket(TcpClient client)
        {
            NetworkStream stream;
            byte[] buffer;

            while (client.Connected)
            {
                if (client.Available > 0)
                {
                    stream = client.GetStream();
                    buffer = new byte[client.Available];

                    var len = stream.Read(buffer, 0, buffer.Length);

                    var packet = Packet.Deserialize(buffer);

                    _onPacketRecieved?.Invoke(packet);
                }
            }

            return (_) => Remove(client);
        }

        public TcpClient[] GetClients() => clients.ToArray();
    }
}