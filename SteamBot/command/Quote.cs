using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamKit2;
using MySql.Data.MySqlClient;
using SteamBot.util;

namespace SteamBot.command {
	class Quote : Command {
		public override String run(CommandInfo cmdInfo, Bot bot) {
			try {
				if (cmdInfo.getArg(0).Equals("add", StringComparison.OrdinalIgnoreCase)) {
					String quote = Util.removeArg0(cmdInfo.getArgsStr());
					bot.sql.update("INSERT INTO sayings (message) VALUES ('" + quote + "')");
					return "Quote added :D";
				} else {
					List<object[]> myReader = bot.sql.query("SELECT message FROM sayings ORDER BY RAND() LIMIT 1");
					if (myReader.Count > 0) {
						return (string) myReader[0][0];
					} else {
						return "Woops, I canne finda de quotes";
					}
				}
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			}
			return "I'm in SPAAAAAAAAAAAAACE! ...and there are no quotes in space :<";
		}
	}
}
