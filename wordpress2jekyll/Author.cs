using System;
using System.Collections.Generic;
using System.Text;

namespace wordpress2jekyll
{
    internal class Author
    {
        public Author(
            string name,
            string url)
        {
            Name = name;
            Url = url;
        }

        public string Name { get; }

        public string Url { get; }
    }
}