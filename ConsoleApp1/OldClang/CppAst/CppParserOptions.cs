﻿// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Schema;

namespace CppAst
{
    /// <summary>
    /// Defines the options used by the <see cref="CppParser"/>
    /// </summary>
    public class CppParserOptions
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CppParserOptions()
        {
            ParseAsCpp = true;
            SystemIncludeFolders = new List<string>();
            IncludeFolders = new List<string>();
            Defines = new List<string>();
            AdditionalArguments = new List<string>()
            {
                "-Wno-pragma-once-outside-header"
            };
            AutoSquashTypedef = true;
            ParseMacros = false;
            ParseComments = true;
            ParseSystemIncludes = true;
            ParseAttributes = false;

            // Default triple targets
            TargetCpu = IntPtr.Size == 8 ? CppTargetCpu.X86_64 : CppTargetCpu.X86;
            TargetCpuSub = string.Empty;
            TargetVendor = "pc";
            TargetSystem = "windows";
            TargetAbi = "";
        }

        /// <summary>
        /// List of the include folders.
        /// </summary>
        public List<string> IncludeFolders { get; private set; }

        /// <summary>
        /// List of the system include folders.
        /// </summary>
        public List<string> SystemIncludeFolders { get; private set; }

        /// <summary>
        /// List of the defines.
        /// </summary>
        public List<string> Defines { get; private set; }

