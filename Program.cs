using System;
using System.Net.Http;
using System.Threading.Tasks;
using static Webpage.Webpage;
using static Script.Script;
using static Handler.Handler;
using static Injection.Injection;
using System.Collections.Generic;

/*
 * This program's primary target is OWASP Juice Shop webpage.
 * Though the best approach would be black-box testing, in order to simplify 
 * presentation of how a test should be performed this application has some
 * gray-box testing phases.
 * 
 * However, not that it can't be changed by continuous development :)
 */

namespace SQLi
{
	public class Program
	{
		private static readonly HttpClient client = new HttpClient();
		public static async Task Main(string[] args)
		{
			string baseUrl = SetURL("https://sqli-example.herokuapp.com");
			Dbms dbms = new Dbms();

			if (baseUrl != "https://sqli-example.herokuapp.com")
				Exit("Program isn't ready to test webpages other than https://sqli-example.herokuapp.com");

			await CheckConnection();

			bool isDynamic = await Analyze();
			if (!isDynamic) Exit("Webpage not dynamic.");

			//Application would have different flow if no scripts were found.
			Dotter("Fetching name of main script");
			string mainScript = await GetMainScript();
			Status("Done");
			Console.WriteLine("Fetched main script: " + mainScript);
			string scriptContent = await GetContent(baseUrl + "/" + mainScript);
			Line();

			Dotter("Crawling paths");
			Console.WriteLine();
			Line();
			List<string> sitemap = GetSitemap(ref scriptContent);
			sitemap.ForEach(Console.WriteLine);
			Line();

			Color(await DoInjection(baseUrl, sitemap) ? "SQL injection performed successfully." : "Best of luck next time :)", ConsoleColor.Green);
		}

	}
}