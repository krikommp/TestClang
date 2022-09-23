using System;
using System.IO;
using ClangSharp.Interop;

namespace ConsoleApp3
{
    unsafe class Program
    {
        private const CXTranslationUnit_Flags defaultTranslationUnitFlags =
            CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes |
            CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes |
            CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies;

        public static CXChildVisitResult VisitTranslationUnit(CXCursor cursor, CXCursor parent, void* data)
        {
            Console.WriteLine($"Cursor: {cursor} of kind {cursor.kind}");
            return CXChildVisitResult.CXChildVisit_Recurse;
        }

        static void Main(string[] args)
        {
            var lines = new string[] { };
            var fileName = "CoreUObject.cpp";
            var fileContent = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp3\header.hpp");
            // var fileContent = File.ReadAllText(@"/home/kriko/TestClang/ConsoleApp3/header.hpp");

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
            Console.WriteLine($"has error {translationUnit.NumDiagnostics}");
            translationUnit.Cursor.VisitChildren(VisitTranslationUnit, clientData: default);
        }
    }
}