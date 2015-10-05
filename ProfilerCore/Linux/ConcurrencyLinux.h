#include <pthread.h>

namespace Profiler
{
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class CriticalSection
	{
		pthread_mutex_t mutex;
		pthread_mutexattr_t mutexAttr;
		CriticalSection( const CriticalSection & );
		CriticalSection& operator=( const CriticalSection&);
		
		void Enter() { pthread_mutex_lock( &mutex ); }
		void Leave() { pthread_mutex_unlock( &mutex ); }
	public:
		CriticalSection()
		{
			pthread_mutexattr_init( &mutexAttr );
			pthread_mutexattr_settype( &mutexAttr, PTHREAD_MUTEX_RECURSIVE );
			pthread_mutex_init( &mutex, &mutexAttr );
		}

		~CriticalSection()
		{
			pthread_mutex_destroy( &mutex );
			pthread_mutexattr_destroy( &mutexAttr );
		}
		friend class CriticalSectionScope;
	};
}