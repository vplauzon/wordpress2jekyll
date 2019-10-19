using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace wordpress2jekyll
{
    internal class Comment
    {
        public static IImmutableList<Comment> FromElements(IEnumerable<XElement> elements)
        {
            var comments = from e in elements
                           let author = e.Element(Post.WP + "comment_author").Value
                           let authorIp = e.Element(Post.WP + "comment_author_IP").Value
                           let content = e.Element(Post.WP + "comment_content").Value
                           let dateGmt = e.Element(Post.WP + "comment_date_gmt").Value
                           let date = DateTime.Parse(dateGmt, null, DateTimeStyles.AssumeUniversal)
                           select new Comment(author, authorIp, date, content);

            return comments.ToImmutableArray();
        }

        private Comment(string author, string authorIp, DateTime date, string content)
        {
            Author = author;
            AuthorIp = authorIp;
            Date = date;
            Content = content;
        }

        public string Author { get; }

        public string AuthorIp { get; }

        public DateTime Date { get; }

        public string Content { get; }
    }
}