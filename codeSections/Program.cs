using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    Console.WriteLine($"Path:  {path}");
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
            var endRegex = new Regex(beginRegexText);
            var beginMatch = beginRegex.Match(content);
            var endMatch = endRegex.Match(content);

            if (beginMatch.Success)
            {
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