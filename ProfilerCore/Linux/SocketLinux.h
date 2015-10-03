#pragma once

#include "Common.h"
#include <string>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <unistd.h>
#include <sys/types.h> 
#include <sys/socket.h>
#include <netinet/in.h>

namespace Profiler
{
	class Socket
	{
		int acceptSocket;
		int listenSocket;
		u_long host;
		sockaddr_in address;

		fd_set recieveSet;

		CriticalSection lock;
		std::string errorMessage;

		void Close()
		{
			if (listenSocket != 0)
			{
				shutdown(listenSocket, 2);
				listenSocket = 0;
			}
		}

		int Bind(short port)
		{
			address.sin_family      = AF_INET;
			address.sin_addr.s_addr = INADDR_ANY;
			address.sin_port        = htons(port);

			if (bind(listenSocket, (sockaddr *)&address, sizeof(address)) == 0)
				return 0;

			return errno;
		}

		void GetErrorMessage()
		{
			errorMessage = strerror(errno);
		}

		void Disconnect()
		{ 
			CRITICAL_SECTION(lock);

			if (acceptSocket != 0)
			{
				shutdown(acceptSocket, 2);
				acceptSocket = 0;
			}
		}
	public:
		Socket() : listenSocket(0), acceptSocket(0), host(0)
		{
			listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
			BRO_VERIFY(listenSocket >= 0, "Can't create socket", GetErrorMessage());
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

				if (result == EADDRINUSE)
					continue;

				BRO_VERIFY(result == 0, "Can't bind to specified port", GetErrorMessage());
				return result == 0;
			}

			return false;
		}

		void Listen()
		{
			int result = listen(listenSocket, 4096);
			BRO_UNUSED(result);
			BRO_VERIFY(result == 0, "Can't start listening", GetErrorMessage());
		}

		void Accept()
		{ 
			int incomingSocket = accept(listenSocket, nullptr, nullptr);
			BRO_VERIFY(incomingSocket != 0, "Can't accept socket", GetErrorMessage());

			CRITICAL_SECTION(lock);
			acceptSocket = incomingSocket;
		}

		bool Send(const char *buf, size_t len)
		{
			CRITICAL_SECTION(lock);

			if (acceptSocket == 0)
				return false;

			if (send(acceptSocket, buf, (int)len, 0) != 0)
			{
				Disconnect();
				return false;
			}

			return true;
		}

		int Receive(char *buf, int len)
		{ 
			CRITICAL_SECTION(lock);

			if (acceptSocket == 0)
				return 0;

			FD_ZERO(&recieveSet);
			FD_SET(acceptSocket, &recieveSet);

			static timeval lim = {0};

			if (select(0, &recieveSet, nullptr, nullptr, &lim) == 1)
				return recv(acceptSocket, buf, len, 0);

			return 0;
		}
	};
}


