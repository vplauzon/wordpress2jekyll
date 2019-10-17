using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace wordpress2jekyll
{
    internal class Post
    {
        private static readonly XNamespace WP = XNamespace.Get("http://wordpress.org/export/1.2/");
        private static readonly XNamespace CONTENT = XNamespace.Get("http://purl.org/rss/1.0/modules/content/");

        private Post(
            string title,
            string link,
            DateTime publicationDate,
            IEnumerable<string> categories,
            IEnumerable<string> tags,
            string content,
            string[] allAttachments)
        {
            Title = title;
            Link = link;
            PublicationDate = publicationDate;
            Categories = categories.ToImmutableArray();
            Tags = tags.ToImmutableArray();
            Assets = ExtractAssets(content, allAttachments, link, publicationDate);
            Content = ComputeContent(content, Assets);
        }

        public string Title { get; }

        public string Link { get; }

        public DateTime PublicationDate { get; }

        public IImmutableList<string> Categories { get; }

        public IImmutableList<string> Tags { get; }

        public string FilePath
        {
            get
            {
                //  Remove leading and trailing '/'
                var parts = Link.Split('/').Skip(1).Reverse().Skip(1).Reverse();
                var name = string.Join('-', parts) + ".md";
                var path = $"_posts/{PublicationDate.Year}/{PublicationDate.Month}/{name}";

                return path;
            }
        }

        public string Content { get; }

        public string ContentWithFrontMatter
        {
            get
            {
                var builder = new StringBuilder();

                builder.AppendLine("---");
                builder.Append("title:  ");
                builder.AppendLine(Title);
                builder.Append("date:  ");
                builder.AppendLine(PublicationDate.ToString());
                builder.Append("permalink:  \"");
                builder.Append(Link);
                builder.AppendLine("\"");
                if (Categories.Any())
                {
                    builder.AppendLine("categories:");
                    foreach (var c in Categories)
                    {
                        builder.Append("- ");
                        builder.AppendLine(c);
                    }
                }
                else
                {
                    builder.AppendLine("categories:  []");
                }
                if (Tags.Any())
                {
                    builder.AppendLine("tags:");
                    foreach (var t in Tags)
                    {
                        builder.Append("- ");
                        builder.AppendLine(t);
                    }
                }
                else
                {
                    builder.AppendLine("tags:  []");
                }
                builder.AppendLine("---");
                builder.Append(Content);

                return builder.ToString();
            }
        }

        public Asset[] Assets { get; }

        public static async Task<Post[]> LoadPublishedAsync(Stream stream)
        {
            var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
            var statusName = WP + "status";
            var postTypeName = WP + "post_type";
            var items = document.Root.Elements().Elements("item");
            var allAttachments = (from i in items
                                  let postType = i.Element(postTypeName)
                                  where postType != null
                                  && postType.Value == "attachment"
                                  let guid = i.Element("guid")
                                  select guid.Value).ToArray();
            var posts = from i in items
                        let status = i.Element(statusName)
                        let postType = i.Element(postTypeName)
                        where status != null
                        && postType != null
                        && status.Value == "publish"
                        && postType.Value == "post"
                        select FromItemElement(i, allAttachments);

            return posts.ToArray();
        }

        private static Asset[] ExtractAssets(
            string content,
            string[] allAttachments,
            string postLink,
            DateTime publicationDate)
        {
            var postName = Path.GetFileName(postLink.Trim('/'));
            var attachments = from attachment in allAttachments
                              where content.Contains(attachment)
                              select attachment;
            var assets = ImmutableList<Asset>.Empty;
            var assetNames = ImmutableHashSet<string>.Empty;

            foreach (var attachment in attachments)
            {
                var uri = new Uri(attachment);
                var fileName = Path.GetFileName(uri.LocalPath);

                if (assetNames.Contains(fileName))
                {
                    fileName = Guid.NewGuid().ToString() + "-" + fileName;
                }
                assetNames = assetNames.Add(fileName);

                var filePath =
                    $"assets/{publicationDate.Year}/{publicationDate.Month}/{postName}/{fileName}";
                var asset = new Asset(uri, filePath);

                assets = assets.Add(asset);
            }

            return assets.ToArray();
        }

        private static string ComputeContent(string content, Asset[] assets)
        {
            if (assets.Any())
            {
                var builder = new StringBuilder(content);

                foreach (var asset in assets)
                {
                    builder.Replace(asset.SourceUri.ToString(), asset.FilePath);
                }

                return builder.ToString();
            }
            else
            {
                return content;
            }
        }

        private static Post FromItemElement(XElement element, string[] allAttachments)
        {
            var title = element.Element("title").Value;
            var link = element.Element("link").Value;
            var pubDateText = element.Element("pubDate").Value;
            var pubDate = DateTime.Parse(pubDateText);
            var categories = from e in element.Elements("category")
                             where e.Attribute("domain").Value == "category"
                             select e.Value;
            var tags = from e in element.Elements("category")
                       where e.Attribute("domain").Value == "post_tag"
                       select e.Value;
            var comments = element.Elements(WP + "comment");
            var encoded = element.Element(CONTENT + "encoded").Value;
            var truncatedLink = new Uri(link).AbsolutePath;

            return new Post(title, truncatedLink, pubDate, categories, tags, encoded, allAttachments);
        }
    }
}