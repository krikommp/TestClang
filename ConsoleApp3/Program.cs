using System;
using System.Collections.Generic;
using System.IO;
using ClangSharp.Interop;
using CppAst;

namespace ConsoleApp3
{
    class Program
    {
        private const CXTranslationUnit_Flags defaultTranslationUnitFlags =
            CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes |
            CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes |
            CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies;
        static void Main(string[] args)
        {
            var lines = new string[0];
            var fileName = "CoreUObject.cpp";
            var content = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp3\Test.cpp");
            var options = new CppParserOptions();
            options.ParseSystemIncludes = false;
            options.AdditionalArguments.AddRange(lines);
            var compilation = CppParser.Parse(content, options, fileName);
            foreach (var message in compilation.Diagnostics.Messages)
            {
                Console.WriteLine($"{message.Type.ToString()}: {message.ToString()}");
            }

            if (!compilation.HasErrors)
            {
                foreach (var cppClass in compilation.Classes)
                {
                    foreach (var cppClassFunction in cppClass.Functions)
                    {
                        Console.WriteLine(cppClassFunction.Name);
                    }
                }
            }
        }
    }
}