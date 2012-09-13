using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using SteamBot.util;

namespace SteamBot.command {
	class Tell : Command {
		public override String run(CommandInfo cmdInfo, Bot bot) {
			foreach (SteamID steam in bot.SteamFriends.getFriends()) {
				Console.WriteLine(bot.SteamFriends.GetFriendPersonaName(steam));
				if (bot.SteamFriends.GetFriendPersonaName(steam).Equals(cmdInfo.getArg(0), StringComparison.OrdinalIgnoreCase)) {
					bot.SteamFriends.SendChatMessage(steam, EChatEntryType.ChatMsg, Util.removeArg0(cmdInfo.getArgsStr()));
					return "Message sent!";
				}
			}
			return "Could not find user :/";
		}
	}
}
