using System.Collections.Generic;
using System.Text.RegularExpressions;

/*
 * There are methods which are operating exclusively on script files
 */

namespace Script
{
	public class Script
	{
		//Useful in different scenarios omitted in this example.
		public static bool IsHashLocationStrategy(ref string script)
		{
			return (new Regex(@"(?:useHash:)\s*(true|1|!0)")).Match(script).Success;
		}

		public static List<string> GetSitemap(ref string script)
		{
			List<string> paths = new List<string>();
			MatchCollection m = new Regex(@"""(\/[^\s""]+?)""").Matches(script);

			foreach (Match match in m)
			{
				if (!paths.Contains(match.Groups[1].Value))
					paths.Add(match.Groups[1].Value);
			}

			paths.Sort();

			return paths;
		}
	}
}