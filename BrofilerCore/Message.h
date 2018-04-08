#pragma once
#include "Common.h"
#include "Serialization.h"

namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
static const uint32 NETWORK_PROTOCOL_VERSION = 22;
static const uint16 NETWORK_APPLICATION_ID = 0xB50F;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct DataResponse
{
	enum Type : uint16
	{
		FrameDescriptionBoard = 0,		// DescriptionBoard for Instrumental Frames
		EventFrame = 1,					// Instrumental Data
		SamplingFrame = 2,				// Sampling Data
		NullFrame = 3,					// Last Fame Mark
		ReportProgress = 4,				// Report Current Progress
		Handshake = 5,					// Handshake Response
		Reserved_0 = 6,					
		SynchronizationData = 7,		// Synchronization Data for the thread
		TagsPack = 8,					// Pack of tags
		CallstackDescriptionBoard = 9,	// DescriptionBoard with resolved function addresses
		CallstackPack = 10,				// Pack of CallStacks
		Reserved_1 = 11,				
		Reserved_2 = 12,				
		Reserved_3 = 13,				
		Reserved_4 = 14,				

		FiberSynchronizationData = 1 << 8, // Synchronization Data for the Fibers
		SyscallPack,
	};

	uint32 version;
	uint32 size;
	Type type;
	uint16 application;

	DataResponse(Type t, uint32 s) : version(NETWORK_PROTOCOL_VERSION), size(s), type(t), application(NETWORK_APPLICATION_ID){}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
OutputDataStream& operator << (OutputDataStream& os, const DataResponse& val);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
class IMessage
{
public:
	enum Type : uint16
	{
		Start,
		Stop,
		TurnSampling,
		COUNT,
	};

	virtual void Apply() = 0;
	virtual ~IMessage() {}

	static IMessage* Create( InputDataStream& str );
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
template<IMessage::Type MESSAGE_TYPE>
class Message : public IMessage
{
	enum { id = MESSAGE_TYPE };
public:
	static uint32 GetMessageType() { return id; }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct StartMessage : public Message<IMessage::Start>
{
	static IMessage* Create(InputDataStream&);
	virtual void Apply() override;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct StopMessage : public Message<IMessage::Stop>
{
	static IMessage* Create(InputDataStream&);
	virtual void Apply() override;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct TurnSamplingMessage : public Message<IMessage::TurnSampling>
{
	int32 index;
	byte isSampling;

	static IMessage* Create(InputDataStream& stream);
	virtual void Apply() override;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
