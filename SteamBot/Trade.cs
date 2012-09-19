using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;
using SteamKit2;
using SteamBot.util;
using Newtonsoft.Json.Linq;

namespace SteamBot {
	public class Trade {
		#region Static
		// Static properties
		public static string SteamCommunityDomain = "steamcommunity.com";
		public static string SteamTradeUrl = "http://steamcommunity.com/trade/{0}/";

		protected static void PrintConsole (String line, ConsoleColor color = ConsoleColor.White)
		{
			Console.ForegroundColor = color;
			Console.WriteLine (line);
			Console.ForegroundColor = ConsoleColor.White;
		}
		#endregion

		#region Properties
		public SteamID meSID;
		public SteamID otherSID;

		// Generic Trade info
		public bool MeReady = false;
		public bool OtherReady = false;

		bool tradeStarted = false;
		public DateTime TradeStart;
		public DateTime LastAction;

		int lastEvent = 0;
		public string pollLock2 = "";
		private byte exc = 0;

		public int MaximumTradeTime = 300;
		public int MaximumActionGap = 30;

		// Items
		public HashSet<ulong> MyTrade = new HashSet<ulong>();
		public HashSet<ulong> OtherTrade = new HashSet<ulong>();
		public HashSet<ulong>[] trades;
		public HashSet<ulong> OMyTrade = new HashSet<ulong>();
		public HashSet<ulong> OOtherTrade = new HashSet<ulong>();
		public HashSet<ulong>[] Otrades;

		public Inventory OtherInventory;
		public Inventory MyInventory;
		public Inventory[] inventories;

		// Internal properties needed for Steam API.
		protected string baseTradeURL;
		protected string steamLogin;
		protected string sessionId;
		protected string apiKey;
		protected int version = 1;
		protected int logpos;
		protected int numEvents;

		public dynamic OtherItems;
		public dynamic MyItems;
		#endregion

		#region Events
		public delegate void ErrorHandler (int eid);
		public event ErrorHandler OnError;

		public delegate void TimeoutHandler ();
		public event TimeoutHandler OnTimeout;

		public delegate void SuccessfulInit ();
		public event SuccessfulInit OnAfterInit;

		public delegate void UserAddItemHandler(ItemInfo schemaItem, Inventory.Item inventoryItem);
		public event UserAddItemHandler OnUserAddItem;

		public delegate void UserRemoveItemHandler(ItemInfo schemaItem, Inventory.Item inventoryItem);
		public event UserAddItemHandler OnUserRemoveItem;

		public delegate void MessageHandler (string msg);
		public event MessageHandler OnMessage;

		public delegate void UserSetReadyStateHandler (bool ready);
		public event UserSetReadyStateHandler OnUserSetReady;

		public delegate void UserAcceptHandler ();
		public event UserAcceptHandler OnUserAccept;

		public delegate void NewVersionHandler();
		public event NewVersionHandler OnNewVersion;

		public delegate void CompleteHandler();
		public event CompleteHandler OnComplete;
		#endregion

		public Trade (SteamID me, SteamID other, string sessionId, string token, string apiKey, TradeListener listener = null) {
			meSID = me;
			otherSID = other;

			trades = new HashSet<ulong>[] { MyTrade, OtherTrade };
			Otrades = new HashSet<ulong>[] { OMyTrade, OOtherTrade };

			this.sessionId = sessionId;
			steamLogin = token;
			this.apiKey = apiKey;

			AddListener (listener);

			baseTradeURL = String.Format (SteamTradeUrl, otherSID.ConvertToUInt64 ());

			// try to poll for the first time
			try {
				Poll ();
			} catch (Exception) {
				PrintConsole ("Failed to connect to Steam!", ConsoleColor.Red);

				if (OnError != null)
					OnError(0);
			}

			try {
				SendMessage("Fetching data");

				// fetch the other player's inventory
				OtherItems = GetInventory (otherSID);
				if (OtherItems == null || OtherItems.success != "true") {
					throw new Exception ("Could not fetch other player's inventory via Trading!");
				}
				
				// fetch our inventory
				MyItems = GetInventory (meSID);
				if (MyItems == null || MyItems.success != "true") {
					throw new Exception ("Could not fetch own inventory via Trading!");
				}
				
				// fetch other player's inventory from the Steam API.
				OtherInventory = Inventory.FetchInventory(otherSID.ConvertToUInt64(), apiKey);
				if (OtherInventory == null) {
					throw new Exception ("Could not fetch other player's inventory via Steam API!");
				}
				
				// fetch our inventory from the Steam API.
				MyInventory = Inventory.FetchInventory(meSID.ConvertToUInt64(), apiKey);
				if (MyInventory == null) {
					throw new Exception ("Could not fetch own inventory via Steam API!");
				}

				SendMessage("Ready");
				inventories = new Inventory[] { MyInventory, OtherInventory };

				if (OnAfterInit != null)
					OnAfterInit();

			} catch (Exception e) {
				if (OnError != null)
					OnError(3);
				Console.WriteLine (e);
			}

		}

