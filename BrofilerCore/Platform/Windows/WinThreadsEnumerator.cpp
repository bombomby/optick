#ifdef _WIN32

#include "..\ThreadsEnumerator.h"
#include <windows.h>
#include <tlhelp32.h>
#include <psapi.h>

#pragma comment( lib, "psapi.lib" )

namespace Brofiler
{
	bool EnumerateAllThreads(std::vector<ThreadInfo>& threads)
	{
		char tempBuffer[MAX_PATH];

		DWORD processId = GetCurrentProcessId();

		HANDLE h = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, 0);
		if (h == INVALID_HANDLE_VALUE)
		{
			return false;
		}

		THREADENTRY32 te;
		te.dwSize = sizeof(te);

		if (Thread32First(h, &te))
		{
			do
			{
				if (te.dwSize >= FIELD_OFFSET(THREADENTRY32, th32OwnerProcessID) + sizeof(te.th32OwnerProcessID))
				{
					bool isFound = false;
					for (auto it = threads.begin(); it != threads.end(); ++it)
					{
						if (it->id.AsUInt64() == (uint64)te.th32ThreadID)
						{
							isFound = true;
							break;
						}
					}

					if (!isFound)
					{
						// sad :( but we can't get thread name on windows
						const char* threadName = "Unknown";

						bool threadFromOtherProcess = false;
						if (te.th32OwnerProcessID != processId)
						{
							threadFromOtherProcess = true;
							threadName = nullptr;

							HANDLE procHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, te.th32OwnerProcessID);
							if (procHandle)
							{
								if (GetModuleFileNameExA(procHandle, 0, tempBuffer, MAX_PATH))
								{
									const char* fileName = &tempBuffer[0];
									const char* p = fileName;
									while (*p)
									{
										if (*p == '\\' || *p == '/')
										{
											fileName = p + 1;
										}
										p++;
									}

									threadName = fileName;
								}

								CloseHandle(procHandle);
							}
						}

						if (!threadName)
						{
							threadName = "<Process>";
						}

						threads.push_back(ThreadInfo(te.th32ThreadID, threadName, threadFromOtherProcess));
					}
				}
				te.dwSize = sizeof(te);
			} while (Thread32Next(h, &te));
		}
		CloseHandle(h);

		return true;
	}
}

#endif