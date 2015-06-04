#include "Common.h"
#include "Core.h"
#include "Event.h"
#include "Message.h"
#include "ProfilerServer.h"
#include "EventDescriptionBoard.h"

namespace Profiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class MessageFactory
{
	typedef IMessage* (*MessageCreateFunction)(InputDataStream& str);
	MessageCreateFunction factory[IMessage::COUNT];

	template<class T>
	void RegisterMessage()
	{
		factory[T::GetMessageType()] = T::Create;
	}

	MessageFactory()
	{
		memset(&factory[0], 0, sizeof(MessageCreateFunction));

		RegisterMessage<StartMessage>();
		RegisterMessage<StopMessage>();
		RegisterMessage<TurnSamplingMessage>();
		RegisterMessage<SetupHookMessage>();

		for (uint msg = 0; msg < IMessage::COUNT; ++msg)
		{
			BRO_ASSERT(factory[msg] != nullptr, "Message is not registered to factory");
		}
	}
public:
	static MessageFactory& Get()
	{
		static MessageFactory instance;
		return instance;
	}

	IMessage* Create(InputDataStream& str)
	{
		int32 messageType = IMessage::COUNT;
		str >> messageType;

		BRO_VERIFY( 0 <= messageType && messageType < IMessage::COUNT && factory[messageType] != nullptr, "Unknown message type!", return nullptr )

		return factory[messageType](str);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& os, const DataResponse& val)
{
	return os << val.version << val.type;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
IMessage* IMessage::Create(InputDataStream& str)
{
	return MessageFactory::Get().Create(str);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void StartMessage::Apply()
{
	Core::Get().Activate(true);

	if (EventDescriptionBoard::Get().HasSamplingEvents())
	{
		Core::Get().StartSampling();
	}
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
IMessage* StartMessage::Create(InputDataStream&)
{
	return new StartMessage();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void StopMessage::Apply()
{
	Core& core = Core::Get();
	core.Activate(false);
	core.DumpFrames();
	core.DumpSamplingData();
	Server::Get().Send(DataResponse::NullFrame, OutputDataStream::Empty);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
IMessage* StopMessage::Create(InputDataStream&)
{
	return new StopMessage();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
IMessage* TurnSamplingMessage::Create( InputDataStream& stream )
{
	TurnSamplingMessage* msg = new TurnSamplingMessage();
	stream >> msg->index;
	stream >> msg->isSampling;
	return msg;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void TurnSamplingMessage::Apply()
{
	EventDescriptionBoard::Get().SetSamplingFlag(index, isSampling != 0);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
IMessage* SetupHookMessage::Create(InputDataStream& stream)
{
	SetupHookMessage* msg = new SetupHookMessage();
	stream >> msg->address;
	stream >> msg->isHooked;
	return msg;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
void SetupHookMessage::Apply()
{
	Core::Get().sampler.SetupHook(address, isHooked != 0);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

}