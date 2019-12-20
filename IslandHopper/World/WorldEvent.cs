using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	public interface WorldEvent {
		ColoredString Desc { get; }
		int ScreenTime { get; set; }
	}
	public class InfoEvent : WorldEvent {
		public int ScreenTime { get; set; } = 150;
        public ColoredString Desc { get; }
		public InfoEvent(ColoredString Desc) {
			this.Desc = Desc;
		}
		public InfoEvent(string Desc, Color? foreground = null) {
            this.Desc = new ColoredString(Desc, foreground ?? Color.White, Color.Black);
		}
	}

}
