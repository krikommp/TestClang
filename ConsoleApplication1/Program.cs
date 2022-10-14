using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleApplication1
{
    internal class Program
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
                    if(FullOutput.Length > 0)
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

        public static string[] ParseClangIncludePath(string output)
        {
            string[] lines = output.Split(Environment.NewLine.ToCharArray());
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

                if (beginParse && !String.IsNullOrWhiteSpace(line))
                {
                    var path = line.Trim();
                    if (path.EndsWith("Frameworks"))
                    {
                        path = $"-F{path}";
                    }
                    else
                    {
                        path = $"-isystem{path}";
                    }
                    systemIncludePath.Add(path);
                }
            }
            return systemIncludePath.ToArray();
        }

        public static void Main(string[] args)
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            Console.WriteLine(folderPath);
            string clangPath = "C:\\Program Files\\LLVM\\bin\\clang.exe";
            string result = RunToolAndCaptureOutput(clangPath, "-c C:\\Users\\chenyifei\\Desktop\\Clang\\main.cpp -v");
            var systemIncludePath = ParseClangIncludePath(result);
            foreach (var s in systemIncludePath)
            {
                Console.WriteLine(s);
            }
        }
    }
}