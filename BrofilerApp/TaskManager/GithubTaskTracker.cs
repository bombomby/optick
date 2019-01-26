using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Profiler.TaskManager
{
    public class GithubTaskTracker : TaskTracker
    {
		public String UserName { get; set; }
		public String ProjectName { get; set; }

		public GithubTaskTracker(String userName, String projectName)
		{
			UserName = userName;
			ProjectName = projectName;
		}

		public override void CreateIssue(Issue issue)
		{
			StringBuilder bodyBuilder = new StringBuilder();

			if (issue.Image != null)
				bodyBuilder.AppendFormat("![Screenshot]({0})\n", issue.Image.URL);

			if (issue.Capture != null)
				bodyBuilder.AppendFormat("Capture: {0}\n", issue.Capture.URL);

			bodyBuilder.Append(issue.Body);

			String path = String.Format("https://github.com/{0}/{1}/issues/new?title={2}&body={3}", UserName, ProjectName, issue.Title, bodyBuilder);
			String url = HttpUtility.UrlEncode(path);
			System.Diagnostics.Process.Start(url);
		}
	}
}
