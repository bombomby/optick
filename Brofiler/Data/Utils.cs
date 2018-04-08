using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.Data
{
    class Utils
    {
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
            int left = BinarySearchClosestIndex(frames, interval.Start);
            int right = BinarySearchClosestIndex(frames, interval.Finish);

            for (int i = left; i <= right && i != -1; ++i)
            {
                action(frames[i]);
            }
        }

        public static void ForEachInsideIntervalStrict<T>(List<T> frames, Durable interval, Action<T> action) where T : ITick
        {
            int left = BinarySearchClosestIndex(frames, interval.Start);
            int right = BinarySearchClosestIndex(frames, interval.Finish);

            for (int i = left; i <= right && i != -1; ++i)
            {
                if (interval.Intersect(frames[i].Start))
                    action(frames[i]);
            }
        }

        public static String ReadBinaryString(BinaryReader reader)
        {
            return System.Text.Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32()));
        }

    }
}
