using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
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
            string content,
            string[] allAttachments)
        {
            Title = title;
            Link = link;
            PublicationDate = publicationDate;
            Content = content;
            Assets = ExtractAssets(content, publicationDate, allAttachments);
        }

        public string Title { get; }

        public string Link { get; }

        public DateTime PublicationDate { get; }

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
            DateTime publicationDate,
            string[] allAttachments)
        {
            var attachments = from attachment in allAttachments
                              where content.Contains(attachment)
                              select attachment;
            var assets = ImmutableList<Asset>.Empty;
            var paths = ImmutableHashSet<string>.Empty;

            foreach (var attachment in attachments)
            {
                var uri = new Uri(attachment);
                var fileName = Path.GetFileName(uri.LocalPath);

                if (paths.Contains(fileName))
                {
                    fileName = Guid.NewGuid().ToString() + "-" + fileName;
                }

                var filePath = $"";
                var asset = new Asset(uri, fileName);

                assets = assets.Add(asset);
                paths = paths.Add(fileName);
            }

            return assets.ToArray();
        }

        private static Post FromItemElement(XElement element, string[] allAttachments)
        {
            var title = element.Element("title").Value;
            var link = element.Element("link").Value;
            var pubDateText = element.Element("pubDate").Value;
            var pubDate = DateTime.Parse(pubDateText);
            var encoded = element.Element(CONTENT + "encoded").Value;
            var truncatedLink = new Uri(link).AbsolutePath;

            return new Post(title, truncatedLink, pubDate, encoded, allAttachments);
        }
    }
}