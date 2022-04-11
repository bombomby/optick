using System.IO;

namespace Profiler.Data
{
	public interface ISavable
	{
		void Save(Stream stream);
	}
}
