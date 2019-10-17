using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace wordpress2jekyll
{
    internal class Asset
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public Asset(Uri sourceUri, string filePath)
        {
            SourceUri = sourceUri;
            FilePath = filePath;
        }

        public Uri SourceUri { get; }

        public string FilePath { get; }

        public async Task<byte[]> GetBytesAsync()
        {
            var response = await _httpClient.GetAsync(SourceUri);
            var content = await response.Content.ReadAsByteArrayAsync();

            return content;
        }
    }
}