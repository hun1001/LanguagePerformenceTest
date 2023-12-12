#include <iostream>
#include <atlstr.h>
#include <WinSock2.h>
#include <WS2tcpip.h>
#include <thread>
#include <vector>
#include "Packet.h"

#pragma comment(lib, "ws2_32.lib")

using namespace std;

vector<SOCKET> g_clients;
vector<thread*> g_threads;

void Broadcast(Packet packet)
{
	BYTE* buf = packet.Serialize();
	int len = sizeof(packet);

	for (SOCKET s : g_clients)
	{
		send(s, (char*)buf, len, 0);
	}
}

void ClientThread(SOCKET sock)
{
	while (true)
	{
		BYTE buf[1024];
		int len = recv(sock, (char*)buf, sizeof(buf), 0);

		if (len == SOCKET_ERROR)
		{
			cout << "recv failed with error: " << WSAGetLastError() << endl;
			return;
		}

		if (len == 0)
		{
			cout << "client disconnected" << endl;
			return;
		}

		Packet packet = Packet::Deserialize(buf, len);;

		Broadcast(packet);
	}
}

int main()
{
	WSADATA wsaData;
	if(WSAStartup(MAKEWORD(2, 2), &wsaData) != NO_ERROR)
	{
		cout << "WSAStartup failed with error: " << WSAGetLastError() << endl;
		return 1;
	}

	SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

	if(s == INVALID_SOCKET)
	{
		cout << "socket failed with error: " << WSAGetLastError() << endl;
		return 1;
	}

	sockaddr_in addr;
	addr.sin_family = AF_INET;
	addr.sin_port = htons(7777);
	inet_pton(AF_INET, "221.140.152.102", &addr.sin_addr);

	if(bind(s, (sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR)
	{
		cout << "bind failed with error: " << WSAGetLastError() << endl;
		return 1;
	}

	if(listen(s, SOMAXCONN) == SOCKET_ERROR)
	{
		cout << "listen failed with error: " << WSAGetLastError() << endl;
		return 1;
	}

	cout << "server started" << endl;
	
	while (true)
	{
		SOCKET accSock = accept(s, NULL, NULL);

		if(accSock == INVALID_SOCKET)
		{
			cout << "accept failed with error: " << WSAGetLastError() << endl;
			return 1;
		}

		cout << "client connected" << endl;

		g_clients.push_back(accSock);
		thread* t = new thread(ClientThread, accSock);
		g_threads.push_back(t);
	}

	return 0;
}