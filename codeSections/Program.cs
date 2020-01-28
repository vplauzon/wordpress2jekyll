using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace codeSections
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("The path to the _post folder should be passed as a parameter");
            }
            else
            {
                var postsPath = args[0];
                var filePaths = GetFilePaths(postsPath);

                Console.WriteLine($"Path to _posts:  {postsPath}");

                foreach (var path in filePaths)
                {
                    await ProcessPostAsync(path);
                }
            }
        }

        private static async Task ProcessPostAsync(string path)
        {
            var content = await File.ReadAllTextAsync(path);
            var beginRegexText = GetResource("begin-code-regex.txt");
            var beginRegex = new Regex(beginRegexText);
            var endRegexText = GetResource("end-code-regex.txt");
            var endRegex = new Regex(endRegexText);
            var beginMatch = beginRegex.Match(content);
            var endMatch = endRegex.Match(content);

            if (beginMatch.Success)
            {
                Console.WriteLine($"Code found in path '{path}'");

                ValidateMatches(beginMatch, endMatch);

                var newContentParts = ReplaceCode(content, 0, beginMatch, endMatch);
                var newContent = string.Concat(newContentParts);

                if (newContent != content)
                {
                    await File.WriteAllTextAsync(path, newContent);
                }
            }
        }

        private static IImmutableList<string> ReplaceCode(
            string content,
            int indexStart,
            Match beginMatch,
            Match endMatch)
        {
            var top = content.Substring(indexStart, beginMatch.Index - indexStart);
            var beginTag = "```" + beginMatch.Groups["lang"].Value + Environment.NewLine;
            var blockEnd = endMatch.Index + endMatch.Value.Length;
            var code = content.Substring(
                beginMatch.Index + beginMatch.Value.Length,
                endMatch.Index - beginMatch.Index - beginMatch.Value.Length);
            var unencodedCode = WebUtility.HtmlDecode(code.Trim('\r', '\n'))
                + Environment.NewLine;
            var endTag = "```";
            var parts = ImmutableList<string>
                .Empty
                .Add(top)
                .Add(beginTag)
                .Add(unencodedCode)
                .Add(endTag);

            if (beginMatch.NextMatch().Success)
            {
                var nextParts = ReplaceCode(
                    content,
                    blockEnd,
                    beginMatch.NextMatch(),
                    endMatch.NextMatch());

                return parts.AddRange(nextParts);
            }
            else
            {
                var remainer = content.Substring(blockEnd);

                return parts.Add(remainer);
            }
        }

        private static void ValidateMatches(Match beginMatch, Match endMatch)
        {
            if (beginMatch.Success != endMatch.Success)
            {
                throw new InvalidDataException("Different cardinality of begin & end of code");
            }
            if (beginMatch.Success)
            {
                if (beginMatch.Index >= endMatch.Index)
                {
                    throw new InvalidDataException("Begin code happens after the end");
                }
                ValidateMatches(beginMatch.NextMatch(), endMatch.NextMatch());
            }
        }

        private static string GetResource(string resourceName)
        {
            var assembly = typeof(Program).Assembly;
            var fullResourceName = "codeSections." + resourceName;

            using (var stream = assembly.GetManifestResourceStream(fullResourceName))
            using (var reader = new StreamReader(stream))
            {
                var text = reader.ReadToEnd();

                return text;
            }
        }

        private static IEnumerable<string> GetFilePaths(string path)
        {
            var pathsFromFolders = from folder in Directory.EnumerateDirectories(path)
                                   select GetFilePaths(folder);
            var pathsFromCurrent = Directory.EnumerateFiles(path);

            return MergeLists(pathsFromFolders.Prepend(pathsFromCurrent));
        }

        private static IEnumerable<T> MergeLists<T>(IEnumerable<IEnumerable<T>> list)
        {
            var head = list.Take(1).FirstOrDefault();
            var tail = list.Skip(1);

            if (head == null)
            {
                return new T[0];
            }
            else
            {
                return head.Concat(MergeLists(tail));
            }
        }
    }
}