using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamBot.util {
	class DualMInt {
		private MutableInt int1;
		private MutableInt int2;

		public DualMInt(uint _1, uint _2) {
			int1 = new MutableInt(_1);
			int2 = new MutableInt(_2);
		}

		public MutableInt get1() {
			return int1;
		}

		public MutableInt get2() {
			return int2;
		}
	}
}
