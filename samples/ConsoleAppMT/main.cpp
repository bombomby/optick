#include <iostream>
#include <array>
#include <queue>
#include <mutex>
#include <thread>
#include <condition_variable>
#include <stdio.h>
#include <functional>
#include "optick.h"

#include "TestEngine.h"

using namespace std;

class MiniScheduler
{
	struct Context
	{
		volatile bool destroying = false;
		std::thread worker;
		std::queue<std::function<void()>> jobQueue;
		std::mutex queueMutex;
		std::condition_variable condition;

		void Update()
		{
			OPTICK_THREAD("Worker");
			while (true)
			{
				std::function<void()> job;
				{
					std::unique_lock<std::mutex> lock(queueMutex);
					condition.wait(lock, [this] { return !jobQueue.empty() || destroying; });
					if (destroying)
						break;
					job = jobQueue.front();
				}
				job();
				{
					std::lock_guard<std::mutex> lock(queueMutex);
					jobQueue.pop();
					condition.notify_one();
				}
			}
		}

		Context() : worker(std::thread(&Context::Update, this)) {}
		~Context() 
		{
			if (worker.joinable())
			{
				destroying = true;
				condition.notify_one();
				worker.join();
			}
		}

		void Add(std::function<void()> function)
		{
			std::lock_guard<std::mutex> lock(queueMutex);
			jobQueue.push(std::move(function));
			condition.notify_one();
		}
	};

	std::array<Context, 4> threads;
public:
	void Add(std::function<void()> function)
	{
		int index = rand() % threads.size();
		threads[index].Add(function);
	}
};

MiniScheduler scheduler;

void UpdateFrame()
{
	// Flip "Main/Update" frame
	OPTICK_FRAME_EVENT(Optick::FrameType::CPU);

	// Root category event 
	OPTICK_CATEGORY("UpdateFrame", Optick::Category::GameLogic);

	// Emulating busy work
	Test::SpinSleep(16);
	cout << '.' << flush;

	// Kicking next "Update Frame"
	scheduler.Add(UpdateFrame);
}

#if OPTICK_MSVC
int wmain()
#else
int main()
#endif
{
	// Registering current thread
	OPTICK_THREAD("MainThread");

	// Adding screenshot, summary, etc.
	OPTICK_SET_STATE_CHANGED_CALLBACK(Test::OnOptickStateChanged);

	cout << "Starting profiler test." << endl;

	// Kicking next "Update Frame"
	scheduler.Add(UpdateFrame);

	// Waiting for any key
	int res = getchar();

	OPTICK_SHUTDOWN();

	return res;
}
