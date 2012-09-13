using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text;
using SteamBot.util;

namespace SteamBot.scrap {
	class ExchangeTrade : Trade.TradeListener {
		private Bot bot;
		private int slot = 0;
		private bool done = false;

		private bool otherOK = false;
		private bool OK = false;

		public ExchangeTrade(Bot bot) {
			this.bot = bot;
		}

		public override void OnTimeout() {
			Util.printConsole("Timeout??");
		}

		public override void OnError(string message) {
			Util.printConsole(message);
		}

		private ulong getU(JValue v) {
			return ((JValue) v).ToObject<ulong>();
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
			List<ulong> excess = new List<ulong>();
			Dictionary<int, MutableInt> otherCount = countItems(trade.OtherItems, trade.OtherInventory);
			Dictionary<int, MutableInt> myCount = countItems(trade.MyItems, trade.MyInventory);

			foreach (int i in bot.toTrade) {
				myCount[i].decrement();
			}

			foreach (var child in trade.MyItems.rgInventory) {
				Inventory.Item item = trade.MyInventory.GetItem(ulong.Parse(((JProperty) child).Name));

				if (bot.toTrade.Contains(item.Defindex) || (myCount.ContainsKey(item.Defindex) ? myCount[item.Defindex].get() : 0) - (otherCount.ContainsKey(item.Defindex) ? otherCount[item.Defindex].get() : 0) > 1) {
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
				Util.printConsole("Success " + bot.SteamClient.SteamID);
				bot.sql.update("UPDATE bots SET items = items + " + (trade.OtherTrade.Count - trade.MyTrade.Count) + " WHERE botid = '" + bot.getBotId() + "'");
			} else {
				Util.printConsole("Failure " + bot.SteamClient.SteamID);
			}
			if (bot.queueHandler.neededItems.Count > 0) {
				QueueHandler.needItems.Add(bot);
			} else if (bot.queueHandler.needItemsBool) {
				bot.queueHandler.needItemsBool = false;
				bot.queueHandler.gotItems = true;
			}
			bot.CurrentTrade = null;
		}

		public override void OnUserAddItem(ItemInfo schemaItem, Inventory.Item invItem) {

		}

		public override void OnUserRemoveItem(ItemInfo schemaItem, Inventory.Item invItem) {

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
