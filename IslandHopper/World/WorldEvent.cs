using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IslandHopper {
	interface WorldEvent {
		string Seen { get; }
		string Heard { get; }
	}
}
