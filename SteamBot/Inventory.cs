using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SteamBot
{
	public class Inventory
	{
		public static Inventory FetchInventory (ulong steamId, string apiKey)
		{
			int tries = 0;
			var url = "http://api.steampowered.com/IEconItems_440/GetPlayerItems/v0001/?key=" + apiKey + "&steamid=" + steamId;
			InventoryResponse result = null;
			while (++tries < 3 && result == null) {
				try {
					string response = SteamWeb.Fetch(url, "GET", null, null, false);
					result = JsonConvert.DeserializeObject<InventoryResponse>(response);
				} catch (Exception)  {}
			}
			if (result == null) {
				throw new Exception("Couldn't fetch inventory");
			}
			return new Inventory(result.result);
		}

		public uint NumSlots { get; set; }
		public Item[] Items { get; set; }

		protected Inventory (InventoryResult apiInventory)
		{
			NumSlots = apiInventory.num_backpack_slots;
			Items = apiInventory.items;
		}

		public Item GetItem (ulong id)
		{
			foreach (Item item in Items) {
				if (item.Id == id) {
					return item;
				}
			}
			return null;
		}

		public List<Item> GetItemsByDefindex (int defindex)
		{
			var items = new List<Item> ();
			foreach (Item item in Items) {
				if (item.Defindex == defindex) {
					items.Add (item);
				}
			}
			return items;
		}

		public List<int> getItems() {
			var items = new List<int>();
			foreach (Item item in Items) {
				items.Add(item.Defindex);
			}
			return items;
		}

		public List<ulong> getItemIds() {
			var items = new List<ulong>();
			foreach (Item item in Items) {
				items.Add(item.OriginalId);
			}
			return items;
		}

		public class Item
		{
			[JsonProperty("id")]
			public ulong Id { get; set; }

			[JsonProperty("original_id")]
			public ulong OriginalId { get; set; }

			[JsonProperty("defindex")]
			public ushort Defindex { get; set; }

			[JsonProperty("level")]
			public byte Level { get; set; }

			[JsonProperty("quality")]
			public byte Quality { get; set; }

			[JsonProperty("pos")]
			public int Position { get; set; }

			[JsonProperty("flag_cannot_craft")]
			public bool IsNotCraftable { get; set; }

			[JsonProperty("attributes")]
			public ItemAttribute[] Attributes { get; set; }
		}

		public class ItemAttribute
		{
			[JsonProperty("defindex")]
			public ushort Defindex { get; set; }

			[JsonProperty("value")]
			public string Value { get; set; }
		}

		protected class InventoryResult
		{
			public string status { get; set; }

			public uint num_backpack_slots { get; set; }

			public Item[] items { get; set; }
		}

		protected class InventoryResponse {
			public InventoryResult result;
		}

	}
}

