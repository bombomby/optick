#pragma once

#include "Message.h"

namespace Brofiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class Socket;
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class Server
	{
		InputDataStream networkStream;

		static const int BIFFER_SIZE = 1024;
		char buffer[BIFFER_SIZE];

		MT::Thread acceptThread;

		Socket* socket;

		MT::Mutex lock;

		bool isInitialized;

		Server(short port);
		~Server();

		bool InitConnection();

		static void AsyncAccept(void* server);
		bool Accept();
	public:
		void Send(DataResponse::Type type, OutputDataStream& stream = OutputDataStream::Empty);
		void Update();

		static Server &Get();
	};
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
