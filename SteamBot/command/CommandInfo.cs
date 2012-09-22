using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.command {
	public class CommandInfo {
		private SteamID steamid;
		private String[] args;
		private String argsStr;

		public CommandInfo(SteamID steamid, String[] args, String argsStr) {
			this.steamid = steamid;
			this.args = args;
			this.argsStr = argsStr;
		}

		public SteamID getSteamid() {
			return steamid;
		}

		public String getArg(int index) {
			return getArg(index, "");
		}

		public String getArg(int index, String def) {
			return argsStr.Length <= 0 ? def : (args.Length > index ? args[index] : def);
		}

		public int getArg(int index, int def) {
			return argsStr.Length <= 0 ? def : (args.Length > index ? int.Parse(args[index]) : def);
		}

		public String[] getArgs() {
			return args;
		}

		public String getArgsStr() {
			return argsStr;
		}
	}
}
