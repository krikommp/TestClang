using System.Collections.Generic;
using System.Reflection;

namespace ConsoleApp3.TemplateEngine
{
    public class Globals
    {
		public Dictionary<string, object> Context
		{
			get;
			set;
		}

		public HashSet<Assembly> Assemblies
		{
			get;
			set;
		}

		public HashSet<string> Namespaces
		{
			get;
			set;
		}

		public Globals()
		{
			Context = new Dictionary<string, object>();
			Assemblies = new HashSet<Assembly>();
			Namespaces = new HashSet<string>();
		}
	}
}
