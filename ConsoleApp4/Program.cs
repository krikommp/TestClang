using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace RoslyTest
{
    public static class Program
    {
        public static readonly string FilePath = @"D:\SandBox\ConsoleApp1\ConsoleApp3\Program.cs";
        public static readonly string OutputPath = @"C:\Users\chenyifei\Documents\Text\Sample\";
        public static readonly string OutputAssemblyPath = Path.Combine(OutputPath, "ConsoleApp1.dll");
        public static readonly string OutputAssemblyPdbPath = Path.Combine(OutputPath, "ConsoleApp1.pdb");
        
        
        public class RealBar
        {
	        public string Foo = "Default";

	        public int StaffId { get; set; }
	        public int UnitId { get; set; }
	        public int Age { get; set; }
        }

        public static RealBar gf = new RealBar();
        
        private static void LogDiagnostics(IEnumerable<Diagnostic> Diagnostics)
        {
	        foreach (var Diag in Diagnostics)
	        {
		        Console.WriteLine(Diag.ToString());
	        }
        }

        static int Main()
        {
            CSharpParseOptions ParseOptions = new CSharpParseOptions(
                languageVersion: LanguageVersion.Latest,
                kind: SourceCodeKind.Regular,
                preprocessorSymbols: null);
            List<SyntaxTree> SyntaxTrees = new List<SyntaxTree>();

            SourceText Source = SourceText.From(File.ReadAllText(FilePath), System.Text.Encoding.UTF8);
            SyntaxTree Tree = CSharpSyntaxTree.ParseText(Source, ParseOptions, FilePath);
            IEnumerable<Diagnostic> Diagnostics = Tree.GetDiagnostics();
            if (Diagnostics.Any())
            {
                Console.WriteLine($"Errors generated while parsing '{FilePath}'");
                LogDiagnostics(Diagnostics);
                return -1;
            }
            SyntaxTrees.Add(Tree);

            DirectoryInfo DirInfo = new DirectoryInfo(OutputPath);
            if (!DirInfo.Exists)
            {
                try
                {
                    DirInfo.Create();
                }
                catch (Exception Ex)
                {
                }
            }

            List<string> ReferencedAssembies = new List<string>();
            //

            List<MetadataReference> MetadataReferences = new List<MetadataReference>();
            if (ReferencedAssembies != null)
            {
                foreach (string Reference in ReferencedAssembies)
                {
                    MetadataReferences.Add(MetadataReference.CreateFromFile(Reference));
                }
            }
            
            MetadataReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.IO").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.IO.FileSystem").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Private.Xml").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Private.Xml.Linq").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Extensions").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location));
			
			// process start dependencies
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.ComponentModel.Primitives").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Diagnostics.Process").Location));
			
			// registry access
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.Win32.Registry").Location));

			// RNGCryptoServiceProvider, used to generate random hex bytes
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.Algorithms").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Security.Cryptography.Csp").Location));
			
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.CodeAnalysis").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("System.Collections.Immutable").Location));
			
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.CodeAnalysis.CSharp.Scripting").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load("Microsoft.CodeAnalysis.Scripting").Location));
			MetadataReferences.Add(MetadataReference.CreateFromFile(typeof(Program).Assembly.Location));

			CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(
				outputKind: OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				warningLevel: 4,
				assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
				reportSuppressedDiagnostics: true);
			
			CSharpCompilation Compilation = CSharpCompilation.Create(
				assemblyName:"ConsoleApp1",
				syntaxTrees:SyntaxTrees,
				references:MetadataReferences,
				options:CompilationOptions
			);

			using (FileStream AssemblyStream = File.OpenWrite(OutputAssemblyPath))
			{
				using (FileStream PdbStream = File.OpenWrite(OutputAssemblyPdbPath))
				{
					EmitOptions EmitOptions = new EmitOptions(includePrivateMembers: true);
					EmitResult Result = Compilation.Emit(peStream: AssemblyStream,
						pdbStream: PdbStream,
						options: EmitOptions);
					LogDiagnostics(Result.Diagnostics);
					if (!Result.Success)
					{
						return -1;
					}
				}
			}


			gf.Foo = "I Fixed it";
			var aaa = Assembly.LoadFile(OutputAssemblyPath);
			var mainMethod = aaa.GetType("ConsoleApp3.Program").GetMethod("Main");
			mainMethod.Invoke(null, new []{ new string[] { "ddd" } });
            return 0;
        }
    }
}