using Profiler.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Profiler.TaskManager
{
	public class Attachment
	{
		public FileAttachment.Type Type { get; set; }
		public String Name { get; set; }
		public Uri URL { get; set; }
	}

	public class Issue
	{
		public String Title { get; set; }
		public String Body { get; set; }
		public List<Attachment> Attachments { get; set; } = new List<Attachment>();
	}

	public enum TrackerType
	{
		GitHub,
		Jira,
	}

	public abstract class TaskTracker
    {
		public String Address { get; set; }

		public abstract TrackerType TrackerType { get; }
		public abstract String ImageTemplate { get; }
		public abstract String LinkTemplate { get; }

		public abstract String DisplayName { get; }
		public abstract String Icon { get; }
		public abstract void CreateIssue(Issue issue);


		public static String BuildBody(TaskTracker tracker, Issue issue)
		{
			StringBuilder bodyBuilder = new StringBuilder();

			if (issue.Attachments.Count > 0)
			{
				Attachment image = issue.Attachments.Find(i => i.Type == FileAttachment.Type.IMAGE);
				if (image != null)
				{
					bodyBuilder.AppendFormat(tracker.ImageTemplate, image.Name, image.URL);
					bodyBuilder.AppendLine();
				}

				bodyBuilder.Append("Attachments: ");
				foreach (Attachment att in issue.Attachments)
				{
					if (att.Type != FileAttachment.Type.CAPTURE)
					{
						bodyBuilder.AppendFormat(tracker.LinkTemplate, att.Name, att.URL);
						bodyBuilder.Append(" ");
					}
				}
				bodyBuilder.AppendLine();

				Attachment capture = issue.Attachments.Find(i => i.Type == FileAttachment.Type.CAPTURE);
				if (capture != null)
				{
					bodyBuilder.AppendFormat("Capture: " + tracker.LinkTemplate, capture.Name, capture.URL);
					bodyBuilder.AppendLine();
				}
			}

			bodyBuilder.Append(issue.Body);

			return bodyBuilder.ToString();
		}
    }


	public class GithubTaskTracker : TaskTracker
	{
		public override string ImageTemplate => "![{0}]({1})";
		public override string LinkTemplate => "[[{0}]({1})]";
		public override string Icon => "appbar_social_github_octocat_solid";
		public override string DisplayName => Address;
		public override TrackerType TrackerType => TrackerType.GitHub;

		public GithubTaskTracker(String address)
		{
			Address = address;
		}

		public override void CreateIssue(Issue issue)
		{
			String body = BuildBody(this, issue);
			String url = String.Format("{0}/issues/new?&title={1}&body={2}", Address, HttpUtility.UrlEncode(issue.Title), HttpUtility.UrlEncode(body));
			System.Diagnostics.Process.Start(url);
		}
	}

	public class JiraTaskTracker : TaskTracker
	{
		public override string ImageTemplate => "!{1}|width=100%!";
		public override string LinkTemplate => @"\[[{0}|{1}]\]";
		public override string Icon => "appbar_social_jira";
		public override string DisplayName => Address;
		public override TrackerType TrackerType => TrackerType.Jira;

		public JiraTaskTracker(String address)
		{
			Address = address;
		}

		public override void CreateIssue(Issue issue)
		{
			String body = BuildBody(this, issue);
			String url = String.Format("{0}&summary={1}&description={2}", Address, HttpUtility.UrlEncode(issue.Title), HttpUtility.UrlEncode(body));
			System.Diagnostics.Process.Start(url);
		}
	}
}
