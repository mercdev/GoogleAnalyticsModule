using System;
using System.IO;
using System.Text;
using System.Web;

namespace GoogleAnalyticsModule
{
	public class GoogleAnalyticsFilterStream : MemoryStream
	{
		private readonly Stream originalStream;
		private readonly Encoding encoding;
		private readonly GoogleAnalyticsFilterStream.FilterReplacementDelegate filterReplacementDelegate;

		public GoogleAnalyticsFilterStream(Stream originalStream, GoogleAnalyticsFilterStream.FilterReplacementDelegate replacementFunction, Encoding encoding)
		{
			this.originalStream = originalStream;
			this.filterReplacementDelegate = replacementFunction;
			this.encoding = encoding;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (HttpContext.Current.Response.ContentType.Contains("html"))
			{
				string str = this.encoding.GetString(buffer, offset, count);
				bool flag = false;
				string str1 = this.filterReplacementDelegate(str, out flag);
				if (flag)
				{
					this.originalStream.Write(this.encoding.GetBytes(str1), 0, this.encoding.GetByteCount(str1));
					return;
				}
			}

			this.originalStream.Write(buffer, offset, count);
		}

		public delegate string FilterReplacementDelegate(string originalStreamString, out bool modified);
	}
}