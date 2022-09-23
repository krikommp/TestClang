using System;
using System.IO;
using CppAst;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConsoleApp1
{
    public class Program
    {
        public static IEnumerable<CppBaseType> GetBasesRecursive(CppClass type) {
            var outClass = new List<CppBaseType>();
            outClass.AddRange(type.BaseTypes);
            if (outClass.Count != 0)
            {
                foreach (var baseType in type.BaseTypes) {
                    if (baseType.Type.TypeKind == CppTypeKind.StructOrClass)
                    {
                        var baseClass = (CppClass)baseType.Type;
                        outClass.AddRange(GetBasesRecursive(baseClass));
                    }
                }
            }
            return outClass;
        }
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines(@"D:\SandBox\ConsoleApp1\ConsoleApp1\2.txt");
            var fileName = "CoreUObject.cpp";
            var content = "#include \"Public/CoreUObject.h\"";
            var options = new CppParserOptions();
            options.ParseSystemIncludes = false;
            options.AdditionalArguments.AddRange(lines);
            options.AdditionalArguments.AddRange(new string[]
            {
                "-isystem/usr/local/opt/llvm/bin/../include/c++/v1",
                "-isystem/usr/local/Cellar/llvm/14.0.6_1/lib/clang/14.0.6/include",
                "-isystem/Library/Developer/CommandLineTools/SDKs/MacOSX12.sdk/usr/include",
                "-F/Library/Developer/CommandLineTools/SDKs/MacOSX12.sdk/System/Library/Frameworks",
                "-Wno-elaborated-enum-base",
                "-x",
                "objective-c",
            });
            var compilation = CppParser.Parse(content, options, fileName);
            if (!compilation.HasErrors)
            {
                foreach (var parseClass in compilation.Classes)
                {
                    if (parseClass.Name.Equals("UClass"))
                    {
                        var templateFileContent = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp1\TestTemplate.txt");
                        var global = new TemplateEngine.Globals();
                        global.Context.Add("type", parseClass);
                        global.Assemblies.Add(typeof(Program).Assembly);
                        global.Assemblies.Add(typeof(Regex).Assembly);
                        global.Namespaces.Add("System.Linq");
                        global.Namespaces.Add("System.Text.RegularExpressions");
                        var o = TemplateEngine.CSharpTemplate.Compile<string>(templateFileContent, global);
                        Console.WriteLine(o);
                    }
                }
            }
            else {
                foreach (var message in compilation.Diagnostics.Messages) {
                    Console.WriteLine(message);
                } 
            }
        }
    }
}
