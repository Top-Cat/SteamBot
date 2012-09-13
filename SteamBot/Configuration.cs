using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;

namespace SteamBot
{
	public class Configuration
	{
		public static Configuration LoadConfiguration (string filename)
		{
			TextReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("SteamBot." + filename));
			string json = reader.ReadToEnd();
			reader.Close();

			Configuration config =  JsonConvert.DeserializeObject<Configuration>(json);

			config.Admins = config.Admins ?? new ulong[0];

			return config;
		}

		public ulong[] Admins { get; set; }
		public BotInfo[] Bots { get; set; }
		public string ApiKey { get; set; }

		public class BotInfo
		{
			public string Username { get; set; }
			public string Password { get; set; }
			public string DisplayName { get; set; }
			public int Id { get; set; }
			public ulong[] Admins;
		}
	}
}
