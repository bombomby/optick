#pragma once

#include <stdint.h>

#if defined(__clang__) || defined(__GNUC__)
#define BRO_GCC (1)
#elif defined(_MSC_VER)
#define BRO_MSVC (1)
#else
#error Compiler not supported
#endif

#if defined(BRO_GCC)
#define BRO_FUNC __PRETTY_FUNCTION__
#elif BRO_MSVC
#define BRO_FUNC __FUNCSIG__
#else
#error Compiler not supported
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// SETTINGS
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Enable\Disable Brofiler
#if !defined(USE_BROFILER)
#define USE_BROFILER 1
#endif


#if USE_BROFILER
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// EXPORTS 
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#ifdef BROFILER_EXPORTS
#define BROFILER_API __declspec(dllexport)
#else
#define BROFILER_API //__declspec(dllimport)
#endif
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#define BRO_CONCAT_IMPL(x, y) x##y
#define BRO_CONCAT(x, y) BRO_CONCAT_IMPL(x, y)

#if BRO_MSVC
#define BRO_INLINE __forceinline
#elif BRO_GCC
#define BRO_INLINE __attribute__((always_inline)) inline
#else
#error Compiler is not supported
#endif


// Vulkan Forward Declarations
#define BRO_DEFINE_HANDLE(object) typedef struct object##_T* object;
BRO_DEFINE_HANDLE(VkDevice);
BRO_DEFINE_HANDLE(VkPhysicalDevice);
BRO_DEFINE_HANDLE(VkQueue);
BRO_DEFINE_HANDLE(VkCommandBuffer);

