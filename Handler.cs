using System;
using System.Net;
using System.Threading;

/*
 * Handles different formatting requirements as well as exit routines.
 */

namespace Handler
{
	public class Handler
	{
		public static void Exit(string message = "N/A")
		{
			Console.ForegroundColor = ConsoleColor.Red;

			Console.WriteLine("Unexpected behaviour.");
			Console.WriteLine("Message: " + message);
			Console.WriteLine("Terminating execution.");

			Console.ResetColor();
			Environment.Exit(0);
		}

		public static bool Repeat(string message = "")
		{
			Console.ForegroundColor = ConsoleColor.Yellow;

			Console.WriteLine("Unexpected input.");
			Console.WriteLine("Message: " + message);

			Console.ResetColor();

			return true;
		}

		public static void Status(HttpStatusCode statusCode)
		{
			Console.Write("[");

			switch (statusCode)
			{
				case HttpStatusCode.OK:
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(statusCode);
					break;
					//Extendable by defining other status codes formats.
			}

			Console.ResetColor();
			Console.WriteLine("]");
		}

		public static void Status(string status)
		{
			Console.Write("[");

			switch (status)
			{
				case "Done":
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write(status);
					break;
					//Extendable by defining other statuses.
			}

			Console.ResetColor();
			Console.WriteLine("]");
		}

		public static void Color(string text, ConsoleColor color, bool newline = true)
		{
			Console.ForegroundColor = color;
			Console.Write(text + (newline ? "\n" : ""));
			Console.ResetColor();
		}

		public static void Dotter(string text)
		{
			for (int i = 0; i < 4; i++)
			{
				Console.Write(i == 0 ? text : ".");
				Thread.Sleep(750);
			}
		}

		public static void Line(char character = '=', int multiplication = 100)
		{
			Console.WriteLine(new string(character, multiplication));
		}
	}
}
