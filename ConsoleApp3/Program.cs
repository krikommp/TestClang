using System;
using System.Collections.Generic;
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
            var lines = new List<string>();
            var fileName = "main.cpp";
            var fileContent = "#include <iostream> int main() { std::cout << dd << std::endl; return 0; }";

            using var unsavedFile = CXUnsavedFile.Create(fileName, fileContent);
            var unsavedFiles = new[] { unsavedFile };
            var index = CXIndex.Create();
            var translationUnit = CXTranslationUnit.Parse(index, fileName, lines.ToArray(), unsavedFiles,
                defaultTranslationUnitFlags);
            if (translationUnit.NumDiagnostics != 0)
            {
                for (uint i = 0; i < translationUnit.NumDiagnostics; ++i)
                {
                    var diagnostic = translationUnit.GetDiagnostic(i);
                    if (diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Error || diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Fatal)
                    {
                        Console.WriteLine($"Error {diagnostic.ToString()}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning {diagnostic.ToString()}");
                    }
                }
            }
        }
    }
}