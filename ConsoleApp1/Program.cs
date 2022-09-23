using System;
using System.IO;
using System.Linq;
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
            if (cursor.Location.IsInSystemHeader)
            {
                return CXChildVisitResult.CXChildVisit_Continue;
            }
            Console.WriteLine($"Cursor: {cursor} of kind {cursor.kind}");
            return CXChildVisitResult.CXChildVisit_Recurse;
        }

        static void Main(string[] args)
        {
            var lines = new string[]
            {
                "-isystem/usr/local/opt/llvm/bin/../include/c++/v1",
                "-isystem/usr/local/Cellar/llvm/14.0.6_1/lib/clang/14.0.6/include",
                "-isystem/Library/Developer/CommandLineTools/SDKs/MacOSX12.sdk/usr/include",
                "-F/Library/Developer/CommandLineTools/SDKs/MacOSX12.sdk/System/Library/Frameworks",
                "-x",
                "objective-c++",
            }.Union(File.ReadAllLines(@"/Users/admin/Documents/HookTest/TestClang/ConsoleApp1/3.txt"));
            
            var fileName = "CoreUObject.h";
            var fileContent = "#include \"Public/CoreUObject.h\"";
            // var fileContent = File.ReadAllText(@"/Users/admin/Documents/HookTest/TestClang/ConsoleApp3/header.hpp");
            // var fileContent = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp3\header.hpp");
            // var fileContent = File.ReadAllText(@"/home/kriko/TestClang/ConsoleApp3/header.hpp");

            using var unsavedFile = CXUnsavedFile.Create(fileName, fileContent);
            var unsavedFiles = new[] { unsavedFile };
            var index = CXIndex.Create();
            var translationUnit = CXTranslationUnit.Parse(index, fileName, lines.ToArray(), unsavedFiles,
                defaultTranslationUnitFlags);
            bool hasError = false;
            if (translationUnit.NumDiagnostics != 0)
            {
                for (uint i = 0; i < translationUnit.NumDiagnostics; ++i)
                {
                    var diagnostic = translationUnit.GetDiagnostic(i);
                    Console.WriteLine($"{diagnostic.Severity}: {diagnostic.Location} {diagnostic}");
                    if (diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Error ||
                        diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Fatal)
                    {
                        hasError = true;
                    }
                }
            }
            if (!hasError)
            {
                translationUnit.Cursor.VisitChildren(VisitTranslationUnit, clientData: default);
            }
        }
    }
}