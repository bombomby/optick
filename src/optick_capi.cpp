// The MIT License(MIT)
//
// Copyright(c) 2019 Vadim Slyusarev
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#include "optick_capi.h"

#if USE_OPTICK

#include "optick_core.h"

#if defined(__MACH__)
#include <stdlib.h>
#else 
#include <malloc.h>
#endif
#include <string.h>
#include <stdarg.h>

OPTICK_API void OptickAPI_SetAllocator(OptickAPI_AllocateFn allocateFn, OptickAPI_DeallocateFn deallocateFn, OptickAPI_InitThreadCb initThreadCb)
{
	::Optick::SetAllocator(allocateFn, deallocateFn, initThreadCb);
}

OPTICK_API void OptickAPI_RegisterThread(const char* inThreadName, uint16_t inThreadNameLength)
{
	Optick::OptickString<256> threadName(inThreadName, inThreadNameLength);
	Optick::RegisterThread(threadName.data);
}

OPTICK_API uint64_t OptickAPI_CreateEventDescription(const char* inFunctionName, const char* inFileName, uint32_t inFileLine, OptickAPI_Category category)
{
	Optick::OptickString<128> name(inFunctionName, (uint16_t)strlen(inFunctionName));
	Optick::OptickString<256> file(inFileName, (uint16_t)strlen(inFileName));
	uint8_t flags = Optick::EventDescription::COPY_NAME_STRING | Optick::EventDescription::COPY_FILENAME_STRING | Optick::EventDescription::IS_CUSTOM_NAME;
	return (uint64_t)::Optick::CreateDescription(name.data, file.data, inFileLine, nullptr, (Optick::Category::Type)category, flags);
}
OPTICK_API uint64_t OptickAPI_PushEvent(uint64_t inEventDescription)
{
	return (uint64_t)Optick::Event::Start(*((Optick::EventDescription*)inEventDescription));
}

OPTICK_API void OptickAPI_PopEvent(uint64_t inEventData)
{
	Optick::Event::Stop(*((Optick::EventData*)inEventData));
}

OPTICK_API uint64_t OptickAPI_PushGPUEvent(uint64_t inEventDescription) 
{
	return (uint64_t)Optick::GPUEvent::Start(*((Optick::EventDescription*)inEventDescription));
}

OPTICK_API void OptickAPI_PopGPUEvent(uint64_t inEventData) 
{
	Optick::GPUEvent::Stop(*((Optick::EventData*)inEventData));
}

OPTICK_API void OptickAPI_NextFrame()
{
	Optick::Event::Pop();
	Optick::EndFrame();
	Optick::Update();
	Optick::BeginFrame();
	Optick::Event::Push(*Optick::GetFrameDescription());
}

OPTICK_API void OptickAPI_StartCapture()
{
	Optick::StartCapture();
}

OPTICK_API void OptickAPI_StopCapture(const char* inFileName, uint16_t inFileNameLength)
{
	Optick::OptickString<256> fileName(inFileName, inFileNameLength);
	Optick::StopCapture();
	Optick::SaveCapture(fileName.data);
}

OPTICK_API void OptickAPI_Shutdown()
{
	Optick::Shutdown();
}

OPTICK_API bool OptickAPI_SetStateChangedCallback(OptickAPI_StateCallback cb) 
{
	return ::Optick::SetStateChangedCallback((Optick::StateCallback)cb);
}

OPTICK_API bool OptickAPI_AttachSummary(const char* key, const char* value)
{
	return ::Optick::AttachSummary(key, value);
}

OPTICK_API bool OptickAPI_AttachFile(OptickAPI_File type, const char* name, const uint8_t* data, uint32_t size)
{
	return ::Optick::AttachFile((Optick::File::Type)type, name, data, size);
}

OPTICK_API void OptickAPI_GPUInitD3D12(ID3D12Device* device, ID3D12CommandQueue** cmdQueues, uint32_t numQueues)
{
	::Optick::InitGpuD3D12(device, cmdQueues, numQueues);
}

OPTICK_API void OptickAPI_GPUInitVulkan(VkDevice* vkDevices, VkPhysicalDevice* vkPhysicalDevices, VkQueue* vkQueues, uint32_t* cmdQueuesFamily, uint32_t numQueues, const OptickAPI_VulkanFunctions* functions)
{
	::Optick::InitGpuVulkan(vkDevices, vkPhysicalDevices, vkQueues, cmdQueuesFamily, numQueues, (const Optick::VulkanFunctions*)functions);
}

OPTICK_API void OptickAPI_GPUContext()
{

}

OPTICK_API void OptickAPI_GPUFlip(void* swapChain)
{
	::Optick::GpuFlip(swapChain);
}

OPTICK_API void OptickAPI_AttachTag_String(uint64_t inEventDescription, const char* inValue)
{
	Optick::Tag::Attach(*(Optick::EventDescription*)inEventDescription, inValue);
}

OPTICK_API void OptickAPI_AttachTag_Int32(uint64_t inEventDescription, int32_t inValue)
{
	Optick::Tag::Attach(*(Optick::EventDescription*)inEventDescription, inValue);
}

OPTICK_API void OptickAPI_AttachTag_Float(uint64_t inEventDescription, float inValue)
{
	Optick::Tag::Attach(*(Optick::EventDescription*)inEventDescription, inValue);
}

OPTICK_API void OptickAPI_AttachTag_UInt32(uint64_t inEventDescription, uint32_t inValue)
{
	Optick::Tag::Attach(*(Optick::EventDescription*)inEventDescription, inValue);
}

OPTICK_API void OptickAPI_AttachTag_UInt64(uint64_t inEventDescription, uint64_t inValue)
{
	Optick::Tag::Attach(*(Optick::EventDescription*)inEventDescription, inValue);
}

OPTICK_API void OptickAPI_AttachTag_Point(uint64_t inEventDescription, float x, float y, float z)
{
	Optick::Tag::Attach(*(Optick::EventDescription*)inEventDescription, x, y, z);
}

#endif //USE_OPTICK
