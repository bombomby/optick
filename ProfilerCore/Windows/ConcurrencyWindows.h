#include <windows.h>

namespace Profiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class CriticalSection
	{
		CRITICAL_SECTION sect;
		CriticalSection( const CriticalSection & ) {}
		CriticalSection& operator=( const CriticalSection&) {}
		
		void Enter() { EnterCriticalSection( &sect ); }
		void Leave() { LeaveCriticalSection( &sect ); }
	public:
		CriticalSection() { InitializeCriticalSection( &sect ); }
		~CriticalSection() { DeleteCriticalSection( &sect ); }
		friend class CriticalSectionScope;
	};
}