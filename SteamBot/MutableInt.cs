using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot {
	class MutableInt {
		int value = 1;
	
		public void increment () {
			++value;
		}

		public void decrement() {
			--value;
		}
	
		public int get () {
			return value;
		}

		public int CompareTo(object arg0) {
			if (arg0 is MutableInt) {
				return get() == ((MutableInt) arg0).get() ? 0 : (get() > ((MutableInt) arg0).get() ? -1 : 1);
			} else {
				return 0;
			}
		}
	}
}
