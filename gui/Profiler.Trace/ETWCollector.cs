using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Profiler.Trace
{
    class ETWCollector : IDisposable
    {
        TraceEventSession Session { get; set; }
        Task ReaderTask { get; set; }

        Dictionary<int, ProcessData> ProcessDataMap = new Dictionary<int, ProcessData>();
        Dictionary<ulong, IOData> IODataMap = new Dictionary<ulong, IOData>();

        ThreadData[] ActiveCoresMap = new ThreadData[Environment.ProcessorCount];
        
        HashSet<String> Filters { get; set; }

        public event Action<ProcessData> ProcessEvent;
		public event Action<SwitchContextData> SwitchContextEvent;

		public const KernelTraceEventParser.Keywords TraceProcessFlags = KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Thread | KernelTraceEventParser.Keywords.ContextSwitch;

		public ETWCollector(KernelTraceEventParser.Keywords flags = TraceProcessFlags)
        {
            Session = new TraceEventSession("Optick");
            Session.BufferSizeMB = 256;

            Session.EnableKernelProvider(flags);

            // Processes
            Session.Source.Kernel.ProcessStart += Kernel_ProcessStart;
            Session.Source.Kernel.ProcessStop += Kernel_ProcessStop;

            // Image
            Session.Source.Kernel.ImageLoad += Kernel_ImageLoad;

            // Threads
            Session.Source.Kernel.ThreadStart += Kernel_ThreadStart;
            Session.Source.Kernel.ThreadStop += Kernel_ThreadStop;

            // IO
            Session.Source.Kernel.FileIORead += Kernel_FileIORead;
            Session.Source.Kernel.FileIOWrite += Kernel_FileIOWrite;
            Session.Source.Kernel.FileIOOperationEnd += Kernel_FileIOOperationEnd;

            // SysCalls
            Session.Source.Kernel.PerfInfoSysClEnter += Kernel_PerfInfoSysClEnter;
            Session.Source.Kernel.PerfInfoSysClExit += Kernel_PerfInfoSysClExit;

            // Switch Contexts
            Session.Source.Kernel.ThreadCSwitch += Kernel_ThreadCSwitch;

            // Samples
            Session.Source.Kernel.StackWalkStack += Kernel_StackWalkStack;
        }

        private void Kernel_StackWalkStack(Microsoft.Diagnostics.Tracing.Parsers.Kernel.StackWalkStackTraceData obj)
        {
            ThreadData thread = GetThreadData(obj);
            if (thread != null)
            {
                CallstackData callstack = new CallstackData() { Timestamp = obj.TimeStamp };

                callstack.Callstack = new UInt64[obj.FrameCount];
                for (int i = 0; i < obj.FrameCount; ++i)
                    callstack.Callstack[obj.FrameCount-i-1] = obj.InstructionPointer(i);

                thread.Callstacks.Add(callstack);   
            }
        }

        private void Kernel_ImageLoad(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ImageLoadTraceData obj)
        {
            ProcessData process = GetProcessData(obj);
            if (process != null)
            {
                process.Images.Add(new ImageData()
                {
                    FileName = obj.FileName,
                    DefaultBase = obj.DefaultBase,
                    ImageBase = obj.ImageBase,
                    ImageChecksum = obj.ImageChecksum,
                    ImageSize = obj.ImageSize
                });
            }
        }

        private void Kernel_ThreadCSwitch(Microsoft.Diagnostics.Tracing.Parsers.Kernel.CSwitchTraceData obj)
        {
			if (SwitchContextEvent != null)
			{
				SwitchContextData sc = new SwitchContextData()
				{
					CPUID = (byte)obj.ProcessorNumber,
					NewThreadID = (ulong)obj.NewThreadID,
					OldThreadID = (ulong)obj.OldThreadID,
					Timestamp = obj.TimeStamp,
				};

				SwitchContextEvent.Invoke(sc);
			}

            //ProcessData newProcess = null;
            //if (ProcessDataMap.TryGetValue(obj.NewProcessID, out newProcess))
            //{
            //    ThreadData thread = newProcess.Threads[obj.NewThreadID];
            //    thread.WorkIntervals.Add(new WorkIntervalData()
            //    {
            //        Start = obj.TimeStamp,
            //        CpuID = obj.ProcessorNumber,
            //        Finish = DateTime.MinValue,
            //    });

            //    ActiveCoresMap[obj.ProcessorNumber] = thread;
            //}
            //else
            //{
            //    ActiveCoresMap[obj.ProcessorNumber] = null;
            //}

            //ProcessData oldProcess = null;
            //if (ProcessDataMap.TryGetValue(obj.OldProcessID, out oldProcess))
            //{
            //    ThreadData thread = oldProcess.Threads[obj.OldThreadID];
            //    if (thread.WorkIntervals.Count > 0)
            //    {
            //        WorkIntervalData interval = thread.WorkIntervals[thread.WorkIntervals.Count - 1];
            //        interval.Finish = obj.TimeStamp;
            //        interval.WaitReason = (int)obj.OldThreadWaitReason;
            //    }
            //}
        }

        private void Kernel_PerfInfoSysClEnter(Microsoft.Diagnostics.Tracing.Parsers.Kernel.SysCallEnterTraceData obj)
        {
            ThreadData thread = ActiveCoresMap[obj.ProcessorNumber];
            if (thread != null)
            {
                thread.SysCalls.Add(new SysCallData()
                {
                    Start = obj.TimeStamp,
                    Finish = DateTime.MinValue,
                    Address = obj.SysCallAddress
                });
            }
        }

        private void Kernel_PerfInfoSysClExit(Microsoft.Diagnostics.Tracing.Parsers.Kernel.SysCallExitTraceData obj)
        {
            ThreadData thread = ActiveCoresMap[obj.ProcessorNumber];
            if (thread != null)
            {
                for (int i = thread.SysCalls.Count - 1; i >= 0; --i)
                {
                    if (thread.SysCalls[i].Finish <= thread.SysCalls[i].Start)
                    {
                        thread.SysCalls[i].Finish = obj.TimeStamp;
                        return;
                    }
                }
            }
        }

        private IOData CreateIOData(IOData.Type type, Microsoft.Diagnostics.Tracing.Parsers.Kernel.FileIOReadWriteTraceData obj)
        {
            IOData ioData = new IOData()
            {
                Start = obj.TimeStamp,
                FileName = obj.FileName,
                Offset = obj.Offset,
                Size = obj.IoSize,
                ThreadID = obj.ThreadID,
                IOType = type
            };

            IODataMap[obj.IrpPtr] = ioData;

            return ioData;
        }

        private void Kernel_FileIORead(Microsoft.Diagnostics.Tracing.Parsers.Kernel.FileIOReadWriteTraceData obj)
        {
            ThreadData thread = GetThreadData(obj);
            if (thread != null)
            {
                thread.IORequests.Add(CreateIOData(IOData.Type.Read, obj));
            }
        }

        private void Kernel_FileIOWrite(Microsoft.Diagnostics.Tracing.Parsers.Kernel.FileIOReadWriteTraceData obj)
        {
            ThreadData thread = GetThreadData(obj);
            if (thread != null)
            {
                thread.IORequests.Add(CreateIOData(IOData.Type.Write, obj));
            }
        }

        private void Kernel_FileIOOperationEnd(Microsoft.Diagnostics.Tracing.Parsers.Kernel.FileIOOpEndTraceData obj)
        {
            IOData ioRequest = null;
            if (IODataMap.TryGetValue(obj.IrpPtr, out ioRequest))
            {
                ioRequest.Finish = obj.TimeStamp;
                IODataMap.Remove(obj.IrpPtr);
            }
        }

        ProcessData GetProcessData(TraceEvent obj)
        {
            ProcessData process = null;
            ProcessDataMap.TryGetValue(obj.ProcessID, out process);
            return process;
        }

        ThreadData GetThreadData(TraceEvent obj)
        {
            ThreadData thread = null;
            ProcessData process = GetProcessData(obj);
            if (process != null)
            {
                process.Threads.TryGetValue(obj.ThreadID, out thread);
            }
            return thread;
        }

        private void Kernel_ThreadStart(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ThreadTraceData obj)
        {
            ProcessData process = GetProcessData(obj);
            if (process != null)
            {
                process.Threads[obj.ThreadID] = new ThreadData()
                {
                    ThreadID = obj.ThreadID,
                    Start = obj.TimeStamp,
                };
            }
        }

        private void Kernel_ThreadStop(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ThreadTraceData obj)
        {
            ThreadData thread = GetThreadData(obj);
            if (thread != null)
            {
                thread.Finish = obj.TimeStamp;
            }
        }

        public void SetProcessFilter(IEnumerable<String> filters)
        {
            Filters = new HashSet<string>(filters, StringComparer.OrdinalIgnoreCase);
        }

        static char[] CharacterToTrim = { '\"', '\'', ' ', '\t', '\n' };

        private static void CollectArtifacts(ProcessData ev)
        {
            for (int start = ev.CommandLine.IndexOf('@'); start != -1; start = ev.CommandLine.IndexOf('@', start + 1))
            {
                int finish = Math.Max(ev.CommandLine.IndexOf(' ', start), ev.CommandLine.Length);
                String path = ev.CommandLine.Substring(start + 1, finish - start - 1);
                path = path.Trim(CharacterToTrim);

                try
                {
                    String text = File.ReadAllText(path);
                    ev.AddArtifact(path, text);
                }
                catch (FileNotFoundException) { }
            }
        }

        private void Kernel_ProcessStart(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ProcessTraceData obj)
        {
            if (Filters.Contains(obj.ImageFileName))
            {
                ProcessData ev = new ProcessData()
                {
                    Name = obj.ImageFileName,
                    CommandLine = obj.CommandLine,
                    Start = obj.TimeStamp,
                    ProcessID = obj.ProcessID,
                    UniqueKey = obj.UniqueProcessKey,
                };

                ProcessDataMap.Add(obj.ProcessID, ev);

                ProcessEvent?.Invoke(ev);

                Task.Run(() => CollectArtifacts(ev));
            }
        }

        private void Kernel_ProcessStop(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ProcessTraceData obj)
        {
            ProcessData ev = null;
            if (ProcessDataMap.TryGetValue(obj.ProcessID, out ev))
            {
                ev.Finish = obj.TimeStamp;
                ev.Result = obj.ExitStatus;
                ProcessDataMap.Remove(obj.ProcessID);
            }
        }

        public void Start()
        {
            ReaderTask = Task.Factory.StartNew(() =>
            {
                Session.Source.Process();
            });
        }


        public void Stop()
        {
			Session?.Flush();
			Session?.Dispose();

		}

        public void Dispose()
        {
            Session?.Dispose();
        }
    }
}
