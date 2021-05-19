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
				{Gate, G },
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
		public string GetString() {
			const int indent = -16;
			return @$"[Controls]

{$"[Escape]",-16}Pause

{$"[{controls[Thrust]}]",indent}Thrust
{$"[{controls[TurnLeft]}]",indent}Turn left
{$"[{controls[TurnRight]}]",indent}Turn right
{$"[{controls[Brake]}]",indent}Brake

{$"[{controls[Powers]}]",indent}Power menu

{$"[{controls[Autopilot]}]",indent}Autopilot
{$"[{controls[ShipMenu]}]",indent}Ship Screen
{$"[{controls[Dock]}]",indent}Dock

{$"[Minus]",indent}Megamap zoom out
{$"[Plus]",indent}Megamap zoom in

{$"[{controls[ClearTarget]}]",indent}Clear target
{$"[{controls[TargetEnemy]}]",indent}Target next enemy
{$"[{controls[TargetFriendly]}]",indent}Target next friendly

{$"[{controls[NextWeapon]}]",indent }Next primary weapon
{$"[{controls[AutoAim]}]",indent    }Turn to aim target
{$"[{controls[FirePrimary]}]",indent}Fire primary weapon

{$"[Left Click]",indent     }Next primary weapon
{$"[Right Click]",indent    }Thrust
{$"[Middle Click]",indent   }Target nearest
{$"[Mouse Wheel]",indent    }Select primary weapon".Replace("\r", null);
		}

    }
}
