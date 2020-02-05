using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;

namespace Profiler.Data
{

	public static class SourceColumns
	{
		public static CustomColumnDescription TotalPercent = new CustomColumnDescription("Total%", 7);
		public static CustomColumnDescription Total = new CustomColumnDescription("Total", 7) { HasSeparator = true };
		public static CustomColumnDescription SelfPercent = new CustomColumnDescription("Self%", 7);
		public static CustomColumnDescription Self = new CustomColumnDescription("Self", 7) { HasSeparator = true };

		public static List<CustomColumnDescription> Default = new List<CustomColumnDescription>() { TotalPercent, Total, SelfPercent, Self };
	}

	public class SourceLine
	{
		public double TotalPercent { get; private set; }
		public double Total { get; private set; }
		public double SelfPercent { get; private set; }
		public double Self { get; private set; }

		public void Add(BaseTreeNode node)
		{
			TotalPercent += node.TotalPercent;
			Total += node.Duration;

			SelfPercent += node.SelfPercent;
			Self += node.SelfDuration;
		}
	}

	public class SourceViewBase
	{
		public Dictionary<int, SourceLine> Lines { get; protected set; }
		public String Text { get; protected set; }
		public FileLine SourceFile { get; protected set; }
	}

	public class SourceView<TItem, TDescription, TNode> : SourceViewBase
	where TItem : BoardItem<TDescription, TNode>, new()
		where TNode : TreeNode<TDescription>
		where TDescription : Description
	{
		public String Description { get { return SourceFile != null ? SourceFile.File : "Unknown File"; } }

		private SourceView(Board<TItem, TDescription, TNode> board, FileLine path, String text)
		{
			Lines = new Dictionary<int, SourceLine>();

			IEnumerable<TItem> boardItems = board.Where(boardItem =>
	  {
		  return (boardItem.Description != null &&
				  boardItem.Description.Path != null &&
								  boardItem.Description.Path.File == path.File);
	  });

			foreach (TItem item in boardItems)
			{
				foreach (TNode node in item.Nodes)
				{
					SourceLine line = null;
					if (!Lines.TryGetValue(node.Description.Path.Line, out line))
					{
						line = new SourceLine();
						Lines.Add(node.Description.Path.Line, line);
					}

					line.Add(node);
				}
			}

			SourceFile = path;
			Text = text;
		}

		public static SourceView<TItem, TDescription, TNode> Create(Board<TItem, TDescription, TNode> board, FileLine path)
		{
			if (path == null || String.IsNullOrEmpty(path.File))
				return null;

			String file = path.File;

			while (!File.Exists(file))
			{
				OpenFileDialog openFileDialog = new OpenFileDialog();

				String filter = path.ShortName;

				openFileDialog.Title = String.Format("Where can I find {0}?", filter);
				openFileDialog.ShowReadOnly = true;

				openFileDialog.FileName = filter;

				if (openFileDialog.ShowDialog() == true)
				{
					file = openFileDialog.FileName;
				}
				else
				{
					return null;
				}
			}

			return new SourceView<TItem, TDescription, TNode>(board, path, File.ReadAllText(file));
		}
	}
}
