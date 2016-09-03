#pragma once

#include <thread>

#include "Concurrency.h"
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

	std::thread acceptThread;
	Socket* socket;

	CriticalSection lock;
	
	Server( short port );
	~Server();

	bool InitConnection();

	static void AsyncAccept(Server* server);
	bool Accept();
public:
	void Send(DataResponse::Type type, OutputDataStream& stream = OutputDataStream::Empty);
	void Update();

	static Server &Get();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}