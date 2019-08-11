#pragma once
#include "optick.config.h"

#if USE_OPTICK
#include "optick_message.h"

#include <mutex>
#include <thread>

namespace Optick
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Socket;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class Server
{
	InputDataStream networkStream;

	static const int BIFFER_SIZE = 1024;
	char buffer[BIFFER_SIZE];

	Socket* socket;

	std::recursive_mutex socketLock;

	CaptureSaveChunkCb saveCb;

	Server( short port );
	~Server();

	bool InitConnection();

	void Send(const char* data, size_t size);

public:
	void SetSaveCallback(CaptureSaveChunkCb cb);

	void SendStart();
	void Send(DataResponse::Type type, OutputDataStream& stream);
	void SendFinish();

	void Update();

	string GetHostName() const;

	static Server &Get();
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#endif //USE_OPTICK