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

		private List<int> reservedGiven = new List<int>();
		private int scrapGiven = 0;
		private bool onlyApplicable = true;
		private bool check = false;
		private bool scrapMode = false;

		private int slot = 0;

		public ScrapTrade(Bot bot) {
			this.bot = bot;
		}

		public override void OnTimeout() {
			bot.SteamFriends.SendChatMessage(trade.otherSID, EChatEntryType.ChatMsg, "Trade took too long, please rejoin queue");
			OnFinished(false);
		}

		public override void OnError(string message) {
			OnFinished(false);
		}

		public override void OnAfterInit() {
			trade.SendMessage("Welcome to ScrapBank!");
			respond();
		}

		public override void OnUserAccept() {
			dynamic js = trade.AcceptTrade();
			if (js.success == true) {
				Util.printConsole("[TradeSystem] Trade was successfull! Resetting System...", ConsoleColor.Green);
				//if (!scrapMode) {
				int scrapReveived = 0;
				foreach (ulong child in trade.OtherTrade) {
					dynamic item = trade.OtherItems.rgInventory[child.ToString()];
					Inventory.Item record = trade.OtherInventory.GetItem(getU(item.id));
					if (record.Defindex != 5000) {
						bot.sql.update("UPDATE items SET stock = stock + 1, `in` = `in` + 1 WHERE schemaid = '" + record.Defindex + "'");
					} else {
						scrapReveived++;
					}
				}
				foreach (int itemid in reservedGiven) {
					bot.sql.update("DELETE FROM reservation WHERE itemid = '" + itemid + "' && steamid = '" + trade.otherSID.ConvertToUInt64() + "' LIMIT 1");
					bot.sql.update("UPDATE items SET stock = stock - 1, `out` = `out` + 1 WHERE schemaid = '" + itemid + "'");
				}
				bot.sql.update("UPDATE bots SET scrap = scrap + " + (scrapReveived - scrapGiven) + ", items = items + " + ((trade.OtherTrade.Count - scrapReveived) - reservedGiven.Count) + " WHERE botid = '" + bot.getBotId() + "'");
				//}
				OnFinished(true);
			} else {
				Util.printConsole("[TradeSystem] Poo! Trade might of Failed :C Well, resetting anyway...", ConsoleColor.Red);
				OnFinished(false);
			}
		}

		public void OnFinished(bool success) {
			bot.queueHandler.tradeEnded();
			bot.CurrentTrade = null;
			bot.sql.update("UPDATE bots SET trades = trades + 1 WHERE botid = '" + bot.getBotId() + "'");
			if (!success) {
				bot.sql.update("INSERT INTO tradelog (steamid, botid, scrap, items, success) VALUES ('" + trade.otherSID.ConvertToUInt64() + "', '" + bot.getBotId() + "', '0', '0', '0')");
			} else {
				bot.sql.update("INSERT INTO tradelog (steamid, botid, scrap, items, success) VALUES ('" + trade.otherSID.ConvertToUInt64() + "', '" + bot.getBotId() + "', '" + scrapGiven + "', '" + reservedGiven.Count + "', '1')");
			}
		}

		public override void OnUserSetReadyState(bool ready) {
			if (ready) {
				if (scrapMode || (check && onlyApplicable)) {
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
			if (message == "/scrap") {
				scrapMode = true;
				foreach (ulong child in trade.MyTrade) {
					trade.removeItem(child);
				}
			}
		}

		public override void OnNewVersion() {
			if (!scrapMode) {
				respond();
			}
		}

		private ulong getU(JValue v) {
			return ((JValue) v).ToObject<ulong>();
		}

		private int[] applicableItems() {
			int[] items = { 0, 0 };
			onlyApplicable = true;
			string chat = "";
			foreach (ulong child in trade.OtherTrade) {
				dynamic item = trade.OtherItems.rgInventory[child.ToString()];
				dynamic itemInfo = trade.OtherItems.rgDescriptions[(string) (item.classid + "_" + item.instanceid)];

				Inventory.Item record = trade.OtherInventory.GetItem(getU(item.id));
				int itemid = record.Defindex;
				bool itemok = (!record.IsNotCraftable && record.Quality == 6);

				List<object[]> myReader = bot.sql.query("SELECT '' FROM items WHERE schemaid='" + itemid + "'");

				if (myReader.Count > 0 && itemok) {
					items[0]++;
				} else if (itemok && record.Defindex == 5000) {
					items[1]++;
				} else {
					onlyApplicable = false;
					chat += "Item '" + itemInfo.name + " (" + itemid + ")' not accepted\n";
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

				List<int> reserved = bot.queueHandler.getReservedItems();

				foreach (ulong child in trade.MyTrade) {
					Inventory.Item item = trade.MyInventory.GetItem(getU(trade.MyItems.rgInventory[child.ToString()].id));
					if (item.Defindex != 5000) {
						if (reserved.Contains(item.Defindex)) {
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
					if (reserved.Contains(item.Defindex)) {
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
