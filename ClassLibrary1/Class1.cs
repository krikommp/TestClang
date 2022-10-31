using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace ClassLibrary1
{
    public class Bar
    {
        public string Foo = "Default";

        public int StaffId { get; set; }
        public int UnitId { get; set; }
        public int Age { get; set; }
    }

    public static class Class1
    {
        public static void Test()
        {
            var scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.AddReferences(typeof(Bar).Assembly);
            Bar dd = new Bar() { Foo = "Hello" };
            var s0 = CSharpScript.Create<Func<Bar, string>>(
                "(dd) => { return $\"Bar.Foo = {dd.Foo}\"; }", scriptOptions);
            s0.Compile();
            var res = s0.RunAsync(null).Result.ReturnValue.Invoke(dd);
            Console.WriteLine(res);
        }
    }
}

