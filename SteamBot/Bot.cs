using System;
using System.Text;
using System.Net;
using System.Linq;
using System.Threading;
using SteamKit2;
using SteamBot.util;
using SteamBot.scrap;
using SteamBot.command;
using System.Collections.Generic;

namespace SteamBot {
	public class Bot {
		public bool IsLoggedIn = false;

		public string DisplayName {
			get;
			private set;
		}
		public ulong[] Admins;

		public SteamFriends SteamFriends;
		public SteamClient SteamClient;
		public SteamTrading SteamTrade;
		public SteamUser SteamUser;

		public QueueHandler queueHandler;

		public Trade CurrentTrade;
		public Trade.TradeListener TradeListener;
		public Trade.TradeListener TradeListenerInternal;
		public Trade.TradeListener TradeListenerAdmin;

		string Username;
		string Password;
		public string apiKey;
		int id;
		string sessionId;
		string token;

		public List<int> toTrade = new List<int>();
		public Sql sql;

		public Dictionary<string, string> responses = new Dictionary<string,string>();

		public Bot(Configuration.BotInfo config, string apiKey, bool debug = false) {
			sql = new Sql();

			Username = config.Username;
			Password = config.Password;
			DisplayName = config.DisplayName;
			Admins = config.Admins;
			id = config.Id;
			this.apiKey = apiKey;

			TradeListener = new ScrapTrade(this);
			TradeListenerInternal = new ExchangeTrade(this);
			TradeListenerAdmin = new AdminTrade(this);

			List<object[]> result = sql.query("SELECT text, response FROM responses");
			foreach (object[] row in result) {
				responses.Add(((string) row[0]).ToLower(), (string) row[1]);
			}

			// Hacking around https
			ServicePointManager.ServerCertificateValidationCallback += SteamWeb.ValidateRemoteCertificate;

			SteamClient = new SteamClient();
			SteamTrade = SteamClient.GetHandler<SteamTrading>();
			SteamUser = SteamClient.GetHandler<SteamUser>();
			SteamFriends = SteamClient.GetHandler<SteamFriends>();
			queueHandler = new QueueHandler(this);

			SteamClient.Connect();

			while (true) {
				Update();
			}
		}

		public int getBotId() {
			return id;
		}

		public void Update() {
			while (true) {
				Thread.Sleep(800);
				CallbackMsg msg = SteamClient.GetCallback(true);

				if (msg == null)
					break;

				HandleSteamMessage(msg);
			}


			if (CurrentTrade != null) {
				Thread.Sleep(800);
				try {
					CurrentTrade.Poll();
				} catch (Exception e) {
					Console.Write("Error polling the trade: ");
					Console.WriteLine(e);
				}
			}
		}

		void HandleSteamMessage(CallbackMsg msg) {
			#region Login
			msg.Handle<SteamClient.ConnectedCallback>(callback => {
				Util.printConsole("Connection Status " + callback.Result, this, ConsoleColor.Magenta);

				if (callback.Result == EResult.OK) {
					SteamUser.LogOn(new SteamUser.LogOnDetails {
						Username = Username,
						Password = Password
					});
				} else {
					Util.printConsole("Failed to Connect to the steam community", this, ConsoleColor.Red);
					SteamClient.Connect();
				}

			});

			msg.Handle<SteamUser.LoggedOnCallback>(callback => {
				if (callback.Result != EResult.OK) {
					Util.printConsole("Login Failure: " + callback.Result, this, ConsoleColor.Red);
				}
			});

			msg.Handle<SteamUser.LoginKeyCallback>(callback => {
				while (true) {
					if (Authenticate(callback)) {
						Util.printConsole("Authenticated.", this, ConsoleColor.Magenta);
						break;
					} else {
						Util.printConsole("Retrying auth...", this, ConsoleColor.Red);
						Thread.Sleep(2000);
					}
				}

				SteamFriends.SetPersonaName(DisplayName);
				SteamFriends.SetPersonaState(EPersonaState.LookingToTrade);

				foreach (SteamID bot in Program.bots) {
					if (SteamFriends.GetFriendRelationship(bot) != EFriendRelationship.Friend) {
						SteamFriends.AddFriend(bot);
					}
				}
				Program.bots.Add(SteamClient.SteamID);

				IsLoggedIn = true;
				queueHandler.canTrade = true;
			});
			#endregion

			#region Friends
			msg.Handle<SteamFriends.PersonaStateCallback>(callback => {
				if (callback.FriendID == SteamUser.SteamID)
					return;

				EFriendRelationship relationship = SteamFriends.GetFriendRelationship(callback.FriendID);
				if (relationship == EFriendRelationship.Friend) {
					queueHandler.acceptedRequest(callback.FriendID);
				} else if (relationship == EFriendRelationship.PendingInvitee) {
					Util.printConsole("Friend Request Pending: " + callback.FriendID + "(" + SteamFriends.GetFriendPersonaName(callback.FriendID) + ")", this, ConsoleColor.DarkCyan, true);
					if (Program.bots.Contains(callback.FriendID) || Admins.Contains(callback.FriendID)) {
						SteamFriends.AddFriend(callback.FriendID);
					}
					//steamFriends.AddFriend(callback.FriendID);
				}
			});

			msg.Handle<SteamFriends.FriendMsgCallback>(callback => {
				//Type (emote or chat)
				EChatEntryType type = callback.EntryType;

				if (type == EChatEntryType.ChatMsg) {
					string response = "";
					if (responses.ContainsKey(callback.Message.ToLower())) {
						response = responses[callback.Message.ToLower()];
					} else {
						string[] args2 = callback.Message.Split(' ');
						string text = Util.removeArg0(callback.Message);
						string[] pArgs = text.Split(' ');

						response = Extensions.getCommand(args2[0].ToLower()).call(callback.Sender, pArgs, text, this);
					}
					SteamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, response);
				}

			});
			#endregion

