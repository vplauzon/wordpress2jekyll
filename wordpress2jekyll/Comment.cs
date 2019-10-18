using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace wordpress2jekyll
{
    internal class Comment
    {
        public Comment(string author, string authorIp, DateTime date, string content)
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