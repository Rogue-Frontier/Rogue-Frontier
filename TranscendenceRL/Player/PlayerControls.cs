using Common;
using SadConsole;
using SadConsole.Input;
using System.Linq;
using TranscendenceRL.Screens;
using static SadConsole.Input.Keys;
using static TranscendenceRL.ControlKeys;
using Helper = Common.Main;
namespace TranscendenceRL {
	public enum ControlKeys {
		Thrust,
		TurnRight,
		TurnLeft,
		Brake,
		Autopilot,
		Dock,
		TargetFriendly,
		ClearTarget,
		ShipMenu,
		TargetEnemy,
		Powers,
		NextWeapon,
		FirePrimary,
		AutoAim
    }
    public class PlayerControls {
		PlayerShip playerShip;
		PlayerMain playerMain;
		PowerMenu powerMenu;
		PauseMenu pauseMenu;

		Console sceneContainer;
		public PlayerControls(PlayerShip playerShip, PlayerMain console, PowerMenu powerMenu, PauseMenu pauseMenu, Console sceneContainer) {
			this.playerShip = playerShip;
			this.playerMain = console;
			this.powerMenu = powerMenu;
			this.pauseMenu = pauseMenu;
			this.sceneContainer = sceneContainer;
        }
		public void ProcessArrows(Keyboard info) {
			var controls = playerShip.player.Settings.controls;
			if (info.IsKeyDown(controls[Thrust])) {
				playerShip.SetThrusting();
			}
			if (info.IsKeyDown(controls[TurnLeft])) {
				playerShip.SetRotating(Rotating.CCW);
			}
			if (info.IsKeyDown(controls[TurnRight])) {
				playerShip.SetRotating(Rotating.CW);
			}
			if (info.IsKeyDown(controls[Brake])) {
				playerShip.SetDecelerating();
			}
		}
		public void ProcessTargeting(Keyboard info) {
			var controls = playerShip.player.Settings.controls;
			if (info.IsKeyPressed(controls[TargetFriendly])) {
				playerShip.NextTargetFriendly();
			}
			if (info.IsKeyPressed(controls[ClearTarget])) {
				if (playerShip.targetIndex > -1) {
					playerShip.ClearTarget();
				}
			}
			if (info.IsKeyPressed(controls[TargetEnemy])) {
				playerShip.NextTargetEnemy();
			}
			if (info.IsKeyPressed(controls[NextWeapon])) {
				playerShip.NextWeapon();
			}
			if (info.IsKeyDown(controls[FirePrimary])) {
				playerShip.SetFiringPrimary();
			}
			if (info.IsKeyDown(controls[AutoAim])) {
				if (playerShip.GetTarget(out SpaceObject target) && playerShip.GetPrimary(out Weapon w)) {
					playerShip.SetRotatingToFace(Helper.CalcFireAngle(target.Position - playerShip.Position, target.Velocity - playerShip.Velocity, w.missileSpeed, out _));
				}
			}
		}
        public void ProcessKeyboard(Keyboard info) {
			var controls = playerShip.player.Settings.controls;
			//Move the player
			ProcessArrows(info);
			ProcessTargeting(info);

			if (info.KeysDown.Select(d => d.Key).Intersect<Keys>(new Keys[] { Keys.LeftControl, Keys.LeftShift, Keys.Enter }).Count() == 3) {
				playerShip.Destroy(playerShip);
			}
			if(info.IsKeyPressed(controls[Autopilot])) {
				playerShip.AddMessage(new InfoMessage($"Autopilot {(playerShip.autopilot ? "disengaged" : "engaged")}"));
				playerShip.autopilot = !playerShip.autopilot;
            }
			if (info.IsKeyPressed(controls[Dock])) {
				if (playerShip.Dock != null) {
					if (playerShip.Dock.docked) {
						playerShip.AddMessage(new InfoMessage("Undocked"));
					} else {
						playerShip.AddMessage(new InfoMessage("Docking sequence canceled"));
					}

					playerShip.Dock = null;
				} else {
					Dockable dest = null;
					if(playerShip.GetTarget(out var t) && (playerShip.Position - t.Position).Magnitude < 8 && t is Dockable d) {
						dest = d;
                    }
					dest = dest ?? playerShip.World.entities.GetAll(p => (playerShip.Position - p).Magnitude < 8).OfType<Dockable>().OrderBy(p => (p.Position - playerShip.Position).Magnitude).FirstOrDefault();
					if (dest != null) {
						playerShip.AddMessage(new InfoMessage("Docking sequence engaged"));
						playerShip.Dock = new Docking(dest);
					}

				}
			}
			if(info.IsKeyPressed(Escape)) {
				pauseMenu.IsVisible = true;
			}
			if (info.IsKeyPressed(controls[ShipMenu])) {
				sceneContainer?.Children.Add(new SceneScan(new ShipScreen(playerMain, playerShip)) { IsFocused = true });
			}
			if (info.IsKeyPressed(controls[Powers])) {
				if(powerMenu != null)
					powerMenu.IsVisible = true;
			}
			
			if (info.IsKeyPressed(C)) {
				if(info.IsKeyDown(LeftShift)) {
					playerShip.Destroy(playerShip);
                } else {
					playerShip.Damage(playerShip, playerShip.Ship.DamageSystem.GetHP() - 5);
				}
			}
			if (info.IsKeyPressed(V)) {
				playerShip.Ship.ControlHijack = new ControlHijack() { ticksLeft = 90, thrustMode = HijackMode.FORCE_ON };
			}
		}
    }
}
