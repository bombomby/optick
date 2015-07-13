using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Profiler.Data
{
  [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
  public class HiddenColumn : Attribute
  {
  }

  [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
  public class ColumnWidth : Attribute
  {
    public double Width { get; private set; }
    public ColumnWidth(double width)
    {
      Width = width;
    }
  }

  public interface IBoardItem
  {
    bool Match(String text);
		void CollectNodeFilter(HashSet<Object> filter);
		void CollectDescriptionFilter(HashSet<Object> filter);
  }

  public class BoardItem<TDesciption, TNode> : IBoardItem
		where TDesciption : Description
		where TNode : TreeNode<TDesciption>
  {
		[HiddenColumn]
		public List<TNode> Nodes = new List<TNode>();

		[HiddenColumn]
		public TDesciption Description { get { return Nodes[0].Description; } }

		public String GetFilterName()
		{
			return Description.Name;
		}

    virtual public void CollectNodeFilter(HashSet<Object> filter) 
		{
			filter.UnionWith(Nodes);
		}

		virtual public void CollectDescriptionFilter(HashSet<Object> filter)
		{
			filter.Add(Description);
		}

		public virtual void Add(TNode node)
		{
			Nodes.Add(node);
		}

    public bool Match(String text)
    {
      return GetFilterName().IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1;
    }

		public BoardItem() { }
  }

  public class EventBoardItem : BoardItem<EventDescription, EventNode>
  {
    [DisplayName("S")]
    public bool IsSampling
    { 
      get { return Description.IsSampling; } 
      set { Description.IsSampling = value; } 
    }

    [ColumnWidth(400)]
    public String Function { get { return Description.Name; } }

		[DisplayName("Self%")]
		public double SelfPercent { get; private set; }
		[DisplayName("Self(ms)")]
		public double SelfTime { get; private set; }


		//[DisplayName("Total%")]
		//public double TotalPercent { get; private set; }
		[DisplayName("Total(ms)")]
		public double Total { get; private set; }

		[DisplayName("Max(ms)")]
    public double MaxTime { get; set; }

    [HiddenColumn]
    public double ChildTime { get; private set; }
    
    public int Count { get; private set; }
		public String Path { get { return Description.Path.ShortPath; } }

    public override void Add(EventNode node)
    {
			base.Add(node);
      MaxTime = Math.Max(MaxTime, node.Entry.Duration);
      ChildTime += node.ChildrenDuration;
			SelfPercent += node.SelfPercent;
      SelfTime += node.SelfDuration;

      if (!node.ExcludeFromTotal)
      {
        Total += node.Entry.Duration;
        //TotalPercent += node.TotalPercent;
      }

      ++Count;
    }
  }

  public class SamplingBoardItem : BoardItem<SamplingDescription, SamplingNode>
  {
		[DisplayName("H")]
		public bool IsHooked
		{
			get { return Description.IsHooked; }
			set { Description.IsHooked = value; }
		}

    [ColumnWidth(400)]
    public String Function { get { return Description.Name; } }

    [DisplayName("Self %")]
    public double SelfPercent { get; private set; }
    public uint Self { get; private set; }

    [DisplayName("Total %")]
    public double TotalPercent { get; private set; }
    public uint Total { get; private set; }
    
    public String Module { get { return Description.ModuleShortName; } }
    public String Path { get { return Description.Path.ShortPath; } }

		public override void CollectDescriptionFilter(HashSet<Object> filter)
		{
			foreach (var node in Nodes)
				filter.Add(node.Description);
		}

    public override void Add(SamplingNode node)
    {
			base.Add(node);
      Self += node.Sampled;
      SelfPercent += node.SelfPercent;

      if (!node.ExcludeFromTotal)
      {
        Total += node.Passed;
        TotalPercent += node.TotalPercent;
      }
    }
  }

	public class Board<TItem, TDescription, TNode> : List<TItem>
    where TItem : BoardItem<TDescription, TNode>, new()
		where TNode : TreeNode<TDescription>
		where TDescription : Description
	{
    public Board(TNode node)
    {
      Dictionary<Object, TItem> items = new Dictionary<Object, TItem>();
      Add(items, node);
      AddRange(items.Values);
    }

		void Add(Dictionary<Object, TItem> items, TNode node)
    {
      TDescription description = node.Description;
      if (description != null)
      {
        TItem item;

				Object key = description.GetSharedKey();

				if (!items.TryGetValue(key, out item))
        {
          item = new TItem();
          items.Add(key, item);
        }

        item.Add(node);
      }

      foreach (TNode child in node.Children)
        Add(items, child);
    }
  }
}
