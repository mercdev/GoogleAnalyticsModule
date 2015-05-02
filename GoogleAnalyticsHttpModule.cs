using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Web;

namespace GoogleAnalyticsModule
{
	public class GoogleAnalyticsHttpModule : IHttpModule
	{
		public GoogleAnalyticsFilterStream.FilterReplacementDelegate FilterStreamDelegate;

		public string AccountId
		{
			get
			{
				return ConfigurationManager.AppSettings["AccountId"];
			}
		}

		public string[] ExcludedExtensions
		{
			get
			{
				return ConfigurationManager.AppSettings["ExcludedExtensions"].Split(new char[] { ',' });
			}
		}

		public string NoAsync
		{
			get
			{
				return ConfigurationManager.AppSettings["NoAsync"];
			}
		}

		public string PostScript
		{
			get
			{
				return ConfigurationManager.AppSettings["PostScript"];
			}
		}

		public string PreScript
		{
			get
			{
				return ConfigurationManager.AppSettings["PreScript"];
			}
		}

		public GoogleAnalyticsHttpModule()
		{
			Console.WriteLine("initializing httpmodule...");
		}

		private void ContextBeginRequest(object sender, EventArgs e)
		{
			if (HttpContext.Current.Response != null)
			{
				GoogleAnalyticsFilterStream.FilterReplacementDelegate filterReplacementDelegate = new GoogleAnalyticsFilterStream.FilterReplacementDelegate(this.NoFilter);
				HttpContext.Current.Response.Filter = new GoogleAnalyticsFilterStream(HttpContext.Current.Response.Filter, filterReplacementDelegate, HttpContext.Current.Response.ContentEncoding);
			}
		}

		private void ContextPostReleaseRequestState(object sender, EventArgs e)
		{
			if (HttpContext.Current.Response != null)
			{
				if (!HttpContext.Current.Response.ContentType.Contains("html"))
				{
					if (HttpContext.Current.Response.Filter is GoogleAnalyticsFilterStream)
					{
						HttpContext.Current.Response.Filter = null;
					}
					return;
				}

				GoogleAnalyticsFilterStream.FilterReplacementDelegate filterReplacementDelegate = new GoogleAnalyticsFilterStream.FilterReplacementDelegate(this.FilterString);
				HttpContext.Current.Response.Filter = new GoogleAnalyticsFilterStream(HttpContext.Current.Response.Filter, filterReplacementDelegate, HttpContext.Current.Response.ContentEncoding);
				HttpContext.Current.Response.AddHeader("X-Analytics-Module", "GoogleAnalyticsHttpModule");
			}
		}

		public void Dispose()
		{
		}

		protected string FilterString(string response, out bool modified)
		{
			modified = false;
			Regex regex = new Regex(string.Concat("(_gat._getTracker\\(\"", this.AccountId, "\"\\))"), RegexOptions.IgnoreCase);
			Regex regex1 = new Regex(string.Concat("_gaq.push(\\['_setAccount', '", this.AccountId, "'\\])"), RegexOptions.IgnoreCase);
			Regex regex2 = new Regex(string.Concat("(_uacct = \"", this.AccountId, "\")"), RegexOptions.IgnoreCase);
			string str = "</body>";
			Regex regex3 = new Regex(str, RegexOptions.IgnoreCase);
			MatchCollection matchCollections = regex3.Matches(response);
			if (!string.IsNullOrEmpty(this.AccountId) && matchCollections.Count > 0 && !regex.IsMatch(response) && !regex2.IsMatch(response) && !regex1.IsMatch(response))
			{
				modified = true;
				string str1 = string.Concat("<script type=\"text/javascript\">\nvar _gaq = _gaq || [];\n_gaq.push(['_setAccount', '", this.AccountId, "']);\n_gaq.push(['_trackPageview']);\n(function() {\n   var ga = document.createElement('script');\n   ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';\n   ga.setAttribute('async', 'true');\n   document.documentElement.firstChild.appendChild(ga);\n})();\n</script>\n");
				string str2 = string.Concat("<script type=\"text/javascript\">\nvar gaJsHost = ((\"https:\" == document.location.protocol) ? \"https://ssl.\" : \"http://www.\");\ndocument.write(unescape(\"%3Cscript src='\" + gaJsHost + \"google-analytics.com/ga.js' type='text/javascript'%3E%3C/script%3E\"));\n</script>\n<script type=\"text/javascript\">\nvar pageTracker = _gat._getTracker(\"", this.AccountId, "\");\npageTracker._initData();\npageTracker._trackPageview();\n</script>\n");
				string[] preScript = new string[]
				{
					this.PreScript,
					"\n",
					null,
					null,
					null
				};
				preScript[2] = (!string.IsNullOrEmpty(this.NoAsync) ? str2 : str1);
				preScript[3] = string.Format("<!-- {1} {2} -->{0}", this.PostScript, VirtualPathUtility.GetDirectory(HttpContext.Current.Request.FilePath), VirtualPathUtility.GetFileName(HttpContext.Current.Request.FilePath));
				preScript[4] = "\n\n";
				string str3 = string.Concat(preScript);
				response = regex3.Replace(response, string.Concat(str3, str), 1, matchCollections[matchCollections.Count - 1].Index);
			}
			return response;
		}

		private static string GetCurrentFileExtension(HttpApplication application)
		{
			string extension;
			try
			{
				extension = VirtualPathUtility.GetExtension(application.Context.Request.FilePath);
			}
			catch
			{
				return string.Empty;
			}
			return extension;
		}

		public void Init(HttpApplication context)
		{
			bool ignore = false;
			string currentFileExtension = GoogleAnalyticsHttpModule.GetCurrentFileExtension(context);
			string[] excludedExtensions = this.ExcludedExtensions;
			for (int i = 0; i < excludedExtensions.Length; i++)
			{
				ignore = ignore | (excludedExtensions[i] == currentFileExtension);
			}


			if (!ignore || string.IsNullOrEmpty(currentFileExtension))	// account for mvc/razor views
			{
				context.BeginRequest += new EventHandler(this.ContextBeginRequest);
				context.PostReleaseRequestState += new EventHandler(this.ContextPostReleaseRequestState);
			}
		}

		protected string NoFilter(string response, out bool modified)
		{
			modified = false;
			return response;
		}
	}
}