		public void Poll () {
			lock (pollLock2) {
				if (!tradeStarted) {
					tradeStarted = true;
					TradeStart = DateTime.Now;
					LastAction = DateTime.Now;
				}


				StatusObj status = GetStatus();
				bool isBot = true;

				if (status != null && status.events != null) {
					if (lastEvent < status.events.Length) {
						for (; lastEvent < status.events.Length; lastEvent++) {
							TradeEvent evt = status.events[lastEvent];
							isBot = evt.steamid != otherSID.ConvertToUInt64().ToString();

							switch (evt.action) {
								case 0:
									trades[isBot ? 0 : 1].Add(evt.assetid);
									Inventory.Item item = inventories[isBot ? 0 : 1].GetItem(evt.assetid);
									Otrades[isBot ? 0 : 1].Add(item.OriginalId);
									if (!isBot) {
										ItemInfo schemaItem = Util.getItemInfo(item.Defindex);
										OnUserAddItem(schemaItem, item);
									}
									break;
								case 1:
									trades[isBot ? 0 : 1].Remove(evt.assetid);
									Inventory.Item item2 = inventories[isBot ? 0 : 1].GetItem(evt.assetid);
									Otrades[isBot ? 0 : 1].Add(item2.OriginalId);
									if (!isBot) {
										ItemInfo schemaItem = Util.getItemInfo(item2.Defindex);
										OnUserRemoveItem(schemaItem, item2);
									}
									break;
								case 2:
									if (!isBot) {
										OtherReady = true;
										OnUserSetReady(true);
									} else {
										MeReady = true;
									}
									break;
								case 3:
									if (!isBot) {
										OtherReady = false;
										OnUserSetReady(false);
									} else {
										MeReady = false;
									}
									break;
								case 4:
									if (!isBot) {
										OnUserAccept();
									}
									break;
								case 7:
									if (!isBot) {
										OnMessage(evt.text);
									}
									break;
								default:
									PrintConsole("Unknown Event ID: " + evt.action, ConsoleColor.Red);
									break;
							}

							if (!isBot)
								LastAction = DateTime.Now;
						}

					} else {
						// check if the user is AFK
						var now = DateTime.Now;

						DateTime actionTimeout = LastAction.AddSeconds(MaximumActionGap);
						int untilActionTimeout = (int) Math.Round((actionTimeout - now).TotalSeconds);

						DateTime tradeTimeout = TradeStart.AddSeconds(MaximumTradeTime);
						int untilTradeTimeout = (int) Math.Round((tradeTimeout - now).TotalSeconds);

						if (untilActionTimeout <= 0 || untilTradeTimeout <= 0) {
							if (OnTimeout != null)
								OnTimeout();
						} else if (untilActionTimeout <= 15 && untilActionTimeout % 5 == 0) {
							SendMessage("Are You AFK? The trade will be canceled in " + untilActionTimeout + " seconds if you don't do something.");
						}
					}
				} else {
					if (status.trade_status == 3) {
						//Other user cancelled
						OnError(2);
					} else if (status.trade_status == 4) {
						//Other user timed out, unlikely as we have a built-in timeout
						OnError(4);
					} else if (status.trade_status == 5) {
						//Trade failed
						OnError(5);
					} else if (status.trade_status == 1) {
						//Success
						OnComplete();
					} else if (exc++ > 3) {
						//More than 3 exceptions, something went wrong :/
						OnError(1);
					}
				}

				// Update Local Variables
				if (status.them != null) {
					OtherReady = status.them.ready == 1 ? true : false;
					MeReady = status.me.ready == 1 ? true : false;
				}


				// Update version
				if (status.newversion) {
					version = status.version;
					OnNewVersion();
				}

				if (status.logpos != 0) {
					logpos = status.logpos;
				}
			}
		}

		#region Trade interaction
		public string SendMessage (string msg)
		{
			var data = new NameValueCollection ();
			data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
			data.Add ("message", msg);
			data.Add ("logpos", "" + logpos);
			data.Add ("version", "" + version);
			return Fetch (baseTradeURL + "chat", "POST", data);
		}

		public bool AddItemByDefindex (int defindex, int slot)
		{
			List<Inventory.Item> items = MyInventory.GetItemsByDefindex (defindex);
			if (items[0] != null) {
				addItem (items[0].Id, slot);
				return true;
			}
			return false;
		}

