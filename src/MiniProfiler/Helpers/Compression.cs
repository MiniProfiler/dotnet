using System;
using System.IO.Compression;
using System.Linq;
using System.Web;

namespace StackExchange.Profiling.Helpers
{
	internal static class Compression
	{
		public static void EncodeStreamAndAppendResponseHeaders(HttpRequest request, HttpResponse response)
		{
			var encoding = request.Headers["Accept-Encoding"];
			EncodeStreamAndAppendResponseHeaders(response, encoding);
		}

		public static void EncodeStreamAndAppendResponseHeaders(HttpResponse response, string acceptEncoding)
		{
			if (acceptEncoding == null) return;

			var preferredEncoding = ParsePreferredEncoding(acceptEncoding);
			if (preferredEncoding == null) return;

			response.AppendHeader("Content-Encoding", preferredEncoding);
			response.AppendHeader("Vary", "Accept-Encoding");
			if (preferredEncoding == "deflate")
			{
				response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress, true);
			}
			if (preferredEncoding == "gzip")
			{
				response.Filter = new GZipStream(response.Filter, CompressionMode.Compress, true);
			}
		}

		static readonly string[] AllowedEncodings = new[] { "gzip", "deflate" };
        static readonly char[] encodingSplit = new[] { ',' };
        static readonly char[] encodingTypeSplit = new[] { ';' };

        static string ParsePreferredEncoding(string acceptEncoding)
		{
			return acceptEncoding
				.Split(encodingSplit, StringSplitOptions.RemoveEmptyEntries)
				.Select(type => type.Split(encodingTypeSplit))
				.Select(parts => new
				{
					encoding = parts[0].Trim(),
					qvalue = ParseQValueFromSecondArrayElement(parts)
				})
				.Where(x => AllowedEncodings.Contains(x.encoding))
				.OrderByDescending(x => x.qvalue)
				.Select(x => x.encoding)
				.FirstOrDefault();
		}

		static float ParseQValueFromSecondArrayElement(string[] parts)
		{
			const float defaultQValue = 1f;
			if (parts.Length < 2) return defaultQValue;
            return float.TryParse(parts[1].Trim(), out float qvalue) ? qvalue : defaultQValue;
		}
	}
}