using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace wordpress2jekyll
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 5;

            Console.WriteLine("Wordpress 2 Jekyll converter");

            if (args.Length < 2)
            {
                Console.WriteLine("There should be 2 arguments for this process:  input file and output file");
                Console.WriteLine("There is also an optional switch:  --no-image=true/false");
            }
            else
            {
                var (input, output, doImages) = GetArguments(args);

                Console.WriteLine($"Input:  {input}");
                Console.WriteLine($"Output:  {output}");
                Console.WriteLine($"Do Images:  {doImages}");
                Console.WriteLine();

                ImportAsync(input, output, doImages).Wait();

                Console.WriteLine();
                Console.WriteLine($"Done.  Output written at {output}");
                Console.WriteLine();
            }
        }

        private static (string input, string output, bool doImages) GetArguments(string[] args)
        {
            if (args[0].StartsWith("--do-images="))
            {
                if (args.Length != 3)
                {
                    throw new NotSupportedException(
                        "Since first argument is '--do-images', there should be 2 more arguments");
                }
                else
                {
                    var doImages = bool.Parse(args[0].Substring("--do-images=".Length));

                    return (args[1], args[2], doImages);
                }
            }
            else
            {
                if (args.Length != 2)
                {
                    throw new NotSupportedException(
                        "Since first argument isn't '--do-images', there should be only 2 arguments");
                }
                else
                {
                    return (args[0], args[1], true);
                }
            }
        }

        private static async Task ImportAsync(
            string exportZipFilePath,
            string jekyllZipFilePath,
            bool doImages)
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
                        await ImportXmlEntryAsync(entry, jekyllArchive, doImages);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"File '{ex.FileName}' not found");
            }
        }

        private static async Task ImportXmlEntryAsync(
            ZipArchiveEntry entry,
            ZipArchive jekyllArchive,
            bool doImages)
        {
            using (var stream = entry.Open())
            {
                var posts = await Post.LoadPublishedAsync(stream);

                Console.WriteLine($"{posts.Length} published posts");

                foreach (var post in posts)
                {
                    await ImportPostAsync(post, jekyllArchive, doImages);
                }
            }
        }

        private static async Task ImportPostAsync(
            Post post,
            ZipArchive jekyllArchive,
            bool doImages)
        {
            Console.WriteLine($"  Processing {post.FilePath}...");

            var assetBundles = from a in post.Assets
                               let task = a.GetBytesAsync()
                               orderby a.FilePath descending
                               select new { Task = task, Asset = a };
            var assetStack = doImages
                ? ImmutableStack.Create(assetBundles.ToArray())
                : ImmutableStack.Create(assetBundles.Take(0).ToArray());

            await WriteToArchiveAsync(jekyllArchive, post.FilePath, post.ContentWithFrontMatter);

            while (!assetStack.IsEmpty)
            {
                var bundle = assetStack.Peek();
                var content = await bundle.Task;

                var asset = bundle.Asset;

                Console.WriteLine($"    Writing {asset.SourceUri}...");
                await WriteToArchiveAsync(jekyllArchive, asset.FilePath, content);
                assetStack = assetStack.Pop();
            }

            if (post.Comments.Any())
            {
                Console.WriteLine($"    Writing comments...");
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