using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.command {
	public abstract class Command {
		public virtual String call(String steamid, String[] args, String argsStr, Bot bot) {
			return run(new CommandInfo(steamid, args, argsStr), bot);
		}

		public virtual String run(CommandInfo cmdInfo, Bot bot) {
			return "";
		}
	}
}
