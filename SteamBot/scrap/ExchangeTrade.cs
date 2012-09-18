using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using SteamBot.util;
using System.Threading;

namespace SteamBot.scrap {
	class ExchangeTrade : Trade.TradeListener {
		private Bot bot;
		private int slot = 0;
		private int scrapDiff = 0;
		private int itemDiff = 0;
		private bool done = false;

		private bool otherOK = false;
		private bool OK = false;

		public ExchangeTrade(Bot bot) {
			this.bot = bot;
		}

		public override void OnTimeout() {
			Util.printConsole("Exchange trade timeout, ignore", bot, ConsoleColor.Red, true);
		}

		public override void OnError(int eid) {
			Util.printConsole("Error(" + eid + ") while exchanging items with another bot", bot, ConsoleColor.Red);
			if (bot.queueHandler.needItemsBool) {
				bot.queueHandler.reQueue();
			}
			bot.CurrentTrade = null;
		}

		private Dictionary<int, MutableInt> countItems(dynamic items, Inventory inv) {
			Dictionary<int, MutableInt> response = new Dictionary<int, MutableInt>();
			foreach (var child in items.rgInventory) {
				Inventory.Item item = inv.GetItem(ulong.Parse(((JProperty) child).Name));
				if (response.ContainsKey(item.Defindex)) {
					response[item.Defindex].increment();
				} else {
					response.Add(item.Defindex, new MutableInt());
				}
			}
			return response;
		}

		public override void OnAfterInit() {
			slot = 0;
			done = false;
			itemDiff = 0;
			scrapDiff = 0;

			List<ulong> excess = new List<ulong>();
			Dictionary<int, MutableInt> otherCount = countItems(trade.OtherItems, trade.OtherInventory);
			Dictionary<int, MutableInt> myCount = countItems(trade.MyItems, trade.MyInventory);

			foreach (int i in bot.toTrade) {
				myCount[i].decrement();
			}
			foreach (int i in bot.queueHandler.getReservedItems()) {
				if (myCount.ContainsKey(i)) {
					myCount[i].decrement();
				}
			}

			foreach (var child in trade.MyItems.rgInventory) {
				Inventory.Item item = trade.MyInventory.GetItem(ulong.Parse(((JProperty) child).Name));

				if (bot.toTrade.Contains(item.Defindex) || (myCount.ContainsKey(item.Defindex) ? myCount[item.Defindex].get() : 0) - (otherCount.ContainsKey(item.Defindex) ? otherCount[item.Defindex].get() : 0) > 1) {
					if (item.Defindex == 5000) {
						scrapDiff--;
					} else {
						itemDiff--;
					}
					trade.addItem(item.Id, slot++);
					if (myCount.ContainsKey(item.Defindex)) {
						myCount[item.Defindex].decrement();
						if (otherCount.ContainsKey(item.Defindex)) {
							otherCount[item.Defindex].increment();
						} else {
							otherCount.Add(item.Defindex, new MutableInt());
						}
					}
					bot.toTrade.Remove(item.Defindex);
				}
			}

			OK = true;
			trade.SendMessage("k");
			Util.printConsole("Sent OK " + bot.SteamClient.SteamID, bot, ConsoleColor.White, true);
			bot.toTrade.Clear();
		}

		public override void OnUserAccept() {
			OnFinished();
		}

		public override void OnUserSetReadyState(bool ready) {
			if (trade.MeReady) {
				OnFinished();
			} else if (ready) {
				trade.SetReady(true);
			}
		}

		public void OnFinished() {
			done = true;
			dynamic js = trade.AcceptTrade();
			if (js.success == true) {
				Util.printConsole("Success " + bot.SteamClient.SteamID, bot, ConsoleColor.Yellow, true);
			} else {
				Util.printConsole("Failure " + bot.SteamClient.SteamID, bot, ConsoleColor.Yellow, true);
			}
		}

		public override void OnComplete() {
			bot.sql.update("UPDATE bots SET items = items + " + itemDiff + ", scrap = scrap + " + scrapDiff + " WHERE botid = '" + bot.getBotId() + "'");
			if (bot.queueHandler.neededItems.Count > 0) {
				QueueHandler.needItems.Add(bot);
				Util.printConsole("Still need to aquire " + bot.queueHandler.neededItems.Count + " items before trading", bot, ConsoleColor.Yellow);
			} else if (bot.queueHandler.needItemsBool) {
				bot.queueHandler.needItemsBool = false;
				bot.queueHandler.gotItems = true;
			}
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
			Util.printConsole("wat, removed item?", bot, ConsoleColor.Cyan, true);
		}

		public override void OnMessage(string message) {
			otherOK = true;
		}

		public override void OnNewVersion() {
			if (!done && OK && otherOK && !trade.MeReady) {
				trade.SetReady(true);
			}
		}
	}
}
