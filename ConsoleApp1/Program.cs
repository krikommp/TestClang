using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClangSharp.Interop;
using ConsoleApp1.TemplateEngine;
using CppAst;

namespace ConsoleApp1
{
    public unsafe class Program
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

        public static void TestAST()
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

            var options = new CppParserOptions();
            options.ParseSystemIncludes = false;
            options.ParseAsCpp = false;
            options.AdditionalArguments.AddRange(lines.ToArray());
            var compilation = CppParser.Parse(fileContent, options, fileName);
            if (compilation.HasErrors)
            {
                foreach (var message in compilation.Diagnostics.Messages) {
                    Console.WriteLine(message);
                } 
            }
            else
            {
                Console.WriteLine("Success");
                foreach (var cppClass in compilation.Classes)
                {
                    Console.WriteLine($"{cppClass.Name} with Size = {cppClass.SizeOf}");
                }
            }
        }

        public static void TestTranslation()
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
        
        static public readonly HashSet<string> functionWhiteList = new HashSet<string>();
        
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

        public static bool IsUClass(CppType cxxType)
        {
            return Regex.IsMatch(cxxType.GetDisplayName(), "^U[A-Z]");
        }

        public static bool IsUFunction(CppFunction cxxFunction)
        {
            return false;
        }

        public static string ToSharpType(CppType type, bool bUnsafe = true, bool bFullname = false)
        {
            var space = bFullname ? "UnrealEngine." : "";
            var returnType = type.ToString();
            if (String.IsNullOrEmpty(returnType))
            {
                return null;
            }
            if (type.TypeKind == CppTypeKind.Pointer)
            {
                if (IsUClass(type))
                {
                    return $"{space}{(type as CppPointerType).ElementType.GetDisplayName()}{(bUnsafe ? "Unsafe*" : "")}";
                }
            }else if (type.TypeKind == CppTypeKind.Reference)
            {
                if (IsUClass(type))
                {
                    var elementType = (type as CppReferenceType)?.ElementType;
                    var pointeeType = ToSharpType(elementType);
                    if (!string.IsNullOrEmpty(pointeeType))
                    {
                        return $"ret {pointeeType}";
                    }
                }
            }
            return returnType;
        }

        public static string ToSharpParamName(CppParameter param, bool covertUnsafe = false)
        {
            var paramName = param.Name[..1].ToLower() + param.Name[1..];
            if (covertUnsafe && param.Type.TypeKind == CppTypeKind.Pointer)
            {
                var pointee = (param.Type as CppPointerType)?.ElementType.GetDisplayName();
                paramName = $"{paramName} == null ? null : {paramName}.Get{pointee}Ptr()";
            }

            return paramName;
        }

        public static bool CanExport(CppClass cxxType, CppFunction cxxFunction)
        {
            if (cxxFunction.Visibility != CppVisibility.Public) return false;
            if (!IsUClass(cxxType)) return false;
            if (IsUClass(cxxType) && !IsUFunction(cxxFunction) && !functionWhiteList.Contains(cxxFunction.Name))
                return false;
            var retSharpType = ToSharpType(cxxFunction.ReturnType);
            if (string.IsNullOrEmpty(retSharpType)) return false;
            foreach (var functionParameter in cxxFunction.Parameters)
            {
                if (string.IsNullOrEmpty(ToSharpType(functionParameter.Type))) return false;
            }
            return true;
        }

        public static string ToSharpDefault(CppParameter param)
        {
            if (param.InitValue.Value != null)
            {
                
            }

            return null;
        }

        public static string ToParams(CppFunction method, bool first, bool bUnsafe, bool bFullname, bool paramName, bool defaultValue = false)
        {
            var ret = "";
            foreach (var parameter in method.Parameters)
            {
                if (first) first = false;
                else ret += $",{(paramName || !bFullname ? " " : "")}";
                var paramSharpType = ToSharpType(parameter.Type, bUnsafe, bFullname);
                ret += paramSharpType;
                if (paramName) ret += $" {ToSharpParamName(parameter)}";
                if (!defaultValue) continue;
                // todo...
                if (parameter.InitValue.Value != null)
                {
                    ret += $" = {parameter.InitExpression}";
                }
            }

            return ret;
        }

        public static string ToInvokeParams(CppFunction method, bool first, bool bUnsafe)
        {
            var ret = "";
            foreach (var param in method.Parameters)
            {
                if (first) first = false;
                else ret += ", ";
                var paramSharpType = ToSharpType(param.Type, bUnsafe);
                if (paramSharpType.StartsWith("ref ")) ret += "ref ";
                ret += ToSharpParamName(param, !bUnsafe);
            }

            return ret;
        }

        public static void TestTemplate()
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
                foreach (var parseClass in compilation.Classes)
                {
                    if (parseClass.Name.Equals("UClass"))
                    {
                        functionWhiteList.Add("GetName");
                        functionWhiteList.Add("GetPathName");
                        functionWhiteList.Add("GetFullName");
                        functionWhiteList.Add("StaticClass");
                        functionWhiteList.Add("GetDefaultObject");
                        functionWhiteList.Add("GetClass");
                        var templateFileContent = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp1\TestTemplate.txt");
                        var global = new Globals(); 
                        global.Context.Add("type", parseClass);
                        global.Assemblies.Add(typeof(Program).Assembly);
                        global.Assemblies.Add(typeof(Regex).Assembly);
                        global.Namespaces.Add("System.Linq");
                        global.Namespaces.Add("System.Text.RegularExpressions");
                        var o = CSharpTemplate.Compile<string>(templateFileContent, global);
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

        static void Main(string[] args)
        {
            TestTemplate();
        }
    }
}