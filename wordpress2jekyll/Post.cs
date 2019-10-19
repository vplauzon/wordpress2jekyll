using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using YamlDotNet.Serialization;

namespace wordpress2jekyll
{
    internal class Post
    {
        private const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss zzz";
        private static readonly XNamespace CONTENT = XNamespace.Get("http://purl.org/rss/1.0/modules/content/");
        private static readonly Regex CODE_REGEX = new Regex(
            @"\[code\s*(lang\s*=\s*(\w*))?\s*\]",
            RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex CLOSING_CODE_REGEX = new Regex(
            @"\[\/code\s*\]",
            RegexOptions.Singleline | RegexOptions.Compiled);

        internal static readonly XNamespace WP = XNamespace.Get("http://wordpress.org/export/1.2/");

        private Post(
            string title,
            string link,
            DateTime publicationDate,
            IEnumerable<string> categories,
            IEnumerable<string> tags,
            string content,
            IImmutableList<string> allAttachments,
            IImmutableList<Comment> comments)
        {
            Title = title;
            Link = link;
            PublicationDate = publicationDate;
            Categories = categories.ToImmutableArray();
            Tags = tags.ToImmutableArray();
            Assets = ExtractAssets(content, allAttachments, link, publicationDate);
            Comments = comments;
            Content = RenderContent(content, Assets);
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

        public string CommentsPath
        {
            get
            {
                var postName = Path.GetFileNameWithoutExtension(FilePath);
                var path = $"_data/{PublicationDate.Year}/{PublicationDate.Month}/{postName}/comments.yaml";

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
                using (var writer = new StringWriter(builder))
                {
                    var metaData = new
                    {
                        title = Title,
                        date = PublicationDate.ToString(DATE_FORMAT),
                        permalink = Link,
                        categories = Categories,
                        tags = Tags,
                    };
                    var serializer = new Serializer();

                    serializer.Serialize(writer, metaData);
                }
                builder.AppendLine("---");
                builder.Append(Content);

                return builder.ToString();
            }
        }

        public IImmutableList<Asset> Assets { get; }

        public IImmutableList<Comment> Comments { get; }

        public string CommentsAsYaml
        {
            get
            {
                var serializer = new Serializer();
                var yaml = serializer.Serialize(Comments);

                return yaml;
            }
        }

        public static async Task<Post[]> LoadPublishedAsync(Stream stream)
        {
            var document = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
            var statusName = WP + "status";
            var postTypeName = WP + "post_type";
            var items = document.Root.Elements().Elements("item");
            var attachments = from i in items
                              let postType = i.Element(postTypeName)
                              where postType != null
                              && postType.Value == "attachment"
                              let url = i.Element(WP + "attachment_url").Value
                              orderby url
                              select url;
            var alternateAttachments = from a in attachments
                                       let alternate = new Uri(a).Scheme == "http"
                                       ? a.Insert(4, "s")
                                       : a.Remove(4, 1)
                                       select alternate;
            var allAttachments =
                attachments.Concat(alternateAttachments).OrderBy(a => a).ToImmutableArray();
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

        private static IImmutableList<Asset> ExtractAssets(
            string content,
            IImmutableList<string> allAttachments,
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

            return assets.ToImmutableArray();
        }

        private static string RenderContent(string content, IImmutableList<Asset> assets)
        {
            content = RenderAssetInContent(content, assets);
            content = SubstituteWordpressCodeBlockInContent(content);
            content = EscapeDoubleCurlyBraces(content);

            return content;
        }

        private static string EscapeDoubleCurlyBraces(string content)
        {
            var builder = new StringBuilder(content.Length);
            var index = 0;

            while (true)
            {
                var openingIndex = content.IndexOf("{{", index);

                if (openingIndex != -1)
                {
                    var closingIndex = content.IndexOf("}}", openingIndex + 2);

                    if (closingIndex != -1)
                    {
                        //  Add everything before {{
                        builder.Append(content, index, openingIndex - index);
                        //  Escape curly braces
                        builder.Append("{% raw %} ");
                        //  Curly braces content itself
                        builder.Append(content, openingIndex, closingIndex - openingIndex + 2);
                        //  Escape curly braces
                        builder.Append(" {% endraw %}");
                        //  Update index
                        index = closingIndex + 2;
                    }
                    else
                    {   //  This case is when there is an opening {{ by itself
                        builder.Append(content, index, openingIndex + 2);
                        index = openingIndex + 2;
                    }
                }
                else if (builder.Length == 0)
                {
                    return content;
                }
                else
                {
                    builder.Append(content, index, content.Length - index);

                    return builder.ToString();
                }
            }
        }

        private static string SubstituteWordpressCodeBlockInContent(string content)
        {
            var builder = new StringBuilder(content.Length);

            while (true)
            {
                var codeMatch = CODE_REGEX.Match(content);

                if (codeMatch.Success)
                {
                    var afterCode = content.Substring(codeMatch.Index + codeMatch.Length);
                    var afterCodeMatch = CLOSING_CODE_REGEX.Match(afterCode);

                    if (afterCodeMatch.Success)
                    {
                        var code =
                            WebUtility.HtmlDecode(afterCode.Substring(0, afterCodeMatch.Index));
                        var remain =
                            afterCode.Substring(afterCodeMatch.Index + afterCodeMatch.Length);

                        //  Get everything before the code block
                        builder.Append(content.Substring(0, codeMatch.Index));
                        //  e.g. ```csharp
                        builder.Append("```");
                        if (codeMatch.Groups[1].Success)
                        {
                            builder.Append(codeMatch.Groups[2].Value);
                        }
                        //  In between code
                        builder.Append(code);
                        //  ```
                        builder.Append("```");
                        content = remain;
                    }
                    else
                    {   //  This case is when there is an opening [code] by itself
                        builder.Append(codeMatch.Value);
                        content = afterCode;
                    }
                }
                else if (builder.Length == 0)
                {
                    return content;
                }
                else
                {
                    builder.Append(content);

                    return builder.ToString();
                }
            }
        }

        private static string RenderAssetInContent(string content, IImmutableList<Asset> assets)
        {
            if (assets.Any())
            {
                var builder = new StringBuilder(content);

                foreach (var asset in assets)
                {
                    builder.Replace(asset.SourceUri.ToString(), '/' + asset.FilePath);
                }

                return builder.ToString();
            }
            else
            {
                return content;
            }
        }

        private static Post FromItemElement(XElement element, IImmutableList<string> allAttachments)
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
            var comments = Comment.FromElements(element.Elements(WP + "comment"));
            var encoded = element.Element(CONTENT + "encoded").Value;
            var truncatedLink = new Uri(link).AbsolutePath;

            return new Post(title, truncatedLink, pubDate, categories, tags, encoded, allAttachments, comments);
        }
    }
}