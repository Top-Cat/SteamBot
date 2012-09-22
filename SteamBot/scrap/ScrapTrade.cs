using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SteamBot.util;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;

namespace SteamBot.scrap {
	class ScrapTrade : Trade.TradeListener {
		private Bot bot;

		private Dictionary<uint, bool> accepted = new Dictionary<uint, bool>();

		private List<int> reservedGiven = new List<int>();
		private int scrapGiven = 0;
		private bool onlyApplicable = true;
		private bool check = false;

		private int slot = 0;

		public ScrapTrade(Bot bot) {
			this.bot = bot;
		}

		public override void OnTimeout() {
			bot.SteamFriends.SendChatMessage(trade.otherSID, EChatEntryType.ChatMsg, "Trade took too long, please rejoin queue");
			OnFinished(false);
		}

		public override void OnError(int eid) {
			Util.printConsole("Error(" + eid + ") during trade with user", bot, ConsoleColor.Red);
			if (eid == 3) {
				bot.SteamFriends.SendChatMessage(trade.otherSID, EChatEntryType.ChatMsg, "I had some issues fetching your inventory, can we try again?");
				bot.SteamTrade.CancelTrade(trade.otherSID);
			} else if (eid == 2) {
				bot.queueHandler.tradeEnded();
				bot.CurrentTrade = null;
				return;
			}
			OnFinished(false);
		}

		public override void OnAfterInit() {
			slot = 0;
			scrapGiven = 0;
	
			trade.SendMessage("Welcome to ScrapBank!");
			respond();
		}

		public override void OnUserAccept() {
			dynamic js = trade.AcceptTrade();
			if (js.success == true) {
				Util.printConsole("Trade Success", bot, ConsoleColor.Green, true);
			} else {
				Util.printConsole("Trade Failure", bot, ConsoleColor.Red, true);
			}
		}

		public override void OnComplete() {
			Util.printConsole("OnComplete called, maybe success?", bot, ConsoleColor.White, true);

			int scrapReveived = 0;
			Dictionary<ushort, DualMInt> itemTotals = new Dictionary<ushort, DualMInt>();
			foreach (ulong child in trade.OtherTrade) {
				dynamic item = trade.OtherItems.rgInventory[child.ToString()];
				Inventory.Item record = trade.OtherInventory.GetItem(getU(item.id));
				if (record.Defindex != 5000) {
					if (itemTotals.ContainsKey(record.Defindex)) {
						itemTotals[record.Defindex].get1().increment();
					} else {
						itemTotals.Add(record.Defindex, new DualMInt(1, 0));
					}
				} else {
					scrapReveived++;
				}
			}
			foreach (ushort itemid in reservedGiven) {
				bot.sql.update("DELETE FROM reservation WHERE itemid = '" + itemid + "' && steamid = '" + trade.otherSID.ConvertToUInt64() + "' LIMIT 1");
				if (itemTotals.ContainsKey(itemid)) {
					itemTotals[itemid].get2().increment();
				} else {
					itemTotals.Add(itemid, new DualMInt(0, 1));
				}
			}

			int id = bot.sql.insert("INSERT INTO tradelog (steamid, botid, scrap, items, success) VALUES ('" + trade.otherSID.ConvertToUInt64() + "', '" + bot.getBotId() + "', '" + scrapGiven + "', '" + reservedGiven.Count + "', '1')");

			foreach (ushort itemid in itemTotals.Keys) {
				bot.sql.update("UPDATE items SET stock = stock + " + (itemTotals[itemid].get1().get() - itemTotals[itemid].get2().get()) + ", `in` = `in` + " + itemTotals[itemid].get1().get() + ", `out` = `out` + " + itemTotals[itemid].get2().get() + " WHERE schemaid = '" + itemid + "'");
				bot.sql.update("INSERT INTO tradeitems (tradeid, schemaid, quantityIn, quantityOut) VALUES (" + id + ", '" + itemid + "', " + itemTotals[itemid].get1().get() + ", " + itemTotals[itemid].get2().get() + ")");
			}

			bot.sql.update("UPDATE bots SET trades = trades + 1, scrap = scrap + " + (scrapReveived - scrapGiven) + ", items = items + " + ((trade.OtherTrade.Count - scrapReveived) - reservedGiven.Count) + " WHERE botid = '" + bot.getBotId() + "'");
			reservedGiven.Clear();
			bot.SteamFriends.SendChatMessage(trade.otherSID, EChatEntryType.ChatMsg, "Thanks for trading with us :)");
			OnFinished(true);
		}

		public void OnFinished(bool success) {
			bot.queueHandler.tradeEnded();
			bot.CurrentTrade = null;
			if (!success) {
				bot.sql.update("INSERT INTO tradelog (steamid, botid, scrap, items, success) VALUES ('" + trade.otherSID.ConvertToUInt64() + "', '" + bot.getBotId() + "', '0', '0', '0')");
				bot.sql.update("UPDATE bots SET trades = trades + 1 WHERE botid = '" + bot.getBotId() + "'");
			}
		}

