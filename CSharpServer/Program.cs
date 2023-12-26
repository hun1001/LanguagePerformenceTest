
MemoryPackChatServer.TcpChatServer memoryServer = new MemoryPackChatServer.TcpChatServer();
CustomPacket.TcpChatServer customServer = new CustomPacket.TcpChatServer();

memoryServer.Run();
customServer.Run();

Console.ReadKey();