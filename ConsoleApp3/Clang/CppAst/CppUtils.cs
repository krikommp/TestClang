using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CppAst
{
    internal interface IToolChain
    {
        public abstract string GetClangToolChainDir();
    }

    internal class VcToolChain : IToolChain
    {
        private bool FindClangToolChain(string toolChainDir, out string toolChain)
        {
            string compilerFile = Path.Combine(toolChainDir, "bin", "clang-cl.exe");
            if (File.Exists(compilerFile))
            {
                toolChain = compilerFile;
                return true;
            }
            toolChain = String.Empty;
            return false;
        }

        public string GetClangToolChainDir()
        {
            string toolChain = String.Empty;
            string manualInstallDir =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LLVM");
            if (FindClangToolChain(manualInstallDir, out toolChain))
            {
                return toolChain;
            }

            string llvmPath = Environment.GetEnvironmentVariable("LLVM_PATH");
            if (!String.IsNullOrEmpty(llvmPath))
            {
                if (FindClangToolChain(llvmPath, out toolChain))
                {
                    return toolChain;
                }
            }

            return toolChain;
        }
    }

    internal class MacToolChain : IToolChain
    {
        private readonly string XCodeDeveloperDir = "xcode-select";
        
        public string GetClangToolChainDir()
        {
            string toolChainDir = Path.Combine(XCodeDeveloperDir, "Toolchains/XcodeDefault.xctoolchain/usr/bin/");
            if (File.Exists(toolChainDir))
            {
                return Path.Combine(toolChainDir, "clang++");
            }

            return "clang++";
        }
    }

    internal class LinuxToolChain : IToolChain
    {
        public string GetClangToolChainDir()
        {
            throw new NotImplementedException();
        }
    }

    public static class CppUtils
    {
        public static string RunLocalProcessAndReturnStdOut(string Command, string Args, out int ExitCode,
            bool LogOutput = false)
        {
            // Process Arguments follow windows conventions in .NET Core
            // Which means single quotes ' are not considered quotes.
            // see https://github.com/dotnet/runtime/issues/29857
            // also see UE-102580
            // for rules see https://docs.microsoft.com/en-us/cpp/cpp/main-function-command-line-args
            Args = Args?.Replace('\'', '\"');

            ProcessStartInfo StartInfo = new ProcessStartInfo(Command, Args);
            StartInfo.UseShellExecute = false;
            StartInfo.RedirectStandardInput = true;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;
            StartInfo.CreateNoWindow = true;
            StartInfo.StandardOutputEncoding = Encoding.UTF8;

            string FullOutput = "";
            string ErrorOutput = "";
            using (Process LocalProcess = Process.Start(StartInfo))
            {
                StreamReader OutputReader = LocalProcess.StandardOutput;
                // trim off any extraneous new lines, helpful for those one-line outputs
                FullOutput = OutputReader.ReadToEnd().Trim();

                StreamReader ErrorReader = LocalProcess.StandardError;
                // trim off any extraneous new lines, helpful for those one-line outputs
                ErrorOutput = ErrorReader.ReadToEnd().Trim();
                if (LogOutput)
                {
                    if (FullOutput.Length > 0)
                    {
                        Console.WriteLine(FullOutput);
                    }

                    if (ErrorOutput.Length > 0)
                    {
                        Console.WriteLine(ErrorOutput);
                    }
                }

                LocalProcess.WaitForExit();
                ExitCode = LocalProcess.ExitCode;
            }

            // trim off any extraneous new lines, helpful for those one-line outputs
            if (ErrorOutput.Length > 0)
            {
                if (FullOutput.Length > 0)
                {
                    FullOutput += Environment.NewLine;
                }

                FullOutput += ErrorOutput;
            }

            return FullOutput;
        }
        
        public static string RunLocalProcessAndReturnStdOut(string Command, string Args)
        {
            int ExitCode;
            return RunLocalProcessAndReturnStdOut(Command, Args, out ExitCode);	
        }
        
        public static string RunToolAndCaptureOutput(string command, string toolArg, string expression = null)
        {
            string processOutput = RunLocalProcessAndReturnStdOut(command, toolArg);
            if (string.IsNullOrEmpty(expression))
            {
                return processOutput;
            }
			
            Match M = Regex.Match(processOutput, expression);
            return M.Success ? M.Groups[1].ToString() : null;	
        }

        public static string FindClangCompiler()
        {
            return null;
        }
    }
}