// D3D12 Forward Declarations
struct ID3D12CommandList;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Brofiler
{
	// Source: http://msdn.microsoft.com/en-us/library/system.windows.media.colors(v=vs.110).aspx
	// Image:  http://i.msdn.microsoft.com/dynimg/IC24340.png
	struct Color
	{
		enum
		{
			Null = 0x00000000,
			AliceBlue = 0xFFF0F8FF,
			AntiqueWhite = 0xFFFAEBD7,
			Aqua = 0xFF00FFFF,
			Aquamarine = 0xFF7FFFD4,
			Azure = 0xFFF0FFFF,
			Beige = 0xFFF5F5DC,
			Bisque = 0xFFFFE4C4,
			Black = 0xFF000000,
			BlanchedAlmond = 0xFFFFEBCD,
			Blue = 0xFF0000FF,
			BlueViolet = 0xFF8A2BE2,
			Brown = 0xFFA52A2A,
			BurlyWood = 0xFFDEB887,
			CadetBlue = 0xFF5F9EA0,
			Chartreuse = 0xFF7FFF00,
			Chocolate = 0xFFD2691E,
			Coral = 0xFFFF7F50,
			CornflowerBlue = 0xFF6495ED,
			Cornsilk = 0xFFFFF8DC,
			Crimson = 0xFFDC143C,
			Cyan = 0xFF00FFFF,
			DarkBlue = 0xFF00008B,
			DarkCyan = 0xFF008B8B,
			DarkGoldenRod = 0xFFB8860B,
			DarkGray = 0xFFA9A9A9,
			DarkGreen = 0xFF006400,
			DarkKhaki = 0xFFBDB76B,
			DarkMagenta = 0xFF8B008B,
			DarkOliveGreen = 0xFF556B2F,
			DarkOrange = 0xFFFF8C00,
			DarkOrchid = 0xFF9932CC,
			DarkRed = 0xFF8B0000,
			DarkSalmon = 0xFFE9967A,
			DarkSeaGreen = 0xFF8FBC8F,
			DarkSlateBlue = 0xFF483D8B,
			DarkSlateGray = 0xFF2F4F4F,
			DarkTurquoise = 0xFF00CED1,
			DarkViolet = 0xFF9400D3,
			DeepPink = 0xFFFF1493,
			DeepSkyBlue = 0xFF00BFFF,
			DimGray = 0xFF696969,
			DodgerBlue = 0xFF1E90FF,
			FireBrick = 0xFFB22222,
			FloralWhite = 0xFFFFFAF0,
			ForestGreen = 0xFF228B22,
			Fuchsia = 0xFFFF00FF,
			Gainsboro = 0xFFDCDCDC,
			GhostWhite = 0xFFF8F8FF,
			Gold = 0xFFFFD700,
			GoldenRod = 0xFFDAA520,
			Gray = 0xFF808080,
			Green = 0xFF008000,
			GreenYellow = 0xFFADFF2F,
			HoneyDew = 0xFFF0FFF0,
			HotPink = 0xFFFF69B4,
			IndianRed = 0xFFCD5C5C,
			Indigo = 0xFF4B0082,
			Ivory = 0xFFFFFFF0,
			Khaki = 0xFFF0E68C,
			Lavender = 0xFFE6E6FA,
			LavenderBlush = 0xFFFFF0F5,
			LawnGreen = 0xFF7CFC00,
			LemonChiffon = 0xFFFFFACD,
			LightBlue = 0xFFADD8E6,
			LightCoral = 0xFFF08080,
			LightCyan = 0xFFE0FFFF,
			LightGoldenRodYellow = 0xFFFAFAD2,
			LightGray = 0xFFD3D3D3,
			LightGreen = 0xFF90EE90,
			LightPink = 0xFFFFB6C1,
			LightSalmon = 0xFFFFA07A,
			LightSeaGreen = 0xFF20B2AA,
			LightSkyBlue = 0xFF87CEFA,
			LightSlateGray = 0xFF778899,
			LightSteelBlue = 0xFFB0C4DE,
			LightYellow = 0xFFFFFFE0,
			Lime = 0xFF00FF00,
			LimeGreen = 0xFF32CD32,
			Linen = 0xFFFAF0E6,
			Magenta = 0xFFFF00FF,
			Maroon = 0xFF800000,
			MediumAquaMarine = 0xFF66CDAA,
			MediumBlue = 0xFF0000CD,
			MediumOrchid = 0xFFBA55D3,
			MediumPurple = 0xFF9370DB,
			MediumSeaGreen = 0xFF3CB371,
			MediumSlateBlue = 0xFF7B68EE,
			MediumSpringGreen = 0xFF00FA9A,
			MediumTurquoise = 0xFF48D1CC,
			MediumVioletRed = 0xFFC71585,
			MidnightBlue = 0xFF191970,
			MintCream = 0xFFF5FFFA,
			MistyRose = 0xFFFFE4E1,
			Moccasin = 0xFFFFE4B5,
			NavajoWhite = 0xFFFFDEAD,
			Navy = 0xFF000080,
			OldLace = 0xFFFDF5E6,
			Olive = 0xFF808000,
			OliveDrab = 0xFF6B8E23,
			Orange = 0xFFFFA500,
			OrangeRed = 0xFFFF4500,
			Orchid = 0xFFDA70D6,
			PaleGoldenRod = 0xFFEEE8AA,
			PaleGreen = 0xFF98FB98,
			PaleTurquoise = 0xFFAFEEEE,
			PaleVioletRed = 0xFFDB7093,
			PapayaWhip = 0xFFFFEFD5,
			PeachPuff = 0xFFFFDAB9,
			Peru = 0xFFCD853F,
			Pink = 0xFFFFC0CB,
			Plum = 0xFFDDA0DD,
			PowderBlue = 0xFFB0E0E6,
			Purple = 0xFF800080,
			Red = 0xFFFF0000,
			RosyBrown = 0xFFBC8F8F,
			RoyalBlue = 0xFF4169E1,
			SaddleBrown = 0xFF8B4513,
			Salmon = 0xFFFA8072,
			SandyBrown = 0xFFF4A460,
			SeaGreen = 0xFF2E8B57,
			SeaShell = 0xFFFFF5EE,
			Sienna = 0xFFA0522D,
			Silver = 0xFFC0C0C0,
			SkyBlue = 0xFF87CEEB,
			SlateBlue = 0xFF6A5ACD,
			SlateGray = 0xFF708090,
			Snow = 0xFFFFFAFA,
			SpringGreen = 0xFF00FF7F,
			SteelBlue = 0xFF4682B4,
			Tan = 0xFFD2B48C,
			Teal = 0xFF008080,
			Thistle = 0xFFD8BFD8,
			Tomato = 0xFFFF6347,
			Turquoise = 0xFF40E0D0,
			Violet = 0xFFEE82EE,
			Wheat = 0xFFF5DEB3,
			White = 0xFFFFFFFF,
			WhiteSmoke = 0xFFF5F5F5,
			Yellow = 0xFFFFFF00,
			YellowGreen = 0xFF9ACD32,
		};
	};
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}


