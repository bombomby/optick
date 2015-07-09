#pragma once

#include "Concurrency.h"
#include "Message.h"

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Socket;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Server
{
	InputDataStream networkStream;

	static const int BIFFER_SIZE = 1024;
	char buffer[BIFFER_SIZE];

	HANDLE acceptThread;
	Socket* socket;

	CriticalSection lock;
	
	Server( short port );
	~Server();

	bool InitConnection();

	static DWORD WINAPI AsyncAccept( LPVOID lpParam );
	bool Accept();
public:
	void Send(DataResponse::Type type, OutputDataStream& stream = OutputDataStream::Empty);
	void Update();

	static Server &Get();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}