			#region Trading
			msg.Handle<SteamTrading.SessionStartCallback>(call => {
				Trade.TradeListener listener = TradeListener;
				if (Admins.Contains(call.OtherClient)) {
					listener = TradeListenerAdmin;
				} else if (Program.bots.Contains(call.OtherClient)) {
					listener = TradeListenerInternal;
				}
				try {
					CurrentTrade = new Trade(SteamUser.SteamID, call.OtherClient, sessionId, token, apiKey, listener);
				} catch (Exception e) {
					Util.printConsole(e.Message, this, ConsoleColor.White, true);
					CurrentTrade = null;
					Thread.Sleep(5000);
					SteamTrade.Trade(call.OtherClient);
				}
			});

			msg.Handle<SteamTrading.TradeProposedCallback>(thing => {
				Util.printConsole("Trade Proposed Callback. Other: " + thing.OtherClient, this, ConsoleColor.White, true);
				if (Program.bots.Contains(thing.OtherClient) && queueHandler.needItemsBool) {
					SteamTrade.RespondToTrade(thing.TradeID, true);
				} else {
					SteamTrade.RespondToTrade(thing.TradeID, false);
					SteamFriends.SendChatMessage(thing.OtherClient, EChatEntryType.ChatMsg, "Please wait your turn, thanks :)");
				}
			});

			msg.Handle<SteamTrading.TradeResultCallback>(thing => {
				Util.printConsole("Trade Status: " + thing.Response, this, ConsoleColor.Magenta, true);

				if (thing.Response == EEconTradeResponse.Accepted) {
					if (!Program.bots.Contains(thing.OtherClient)) {
						Util.printConsole("Trade accepted!", this, ConsoleColor.Yellow);
					}
				} else if (thing.Response == EEconTradeResponse.TargetAlreadyTrading) {
					Util.printConsole("User is already trading!", this, ConsoleColor.Magenta);
					SteamFriends.SendChatMessage(thing.OtherClient, EChatEntryType.ChatMsg, "You're at the top of the trade queue, but are in trade. We don't have all day :c");
					Thread.Sleep(10000);
					queueHandler.ignoredTrade(thing.OtherClient);
				} else if (thing.Response == EEconTradeResponse.Declined) {
					Util.printConsole("User declined trade???", this, ConsoleColor.Magenta);
					Thread.Sleep(5000);
					queueHandler.ignoredTrade(thing.OtherClient);
				} else {
					Util.printConsole("Assume User Ignored Trade Request...", this, ConsoleColor.Magenta);
					queueHandler.ignoredTrade(thing.OtherClient);
				}
			});
			#endregion

			#region Disconnect
			msg.Handle<SteamUser.LoggedOffCallback>(callback => {
				Util.printConsole("Told to log off by server (" + callback.Result + "), attemping to reconnect", this, ConsoleColor.Magenta);
				SteamClient.Connect();
			});

			msg.Handle<SteamClient.DisconnectedCallback>(callback => {
				IsLoggedIn = false;
				if (CurrentTrade != null) {
					CurrentTrade = null;
				}
				Util.printConsole("Disconnected from Steam Network, attemping to reconnect", this, ConsoleColor.Magenta);
				SteamClient.Connect();
			});
			#endregion
		}

		// Authenticate. This does the same as SteamWeb.DoLogin(),
		// but without contacting the Steam Website.
		// Should this one doesnt work anymore, use SteamWeb.DoLogin().
		bool Authenticate(SteamUser.LoginKeyCallback callback) {
			sessionId = WebHelpers.EncodeBase64(callback.UniqueID.ToString());

			Util.printConsole("Got login key, performing web auth...", this, ConsoleColor.Magenta, true);

			using (dynamic userAuth = WebAPI.GetInterface("ISteamUserAuth")) {
				// generate an AES session key
				var sessionKey = CryptoHelper.GenerateRandomBlock(32);

				// rsa encrypt it with the public key for the universe we're on
				byte[] cryptedSessionKey = null;
				using (var rsa = new RSACrypto(KeyDictionary.GetPublicKey(SteamClient.ConnectedUniverse))) {
					cryptedSessionKey = rsa.Encrypt(sessionKey);
				}

				var loginKey = new byte[20];
				Array.Copy(Encoding.ASCII.GetBytes(callback.LoginKey), loginKey, callback.LoginKey.Length);

				// aes encrypt the loginkey with our session key
				byte[] cryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);

				KeyValue authResult;

				try {
					authResult = userAuth.AuthenticateUser(
						steamid:SteamClient.SteamID.ConvertToUInt64(),
						sessionkey:WebHelpers.UrlEncode(cryptedSessionKey),
						encrypted_loginkey:WebHelpers.UrlEncode(cryptedLoginKey),
						method:"POST"
					);
				} catch (Exception) {
					return false;
				}

				token = authResult["token"].AsString();

				return true;
			}
		}
	}
}