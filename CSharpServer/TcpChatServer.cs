using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;

namespace MemoryPackChatServer
{
    public class TcpChatServer
    {
        const int PORT = 7777;

        private TcpListener _listener;

        private Clients _clients;

        private Task _acceptTask;

        public TcpChatServer()
        {
            _listener = new TcpListener(IPAddress.Any, PORT);
            _clients = new Clients();

            _acceptTask = new Task(AcceptClientThread);
        }

        public void Run()
        {
            _listener.Start();
            _clients.OnPacketRecieved(OnReceivePacketAction);
            
            Console.WriteLine($"Server started on {_listener.LocalEndpoint}");
            
            _acceptTask.Start();
            _acceptTask.Wait();
        }

        private void OnReceivePacketAction(Packet packet)
        {
            Broadcast(packet);
        }

        private async void AcceptClientThread()
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _clients.Add(client);

            AcceptClientThread();
        }

        private void Broadcast(Packet packet)
        {
            var clients = _clients.GetClients();
            foreach (var client in clients)
            {
                Send(client, packet);
            }
        }

        private void Send(TcpClient client, Packet packet)
        {
            var bin = MemoryPackSerializer.Serialize(packet);

            if (client.Connected)
            {
                var stream = client.GetStream();
                stream.Write(bin, 0, bin.Length);
                stream.Flush();
            }
            else
            {
                Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                _clients.Remove(client);
            }
        }
    }
}

namespace CustomPacket
{
    public class TcpChatServer
    {
        const int PORT = 7777;

        private TcpListener _listener;

        private Clients _clients;

        private Task _acceptTask;

        public TcpChatServer()
        {
            _listener = new TcpListener(IPAddress.Any, PORT);
            _clients = new Clients();

            _acceptTask = new Task(AcceptClientThread);
        }

        public void Run()
        {
            _listener.Start();
            _clients.OnPacketRecieved(OnReceivePacketAction);

            Console.WriteLine($"Server started on {_listener.LocalEndpoint}");

            _acceptTask.Start();
            _acceptTask.Wait();
        }

        private void OnReceivePacketAction(Packet packet)
        {
            Broadcast(packet);
        }

        private async void AcceptClientThread()
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            _clients.Add(client);

            AcceptClientThread();
        }

        private void Broadcast(Packet packet)
        {
            var clients = _clients.GetClients();
            foreach (var client in clients)
            {
                Send(client, packet);
            }
        }

        private void Send(TcpClient client, Packet packet)
        {
            var bin = packet.Serialize();

            if (client.Connected)
            {
                var stream = client.GetStream();
                stream.Write(bin, 0, bin.Length);
                stream.Flush();
            }
            else
            {
                Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                _clients.Remove(client);
            }
        }
    }
}
