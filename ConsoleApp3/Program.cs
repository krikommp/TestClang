using System;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ConsoleApp3
{
    public class Program
    {
        public class Bar
        {
            public string Foo = "Default";

            public int StaffId { get; set; }
            public int UnitId { get; set; }
            public int Age { get; set; }
        }

        public static Bar gBar = new Bar();

        public static void Main(string[] args)
        {
            var sciptOptions = ScriptOptions.Default;
            sciptOptions = sciptOptions.AddReferences(typeof(Bar).Assembly);
            sciptOptions = sciptOptions.AddReferences(
                Assembly.LoadFile(@"D:\SandBox\ConsoleApp1\ConsoleApp4\bin\Debug\netcoreapp3.1\ConsoleApp4.dll"));

            string ss = "Empty String";
            Bar dd = new Bar() { StaffId = 1223 };
            gBar.Foo = "I Fixed it";
            var s0 = CSharpScript.Create<Func<string, string>>(
                "(ss) => { return $\"Hello World! gBar.StaffId = {ConsoleApp3.Program.gBar.Foo}, gf = {RoslyTest.Program.gf.Foo} \"; }", sciptOptions);
            s0.Compile();
            var res = s0.RunAsync(null).Result.ReturnValue.Invoke(ss);
            Console.WriteLine(res);
        }
    }
}