		public void addItem (ulong itemid, int slot)
		{
			var data = new NameValueCollection ();
			data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
			data.Add ("appid", "440");
			data.Add ("contextid", "2");
			data.Add ("itemid", "" + itemid);
			data.Add ("slot", "" + slot);
			Fetch (baseTradeURL + "additem", "POST", data);
		}

		public void removeItem (ulong itemid)
		{
			var data = new NameValueCollection ();
			data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
			data.Add ("appid", "440");
			data.Add ("contextid", "2");
			data.Add ("itemid", "" + itemid);
			Fetch (baseTradeURL + "removeitem", "POST", data);
		}

		public void SetReady (bool ready)
		{
			var data = new NameValueCollection ();
			data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
			data.Add ("ready", ready ? "true" : "false");
			data.Add ("version", "" + version);
			Fetch (baseTradeURL + "toggleready", "POST", data);
		}

		public dynamic AcceptTrade ()
		{
			var data = new NameValueCollection ();
			data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
			data.Add ("version", "" + version);
			string response = Fetch (baseTradeURL + "confirm", "POST", data);

			return JsonConvert.DeserializeObject (response);
		}
		#endregion

		public void AddListener (TradeListener listener)
		{
			OnError += listener.OnError;
			OnTimeout += listener.OnTimeout;
			OnAfterInit += listener.OnAfterInit;
			OnUserAddItem += listener.OnUserAddItem;
			OnUserRemoveItem += listener.OnUserRemoveItem;
			OnMessage += listener.OnMessage;
			OnUserSetReady += listener.OnUserSetReadyState;
			OnUserAccept += listener.OnUserAccept;
			OnNewVersion += listener.OnNewVersion;
			OnComplete += listener.OnComplete;
			listener.trade = this;
		}

		protected StatusObj GetStatus ()
		{
			var data = new NameValueCollection ();
			data.Add ("sessionid", Uri.UnescapeDataString (sessionId));
			data.Add ("logpos", "" + logpos);
			data.Add ("version", "" + version);

			string response = Fetch (baseTradeURL + "tradestatus", "POST", data);

			return JsonConvert.DeserializeObject<StatusObj> (response);
		}

		protected dynamic GetInventory (SteamID steamid)
		{
			string url = String.Format (
				"http://steamcommunity.com/profiles/{0}/inventory/json/440/2/?trading=1",
		        steamid.ConvertToUInt64 ()
			);

			try {
				string response = Fetch (url, "GET", null, false);
				return JsonConvert.DeserializeObject (response);
			} catch (Exception) {
				return JsonConvert.DeserializeObject ("{\"success\":\"false\"}");
			}
		}

		protected string Fetch (string url, string method, NameValueCollection data = null, bool sendLoginData = true)
		{
			var cookies = new CookieContainer();
			if (sendLoginData) {
				cookies.Add (new Cookie ("sessionid", sessionId, String.Empty, SteamCommunityDomain));
				cookies.Add (new Cookie ("steamLogin", steamLogin, String.Empty, SteamCommunityDomain));
			}

			return SteamWeb.Fetch(url, method, data, cookies);
		}

		public abstract class TradeListener
		{
			public Trade trade;

			public abstract void OnError (int eid);

			public abstract void OnTimeout ();

			public abstract void OnAfterInit ();

			public abstract void OnUserAddItem(ItemInfo schemaItem, Inventory.Item inventoryItem);

			public abstract void OnUserRemoveItem(ItemInfo schemaItem, Inventory.Item inventoryItem);

			public abstract void OnMessage (string msg);

			public abstract void OnUserSetReadyState (bool ready);

			public abstract void OnUserAccept ();

			public abstract void OnNewVersion();

			public abstract void OnComplete();

			protected ulong getU(JValue v) {
				return ((JValue) v).ToObject<ulong>();
			}
		}

		#region JSON classes
		protected class StatusObj
		{
			public string error { get; set; }

			public bool newversion { get; set; }

			public bool success { get; set; }

			public long trade_status { get; set; }

			public int version { get; set; }

			public int logpos { get; set; }

			public TradeUserObj me { get; set; }

			public TradeUserObj them { get; set; }

			public TradeEvent[] events { get; set; }
		}

		protected class TradeEvent
		{
			public string steamid { get; set; }

			public int action { get; set; }

			public ulong timestamp { get; set; }

			public int appid { get; set; }

			public string text { get; set; }

			public int contextid { get; set; }

			public ulong assetid { get; set; }
		}

		protected class TradeUserObj
		{
			public int ready { get; set; }

			public int confirmed { get; set; }

			public int sec_since_touch { get; set; }
		}
		#endregion
	}
}

