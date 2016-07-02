#pragma once
#include <windows.h>
#ifndef __WINSOCK2_H
#include <winsock2.h>
#endif

class Wsa
{
	static bool	  m_present;
	static WSADATA m_data;

	static bool init()
	{
		if( m_present )
			return true;

			if( m_present == false )
			{
				int res = WSAStartup(0x0202,&m_data);

				if( res )
					return false;

				m_present = true;
			}

			return true;
	}

public:

	class _socket
	{
		SOCKET m_s;
		SOCKET client_s;
		u_long m_host;
		short  m_port;

		sockaddr_in a;

		void close()
		{
			closesocket(m_s);
		}

	public:

		 _socket() : client_s(0)
		{
			init();

			m_s = socket(AF_INET,SOCK_STREAM,0);
		}
		~_socket()
		{
			close();
		}

		void bind(short port)
		{
			a.sin_family      = AF_INET;
			a.sin_addr.s_addr = inet_addr("127.0.0.1");
			a.sin_port        = htons(port);

			::bind(m_s, (sockaddr *)&a, sizeof(a));
		}

		void listen()
		{
			::listen(m_s, 5);
		}

		void accept()
		{
			int length = sizeof(a);
			 SOCKET sock = ::accept(m_s, (sockaddr *)&a, &length);
			 ::closesocket(client_s);
			 client_s = sock;
		}

		void send(const char *buf, int len, int flags = 0)
		{
			::send(client_s, buf, len, flags);
		}

		int recv(char *buf, int len, int flags = 0)
		{
			static fd_set set;

			set.fd_count    = 1;
			set.fd_array[0] = client_s;

			static timeval lim = {0};

			int res = select(0,&set,NULL,NULL,&lim);

			if( res == 1 )
			{
				return ::recv(client_s,buf,len,flags);
			}
			else
				return 0;
		}

	};

};

typedef Wsa::_socket Socket;

bool    Wsa::m_present = false;
WSADATA Wsa::m_data;

namespace UDP
{
	class Server : public Socket
	{
	public:
		Server(short port)
		{
			bind(port);
			listen();
		}
	};
}
