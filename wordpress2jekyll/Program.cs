using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace wordpress2jekyll
{
    class Program
    {
        static void Main(string[] args)
        {
            ImportAsync("export.zip").Wait();
        }

        private static async Task ImportAsync(string zipFilePath)
        {
            try
            {
                using (var stream = File.OpenRead(zipFilePath))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    var xmlEntries = from e in archive.Entries
                                     where e.Length != 0
                                     && e.Name.EndsWith(".xml")
                                     orderby e.FullName
                                     select e;

                    foreach (var entry in xmlEntries)
                    {
                        Console.WriteLine($"Opening '{entry.FullName}'...");
                        await ImportEntryAsync(entry);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"File '{ex.FileName}' not found");
            }
        }

        private static async Task ImportEntryAsync(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            {
                var posts = await Post.LoadPublishedAsync(stream);

                Console.WriteLine($"{posts.Length} published posts");

                throw new NotImplementedException();
            }
        }
    }
}