using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamBot.util;

namespace SteamBot.command {
	class Missing : Command {
		private static int[] essentialWeapons = {
												450, 772,  46, 325, 317, 163, 355, 812,  45, 221, 222, 773,  44, 220, 448, 349, 449, 648,
												226, 730, 228, 129, 354, 441, 127, 447, 128, 775, 133, 414, 444, 416, 442, 237,
												 38, 326,  40, 215, 351,  39, 153, 739, 595, 813, 594, 214, 741, 740, 348, 593,
												405, 131, 327, 132, 308, 404, 172, 130, 406, 265, 307,
												312, 311, 159, 426, 425, 331, 239, 656, 811,  43,  41,  42, 424, 310,
												589, 141, 142, 329, 588, 528, 155, 527, 140,
												304,  36, 305,  35, 412, 411, 413,  37, 173,
												402, 232, 751, 642, 231, 752,  56,  58, 526,  57, 401, 230, 171,
												 61, 461,  60, 356,  59, 525, 460, 224, 810, 649, 225,
												357, 154, 415
											};
		private static int[] alternativeWeapons = {
												660, 669, 452, 572,
												658, 513,
												659, 466, 457,
												608, 661, 266, 482, 609,
												587, 654, 433, 298,
												662, 169,
												663,
												851, 664,
												161, 727, 297, 665, 638, 574,
												474, 264, 294, 423
											};
		private static int[] defaultWeapons = {
												190, 200,
												205, 196,
												208, 192,
												191, 206, 207,
												202, 195,
												197,
												198, 211, 204,
												193, 203, 201,
												212, 210, 194,
												199, 209
											};
		private static Dictionary<String, int[]> lists = new Dictionary<String, int[]>();
	
		static Missing() {
			lists.Add("e", essentialWeapons);
			lists.Add("a", alternativeWeapons);
			lists.Add("d", defaultWeapons);
		}

		public override String run(CommandInfo cmdInfo, Bot bot) {
			List<int> weapons = new List<int>();
			String include = cmdInfo.getArg(0, "e");
			foreach (string key in lists.Keys) {
				if (include.Contains(key)) {
					foreach (int i in lists[key]) {
						weapons.Add(i);
					}
				}
			}

			dynamic json = Util.CreateSteamRequest(String.Format("http://api.steampowered.com/ITFItems_440/GetPlayerItems/v0001/?key=" + bot.apiKey + "&SteamID={0}&format=json",cmdInfo.getSteamid().ConvertToUInt64()),"GET");

			foreach (dynamic item in json.result.items.item) {
				weapons.Remove((int) item.defindex);
			}
		
			string output = "Woah, you sir are a god among gigs\nYou have all essential items :D";
			if (weapons.Count > 0) {
				output = "You are missing these items:";

				foreach (int it in weapons) {
					output += "\n" + Util.getItemInfo(it).getName();
				}
			}
		
			return output;
		}
	}
}
