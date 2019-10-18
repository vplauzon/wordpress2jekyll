using System;
using System.Collections.Immutable;
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
            ImportAsync("export.zip", "jekyll.zip", null).Wait();
        }

        private static async Task ImportAsync(
            string exportZipFilePath,
            string jekyllZipFilePath,
            int? maxPostCount = null)
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
                        maxPostCount = await ImportXmlEntryAsync(entry, jekyllArchive, maxPostCount);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"File '{ex.FileName}' not found");
            }
        }

        private static async Task<int?> ImportXmlEntryAsync(ZipArchiveEntry entry, ZipArchive jekyllArchive, int? maxPostCount)
        {
            using (var stream = entry.Open())
            {
                var posts = await Post.LoadPublishedAsync(stream);

                Console.WriteLine($"{posts.Length} published posts");

                foreach (var post in posts)
                {
                    if (maxPostCount != null)
                    {   //  This is for tests only, in order to minimize # of downloads of assets
                        if (maxPostCount > 0)
                        {
                            --maxPostCount;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    await ImportPostAsync(post, jekyllArchive);
                }
            }

            return maxPostCount;
        }

        private static async Task ImportPostAsync(Post post, ZipArchive jekyllArchive)
        {
            Console.WriteLine($"  Processing {post.FilePath}...");

            //var assetBundles = from a in post.Assets
            //              let task = a.GetBytesAsync()
            //              orderby a.FilePath descending
            //              select new { Task = task, Asset = a };
            //var assetStack = ImmutableStack.Create(assetBundles.ToArray());

            await WriteToArchiveAsync(jekyllArchive, post.FilePath, post.ContentWithFrontMatter);

            //while (!assetStack.IsEmpty)
            //{
            //    var bundle = assetStack.Peek();
            //    var content = await bundle.Task;

            //    var asset = bundle.Asset;

            //    Console.WriteLine($"    Writing {asset.SourceUri}...");
            //    await WriteToArchiveAsync(jekyllArchive, asset.FilePath, content);
            //    assetStack = assetStack.Pop();
            //}

            if (post.Comments.Any())
            {
                await WriteToArchiveAsync(jekyllArchive, post.CommentsPath, post.CommentsAsYaml);
            }
        }

        private static async Task WriteToArchiveAsync(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path);

            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(content);
            }
        }

        private static async Task WriteToArchiveAsync(ZipArchive archive, string path, byte[] content)
        {
            var entry = archive.CreateEntry(path);

            using (var stream = entry.Open())
            {
                await stream.WriteAsync(content);
            }
        }
    }
}