        /// <summary>
        /// List of the additional arguments passed directly to the C++ Clang compiler.
        /// </summary>
        public List<string> AdditionalArguments { get; private set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether the files will be parser as C++. Default is <c>true</c>. Otherwise parse as C.
        /// </summary>
        public bool ParseAsCpp { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether to parser non-Doxygen comments in addition to Doxygen comments. Default is <c>true</c>
        /// </summary>
        public bool ParseComments { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether to parse macros. Default is <c>false</c>.
        /// </summary>
        public bool ParseMacros { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether un-named enum/struct referenced by a typedef will be renamed directly to the typedef name. Default is <c>true</c>
        /// </summary>
        public bool AutoSquashTypedef { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether to parse System Include headers. Default is <c>true</c>
        /// </summary>
        public bool ParseSystemIncludes { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether to parse Attributes. Default is <c>false</c>
        /// </summary>
        public bool ParseAttributes { get; set; }

        /// <summary>
        /// Sets <see cref="ParseMacros"/> to <c>true</c> and return this instance.
        /// </summary>
        /// <returns>This instance</returns>
        public CppParserOptions EnableMacros()
        {
            ParseMacros = true;
            return this;
        }

        /// <summary>
        /// Cpu Clang target. Default is <see cref="CppTargetCpu.X86"/>
        /// </summary>
        public CppTargetCpu TargetCpu { get; set; }

        /// <summary>
        /// Cpu sub Clang target. Default is ""
        /// </summary>
        public string TargetCpuSub { get; set; }

        /// <summary>
        /// Vendor Clang target. Default is "pc"
        /// </summary>
        public string TargetVendor { get; set; }

        /// <summary>
        /// System Clang target. Default is "windows"
        /// </summary>
        public string TargetSystem { get; set; }

        /// <summary>
        /// Abi Clang target. Default is ""
        /// </summary>
        public string TargetAbi { get; set; }

        /// <summary>
        /// Gets or sets a C/C++ pre-header included before the files/text to parse
        /// </summary>
        public string PreHeaderText { get; set; }

        /// <summary>
        /// Gets or sets a C/C++ post-header included after the files/text to parse
        /// </summary>
        public string PostHeaderText { get; set; }

        /// <summary>
        /// Clone this instance.
        /// </summary>
        /// <returns>Return a copy of this options.</returns>
        public virtual CppParserOptions Clone()
        {
            var newOptions = (CppParserOptions)MemberwiseClone();

            // Copy lists
            newOptions.IncludeFolders = new List<string>(IncludeFolders);
            newOptions.SystemIncludeFolders = new List<string>(SystemIncludeFolders);
            newOptions.Defines = new List<string>(Defines);
            newOptions.AdditionalArguments = new List<string>(AdditionalArguments);

            return newOptions;
        }

        /// <summary>
        /// Configure this instance with Windows and MSVC.
        /// </summary>
        /// <returns>This instance</returns>
        public CppParserOptions ConfigureForWindowsMsvc(CppTargetCpu targetCpu = CppTargetCpu.X86, CppVisualStudioVersion vsVersion = CppVisualStudioVersion.VS2019)
        {
            // 1920
            var highVersion = ((int)vsVersion) / 100;  // => 19
            var lowVersion = ((int)vsVersion) % 100;   // => 20

            var versionAsString = $"{highVersion}.{lowVersion}";

            TargetCpu = targetCpu;
            TargetCpuSub = string.Empty;
            TargetVendor = "pc";
            TargetSystem = "windows";
            TargetAbi = $"msvc{versionAsString}";

            // See https://docs.microsoft.com/en-us/cpp/preprocessor/predefined-macros?view=vs-2019

            Defines.Add($"_MSC_VER={(int)vsVersion}");
            Defines.Add("_WIN32=1");

            switch (targetCpu)
            {
                case CppTargetCpu.X86:
                    Defines.Add("_M_IX86=600");
                    break;
                case CppTargetCpu.X86_64:
                    Defines.Add("_M_AMD64=100");
                    Defines.Add("_M_X64=100");
                    Defines.Add("_WIN64=1");
                    break;
                case CppTargetCpu.ARM:
                    Defines.Add("_M_ARM=7");
                    break;
                case CppTargetCpu.ARM64:
                    Defines.Add("_M_ARM64=1");
                    Defines.Add("_WIN64=1");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetCpu), targetCpu, null);
            }

            AdditionalArguments.Add("-fms-extensions");
            AdditionalArguments.Add("-fms-compatibility");
            AdditionalArguments.Add($"-fms-compatibility-version={versionAsString}");
            return this;
        }
        
        public CppParserOptions ConfigureForWithClangSystemInclude()
        {
            Func<string, bool, string[]> parseClangIncludePath = (context, bExcludeClang) =>
            {
                string[] lines = context.Split(Environment.NewLine.ToCharArray());
                bool beginParse = false;
                List<string> systemIncludePath = new List<string>();
                foreach (var line in lines)
                {
                    if (Regex.IsMatch(line, @"#include\s<...>\ssearch\sstarts\shere:"))
                    {
                        beginParse = true;
                        continue;
                    }

                    if (Regex.IsMatch(line, @"End of search list."))
                    {
                        beginParse = false;
                        continue;
                    }

                    if (beginParse && bExcludeClang && Regex.IsMatch(line, @".*\\clang\\.*"))
                    {
                        continue;
                    }

                    if (beginParse && !String.IsNullOrWhiteSpace(line))
                    {
                        var path = line.Trim();
                        if (path.EndsWith(" (framework directory)"))
                        {
                            path = $"-F{path.Replace(" (framework directory)", "")}";
                        }
                        else
                        {
                            path = $"-isystem{path}";
                        }
                        systemIncludePath.Add(path);
                    }
                }
                return systemIncludePath.ToArray();
            };
            string toolChain = CppUtils.FindClangCompiler();
            if (!string.IsNullOrEmpty(toolChain))
            {
                string tmpFileName = CppUtils.GetTempPathFileName($"{Path.GetRandomFileName()}.cpp");
                Console.WriteLine($"Using {tmpFileName}");
                using (var fs = File.Create(tmpFileName))
                {
                    fs.Flush();
                    fs.Close();
                }
                string res = CppUtils.RunToolAndCaptureOutput($"{toolChain}", $" -c {tmpFileName} -v");
                this.AdditionalArguments.AddRange(parseClangIncludePath(res, RuntimeInformation.IsOSPlatform(OSPlatform.Windows)));
                File.Delete(tmpFileName);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                this.AdditionalArguments.AddRange(new string[]
                {
                    "-x",
                    "objective-c++",
                });
                ParseAsCpp = false;
            }

            return this;
        }
    }
}
