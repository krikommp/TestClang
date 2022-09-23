using System;
using System.IO;
using ClangSharp.Interop;

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
            var lines = File.ReadAllLines(@"D:\SandBox\ConsoleApp1\ConsoleApp1\2.txt");
            var fileName = "CoreUObject.cpp";
            var fileContent = "#include \"Public/CoreUObject.h\"";

            using var unsavedFile = CXUnsavedFile.Create(fileName, fileContent);
            var unsavedFiles = new[] { unsavedFile };
            var index = CXIndex.Create();
            var translationUnit = CXTranslationUnit.Parse(index, fileName, lines, unsavedFiles,
                defaultTranslationUnitFlags);
            if (translationUnit.NumDiagnostics != 0)
            {
                for (uint i = 0; i < translationUnit.NumDiagnostics; ++i)
                {
                    var diagnostic = translationUnit.GetDiagnostic(i);
                    if (diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Error)
                    {
                        Console.WriteLine( diagnostic.ToString());
                    }
                    else
                    {
                        Console.WriteLine( diagnostic.ToString());
                    }
                }
            }
        }
    }
}