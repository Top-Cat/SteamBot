using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using SteamBot.util;

namespace SteamBot.scrap {
	class AdminTrade : Trade.TradeListener {
		private Bot bot;

		private int scrapDiff = 0;
		private int itemDiff = 0;
		private int slot = 0;

		public AdminTrade(Bot bot) {
			this.bot = bot;
		}

		public override void OnTimeout() {
			Util.printConsole("Timeout during trade with admin", bot, ConsoleColor.Red, true);
		}

		public override void OnError(int eid) {
			Util.printConsole("Error(" + eid + ") while trading with admin", bot, ConsoleColor.Red);
			bot.queueHandler.tradeEnded();
			bot.CurrentTrade = null;
		}

		public override void OnAfterInit() {
			slot = 0;
			itemDiff = 0;
			scrapDiff = 0;

			List<object[]> result = bot.sql.query("SELECT schemaid, stock - COUNT(reservation.Id) + IF(highvalue=2,4,0) as stk FROM items LEFT JOIN reservation ON items.schemaid = reservation.itemid WHERE highvalue != 1 GROUP BY items.schemaid HAVING stk > 4");
			Dictionary<uint, MutableInt> count = new Dictionary<uint, MutableInt>();
			foreach (object[] row in result) {
				count.Add((uint) row[0], new MutableInt((uint)(ulong)row[1]));
			}

			foreach (var child in trade.MyItems.rgInventory) {
				Inventory.Item item = trade.MyInventory.GetItem(ulong.Parse(((JProperty) child).Name));

				if (count.ContainsKey(item.Defindex) && count[item.Defindex].get() > 3) {
					count[item.Defindex].decrement();

					if (item.Defindex == 5000) {
						scrapDiff--;
					} else {
						itemDiff--;
					}
					trade.addItem(item.Id, slot++);
				}
			}
		}

		public override void OnUserAccept() {
			OnFinished();
		}

		public override void OnUserSetReadyState(bool ready) {
			trade.SetReady(ready);
		}

		public void OnFinished() {
			dynamic js = trade.AcceptTrade();
			if (js.success == true) {
				Util.printConsole("Success " + bot.SteamClient.SteamID, bot, ConsoleColor.Yellow, true);
			} else {
				Util.printConsole("Failure " + bot.SteamClient.SteamID, bot, ConsoleColor.Yellow, true);
			}
		}

		public override void OnComplete() {
			foreach (ulong child in trade.OtherTrade) {
				dynamic item = trade.OtherItems.rgInventory[child.ToString()];
				Inventory.Item record = trade.OtherInventory.GetItem(getU(item.id));
				if (record.Defindex != 5000) {
					bot.sql.update("UPDATE items SET stock = stock + 1, `in` = `in` + 1 WHERE schemaid = '" + record.Defindex + "'");
				}
			}
			foreach (ulong child in trade.MyTrade) {
				dynamic item = trade.MyItems.rgInventory[child.ToString()];
				Inventory.Item record = trade.MyInventory.GetItem(getU(item.id));
				if (record.Defindex != 5000) {
					bot.sql.update("UPDATE items SET stock = stock - 1 WHERE schemaid = '" + record.Defindex + "'");
				}
			}
			bot.sql.update("UPDATE bots SET items = items + " + itemDiff + ", scrap = scrap + " + scrapDiff + " WHERE botid = '" + bot.getBotId() + "'");

			bot.queueHandler.tradeEnded();
			bot.CurrentTrade = null;
		}

		public override void OnUserAddItem(ItemInfo schemaItem, Inventory.Item invItem) {
			if (invItem.Defindex == 5000) {
				scrapDiff++;
			} else {
				itemDiff++;
			}
		}

		public override void OnUserRemoveItem(ItemInfo schemaItem, Inventory.Item invItem) {
			
		}

		public override void OnMessage(string message) {
			if (message.StartsWith("scrap")) {
				int count = int.Parse(message.Substring(6));
				foreach (var child in trade.MyItems.rgInventory) {
					Inventory.Item item = trade.MyInventory.GetItem(ulong.Parse(((JProperty) child).Name));

					if (item.Defindex == 5000 && count > 0 && !trade.MyTrade.Contains(item.Id)) {
						count--;
						scrapDiff--;
						trade.addItem(item.Id, slot++);
					}
				}
			}
		}

		public override void OnNewVersion() {
			
		}
	}
}
