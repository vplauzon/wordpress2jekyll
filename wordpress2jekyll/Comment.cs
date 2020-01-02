using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace wordpress2jekyll
{
    internal class Comment
    {
        public static IImmutableList<Comment> FromElements(IEnumerable<XElement> elements)
        {
            var comments = from e in elements
                           let id = e.Element(Post.WP + "comment_id").Value
                           let author = e.Element(Post.WP + "comment_author").Value
                           let authorIp = e.Element(Post.WP + "comment_author_IP").Value
                           let authorEmail = e.Element(Post.WP + "comment_author_email").Value
                           let authorUrl = e.Element(Post.WP + "comment_author_url").Value
                           let content = e.Element(Post.WP + "comment_content").Value
                           let dateGmt = e.Element(Post.WP + "comment_date_gmt").Value
                           let date = DateTime.Parse(dateGmt, null, DateTimeStyles.AssumeUniversal)
                           select new Comment(
                               id,
                               author,
                               authorIp,
                               authorEmail,
                               authorUrl,
                               date,
                               content);

            return comments.ToImmutableArray();
        }

        private Comment(
            string id,
            string author,
            string authorIp,
            string authorEmail,
            string authorUrl,
            DateTime date,
            string content)
        {
            Id = id;
            Author = author;
            AuthorIp = authorIp;
            AuthorEmail = authorEmail;
            AuthorUrl = authorUrl;
            Date = date;
            Content = content;
        }

        public string Id { get; }

        public string Author { get; }

        public string AuthorIp { get; }

        public string AuthorEmail { get; }

        public string AuthorUrl { get; }

        public DateTime Date { get; }

        public string Content { get; }

        public string AsYaml()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(this);

            return yaml;
        }
    }
}