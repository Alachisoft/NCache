// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace Alachisoft.Common
{
    public static class HttpUtil
    {
        public static string GetExtension(string url)
        {
            string extension = Path.GetExtension(url);
            if (extension.Length > 0)
                extension = extension.Substring(1).ToLower();
            return extension;
        }

        public static string MakeAbsoluteUrl(Uri baseUrl, string url)
        {
            Uri absoluteUri;
            Uri.TryCreate(baseUrl, url, out absoluteUri);
            url = absoluteUri.ToString();
            return url;
        }

        public static Stream DownloadContent(this WebClient client, string url)
        {
            byte[] data = client.DownloadData(url);
            string encoding = (client.ResponseHeaders[HttpResponseHeader.ContentEncoding] ?? "").ToLower();
            Stream stream = new MemoryStream(data);
            if (encoding == "gzip")
                stream = new GZipStream(stream, CompressionMode.Decompress);
            else if (encoding == "deflate")
                stream = new DeflateStream(stream, CompressionMode.Decompress);
            return stream;
        }
    }
}