		public override void OnUserSetReadyState(bool ready) {
			if (ready) {
				if (check && onlyApplicable) {
					trade.SetReady(true);
				} else {
					trade.SendMessage("Please fix issues with your items before the trade can be completed");
				}
			} else {
				trade.SetReady(false);
			}
		}

		public override void OnUserAddItem(ItemInfo schemaItem, Inventory.Item invItem) {
			
		}

		public override void OnUserRemoveItem(ItemInfo schemaItem, Inventory.Item invItem) {

		}

		public override void OnMessage(string message) {
			
		}

		public override void OnNewVersion() {
			respond();
		}

		private int[] applicableItems() {
			int[] items = { 0, 0 };
			onlyApplicable = true;
			string chat = "";
			foreach (ulong child in trade.OtherTrade) {
				dynamic item = trade.OtherItems.rgInventory[child.ToString()];
				dynamic itemInfo = trade.OtherItems.rgDescriptions[(string) (item.classid + "_" + item.instanceid)];

				Inventory.Item record = trade.OtherInventory.GetItem(getU(item.id));
				bool itemok = (!record.IsNotCraftable && record.Quality == 6);

				if (!accepted.ContainsKey(record.Defindex)) {
					List<object[]> myReader = bot.sql.query("SELECT '' FROM items WHERE schemaid='" + record.Defindex + "'");
					accepted.Add(record.Defindex, myReader.Count > 0);
				}

				if (accepted[record.Defindex] && itemok) {
					items[0]++;
				} else if (itemok && record.Defindex == 5000) {
					items[1]++;
				} else {
					onlyApplicable = false;
					chat += "Item '" + itemInfo.name + " (" + record.Defindex + ")' not accepted\n";
				}
			}
			trade.SendMessage(chat);
			return items;
		}

		private void respond() {
			try {
				int[] items = applicableItems();
				trade.SendMessage("Applicable Items: " + items[0]);

				int scrapA = items[1];
				int scrapB = items[0] / 2;
				int cScrap = 0;

				List<uint> reserved = bot.queueHandler.getReservedItems();
				List<ulong> alreadyTrade = new List<ulong>();

				foreach (ulong child in trade.MyTrade) {
					Inventory.Item item = trade.MyInventory.GetItem(getU(trade.MyItems.rgInventory[child.ToString()].id));
					if (item.Defindex != 5000) {
						if (reserved.Contains(item.Defindex)) {
							alreadyTrade.Add(child);
							reserved.Remove(item.Defindex);
							if (scrapA > 0) {
								scrapA--;
							} else {
								scrapB--;
							}
						} else {
							trade.removeItem(child);
							reservedGiven.Remove(item.Defindex);
							slot--;
						}
					}
				}

				foreach (var child in trade.MyItems.rgInventory) {
					Inventory.Item item = trade.MyInventory.GetItem(ulong.Parse(((JProperty) child).Name));
					if (reserved.Contains(item.Defindex) && !alreadyTrade.Contains(item.Id)) {
						trade.addItem(ulong.Parse(((JProperty) child).Name), slot++);
						reservedGiven.Add(item.Defindex);
						reserved.Remove(item.Defindex);
						if (scrapA > 0) {
							scrapA--;
						} else {
							scrapB--;
						}
					}
				}

				if (reserved.Count > 0) {
					trade.SendMessage("Missing " + reserved.Count + " of your reserved items. Join the queue again to get them.");
				}

				foreach (ulong child in trade.MyTrade) {
					dynamic item = trade.MyItems.rgInventory[child.ToString()];
					if (item.classid == "2675") {
						cScrap++;
						if (cScrap > scrapB) {
							trade.removeItem(child);
							scrapGiven--;
							slot--;
							//sendChat("Remove some scrap?" + scrap + "/" + cScrap);
						}
					}
				}
				if (cScrap < scrapB) {
					foreach (var child in trade.MyItems.rgInventory) {
						if (trade.MyItems.rgInventory[((JProperty) child).Name].classid == "2675" && !trade.MyTrade.Contains(ulong.Parse(((JProperty) child).Name))) {
							trade.addItem(ulong.Parse(((JProperty) child).Name), slot++);
							scrapGiven++;
							cScrap++;
							if (cScrap >= scrapB) {
								break;
							}
						}
					}

					if (cScrap < scrapB) {
						trade.SendMessage("I don't have enough scrap to complete this transaction D:");
					}
				}
				check = scrapA == 0 && cScrap >= scrapB && scrapB >= 0 && items[0] % 2 == 0;
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}
		}
	}
}
