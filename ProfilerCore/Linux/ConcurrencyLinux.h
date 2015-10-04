#include <pthread.h>

namespace Profiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class CriticalSection
	{
		pthread_mutex_t mutex;
		CriticalSection( const CriticalSection & ) {}
		CriticalSection& operator=( const CriticalSection&) {}
		
		void Enter() { pthread_mutex_lock( &mutex ); }
		void Leave() { pthread_mutex_unlock( &mutex ); }
	public:
		CriticalSection() { }
		~CriticalSection() { }
		friend class CriticalSectionScope;
	};
}