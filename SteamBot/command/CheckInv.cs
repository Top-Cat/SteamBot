using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamBot.util;

namespace SteamBot.command {
	class CheckInv : Command {
		public override String run(CommandInfo cmdInfo, Bot bot) {
			String output = "Here are the items you have duplicates of:";
			dynamic json = Util.CreateSteamRequest(String.Format("http://api.steampowered.com/ITFItems_440/GetPlayerItems/v0001/?key=" + bot.apiKey + "&SteamID={0}&format=json",cmdInfo.getSteamid()),"GET");
		
			int limit = cmdInfo.getArg(0, 2);
			Dictionary<int, MutableInt> freq = new Dictionary<int, MutableInt>();
			foreach (dynamic i in json.result.items.item) {
				int defindex = i.defindex;
				if (freq.ContainsKey(defindex)) {
					freq[defindex].increment();
				} else {
					freq.Add(defindex, new MutableInt());
				}
			}

			List<KeyValuePair<int, MutableInt>> myList = freq.ToList();
			myList.Sort((firstPair,nextPair) =>
				{
					return firstPair.Value.CompareTo(nextPair.Value);
				}
			);
			
			foreach (KeyValuePair<int, MutableInt> entry in myList) {
				if (entry.Value.get() < limit) {
					break;
				}
				ItemInfo itemInfo = Util.getItemInfo(entry.Key);
				output += "\n" + itemInfo.getName() + " x" + entry.Value.get();
			}
			return output;
		}
	}
}
