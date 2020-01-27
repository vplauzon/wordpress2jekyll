using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var postPath = args[0];
                var filePaths = GetFilePaths(postPath);

                Console.WriteLine($"Path to _posts:  {postPath}");

                foreach (var path in filePaths)
                {
                    Console.WriteLine($"Path:  {path}");
                }
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