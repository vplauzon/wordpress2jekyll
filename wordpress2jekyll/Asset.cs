using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wordpress2jekyll
{
    internal class Asset
    {
        public Asset(Uri sourceUri, string filePath)
        {
            SourceUri = sourceUri;
            FilePath = filePath;
        }

        public Uri SourceUri { get; }

        public string FilePath { get; }

        public Task<byte[]> GetBytesAsync()
        {
            return Task.FromResult(new byte[] { 1, 42, 78, 45 });
        }
    }
}