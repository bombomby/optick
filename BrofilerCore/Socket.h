#pragma once
#include "Common.h"
#include <string>


#if MT_MSVC_COMPILER_FAMILY
#pragma warning( push )

//C4127. Conditional expression is constant
#pragma warning( disable : 4127 )
#endif



#if MT_PLATFORM_WINDOWS
#define USE_WINDOWS_SOCKETS (1)
#else
#define USE_BERKELEY_SOCKETS (1)
#endif


#define SOCKET_PROTOCOL_TCP (6)


#if USE_BERKELEY_SOCKETS

#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>

#include <unistd.h>
#include <fcntl.h>
typedef int TcpSocket;


#elif USE_WINDOWS_SOCKETS

#if BRO_UWP
#include <WinSock2.h>
#else
#include <winsock.h>
#endif

#include <basetsd.h>
typedef UINT_PTR TcpSocket;

#else

#error Platform not supported

#endif

namespace Brofiler
{
#if USE_WINDOWS_SOCKETS
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
#endif


	inline bool IsValidSocket(TcpSocket socket)
	{
#ifdef USE_WINDOWS_SOCKETS
		if (socket == INVALID_SOCKET)
		{
			return false;
		}
#else
		if (socket < 0)
		{
			return false;
		}
#endif
		return true;
	}

	inline void CloseSocket(TcpSocket& socket)
	{
#ifdef USE_WINDOWS_SOCKETS
		closesocket(socket);
		socket = INVALID_SOCKET;
#else
		close(socket);
		socket = -1;
#endif
	}

	inline bool SetSocketBlockingMode(TcpSocket socket, bool isBlocking)
	{
#ifdef USE_WINDOWS_SOCKETS
		unsigned long mode = isBlocking ? 0 : 1;
		return (ioctlsocket(socket, FIONBIO, &mode) == 0) ? true : false;
#else
		int flags = fcntl(socket, F_GETFL, 0);
		if (flags < 0) return false;
		flags = isBlocking ? (flags & ~O_NONBLOCK) : (flags | O_NONBLOCK);
		return (fcntl(socket, F_SETFL, flags) == 0) ? true : false;
#endif
	}


	class Socket
	{
		TcpSocket acceptSocket;
		TcpSocket listenSocket;
		sockaddr_in address;

		fd_set recieveSet;

		MT::Mutex lock;
		std::wstring errorMessage;

		void Close()
		{
			if (!IsValidSocket(listenSocket))
			{
				CloseSocket(listenSocket);
			}
		}

		bool Bind(short port)
		{
			address.sin_family = AF_INET;
			address.sin_addr.s_addr = INADDR_ANY;
			address.sin_port = htons(port);

			if (::bind(listenSocket, (sockaddr *)&address, sizeof(address)) == 0)
			{
				return true;
			}

			return false;
		}

		void Disconnect()
		{ 
			MT::ScopedGuard guard(lock);

			if (!IsValidSocket(acceptSocket))
			{
				CloseSocket(acceptSocket);
			}
		}
	public:
		Socket() : acceptSocket((TcpSocket)-1), listenSocket((TcpSocket)-1)
		{
#ifdef USE_WINDOWS_SOCKETS
			Wsa::Init();
#endif
			listenSocket = ::socket(AF_INET, SOCK_STREAM, SOCKET_PROTOCOL_TCP);
			BRO_ASSERT(IsValidSocket(listenSocket), "Can't create socket");

			SetSocketBlockingMode(listenSocket, false);
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

				if (result == false)
					continue;

				return true;
			}

			return false;
		}

		void Listen()
		{
			int result = ::listen(listenSocket, 8);
			BRO_UNUSED(result);
			BRO_ASSERT(result == 0, "Can't start listening");
		}

		bool Accept()
		{ 
			TcpSocket incomingSocket = ::accept(listenSocket, nullptr, nullptr);

			if (IsValidSocket(incomingSocket))
			{
				MT::ScopedGuard guard(lock);
				acceptSocket = incomingSocket;
				SetSocketBlockingMode(acceptSocket, true);
			}

			return IsValidSocket(acceptSocket);
		}

		bool Send(const char *buf, size_t len)
		{
			MT::ScopedGuard guard(lock);

			if (!IsValidSocket(acceptSocket))
				return false;

			if (::send(acceptSocket, buf, (int)len, 0) >= 0)
			{
				Disconnect();
				return false;
			}

			return true;
		}

		int Receive(char *buf, int len)
		{ 
			MT::ScopedGuard guard(lock);

			if (!IsValidSocket(acceptSocket))
				return 0;

			FD_ZERO(&recieveSet);
			FD_SET(acceptSocket, &recieveSet);

			static timeval lim = {0};

#if USE_BERKELEY_SOCKETS
			if (::select(acceptSocket + 1, &recieveSet, nullptr, nullptr, &lim) == 1)
#elif USE_WINDOWS_SOCKETS
			if (::select(0, &recieveSet, nullptr, nullptr, &lim) == 1)
#else
#error Platform not supported
#endif
			{
				return ::recv(acceptSocket, buf, len, 0);
			}

			return 0;
		}
	};
}


#if MT_MSVC_COMPILER_FAMILY
#pragma warning( pop )
#endif
