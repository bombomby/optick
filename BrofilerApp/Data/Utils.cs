using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Profiler.Data
{
	public static class Utils
	{
		public static int BinarySearchIndex<T, U>(List<T> frames, U value, Func<T, U> mapping) where U : IComparable
		{
			if (frames == null || frames.Count == 0)
				return -1;

			int left = 0;
			int right = frames.Count - 1;

			if (value.CompareTo(mapping(frames[0])) <= 0)
				return left;

			if (value.CompareTo(mapping(frames[frames.Count - 1])) >= 0)
				return right;

			while (left != right)
			{
				int index = (left + right + 1) / 2;

				if (value.CompareTo(mapping(frames[index])) < 0)
					right = index - 1;
				else
					left = index;
			}

			return left;
		}

		public static int BinarySearchClosestIndex<T>(List<T> frames, long value) where T : ITick
		{
			if (frames == null || frames.Count == 0)
				return -1;

			int left = 0;
			int right = frames.Count - 1;

			if (value <= frames[0].Start)
				return left;

			if (value >= frames[frames.Count - 1].Start)
				return right;

			while (left != right)
			{
				int index = (left + right + 1) / 2;

				if (frames[index].Start > value)
					right = index - 1;
				else
					left = index;
			}

			return left;
		}

		public static int BinarySearchExactIndex<T>(List<T> frames, long value) where T : IDurable
		{
			int index = BinarySearchClosestIndex(frames, value);
			if (index < 0)
				return -1;

			for (int i = index; i < Math.Min(index + 2, frames.Count); ++i)
			{
				if (frames[i].Start <= value && value < frames[i].Finish)
					return i;
			}

			return -1;
		}

		public static void ForEachInsideInterval<T>(List<T> frames, Durable interval, Action<T> action) where T : ITick
		{
			ForEachInsideInterval(frames, interval.Start, interval.Finish, action);
		}

		public static void ForEachInsideInterval<T>(List<T> frames, long start, long finish, Action<T> action) where T : ITick
		{
			int left = BinarySearchClosestIndex(frames, start);
			int right = BinarySearchClosestIndex(frames, finish);

			for (int i = left; i <= right && i != -1; ++i)
			{
				action(frames[i]);
			}
		}

		public static void ForEachInsideInterval<T>(List<T> frames, Durable interval, Action<T, int> action) where T : ITick
		{
			ForEachInsideInterval(frames, interval.Start, interval.Finish, action);
		}

		public static void ForEachInsideInterval<T>(List<T> frames, long start, long finish, Action<T, int> action) where T : ITick
		{
			int left = BinarySearchClosestIndex(frames, start);
			int right = BinarySearchClosestIndex(frames, finish);

			for (int i = left; i <= right && i != -1; ++i)
			{
				action(frames[i], i);
			}
		}

		public static void ForEachInsideIntervalStrict<T>(List<T> frames, Durable interval, Action<T> action) where T : ITick
		{
			ForEachInsideIntervalStrict(frames, interval.Start, interval.Finish, action);
		}

		public static void ForEachInsideIntervalStrict<T>(List<T> frames, long start, long finish, Action<T> action) where T : ITick
		{
			int left = BinarySearchClosestIndex(frames, start);
			int right = BinarySearchClosestIndex(frames, finish);

			for (int i = left; i <= right && i != -1; ++i)
			{
				if (start <= frames[i].Start && frames[i].Start < finish)
					action(frames[i]);
			}
		}

		public static String ReadBinaryString(BinaryReader reader)
		{
			return System.Text.Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadInt32()));
		}

		public static String ReadBinaryWideString(BinaryReader reader)
		{
			return System.Text.Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
		}


		public static bool IsSorted<T>(this List<T> list) where T : IComparable<T>
		{
			for (int i = 0; i < list.Count - 1; ++i)
				if (list[i].CompareTo(list[i + 1]) > 0)
					return false;

			return true;
		}

		public static String GenerateShortGUID()
		{
			return Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
		}

		public static void CopyStream(Stream from, Stream to, Action<double> onProgress, int bufferSize = 64 << 10)
		{
			byte[] buffer = new byte[bufferSize];

			int read = 0;
			int totalRead = 0;

			while ((read = from.Read(buffer, 0, bufferSize)) > 0)
			{
				to.Write(buffer, 0, read);
				totalRead += read;
				onProgress((double)totalRead / from.Length);
			}
		}
	}
}
