using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.TaskManager
{
	public class Attachment
	{
		public String Name { get; set; }
		public Uri URL { get; set; }
	}

	public class Issue
	{
		public String Title { get; set; }
		public String Body { get; set; }
		public Attachment Image { get; set; }
		public Attachment Capture { get; set; }

		public delegate void ProgressDeletate(String test, double progress);
		public event ProgressDeletate OnProgress;

		public delegate void CompleteDeletate();
		public event CompleteDeletate OnCompleted;
	}

	public abstract class TaskTracker
    {
		public abstract void CreateIssue(Issue issue);
    }
}
