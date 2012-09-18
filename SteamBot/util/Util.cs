using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;

namespace SteamBot.util {
	class Util {
		static Util() {
			itemSchema = JsonConvert.DeserializeObject(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("SteamBot.schema440-en_US.json")).ReadToEnd());
		}

		public static String removeArg0(String a) {
			return a.Contains(" ") ? a.Substring(a.IndexOf(" ") + 1) : "";
		}

		private static Dictionary<int, ItemInfo> itemInfo = new Dictionary<int, ItemInfo>();
		private static dynamic itemSchema;

		public static ItemInfo getItemInfo(int itemid) {
			if (!itemInfo.ContainsKey(itemid)) {
				itemInfo.Add(itemid, new ItemInfo(itemid, itemSchema[itemid.ToString()]));
			}
			return itemInfo[itemid];
		}

		public static dynamic CreateSteamRequest(string requestURL, string method = "GET") {
			HttpWebRequest webRequest = WebRequest.Create(requestURL) as HttpWebRequest;

			webRequest.Method = method;

			//The Correct headers :D
			webRequest.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
			webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			webRequest.Host = "steamcommunity.com";
			webRequest.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.47 Safari/536.11";

			webRequest.Headers.Add("Origin", "http://steamcommunity.com");
			webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
			webRequest.Headers.Add("X-Prototype-Version", "1.7");

			HttpWebResponse resp = webRequest.GetResponse() as HttpWebResponse;
			Stream str = resp.GetResponseStream();
			StreamReader reader = new StreamReader(str);
			string res = reader.ReadToEnd();

			return JsonConvert.DeserializeObject(res);
		}

		public static bool isDebugMode = true;
		public static void printConsole(String line, Bot bot, ConsoleColor color = ConsoleColor.White, bool isDebug = false) {
			Console.ForegroundColor = color;
			if (isDebug && isDebugMode) {
				Console.WriteLine("(" + bot.getBotId() + ") [DEBUG] " + line);
			} else if (!isDebug) {
				Console.WriteLine("(" + bot.getBotId() + ")         " + line);
				bot.sql.update("INSERT INTO botLogs (botid, message, color) VALUES ('" + bot.getBotId() + "', '" + line + "', '" + ((int) color) + "')");
			}
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
