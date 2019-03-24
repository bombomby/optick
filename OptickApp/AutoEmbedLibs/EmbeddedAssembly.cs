using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace Profiler.AutoEmbedLibs
{
	class EmbeddedAssembly
	{
		static Dictionary<String, Assembly> libs = new Dictionary<string,Assembly>();

		public static Assembly Get(String name)
		{
			if (libs.ContainsKey(name))
				return libs[name];

			Assembly assembly = null;

			try
			{
				int index = name.IndexOf(",");
				string resource = String.Format("Profiler.AutoEmbedLibs.{0}.dll", index < 0 ? name : name.Substring(0, index));
				Assembly curAsm = Assembly.GetExecutingAssembly();
				using (Stream stream = curAsm.GetManifestResourceStream(resource))
				{
					if (stream != null)
					{
						byte[] buffer = new byte[(int)stream.Length];
						stream.Read(buffer, 0, (int)stream.Length);
						assembly = Assembly.Load(buffer);
					}
				}
			}
			catch (Exception) { }

			libs[name] = assembly;

			return assembly;
		}
	}
}
