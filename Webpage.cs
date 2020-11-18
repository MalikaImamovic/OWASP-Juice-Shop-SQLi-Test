using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Handler.Handler;

/*
 * This part of application examines webpage in order to get valuable information
 */

namespace Webpage
{
	public class Webpage
	{
		private static readonly HttpClient client = new HttpClient();
		public static string baseUrl = "";

		public static string SetURL(string url = "")
		{
			if (url == "")
			{
				Console.Write("Enter target URL: ");
				url = Console.ReadLine();
			}
			else baseUrl = url;

			return url;
		}

		public static async Task CheckConnection()
		{
			Dotter("Checking connection");
			HttpStatusCode status = (await client.GetAsync(baseUrl)).StatusCode;
			if (status != HttpStatusCode.OK)
				Exit(status.ToString());
			Status(status);
			Thread.Sleep(1000);
			Line();
		}

		public static async Task<bool> Analyze()
		{
			Console.WriteLine("Starting webpage analysis");
			Line();
			Thread.Sleep(1000);

			Dotter("Fetching webapp content");
			string content = await GetContent(baseUrl);
			Status("Done");

			Dotter("Extracting content of HTML body");
			string body = GetBodyHTML(content);
			Status("Done");

			Dotter("Getting list of tags inside HTML body");
			List<string> tags = new List<string>();
			GetTagsInsideBody(tags, body);
			Status("Done");
			Console.WriteLine("Extracted tag list: " + string.Join(" ", tags));
			Console.WriteLine("Total number of tags: " + tags.Count);
			Console.WriteLine();

			Console.WriteLine("Grouped tags (tag, count):"); IEnumerable<IGrouping<string, string>> tagGroups = tags.GroupBy(x => x);
			List<string> groupedTagNames = new List<string>();
			List<int> groupedTagCounts = new List<int>();
			foreach (var tagGroup in tagGroups)
			{
				groupedTagNames.Add(tagGroup.Key);
				groupedTagCounts.Add(tagGroup.Count());
			}
			for (int i = 0; i < groupedTagNames.Count; i++)
			{
				Console.WriteLine("     " + groupedTagNames[i] + ": " + groupedTagCounts[i]);
			}
			Console.WriteLine();

			Console.WriteLine("Determining if scripts are attached inside body tag: " + (tags.Contains("script") ? "yes" : "no"));
			int tagsNotScriptCount = tags.Count - groupedTagCounts[groupedTagNames.FindIndex(x => x.Contains("script"))];
			Console.WriteLine("Number of other tags when scripts are omitted: " + tagsNotScriptCount);
			Console.WriteLine();

			//Execution is complex if there was more tags, as well as instances of same ones. 
			//This is simplified version applicable to OWASP Juice Shop current structure.
			if (tagsNotScriptCount == 1)
			{
				Console.WriteLine("Webapp has only one tag inside body left to inspect before continuing.");
				Console.Write("Determining content inside remaining tag(s): ");

				string tag = "";
				string tagHTML = "";

				foreach (string name in tags)
				{
					if (!name.Equals("script"))
					{
						tag = name;
						tagHTML = GetTagHTML(content, name);
						Console.WriteLine(tagHTML == "" ? "none" : tagHTML);
					}
				}

				Console.WriteLine();

				if (tagHTML == "")
				{
					Color(
						  "As content inside <" + tag + "> tag is empty, it implies that this is webapp which loads DOM content dynamically via scripts. " +
						  "Finding data entry points of such structured website requires JavaScript rendering to populate with dynamic objects which requires tool like browser to perform. " +
					      "This application will attempt to fetch data entry points identifiable within webapp's core script.",
						  ConsoleColor.DarkYellow);
					Console.WriteLine();
					return true;
				}
			}
			return false;				
		}

		public static async Task<string> GetContent(string url)
		{
			return await client.GetStringAsync(url);
		}

		private static string GetBodyHTML(string content)
		{
			return (new Regex(@"<body([\s\S]*?)<\/body>").Match(content).Groups[1].Value);
		}

		private static void GetTagsInsideBody(List<string> list, string body)
		{
			MatchCollection matches = (new Regex(@"<[a-z]+-?[a-z]*")).Matches(body);
			foreach (Match match in matches) { list.Add(match.Value.Substring(1)); }
		}

		private static string GetTagHTML(string content, string tag)
		{
			return (new Regex(@"<"+tag+ @".*>([\s\S]*?)<\/" + tag+@">").Match(content).Groups[1].Value);
		}

		public static async Task<string> GetMainScript()
		{
			string body = GetBodyHTML(await GetContent(baseUrl));
			MatchCollection m = GetTagAttributeValue(body, "script", "src");
			string mainScript = "";

			foreach (Match match in m)
			{
				//Adding other common values and validating ECMAScript revision check would yield better results for more generalized approach.
				//Also, checking all scripts for valuable information could be done, but this is yet another simplified version for particular web-application.
				if (match.Groups[1].Value.Contains("main-es2018"))
					mainScript = match.Groups[1].Value;
			}

			return mainScript;
		}

		private static MatchCollection GetTagAttributeValue(string content, string tag, string attribute)
		{
			//Improvable by finding alternative to \K match reset, instead of using groups.
			return new Regex(@"(?:<" + tag + @")[^>]+(?:"+ attribute + @")=[""']?((?:.(?![""']?\s+(?:\S +)=|[>""']))+.)(?=[""'])?").Matches(content);
		}
	}
}