namespace Brofiler
{
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct Mode
{
	enum Type
	{
		OFF = 0x0,
		INSTRUMENTATION_CATEGORIES = (1 << 0),
		INSTRUMENTATION_EVENTS = (1 << 1),
		INSTRUMENTATION = (INSTRUMENTATION_CATEGORIES | INSTRUMENTATION_EVENTS),
		SAMPLING = (1 << 2),
		TAGS = (1 << 3),
		AUTOSAMPLING = (1 << 4),
		SWITCH_CONTEXT = (1 << 5),
		IO = (1 << 6),
		GPU = (1 << 7),
		END_SCREENSHOT = (1 << 8),
		RESERVED_0 = (1 << 9),
		RESERVED_1 = (1 << 10),
		HW_COUNTERS = (1 << 11),
		LIVE = (1 << 12),
		RESERVED_2 = (1 << 13),
		RESERVED_3 = (1 << 14),
		RESERVED_4 = (1 << 15),

		DEFAULT = INSTRUMENTATION & AUTOSAMPLING & SWITCH_CONTEXT & IO & GPU,
	};
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API int64_t GetHighPrecisionTime();
BROFILER_API int64_t GetHighPrecisionFrequency();
BROFILER_API uint32_t NextFrame();
BROFILER_API bool IsActive();
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventStorage;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API bool RegisterFiber(uint64_t fiberId, EventStorage** slot);
BROFILER_API bool RegisterThread(const char* name);
BROFILER_API bool RegisterThread(const wchar_t* name);
BROFILER_API bool UnRegisterThread();
BROFILER_API EventStorage** GetEventStorageSlotForCurrentThread();
BROFILER_API bool IsFiberStorage(EventStorage* fiberStorage);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API EventStorage* RegisterStorage(const char* name);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
enum BroState
{
	// Starting a new capture
	BRO_START_CAPTURE,
	// Stopping current capture
	BRO_STOP_CAPTURE,
	// Dumping capture to the GUI
	// Useful for attaching summary and screenshot to the capture
	BRO_DUMP_CAPTURE,
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Sets a state change callback
typedef void (*BroStateCallback)(BroState state);
BROFILER_API bool SetStateChangedCallback(BroStateCallback cb);

// Attaches a key-value pair to the capture's summary
// Example: AttachSummary("Version", "v12.0.1");
//			AttachSummary("Platform", "Windows");
//			AttachSummary("Config", "Release_x64");
//			AttachSummary("Settings", "Ultra");
//			AttachSummary("Map", "Atlantida");
//			AttachSummary("Position", "123.0,120.0,41.1");
//			AttachSummary("CPU", "Intel(R) Xeon(R) CPU E5410@2.33GHz");
//			AttachSummary("GPU", "NVIDIA GeForce GTX 980 Ti");
BROFILER_API bool AttachSummary(const char* key, const char* value);

struct BroFile
{
	enum Type
	{
		// Supported formats: PNG, JPEG, BMP, TIFF
		BRO_IMAGE,
		
		// Text file
		BRO_TEXT,

		// Any other type
		BRO_OTHER,
	};
};
// Attaches a file to the current capture
BROFILER_API bool AttachFile(BroFile::Type type, const char* name, const uint8_t* data, uint32_t size);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventDescription;
struct Frame;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventTime
{
	static const int64_t INVALID_TIMESTAMP = (int64_t)-1;

	int64_t start;
	int64_t finish;

	BRO_INLINE void Start() { start  = Brofiler::GetHighPrecisionTime(); }
	BRO_INLINE void Stop() 	{ finish = Brofiler::GetHighPrecisionTime(); }
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct EventData : public EventTime
{
	const EventDescription* description;

	bool operator<(const EventData& other) const
	{
		if (start != other.start)
			return start < other.start;

		// Reversed order for finish intervals (parent first)
		return  finish > other.finish;
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API SyncData : public EventTime
{
	uint64_t newThreadId;
	uint64_t oldThreadId;
	uint8_t core;
	int8_t reason;
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API FiberSyncData : public EventTime
{
	uint64_t threadId;

	static void AttachToThread(EventStorage* storage, uint64_t threadId);
	static void DetachFromThread(EventStorage* storage);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API EventDescription
{
	// HOT  \\
	// Have to place "hot" variables at the beginning of the class (here will be some padding)
	// COLD //

	const char* name;
	const char* file;
	uint32_t line;
	uint32_t index;
	uint32_t color;
	uint32_t filter;
	float budget;

	static EventDescription* Create(const char* eventName, const char* fileName, const unsigned long fileLine, const unsigned long eventColor = Color::Null, const unsigned long filter = 0);
	static EventDescription* CreateShared(const char* eventName, const char* fileName = nullptr, const unsigned long fileLine = 0, const unsigned long eventColor = Color::Null, const unsigned long filter = 0);

	EventDescription();
private:
	friend class EventDescriptionBoard;
	EventDescription& operator=(const EventDescription&);
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API Event
{
	EventData* data;

	static EventData* Start(const EventDescription& description);
	static void Stop(EventData& data);

	static void Push(const char* name);
	static void Push(const EventDescription& description);
	static void Pop();

	static void Add(EventStorage* storage, const EventDescription* description, int64_t timestampStart, int64_t timestampFinish);
	static void Push(EventStorage* storage, const EventDescription* description, int64_t timestampStart);
	static void Pop(EventStorage* storage, int64_t timestampStart);


	Event(const EventDescription& description)
	{
		data = Start(description);
	}

	~Event()
	{
		if (data)
			Stop(*data);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API GPUEvent
{
	EventData* data;

	static EventData* Start(const EventDescription& description);
	static void Stop(EventData& data);

	GPUEvent(const EventDescription& description)
	{
		data = Start(description);
	}

	~GPUEvent()
	{
		if (data)
			Stop(*data);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API Tag
{
	static void Attach(const EventDescription& description, float val);
	static void Attach(const EventDescription& description, int32_t val);
	static void Attach(const EventDescription& description, uint32_t val);
	static void Attach(const EventDescription& description, uint64_t val);
	static void Attach(const EventDescription& description, float val[3]);
	static void Attach(const EventDescription& description, const char* val);

	// Derived
	static void Attach(const EventDescription& description, float x, float y, float z)
	{
		float p[3] = { x, y, z }; Attach(description, p);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API Category : public Event
{
	Category( const EventDescription& description );
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct ThreadScope
{
	ThreadScope(const char* name)
	{
		RegisterThread(name);
	}

	ThreadScope(const wchar_t* name)
	{
		RegisterThread(name);
	}

	~ThreadScope()
	{
		UnRegisterThread();
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
enum BROFILER_API GPUQueueType
{
	GPU_QUEUE_GRAPHICS,
	GPU_QUEUE_COMPUTE,
	GPU_QUEUE_TRANSFER,
	GPU_QUEUE_VSYNC,

	GPU_QUEUE_COUNT,
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API GPUContext
{
	void* cmdBuffer;
	GPUQueueType queue;
	int node;
	GPUContext(void* c = nullptr, GPUQueueType q = GPU_QUEUE_GRAPHICS, int n = 0) : cmdBuffer(c), queue(q), node(n) {}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
BROFILER_API void InitGpuD3D12(void* device, void** cmdQueues, uint32_t numQueues);
BROFILER_API void InitGpuVulkan(VkDevice* devices, VkPhysicalDevice* physicalDevices, VkQueue* cmdQueues, uint32_t* cmdQueuesFamily, uint32_t numQueues);
BROFILER_API void GpuFlip(void* swapChain);
BROFILER_API GPUContext SetGpuContext(GPUContext context);
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
struct BROFILER_API GPUContextScope
{
	GPUContext prevContext;

	GPUContextScope(ID3D12CommandList* cmdList, GPUQueueType queue = GPU_QUEUE_GRAPHICS, int node = 0)
	{
		prevContext = SetGpuContext(GPUContext(cmdList, queue, node));
	}

	GPUContextScope(VkCommandBuffer cmdBuffer, GPUQueueType queue = GPU_QUEUE_GRAPHICS, int node = 0)
	{
		prevContext = SetGpuContext(GPUContext(cmdBuffer, queue, node));
	}

	~GPUContextScope()
	{
		SetGpuContext(prevContext);
	}
};
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

#define BRO_UNUSED(x) (void)(x)


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Scoped profiling event with a STATIC name. 
// Useful for measuring multiple code blocks within a function call.
// Example:
//		{
//			BROFILER_EVENT("ScopeName");
//			... code ...
//		}
// Notes:
//		Brofiler holds a pointer to the name, so please keep this pointer alive. 
#define BROFILER_EVENT(NAME) static ::Brofiler::EventDescription* BRO_CONCAT(autogenerated_description_, __LINE__) = nullptr; \
							 if (BRO_CONCAT(autogenerated_description_, __LINE__) == nullptr) BRO_CONCAT(autogenerated_description_, __LINE__) = ::Brofiler::EventDescription::Create( NAME, __FILE__, __LINE__ ); \
							 ::Brofiler::Event BRO_CONCAT(autogenerated_event_, __LINE__)( *(BRO_CONCAT(autogenerated_description_, __LINE__)) ); \

// Scoped profiling event which automatically grabs current function name.
// Use tis macro 95% of the time.
// Example:
//		void Function()
//		{
//			BROFILE;
//			... code ...
//		}
// Notes:
//		Brofiler captures full name of the function including name space and arguments.
//		Full name is usually shortened in the Brofiler GUI in order to highlight the most important bits.
#define BROFILE BROFILER_EVENT( BRO_FUNC )

// Backward compatibility with previous versions of Brofiler
#if !defined(PROFILE)
#define PROFILE BROFILE
#endif

// Inlined profiling event.
// Useful for wrapping one-line call into with a profiling macro.
// Example:
//		BROFILER_INLINE_EVENT("ScopeName", FunctionCall());
#define BROFILER_INLINE_EVENT(NAME, CODE) { BROFILER_EVENT(NAME) CODE; }

// Scoped profiling macro with predefined color.
// Use this macro for high-level function calls (e.g. AI, Physics, Audio, Render etc.).
// Example:
//		void UpdateAI()
//		{
//			BROFILER_CATEGORY("UpdateAI", Brofiler::Color::LimeGreen);
//			... code ...
//		}
//	
//		You could also use BRO_FUNC to capture current function name:
//		void UpdateAI()
//		{
//			BROFILER_CATEGORY(BRO_FUNC, Brofiler::Color::LimeGreen);
//			... code ...
//		}
#define BROFILER_CATEGORY(NAME, COLOR)		  static ::Brofiler::EventDescription* BRO_CONCAT(autogenerated_description_, __LINE__) = nullptr; \
											  if (BRO_CONCAT(autogenerated_description_, __LINE__) == nullptr) BRO_CONCAT(autogenerated_description_, __LINE__) = ::Brofiler::EventDescription::Create( NAME, __FILE__, __LINE__, (unsigned long)COLOR ); \
											  ::Brofiler::Category BRO_CONCAT(autogenerated_event_, __LINE__)( *(BRO_CONCAT(autogenerated_description_, __LINE__)) ); \

// Profiling event for Main Loop update.
// You need to call this function in the beginning of the each new frame.
// Example:
//		while (true)
//		{
//			BROFILER_FRAME("MainThread");
//			... code ...
//		}
#define BROFILER_FRAME(FRAME_NAME)  static ::Brofiler::ThreadScope mainThreadScope(FRAME_NAME);		\
									BRO_UNUSED(mainThreadScope);									\
									uint32_t frameNumber = ::Brofiler::NextFrame();					\
									BROFILER_CATEGORY("CPU Frame", ::Brofiler::Color::LimeGreen);	\
									BROFILER_TAG("Frame", frameNumber);


// Thread registration macro.
// Example:
//		void WorkerThread(...)
//		{
//			BROFILER_THREAD("Worker");
//			while (isRunning)
//			{
//				...
//			}
//		}
#define BROFILER_THREAD(THREAD_NAME) ::Brofiler::ThreadScope brofilerThreadScope(THREAD_NAME);	\
									 BRO_UNUSED(brofilerThreadScope);							\


// Thread registration macros.
// Useful for integration with custom job-managers.
#define BROFILER_START_THREAD(FRAME_NAME) ::Brofiler::RegisterThread(FRAME_NAME);
#define BROFILER_STOP_THREAD() ::Brofiler::UnRegisterThread();

// Attaches a custom data-tag.
// Supported types: int32, uint32, uint64, vec3, string (cut to 32 characters)
// Example:
//		BROFILER_TAG("PlayerName", name[index]);
//		BROFILER_TAG("Health", 100);
//		BROFILER_TAG("Score", 0x80000000u);
//		BROFILER_TAG("Height(cm)", 176.3f);
//		BROFILER_TAG("Address", (uint64)*this);
//		BROFILER_TAG("Position", 123.0f, 456.0f, 789.0f);
#define BROFILER_TAG(NAME, ...)		static ::Brofiler::EventDescription* BRO_CONCAT(autogenerated_tag_, __LINE__) = nullptr; \
									if (BRO_CONCAT(autogenerated_tag_, __LINE__) == nullptr) BRO_CONCAT(autogenerated_tag_, __LINE__) = ::Brofiler::EventDescription::Create( NAME, __FILE__, __LINE__ ); \
									::Brofiler::Tag::Attach(*BRO_CONCAT(autogenerated_tag_, __LINE__), __VA_ARGS__); \

// Scoped macro with DYNAMIC name.
// Brofiler holds a copy of the provided name.
// Each scope does a search in hashmap for the name.
// Please use variations with STATIC names where it's possible.
// Use this macro for quick prototyping or intergratoin with other profiling systems (e.g. UE4)
// Example:
//		const char* name = ... ;
//		BROFILER_EVENT_DYNAMIC(name);
#define BROFILER_EVENT_DYNAMIC(NAME)	BROFILER_CUSTOM_EVENT(::Brofiler::EventDescription::CreateShared(NAME, __FILE__, __LINE__));
// Push\Pop profiling macro with DYNAMIC name.
#define BROFILER_PUSH_DYNAMIC(NAME)		::Brofiler::Event::Push(NAME);		

// Push\Pop profiling macro with STATIC name.
// Please avoid using Push\Pop approach in favor for scoped macros.
// For backward compatibility with some engines.
// Example:
//		BROFILER_PUSH("ScopeName");
//		...
//		BROFILER_POP();
#define BROFILER_PUSH(NAME)				static ::Brofiler::EventDescription* BRO_CONCAT(autogenerated_description_, __LINE__) = nullptr; \
										if (BRO_CONCAT(autogenerated_description_, __LINE__) == nullptr) BRO_CONCAT(autogenerated_description_, __LINE__) = ::Brofiler::EventDescription::Create( NAME, __FILE__, __LINE__ ); \
										::Brofiler::Event::Push(*BRO_CONCAT(autogenerated_description_, __LINE__));		
#define BROFILER_POP()					::Brofiler::Event::Pop();


// Scoped macro with predefined Brofiler::EventDescription.
// Use these events instead of DYNAMIC macros to minimize overhead.
// Common use-case: integrating Brofiler with internal script languages (e.g. Lua, Actionscript(Scaleform), etc.).
// Example:
//		Generating EventDescription once during initialization:
//		Brofiler::EventDescription* description = Brofiler::EventDescription::CreateShared("FunctionName");
//
//		Then we could just use a pointer to cached description later for profiling:
//		BROFILER_CUSTOM_EVENT(description);
#define BROFILER_CUSTOM_EVENT(DESCRIPTION) 							::Brofiler::Event						  BRO_CONCAT(autogenerated_event_, __LINE__)( *DESCRIPTION ); \

// Registration of a custom EventStorage (e.g. GPU, IO, etc.)
// Use it to present any extra information on the timeline.
// Example:
//		Brofiler::EventStorage* IOStorage = Brofiler::RegisterStorage("I/O");
// Notes:
//		Registration of a new storage is thread-safe.
#define BROFILER_STORAGE_REGISTER(STORAGE_NAME)														::Brofiler::RegisterStorage(STORAGE_NAME);

// Adding events to the custom storage.
// Helps to integrate Brofiler into already existing profiling systems (e.g. GPU Profiler, I/O profiler, etc.).
// Example:
//			//Registering a storage - should be done once during initialization
//			static Brofiler::EventStorage* IOStorage = Brofiler::RegisterStorage("I/O");
//
//			int64_t cpuTimestampStart = Brofiler::GetHighPrecisionTime();
//			...
//			int64_t cpuTimestampFinish = Brofiler::GetHighPrecisionTime();
//
//			//Creating a shared event-description
//			static Brofiler::EventDescription* IORead = Brofiler::EventDescription::CreateShared("IO Read");
// 
//			BROFILER_STORAGE_EVENT(IOStorage, IORead, cpuTimestampStart, cpuTimestampFinish);
// Notes:
//		It's not thread-safe to add events to the same storage from multiple threads.
//		Please guarantee thread-safety on the higher level if access from multiple threads to the same storage is required.
#define BROFILER_STORAGE_EVENT(STORAGE, DESCRIPTION, CPU_TIMESTAMP_START, CPU_TIMESTAMP_FINISH)		if (::Brofiler::IsActive()) { ::Brofiler::Event::Add(STORAGE, DESCRIPTION, CPU_TIMESTAMP_START, CPU_TIMESTAMP_FINISH); }
#define BROFILER_STORAGE_PUSH(STORAGE, DESCRIPTION, CPU_TIMESTAMP_START)							if (::Brofiler::IsActive()) { ::Brofiler::Event::Push(STORAGE, DESCRIPTION, CPU_TIMESTAMP_START); }
#define BROFILER_STORAGE_POP(STORAGE, CPU_TIMESTAMP_FINISH)											if (::Brofiler::IsActive()) { ::Brofiler::Event::Pop(STORAGE, CPU_TIMESTAMP_FINISH); }

// GPU events
#define BROFILER_GPU_INIT_D3D12(DEVICE, CMD_QUEUES, NUM_CMD_QUEUS)			::Brofiler::InitGpuD3D12(DEVICE, CMD_QUEUES, NUM_CMD_QUEUS);
#define BROFILER_GPU_INIT_VULKAN(DEVICES, PHYSICAL_DEVICES, CMD_QUEUES, CMD_QUEUES_FAMILY, NUM_CMD_QUEUS)			::Brofiler::InitGpuVulkan(DEVICES, PHYSICAL_DEVICES, CMD_QUEUES, CMD_QUEUES_FAMILY, NUM_CMD_QUEUS);

// Setup GPU context:
// Params:
//		(CommandBuffer\CommandList, [Optional] Brofiler::GPUQueue queue, [Optional] int NodeIndex)
// Examples:
//		BROFILER_GPU_CONTEXT(cmdBuffer); - all BROFILER_GPU_EVENT will use the same command buffer within the scope
//		BROFILER_GPU_CONTEXT(cmdBuffer, Brofiler::GPU_QUEUE_COMPUTE); - all events will use the same command buffer and queue for the scope 
//		BROFILER_GPU_CONTEXT(cmdBuffer, Brofiler::GPU_QUEUE_COMPUTE, gpuIndex); - all events will use the same command buffer and queue for the scope 
#define BROFILER_GPU_CONTEXT(...)	 ::Brofiler::GPUContextScope BRO_CONCAT(gpu_autogenerated_context_, __LINE__)(__VA_ARGS__); \
									 (void)BRO_CONCAT(gpu_autogenerated_context_, __LINE__);

#define BROFILER_GPU_EVENT(NAME)	 BROFILER_EVENT(NAME); \
									 static ::Brofiler::EventDescription* BRO_CONCAT(gpu_autogenerated_description_, __LINE__) = nullptr; \
									 if (BRO_CONCAT(gpu_autogenerated_description_, __LINE__) == nullptr) BRO_CONCAT(gpu_autogenerated_description_, __LINE__) = ::Brofiler::EventDescription::Create( NAME, __FILE__, __LINE__ ); \
									 ::Brofiler::GPUEvent BRO_CONCAT(gpu_autogenerated_event_, __LINE__)( *(BRO_CONCAT(gpu_autogenerated_description_, __LINE__)) ); \

#define BROFILER_GPU_FLIP(SWAP_CHAIN)		::Brofiler::GpuFlip(SWAP_CHAIN);

#else
#define BROFILER_EVENT(NAME)
#define BROFILE
#define BROFILER_INLINE_EVENT(NAME, CODE) { CODE; }
#define BROFILER_CATEGORY(NAME, COLOR)
#define BROFILER_FRAME(NAME)
#define BROFILER_THREAD(FRAME_NAME)
#define BROFILER_START_THREAD(FRAME_NAME)
#define BROFILER_STOP_THREAD()
#define BROFILER_TAG(NAME, DATA)
#define BROFILER_EVENT_DYNAMIC(NAME)	
#define BROFILER_PUSH_DYNAMIC(NAME)		
#define BROFILER_PUSH(NAME)				
#define BROFILER_POP()		
#define BROFILER_CUSTOM_EVENT(DESCRIPTION)
#define BROFILER_STORAGE_REGISTER(STORAGE_NAME)
#define BROFILER_STORAGE_EVENT(STORAGE, DESCRIPTION, CPU_TIMESTAMP_START, CPU_TIMESTAMP_FINISH)
#define BROFILER_STORAGE_PUSH(STORAGE, DESCRIPTION, CPU_TIMESTAMP_START)
#define BROFILER_STORAGE_POP(STORAGE, CPU_TIMESTAMP_FINISH)				
#endif
