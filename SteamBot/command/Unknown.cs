using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.command {
	class Unknown : Command {
		public override String run(CommandInfo cmdInfo, Bot bot) {
			return "You must be gig... herp derp derp";
		}
	}
}
