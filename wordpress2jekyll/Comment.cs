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
                           let authorUrl = e.Element(Post.WP + "comment_author_url").Value
                           let content = e.Element(Post.WP + "comment_content").Value
                           let dateGmt = e.Element(Post.WP + "comment_date_gmt").Value
                           let date = DateTime.Parse(dateGmt, null, DateTimeStyles.AssumeUniversal)
                           select new Comment(
                               id,
                               new Author(author, authorUrl),
                               date,
                               content);

            return comments.ToImmutableArray();
        }

        private Comment(
            string id,
            Author author,
            DateTime date,
            string content)
        {
            Id = id;
            Author = author;
            Date = date;
            Content = content;
        }

        public string Id { get; }

        public Author Author { get; }

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