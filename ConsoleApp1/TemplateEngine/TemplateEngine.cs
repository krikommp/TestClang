using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;


namespace ConsoleApp1.TemplateEngine
{
    class ErrorWrite : TextWriter
    {
        public override Encoding Encoding { get; }
    }

    public enum TokenType
    {
        SingleCode,
        Code,
        Eval,
        Text,
    }

    public class Chunk
    {
        public TokenType Type { get; set; }
        public string Text { get; set; }

        public Chunk(TokenType type, string text)
        {
            Type = type;
            Text = text;
        }
    }

    class TemplateFormatException : Exception
    {
        public TemplateFormatException(string message)
        {
        }
    }

    public class TemplateEngine
    {
    }

    public class Parser
    {
        public static string RegexString { get; private set; }
        public static string RegexVariable { get; private set; }

        static Parser()
        {
            RegexString = GetRegexString();
            RegexVariable = @"(?s)({{.*?}})";
        }

        static string GetRegexString()
        {
            string regexBadUnopened = @"(?<error>((?!<%).)*%>)";
            string regexText = @"(?<text>((?!<%).)+)";
            string regexNoCode = @"(?<nocode><%=?%>)";
            string regexSingleCode = @"*<%(?<singleCode>[^=]((?!<%|%>).)*)%>\r\n";
            string regexCode = @"<%(?<code>[^=]((?!<%|%>).)*)%>";
            string regexEval = @"<%=(?<eval>((?!<%|%>).)*)%>";
            string regexBadUnclosed = @"(?<error><%.*)";
            string regexBadEmpty = @"(?<error>^$)";

            return '(' + regexBadUnopened
                       + '|' + regexText
                       + '|' + regexNoCode
                       + '|' + regexSingleCode
                       + '|' + regexCode
                       + '|' + regexEval
                       + '|' + regexBadUnclosed
                       + '|' + regexBadEmpty
                       + ")*";
        }

        /// <summary>
        /// Replaces special characters with their literal representation.
        /// </summary>
        /// <returns>Resulting string.</returns>
        /// <param name="input">Input string.</param>
        static string EscapeString(string input)
        {
            var output = input
                .Replace("\\", @"\\")
                .Replace("\'", @"\'")
                .Replace("\"", @"\""")
                .Replace("\n", @"\n")
                .Replace("\t", @"\t")
                .Replace("\r", @"\r")
                .Replace("\b", @"\b")
                .Replace("\f", @"\f")
                .Replace("\a", @"\a")
                .Replace("\v", @"\v")
                .Replace("\0", @"\0");
            /*          var surrogateMin = (char)0xD800;
            var surrogateMax = (char)0xDFFF;
            for (char sur = surrogateMin; sur <= surrogateMax; sur++)
                output.Replace(sur, '\uFFFD');*/
            return output;
        }

        public static List<Chunk> Parse(string snippet)
        {
            Regex templateRegex = new Regex(
                RegexString,
                RegexOptions.ExplicitCapture | RegexOptions.Singleline);
            Match matchs = templateRegex.Match(snippet);
            if (matchs.Groups["error"].Length > 0)
            {
                throw new TemplateFormatException("Messed up brackets");
            }

            List<Chunk> chunks = matchs.Groups["singleCode"].Captures
                .Cast<Capture>()
                .Select(p => new { Type = TokenType.SingleCode, p.Value, p.Index }).Concat(
                    matchs.Groups["code"].Captures
                        .Cast<Capture>()
                        .Select(p => new { Type = TokenType.Code, p.Value, p.Index }))
                .Concat(matchs.Groups["text"].Captures
                    .Cast<Capture>()
                    .Select(p => new { Type = TokenType.Text, Value = EscapeString(p.Value), p.Index }))
                .Concat(matchs.Groups["eval"].Captures
                    .Cast<Capture>()
                    .Select(p => new { Type = TokenType.Eval, p.Value, p.Index }))
                .OrderBy(p => p.Index)
                .Select(m => new Chunk(m.Type, m.Value))
                .ToList();
            if (chunks.Count == 0)
            {
                throw new TemplateFormatException("Empty template");
            }

            return chunks;
        }
    }

    public class CSharpTemplate
    {
        private static string ConvertToStringLiteral(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        public static string ComposeCode(List<Chunk> chunks, Globals global)
        {
            StringBuilder code = new StringBuilder();

            foreach (var context in global.Context)
            {
                code.Append(
                    $"var {context.Key} = ({context.Value.GetType().GetFriendlyName()})Context[{ConvertToStringLiteral(context.Key)}];");
            }

            code.Append("var result = new List<string>();\r\n");
            for (int index = 0; index < chunks.Count; ++index)
            {
                var chunk = chunks[index];
                switch (chunk.Type)
                {
                    case TokenType.Text:
                        code.Append("result.Add(\"" + chunk.Text + "\");\r\n");
                        break;
                    case TokenType.Eval:
                        code.Append("result.Add(Convert.ToString(" + chunk.Text + "));\r\n");
                        break;
                    case TokenType.SingleCode:
                        code.Append(chunk.Text + "\r\n");
                        break;
                    case TokenType.Code:
                        code.Append(chunk.Text + "\r\n");
                        break;
                }
            }

            code.Append("return string.Join(string.Empty, result);\r\n");
            Console.WriteLine(code.ToString());
            return code.ToString();
        }


        public static T Compile<T>(string snippet, Globals global)
        {
            global.Namespaces.Add("System");
            global.Namespaces.Add("System.Collections.Generic");
            foreach (var variable in global.Context.Values)
            {
                Type t = variable.GetType();
                if (!t.Assembly.GetName().Name.Equals("mscorlib"))
                {
                    global.Assemblies.Add(t.Assembly);
                }

                global.Namespaces.Add(t.Namespace);
            }

            var sciptOptions = ScriptOptions.Default.WithImports(global.Namespaces.ToArray())
                .WithReferences(global.Assemblies.ToArray());
            var scipt = CSharpScript.RunAsync(ComposeCode(Parser.Parse(snippet), global), sciptOptions, global);
            return (T)scipt.Result.ReturnValue;
        }
    }
}