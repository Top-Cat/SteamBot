using SteamBot.command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.command {
	public static class Extensions {
		private static Dictionary<String, Command> map = new Dictionary<String, Command>();
		private static Unknown unknown = new Unknown();

		public static Command getCommand(String cmd) {
			if (map.ContainsKey(cmd)) {
				return map[cmd];
			} else {
				return unknown;
			}
		}
	
		static Extensions() {
			map.Add("missing", new Missing());
			map.Add("checkinv", new CheckInv());
			map.Add("say", new Say());
			map.Add("tell", new Tell());
			map.Add("quote", new Quote());
		}
	}
}
