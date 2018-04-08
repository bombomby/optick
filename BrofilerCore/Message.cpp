#include "Common.h"
#include "Core.h"
#include "Event.h"
#include "Message.h"
#include "ProfilerServer.h"
#include "EventDescriptionBoard.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct MessageHeader
{
	uint32 mark;
	uint32 length;

	static const uint32 MESSAGE_MARK = 0xB50FB50F;

	bool IsValid() const { return mark == MESSAGE_MARK; }

	MessageHeader() : mark(0), length(0) {}
};
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

		for (uint32 msg = 0; msg < IMessage::COUNT; ++msg)
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
		MessageHeader header;
		str.Read(header);

		size_t length = str.Length();

		uint16 applicationID = 0;
		uint16 messageType = IMessage::COUNT;

		str >> applicationID;
		str >> messageType;

		BRO_VERIFY( 0 <= messageType && messageType < IMessage::COUNT && factory[messageType] != nullptr, "Unknown message type!", return nullptr )

		IMessage* result = factory[messageType](str);

		if (header.length + str.Length() != length)
		{
			BRO_FAILED("Message Stream is corrupted! Invalid Protocol?")
			return nullptr;
		}

		return result;
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator<<(OutputDataStream& os, const DataResponse& val)
{
	return os << val.version << (uint32)val.type;
}
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

IMessage* IMessage::Create(InputDataStream& str)
{
	MessageHeader header;

	while (str.Peek(header))
	{
		if (header.IsValid())
		{
			if (str.Length() < header.length + sizeof(MessageHeader))
				break; // Not enough data yet

			return MessageFactory::Get().Create(str);
		} 
		else
		{
			// Some garbage in the stream?
			str.Skip(1);
		}
	}

	return nullptr;
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
}
