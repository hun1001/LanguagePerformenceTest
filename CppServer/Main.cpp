#include <iostream>
#include <WinSock2.h>
#include <WS2tcpip.h>
#include <thread>
#include <vector>
#include "Packet.h"

#pragma comment(lib, "ws2_32.lib")

using namespace std;

vector<SOCKET> g_clients;
vector<thread*> g_threads;

void broadcast(Packet packet)
{
	cout << "broadcasting packet" << '\n';

	BYTE* buf = packet.Serialize();

	for (const SOCKET s : g_clients)
	{
		constexpr int len = sizeof(packet);
		send(s, reinterpret_cast<char*>(buf), len, 0);
	}
}

void client_thread(const SOCKET sock)
{
	while (true)
	{
		BYTE buf[1024];
		const int len = recv(sock, reinterpret_cast<char*>(buf), sizeof(buf), 0);

		const Packet packet = Packet::Deserialize(buf, len);

		broadcast(packet);
	}
}

int main()
{
	WSADATA wsa_data;
	if(WSAStartup(MAKEWORD(2, 2), &wsa_data) != NO_ERROR)
	{
		cout << "WSAStartup failed with error: " << WSAGetLastError() << '\n';
		return 1;
	}

	const SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

	if(s == INVALID_SOCKET)
	{
		cout << "socket failed with error: " << WSAGetLastError() << '\n';
		return 1;
	}

	sockaddr_in addr;
	addr.sin_family = AF_INET;
	addr.sin_port = htons(3333);
	inet_pton(AF_INET, "127.0.0.1", &addr.sin_addr);

	if(bind(s, reinterpret_cast<sockaddr*>(&addr), sizeof(addr)) == SOCKET_ERROR)
	{
		cout << "bind failed with error: " << WSAGetLastError() << '\n';
		return 1;
	}

	if(listen(s, SOMAXCONN) == SOCKET_ERROR)
	{
		cout << "listen failed with error: " << WSAGetLastError() << '\n';
		return 1;
	}

	cout << "server started" << '\n';
	
	while (true)
	{
		SOCKET acc_sock = accept(s, nullptr, nullptr);

		if(acc_sock == INVALID_SOCKET)
		{
			cout << "accept failed with error: " << WSAGetLastError() << '\n';
			return 1;
		}

		cout << "client connected" << '\n';

		g_clients.push_back(acc_sock);
		auto t = new thread(client_thread, acc_sock);
		g_threads.push_back(t);
	}
}
