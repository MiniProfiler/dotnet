using System.IO;
using System.IO.Compression;
using System.Web;

namespace StackExchange.Profiling.Helpers
{
    internal static class Compression
	{
		public static void EncodeStreamAndAppendResponseHeaders(HttpRequest request, HttpResponse response)
		{
			var acceptEncoding = request.Headers["Accept-Encoding"];
            if (acceptEncoding != null)
            {
                void Compress(string encoding, Stream stream)
                {
                    response.AppendHeader("Content-Encoding", encoding);
                    response.AppendHeader("Vary", "Accept-Encoding");
                    response.Filter = stream;
                }

                if (acceptEncoding.Contains("gzip"))
                {
                    Compress("gzip", new GZipStream(response.Filter, CompressionMode.Compress, true));
                }
                else if (acceptEncoding.Contains("deflate"))
                {
                    Compress("deflate", new DeflateStream(response.Filter, CompressionMode.Compress, true));
                }
            }
		}
	}
}