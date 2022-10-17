﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClangSharp.Interop;
using ConsoleApp1.TemplateEngine;
using CppAst;
using Microsoft.CodeAnalysis;

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

        static public readonly Dictionary<string, string> cxxRecordNameToSharpTypeName =
            new Dictionary<string, string>()
            {
                { "FString", "string" },
                { "FName", "FName" },
                { "FText", "FText" }
            };
        static public readonly HashSet<string> exportedEnumTypes = new HashSet<string>();
        static public readonly Dictionary<string, CppType> exportedEnumTypesMap = new Dictionary<string, CppType>();

        public delegate IEnumerable<CppBaseType> BasesGetter(CppType cppType);

        static public BasesGetter basesGetter;

        public static IEnumerable<CppBaseType> GetBases(CppType cppType)
        {
            var outClass = new List<CppBaseType>();
            if (cppType.TypeKind == CppTypeKind.StructOrClass && cppType is CppClass cppClass)
            {
                foreach (var baseType in cppClass.BaseTypes)
                {
                    outClass.Add(baseType);
                }
            }

            return outClass;
        }

        public static IEnumerable<CppBaseType> GetBasesRecursive(CppClass type)
        {
            var outClass = new List<CppBaseType>();
            var bases = basesGetter?.Invoke(type);
            if (bases != null)
            {
                foreach (var baseType in bases)
                {
                    if (baseType.Type is CppClass baseClass)
                    {
                        if (baseClass.TemplateParameters.Count == 0)
                        {
                            outClass.Add(baseType);
                        }
                    }
                }
            }
            if (outClass.Count != 0)
            {
                foreach (var baseType in bases) {
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

        public static bool IsUClass(string cxxTypeName)
        {
            return Regex.IsMatch(cxxTypeName, "^U[A-Z]");
        }

        public static bool IsUFunction(CppFunction cxxFunction)
        {
            return false;
        }

        public static string ToSharpType(CppType type, bool bUnsafe = true, bool bFullname = false)
        {
            var space = bFullname ? "UnrealEngine." : "";
            if (type.TypeKind == CppTypeKind.Pointer && type is CppPointerType pointerType)
            {
                var realType = pointerType.ElementType;
                if (realType.TypeKind == CppTypeKind.Qualified && realType is CppQualifiedType cppQualifiedType)
                {
                    realType = cppQualifiedType.ElementType;
                }
                if (IsUClass(realType))
                {
                    return $"{space}{realType.GetDisplayName()}{(bUnsafe ? "Unsafe*" : "")}";
                }
            }else if (type.TypeKind == CppTypeKind.Reference && type is CppReferenceType cppReference)
            {
                var elementType = cppReference.ElementType;
                var pointeeType = ToSharpType(elementType);
                if (!string.IsNullOrEmpty(pointeeType))
                {
                    return $"ref {pointeeType}";
                }
            }else if (type.TypeKind == CppTypeKind.StructOrClass && type is CppClass cppClass)
            {
                if (cppClass.TemplateParameters.Count > 0)
                {
                    return null;
                }
                if (cxxRecordNameToSharpTypeName.TryGetValue(type.GetDisplayName(), out var sharpType))
                {
                    return $"{space}{sharpType}";
                }
            }else if (type.TypeKind == CppTypeKind.Enum)
            {
                var typeName = type.GetDisplayName();
                if (!exportedEnumTypes.Contains(typeName) && !exportedEnumTypesMap.ContainsKey(typeName))
                {
                    exportedEnumTypesMap.Add(typeName, type);
                }
                return $"{space}{typeName}";
            }
            else if (type.TypeKind == CppTypeKind.Typedef && type is CppTypedef typedef)
            {
                return ToSharpType(typedef.ElementType);
            }else if (type.TypeKind == CppTypeKind.Qualified && type is CppQualifiedType qualifiedType)
            {
                return ToSharpType(qualifiedType.ElementType);
            }
            else
            {
                return type.ToString();
            }

            return null;
        }

        public static string ToSharpParamName(CppParameter param, bool covertUnsafe = false)
        {
            var paramName = param.Name[..1].ToLower() + param.Name[1..];
            if (covertUnsafe && param.Type.TypeKind == CppTypeKind.Pointer && param.Type is CppPointerType pointerTypes)
            {
                var sharpType = ToSharpType(pointerTypes, false);
                paramName = $"{paramName} == null ? null : {paramName}.Get{sharpType}Ptr()";
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
            if (param.InitValue != null && param.InitValue.Value != null)
            {
                if (param.Type.TypeKind == CppTypeKind.Enum && param.Type is CppEnum enumType)
                {
                    return $" = {enumType.Name}.{enumType.Items[ Convert.ToInt32((long)param.InitValue.Value)].Name}";
                }
                else
                {
                    return $" = {param.InitExpression}";
                }
            }else if (param.Type.TypeKind == CppTypeKind.Pointer)
            {
                return $" = null";
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
                if (parameter.InitExpression != null)
                {
                    ret += ToSharpDefault(parameter);
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

        public static void Generate(CppType type)
        {
            var templateFileContent = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp1\TestTemplate.txt");
            var global = new Globals(); 
            global.Context.Add("type", type);
            global.Assemblies.Add(typeof(Program).Assembly);
            global.Assemblies.Add(typeof(Regex).Assembly);
            global.Namespaces.Add("System.Linq");
            global.Namespaces.Add("System.Text.RegularExpressions");
            var o = CSharpTemplate.Compile<string>(templateFileContent, global);
            string filename = @"C:\Users\chenyifei\Documents\Test\" + type.GetDisplayName() + ".cs";
            FileStream fs = File.Create(filename);
            fs.Close();
            StreamWriter sw = new StreamWriter(filename);
            sw.WriteLine(o);
            sw.Flush();
            sw.Close();
        }

        public static void GenerateCpp(string moduleName ,List<CppType> types)
        {
            var templateFileContent = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp1\ModuleTemplate.txt");
            var global = new Globals(); 
            global.Context.Add("types", types);
            global.Assemblies.Add(typeof(Program).Assembly);
            global.Assemblies.Add(typeof(Regex).Assembly);
            global.Namespaces.Add("System.Linq");
            global.Namespaces.Add("System.Text.RegularExpressions");
            global.Namespaces.Add("CppAst");
            var o = CSharpTemplate.Compile<string>(templateFileContent, global);
            string filename = @"C:\Users\chenyifei\Documents\Test\" + moduleName + ".sharp.h";
            FileStream fs = File.Create(filename);
            fs.Close();
            StreamWriter sw = new StreamWriter(filename);
            sw.WriteLine(o);
            sw.Flush();
            sw.Close();
        }

        public static void FinishGenerate(string moduleName)
        {
            var templateFileContent = File.ReadAllText(@"D:\SandBox\ConsoleApp1\ConsoleApp1\ModuleTemplate.txt");
            var global = new Globals(); 
            global.Context.Add("moduleName", moduleName);
            global.Context.Add("exportedEnumTypesMap", exportedEnumTypesMap);
            global.Assemblies.Add(typeof(Program).Assembly);
            global.Assemblies.Add(typeof(Regex).Assembly);
            global.Namespaces.Add("System.Linq");
            global.Namespaces.Add("System.Text.RegularExpressions");
            global.Namespaces.Add("CppAst");
            var o = CSharpTemplate.Compile<string>(templateFileContent, global);
            string filename = @"C:\Users\chenyifei\Documents\Test\" + moduleName + ".cs";
            FileStream fs = File.Create(filename);
            fs.Close();
            StreamWriter sw = new StreamWriter(filename);
            sw.WriteLine(o);
            sw.Flush();
            sw.Close();
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
            functionWhiteList.Add("GetName");
            functionWhiteList.Add("GetPathName");
            functionWhiteList.Add("GetFullName");
            functionWhiteList.Add("StaticClass");
            functionWhiteList.Add("GetDefaultObject");
            functionWhiteList.Add("GetClass");
            if (!compilation.HasErrors)
            {
                basesGetter = GetBases;
                CppType typeField = null, typeProperty = null;
                List<CppType> declProps = new List<CppType>();
                foreach (var  parseClass in compilation.Classes)
                {
                    if (parseClass.Name.Equals("FField"))
                    {
                        typeField = parseClass;
                    }else if (parseClass.Name.Equals("FProperty"))
                    {
                        typeProperty = parseClass;
                    }else if (Regex.IsMatch(parseClass.Name, "^F.*Property$"))
                    {
                        declProps.Add(parseClass);
                    }else if (Regex.IsMatch(parseClass.Name, "^U[A-Z]"))
                    {
                        Generate(parseClass);
                    }
                }
                
                Generate(typeField);
                basesGetter = type => type.GetDisplayName() switch
                {
                    "FField" => null,
                    "FProperty" => new []{ new CppBaseType(typeField) },
                    _ => new []{ new CppBaseType(typeProperty) }
                };
                Generate(typeProperty);

                foreach (var declProp in declProps)
                {
                    Generate(declProp);
                }
                
                // GenerateCpp("CoreUObject", uClassTypes);

                FinishGenerate("CoreUObject");
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