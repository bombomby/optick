// The MIT License (MIT)
//
// 	Copyright (c) 2015 Sergey Makeev, Vadim Slyusarev
//
// 	Permission is hereby granted, free of charge, to any person obtaining a copy
// 	of this software and associated documentation files (the "Software"), to deal
// 	in the Software without restriction, including without limitation the rights
// 	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// 	copies of the Software, and to permit persons to whom the Software is
// 	furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
// 	all copies or substantial portions of the Software.
//
// 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// 	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// 	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// 	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// 	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// 	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// 	THE SOFTWARE.

#pragma once

#ifndef __MT_FIBER__
#define __MT_FIBER__

#include <ucontext.h>
#include <stdlib.h>
#include <string.h>
#include <pthread.h>
#include <sys/mman.h>

#ifndef MAP_ANONYMOUS
    #define MAP_ANONYMOUS MAP_ANON
#endif

#ifndef MAP_STACK
    #define MAP_STACK (0)
#endif

#include <MTAppInterop.h>
#include "MTAtomic.h"

namespace MT
{

	//
	//
	//
	class Fiber
	{
		void* funcData;
		TThreadEntryPoint func;

		Memory::StackDesc stackDesc;

		ucontext_t fiberContext;
		bool isInitialized;

		static void FiberFuncInternal(void* pFiber)
		{
			MT_ASSERT(pFiber != nullptr, "Invalid fiber");
			Fiber* self = (Fiber*)pFiber;

			MT_ASSERT(self->isInitialized == true, "Using non initialized fiber");

			MT_ASSERT(self->func != nullptr, "Invalid fiber func");
			self->func(self->funcData);
		}

		void CleanUp()
		{
			if (isInitialized)
			{
				// if func != null than we have stack memory ownership
				if (func != nullptr)
				{
					Memory::FreeStack(stackDesc);
				}

				isInitialized = false;
			}
		}

	public:

		MT_NOCOPYABLE(Fiber);

		Fiber()
			: funcData(nullptr)
			, func(nullptr)
			, isInitialized(false)
		{
			memset(&fiberContext, 0, sizeof(ucontext_t));
		}

		~Fiber()
		{
			CleanUp();
		}


		void CreateFromCurrentThreadAndRun(TThreadEntryPoint entryPoint, void *userData)
		{
			MT_ASSERT(!isInitialized, "Already initialized");

			int res = 0;
			void* stackAddr = nullptr;
			size_t stackSize = 0;
			pthread_t callThread = pthread_self();

#if MT_PLATFORM_OSX

			stackAddr = pthread_get_stackaddr_np(callThread);
			stackSize = pthread_get_stacksize_np(callThread);

#else
			// get current thread attributes
			pthread_attr_t threadAttr;
			
			res = pthread_getattr_np(callThread, &threadAttr);
			MT_USED_IN_ASSERT(res);
			MT_ASSERT(res == 0, "pthread_getattr_np - failed");

			// get current thread stack
			res = pthread_attr_getstack(&threadAttr, &stackAddr, &stackSize);
			MT_USED_IN_ASSERT(res);
			MT_ASSERT(res == 0, "pthread_attr_getstack - failed");
#endif

			MT_ASSERT(stackAddr != nullptr, "Invalid stack address");
			MT_ASSERT(stackSize > 0, "Invalid stack size");

			// get execution context
			res = getcontext(&fiberContext);
			MT_USED_IN_ASSERT(res);
			MT_ASSERT(res == 0, "getcontext - failed");

			// setup context
			fiberContext.uc_link = nullptr;
			fiberContext.uc_stack.ss_sp = stackAddr;
			fiberContext.uc_stack.ss_size = stackSize;
			fiberContext.uc_stack.ss_flags = 0;

			func = nullptr;
			funcData = nullptr;

			isInitialized = true;

			entryPoint(userData);

			CleanUp();
		}


		void Create(size_t stackSize, TThreadEntryPoint entryPoint, void *userData)
		{
			MT_ASSERT(!isInitialized, "Already initialized");
			MT_ASSERT(stackSize >= PTHREAD_STACK_MIN, "Stack to small");

			func = entryPoint;
			funcData = userData;

			int res = getcontext(&fiberContext);
			MT_USED_IN_ASSERT(res);
			MT_ASSERT(res == 0, "getcontext - failed");

			stackDesc = Memory::AllocStack(stackSize);

			fiberContext.uc_link = nullptr;
			fiberContext.uc_stack.ss_sp = stackDesc.stackBottom;
			fiberContext.uc_stack.ss_size = stackDesc.GetStackSize();
			fiberContext.uc_stack.ss_flags = 0;

			makecontext(&fiberContext, (void(*)())&FiberFuncInternal, 1, (void *)this);

			isInitialized = true;
		}

#ifdef MT_INSTRUMENTED_BUILD
		void SetName(const char* fiberName)
		{
			MT_UNUSED(fiberName);
		}
#endif

		static void SwitchTo(Fiber & from, Fiber & to)
		{
			HardwareFullMemoryBarrier();

			MT_ASSERT(from.isInitialized, "Invalid from fiber");
			MT_ASSERT(to.isInitialized, "Invalid to fiber");

			int res = swapcontext(&from.fiberContext, &to.fiberContext);
			MT_USED_IN_ASSERT(res);
			MT_ASSERT(res == 0, "setcontext - failed");

		}



	};


}


#endif
