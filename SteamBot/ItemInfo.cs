using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot {
	public class ItemInfo {
		private String name;
		private String itemClass;
		private Dictionary<int, double> attributes = new Dictionary<int, double>();
	
		public ItemInfo(int itemid, dynamic info) {
			itemClass = info.item_class;
			name = info.item_name;
			if (info.attributes != null) {
				if (info.attributes is Newtonsoft.Json.Linq.JArray) {
					foreach (dynamic attr in info.attributes) {
						addAttribute(attr);
					}
				} else {
					foreach (dynamic attr in info.attributes) {
						addAttribute(info.attributes[((Newtonsoft.Json.Linq.JProperty) attr).Name]);
					}
				}
			}
		}
	
		private void addAttribute(dynamic obj) {
			double val = obj["value"];
			int index = obj.defindex;
			attributes.Add(index, val);
		}

		public String getName() {
			if (itemClass.Equals("supply_crate", StringComparison.OrdinalIgnoreCase)) {
				return name + " #" + ((int) attributes[187]);
			}
			return name;
		}
	}
}
