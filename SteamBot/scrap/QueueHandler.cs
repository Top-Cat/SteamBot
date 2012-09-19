using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using SteamBot;
using MySql.Data.MySqlClient;
using SteamKit2;
using SteamBot.util;

namespace SteamBot.scrap {
	public class QueueHandler {
		private List<dbRow> users = new List<dbRow>();
		private Queue<dbRow> tradeQueue = new Queue<dbRow>();
		public dbRow currentTrader;
		public bool canTrade = false;
		private List<uint> reserved = new List<uint>();
		private Bot bot;

		public static List<Bot> needItems = new List<Bot>();
		public List<uint> neededItems = new List<uint>();
		public bool needItemsBool = false;
		public bool gotItems = false;

		public QueueHandler(Bot bot) {
			this.bot = bot;
			bot.sql.update("UPDATE queue SET queued = '-1' WHERE queued = '" + bot.getBotId() + "'");
			Thread oThread = new Thread(new ThreadStart(this.run));
			oThread.Start();
		}

		private void run() {
			while (true) {
				Thread.Sleep(5000);
				if (users.Count < 5) {
					try {
						List<object[]> reader = bot.sql.query("SELECT Id, steamid FROM queue WHERE (botid = -1 || botid = " + bot.getBotId() + ") && queued = -1 ORDER BY Id LIMIT 1");
						if (reader.Count > 0) {
							uint id = (uint) reader[0][0];
							if (bot.sql.update("UPDATE queue SET queued = '" + bot.getBotId() + "' WHERE Id = '" + id + "' and queued = -1") > 0) {
								SteamID other = new SteamID((UInt64) reader[0][1]);
								if (bot.SteamFriends.GetFriendRelationship(other) == EFriendRelationship.Friend) {
									tradeQueue.Enqueue(new dbRow(id, other));
								} else {
									bot.SteamFriends.AddFriend(other);
									users.Add(new dbRow(id, other));
								}
							}
						}
					} catch (MySqlException e) {
						Util.printConsole(e.Message, bot, ConsoleColor.White);
					}
				}

				List<dbRow> toremove = new List<dbRow>();
				foreach (dbRow row in users) {
					if (row.secondsSince() > 60 * 5) {
						bot.SteamFriends.RemoveFriend(row.getSteamId());
						toremove.Add(row);
						bot.sql.update("DELETE FROM queue WHERE Id = '" + row.getRowId() + "'");
					}
				}
				foreach (dbRow row in toremove) {
					users.Remove(row);
				}

				if (canTrade && (currentTrader == null || needItemsBool)) {
					Bot toRemove = null;
					foreach (Bot bot in needItems) {
						if (bot != this.bot) {
							List<int> MyInventory = Inventory.FetchInventory(this.bot.SteamClient.SteamID, bot.apiKey).getItems();
							List<int> toTrade = new List<int>();
							foreach (int itemid in bot.queueHandler.neededItems) {
								if (MyInventory.Contains(itemid)) {
									toTrade.Add(itemid);
									MyInventory.Remove(itemid);
								}
							}
							foreach (uint itemid in toTrade) {
								bot.queueHandler.neededItems.Remove(itemid);
							}
							if (toTrade.Count > 0) {
								toRemove = bot;
								this.bot.toTrade = toTrade;
								this.bot.SteamTrade.Trade(bot.SteamClient.SteamID);
								break;
							}
						}
					}
					if (toRemove != null) {
						needItems.Remove(toRemove);
					}
				}

				if (gotItems) {
					Util.printConsole("Items acquired, sending trade request", bot, ConsoleColor.Yellow);
					bot.SteamTrade.Trade(currentTrader.getSteamId());
					gotItems = false;
				}

				if (canTrade && tradeQueue.Count > 0 && currentTrader == null) {
					try {
						Util.printConsole("Fetching next user from queue", bot, ConsoleColor.Yellow);
						currentTrader = tradeQueue.Dequeue();

						List<int> MyInventory = Inventory.FetchInventory(bot.SteamClient.SteamID, bot.apiKey).getItems();
						List<object[]> reader = bot.sql.query("SELECT itemid FROM reservation WHERE steamid = '" + currentTrader.getSteamId().ConvertToUInt64() + "'");
						reserved.Clear();
						foreach (object[] row in reader) {
							reserved.Add((uint) row[0]);
						}
						List<uint> reservedTmp = reserved.GetRange(0, reserved.Count);
						foreach (uint itemid in MyInventory) {
							reservedTmp.Remove(itemid);
						}

						if (reservedTmp.Count > 0) {
							Util.printConsole("Need to aquire " + reservedTmp.Count + " items before trading", bot, ConsoleColor.Yellow);
							//We don't have all the items we need... :/
							neededItems = reservedTmp;
							needItems.Add(bot);
							needItemsBool = true;
						} else {
							Util.printConsole("Sending new trade request", bot, ConsoleColor.Yellow);
							bot.SteamTrade.Trade(currentTrader.getSteamId());
						}
					} catch (Exception e) {
						if (e is MySqlException || e is WebException) {
							Util.printConsole(e.Message, bot, ConsoleColor.White, true);
							if (currentTrader != null) {
								tradeQueue.Enqueue(currentTrader);
								currentTrader = null;
							}
						} else {
							throw e;
						}
					}
				}
			}
		}

		public void reQueue() {
			tradeQueue.Enqueue(currentTrader);
			needItemsBool = false;
			neededItems.Clear();
			needItems.Remove(bot);
			currentTrader = null;
		}

		public void tradeEnded() {
			if (currentTrader != null) {
				Util.printConsole("Trade ended", bot, ConsoleColor.Yellow);
				if (!Program.bots.Contains(currentTrader.getSteamId()) && !bot.Admins.Contains(currentTrader.getSteamId())) {
					bot.SteamFriends.RemoveFriend(currentTrader.getSteamId());
				}
				bot.sql.update("DELETE FROM queue WHERE Id = '" + currentTrader.getRowId() + "'");
				currentTrader = null;
			}
		}

		public void acceptedRequest(SteamID steamid) {
			List<dbRow> toremove = new List<dbRow>();
			foreach (dbRow row in users) {
				if (row.getSteamId().Equals(steamid)) {
					toremove.Add(row);
					tradeQueue.Enqueue(row);
				}
			}
			foreach (dbRow row in toremove) {
				users.Remove(row);
			}
		}

		public void ignoredTrade(SteamID steamID) {
			if (currentTrader != null && steamID.Equals(currentTrader.getSteamId())) {
				if (currentTrader.incAttempts() < 3) {
					bot.SteamTrade.Trade(currentTrader.getSteamId());
				} else {
					tradeEnded();
				}
			}
		}

		public List<uint> getReservedItems() {
			return reserved.GetRange(0, reserved.Count);
		}
	}

	public class dbRow {
		private uint rowId;
		private SteamID steamid;
		private byte tradeAttempts = 0;
		private DateTime timeImportant = DateTime.Now;

		public dbRow(uint rowId, SteamID steamid) {
			this.rowId = rowId;
			this.steamid = steamid;
		}

		public int secondsSince() {
			return (int) (DateTime.Now - timeImportant).TotalSeconds;
		}

		public void added() {
			timeImportant = DateTime.Now;
		}

		public uint getRowId() {
			return rowId;
		}

		public SteamID getSteamId() {
			return steamid;
		}

		public byte incAttempts() {
			return ++tradeAttempts;
		}
	}
}
