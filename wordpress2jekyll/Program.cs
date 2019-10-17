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
            ImportAsync("export.zip", "jekyll.zip").Wait();
        }

        private static async Task ImportAsync(string exportZipFilePath, string jekyllZipFilePath)
        {
            try
            {
                using (var exportStream = File.OpenRead(exportZipFilePath))
                using (var jekyllStream = File.OpenWrite(jekyllZipFilePath))
                using (var exportArchive = new ZipArchive(exportStream, ZipArchiveMode.Read))
                using (var jekyllArchive = new ZipArchive(jekyllStream, ZipArchiveMode.Create))
                {
                    var xmlEntries = from e in exportArchive.Entries
                                     where e.Length != 0
                                     && e.Name.EndsWith(".xml")
                                     orderby e.FullName
                                     select e;

                    foreach (var entry in xmlEntries)
                    {
                        Console.WriteLine($"Opening '{entry.FullName}'...");
                        await ImportEntryAsync(entry, jekyllArchive);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"File '{ex.FileName}' not found");
            }
        }

        private static async Task ImportEntryAsync(ZipArchiveEntry entry, ZipArchive jekyllArchive)
        {
            using (var stream = entry.Open())
            {
                var posts = await Post.LoadPublishedAsync(stream);

                Console.WriteLine($"{posts.Length} published posts");

                foreach (var post in posts)
                {
                    await ImportPostAsync(post, jekyllArchive);
                }
            }
        }

        private static async Task ImportPostAsync(Post post, ZipArchive jekyllArchive)
        {
            var entry = jekyllArchive.CreateEntry(post.FilePath);

            Console.WriteLine($"Processing {post.FilePath}");

            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(post.Content);
            }
        }
    }
}