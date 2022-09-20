using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppAst;
using System.IO;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines(@"D:\SandBox\ConsoleApp1\ConsoleApp1\2.txt");
            var fileName = "CoreUObject.cpp";
            var content = "#include \"Public/CoreUObject.h\"";
            var options = new CppParserOptions();
            options.ParseSystemIncludes = false;
            options.AdditionalArguments.AddRange(lines);
            var compilation = CppParser.Parse(content, options, fileName);
            if (!compilation.HasErrors)
            {
                foreach (var coreClass in compilation.Classes)
                {
                    if (coreClass.Name.Equals("UClass"))
                    {
                        foreach (var baseType in coreClass.BaseTypes)
                        {
                            Console.WriteLine(baseType.ToString());
                        }
                    }
                }
            }
        }
    }
}
