using System;
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

        private Post(string title, string content)
        {
            Title = title;
            Content = content;
        }

        public string Title { get; }

        public string Content { get; }

        public static async Task<Post[]> LoadPublishedAsync(Stream stream)
        {
            var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
            var statusName = WP + "status";
            var postTypeName = WP + "post_type";
            var posts = from i in document.Root.Elements().Elements("item")
                        let status = i.Element(statusName)
                        let postType = i.Element(postTypeName)
                        where status != null
                        && postType != null
                        && status.Value == "publish"
                        && postType.Value == "post"
                        select FromItemElement(i);

            return posts.ToArray();
        }

        private static Post FromItemElement(XElement element)
        {
            var title = element.Element("title").Value;
            var encoded = element.Element(CONTENT + "encoded").Value;

            return new Post(title, encoded);
        }
    }
}