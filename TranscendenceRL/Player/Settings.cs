using Newtonsoft.Json;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Text;
using static SadConsole.Input.Keys;
using static TranscendenceRL.ControlKeys;
namespace TranscendenceRL {
    public class Settings {
		//Remember to update whenever we load game
		public Dictionary<ControlKeys, Keys> controls;
		[JsonIgnore]
		public static Settings standard => new Settings() {
			controls = new Dictionary<ControlKeys, Keys>() {
				{ Thrust, Up },
				{ TurnRight, Right },
				{ TurnLeft, Left },
				{ Brake, Down },
				{ Autopilot, A },
				{ Dock, D },
				{ TargetFriendly, F },
				{ ClearTarget, R },
				{ ShipMenu, S },
				{ TargetEnemy, T },
				{ Powers, P },
				{ NextWeapon, W },
				{ FirePrimary, X },
				{ AutoAim, Z }
			}
		};
        public Settings() {
            controls = new Dictionary<ControlKeys, Keys>();
        }

    }
}
