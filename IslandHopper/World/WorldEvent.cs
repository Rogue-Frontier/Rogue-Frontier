using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	public interface WorldEvent {
		ColoredString Self { get; }
		ColoredString Seen { get; }
		ColoredString Heard { get; }
		int ScreenTime { get; set; }
	}
	public class SelfEvent : WorldEvent {
        public int Count;
		public int ScreenTime { get; set; } = 90;
        public ColoredString Self { get; }
		public ColoredString Seen => Self;
		public ColoredString Heard => Self;
		public SelfEvent(ColoredString Self) {
            Count = 1;
            this.Self = Self;
			ScreenTime = 90;
		}
	}

}
