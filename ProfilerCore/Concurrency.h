#pragma once

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
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class CriticalSectionScope
	{
		CriticalSection &section;
	private:
		CriticalSectionScope &operator=( CriticalSectionScope& ) {}
	public:
		CriticalSectionScope( CriticalSection& _lock ) : section(_lock) 
		{
			section.Enter(); 
		}

		~CriticalSectionScope() 
		{ 
			section.Leave(); 
		}
	};
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	#define CRITICAL_SECTION(criticalSection) CriticalSectionScope generatedCriticalSectionScope##__LINE__(criticalSection); 
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}