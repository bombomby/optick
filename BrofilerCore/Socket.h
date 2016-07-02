#pragma once
#include "Common.h"
#include <string>
#include <windows.h>
#include <winsock.h>
//#ifndef __WINSOCK2_H
//#include <winsock2.h>
//#endif

namespace Profiler
{
	class Wsa
	{
		bool isInitialized;
		WSADATA data;

		Wsa()
		{
			isInitialized = WSAStartup(0x0202, &data) == ERROR_SUCCESS;
			BRO_ASSERT(isInitialized, "Can't initialize WSA");
		}

		~Wsa()
		{
			if (isInitialized)
			{
				WSACleanup();
			}
		}
	public:
		static bool Init()
		{
			static Wsa wsa;
			return wsa.isInitialized;
		}
	};

	class Socket
	{
		SOCKET acceptSocket;
		SOCKET listenSocket;
		u_long host;
		sockaddr_in address;

		fd_set recieveSet;

		CriticalSection lock;
		std::string errorMessage;

		void Close()
		{
			if (listenSocket != INVALID_SOCKET)
			{
				::closesocket(listenSocket);
				listenSocket = INVALID_SOCKET;
			}
		}

		int Bind(short port)
		{
			address.sin_family      = AF_INET;
			address.sin_addr.s_addr = INADDR_ANY;
			address.sin_port        = htons(port);

			if (::bind(listenSocket, (sockaddr *)&address, sizeof(address)) == 0)
				return ERROR_SUCCESS;

			return WSAGetLastError();
		}

		void GetErrorMessage()
		{
			LPSTR errString = NULL;  
			FormatMessage( FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM, 0, WSAGetLastError(), 0, (LPSTR)&errString, 0, 0 );
			errorMessage = errString;
			LocalFree( errString );
		}

		void Disconnect()
		{ 
			CRITICAL_SECTION(lock);

			if (acceptSocket != INVALID_SOCKET)
			{
				::closesocket(acceptSocket);
				acceptSocket = INVALID_SOCKET;
			}
		}
	public:
		Socket() : listenSocket(0), acceptSocket(0), host(0)
		{
			Wsa::Init();
			listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
			BRO_VERIFY(listenSocket != INVALID_SOCKET, "Can't create socket", GetErrorMessage());
		}

		~Socket()
		{
			Disconnect();
			Close();
		}

		bool Bind(short startPort, short portRange)
		{
			for (short port = startPort; port < startPort + portRange; ++port)
			{
				int result = Bind(port);

				if (result == WSAEADDRINUSE)
					continue;

				BRO_VERIFY(result == ERROR_SUCCESS, "Can't bind to specified port", GetErrorMessage());
				return result == ERROR_SUCCESS;
			}

			return false;
		}

		void Listen()
		{
			int result = listen(listenSocket, SOMAXCONN);
			BRO_UNUSED(result);
			BRO_VERIFY(result == ERROR_SUCCESS, "Can't start listening", GetErrorMessage());
		}

		void Accept()
		{ 
			SOCKET incomingSocket = accept(listenSocket, nullptr, nullptr);
			BRO_VERIFY(incomingSocket != INVALID_SOCKET, "Can't accept socket", GetErrorMessage());

			CRITICAL_SECTION(lock);
			acceptSocket = incomingSocket;
		}

		bool Send(const char *buf, size_t len)
		{
			CRITICAL_SECTION(lock);

			if (acceptSocket == INVALID_SOCKET)
				return false;

			if (::send(acceptSocket, buf, (int)len, 0) == SOCKET_ERROR)
			{
				Disconnect();
				return false;
			}

			return true;
		}

		int Receive(char *buf, int len)
		{ 
			CRITICAL_SECTION(lock);

			if (acceptSocket == INVALID_SOCKET)
				return 0;

			recieveSet.fd_count = 1;
			recieveSet.fd_array[0] = acceptSocket;

			static timeval lim = {0};

			if (select(0, &recieveSet, nullptr, nullptr, &lim) == 1)
				return ::recv(acceptSocket, buf, len, 0);

			return 0;
		}
	};
}


