using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.command {
	class Say : Command {
		public override String run(CommandInfo cmdInfo, Bot bot) {
			return cmdInfo.getArgsStr();
		}
	}
}
