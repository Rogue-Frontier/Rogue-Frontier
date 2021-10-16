using Common;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
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
		Gate,
		TargetEnemy,
		Powers,
		NextWeapon,
		FirePrimary,
		AutoAim
    }
	public class PlayerControls {
		PlayerShip playerShip;
		PlayerMain playerMain;
		public PlayerControls(PlayerShip playerShip, PlayerMain console) {
			this.playerShip = playerShip;
			this.playerMain = console;
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
				if (info.IsKeyDown(LeftShift)) {
					playerMain.TargetMouse();
				} else {
					playerShip.NextTargetFriendly();
				}
			}
			if (info.IsKeyPressed(controls[ClearTarget])) {
				if (playerShip.targetIndex > -1) {
					playerShip.ClearTarget();
				}
			}
			if (info.IsKeyPressed(controls[TargetEnemy])) {
				if (info.IsKeyDown(LeftShift)) {
					playerMain.TargetMouse();
				} else {
					playerShip.NextTargetEnemy();
				}
			}
			if (info.IsKeyPressed(controls[NextWeapon])) {
				playerShip.NextWeapon();
			}
			if (info.IsKeyDown(controls[FirePrimary])) {
				playerShip.SetFiringPrimary();
			}
			if (info.IsKeyDown(controls[AutoAim])) {
				
				if (playerShip.GetTarget(out SpaceObject target) && playerShip.GetPrimary(out Weapon w)) {
					playerShip.SetRotatingToFace(Helper.CalcFireAngle(target.position - playerShip.position, target.velocity - playerShip.velocity, w.missileSpeed, out _));
				}
			}
		}

		public void ProcessPowerMenu(Keyboard info) {
			ProcessArrows(info);
			ProcessTargeting(info);
			ProcessCommon(info);
		}
		public void ProcessCommon(Keyboard info) {
			var controls = playerShip.player.Settings.controls;
			if (info.IsKeyPressed(Tab)) {
				playerMain.uiMain.IsVisible = !playerMain.uiMain.IsVisible;
			}
			if (info.IsKeyPressed(controls[ControlKeys.Gate])) {
				playerShip.DisengageAutopilot();
				playerMain.Gate();
			}
			if (!playerMain.autopilotUpdate && info.IsKeyPressed(controls[Autopilot])) {
				playerShip.autopilot = !playerShip.autopilot;
				playerShip.AddMessage(new Message($"Autopilot {(playerShip.autopilot ? "engaged" : "disengaged")}"));
			}

			if (info.IsKeyPressed(controls[Dock])) {
				if (playerShip.dock != null) {
					if (playerShip.dock.docked) {
						playerShip.AddMessage(new Message("Undocked"));
					} else {
						playerShip.AddMessage(new Message("Docking sequence canceled"));
					}

					playerShip.dock = null;
				} else {
					Dockable dest = null;
					if (playerShip.GetTarget(out var t) && (playerShip.position - t.position).magnitude < 24 && t is Dockable d) {
						dest = d;
					}
					dest = dest ?? playerShip.world.entities.GetAll(p => (playerShip.position - p).magnitude < 8).OfType<Dockable>().OrderBy(p => (p.position - playerShip.position).magnitude).FirstOrDefault();
					if (dest != null) {
						playerShip.AddMessage(new Message("Docking sequence engaged"));
						playerShip.dock = new Docking(dest);
					}

				}
			}
			if (info.IsKeyPressed(controls[ShipMenu])) {
				playerShip.DisengageAutopilot();
				playerMain.sceneContainer?.Children.Add(new ShipScreen(playerMain, playerShip) { IsFocused = true });
			}
		}
        public void ProcessKeyboard(Keyboard info) {
			var controls = playerShip.player.Settings.controls;

			//Move the player
			ProcessArrows(info);
			ProcessTargeting(info);
			ProcessCommon(info);

			if(info.IsKeyPressed(Escape)) {
				playerMain.pauseMenu.IsVisible = true;
			}
			if (info.IsKeyPressed(controls[Powers])) {
				if (playerMain.powerMenu != null) {
					playerMain.powerMenu.IsVisible = !playerMain.powerMenu.IsVisible;
				}
			}
#if DEBUG
			if (info.IsKeyPressed(C)) {
				if(info.IsKeyDown(LeftShift)) {
					playerShip.Destroy(playerShip);
                } else {
					playerShip.Damage(playerShip, playerShip.ship.damageSystem.GetHP() - 5);
				}
			}

			if (info.IsKeyPressed(V)) {
				playerShip.ship.controlHijack = new Disrupt() { ticksLeft = 90, thrustMode = DisruptMode.FORCE_ON };
			}
#endif

		}
    }

	public class PlayerInput {
		public bool Thrust, TurnLeft, TurnRight, Brake;
		public bool TargetFriendly, TargetMouse, TargetEnemy, ClearTarget, NextWeapon, FirePrimary, AutoAim;
		public bool ToggleUI, Gate, Autopilot, Dock, ShipMenu;
		public PlayerInput() {}
		public PlayerInput(Dictionary<ControlKeys, Keys> controls, Keyboard info) {
			Thrust =	info.IsKeyDown(controls[ControlKeys.Thrust]);
			TurnLeft =	info.IsKeyDown(controls[ControlKeys.TurnLeft]);
			TurnRight = info.IsKeyDown(controls[ControlKeys.TurnRight]);
			Brake =		info.IsKeyDown(controls[ControlKeys.Brake]);

			TargetFriendly = info.IsKeyPressed(controls[ControlKeys.TargetFriendly])
							&& !info.IsKeyDown(LeftShift);
			TargetMouse = info.IsKeyPressed(controls[ControlKeys.TargetFriendly])
							&& info.IsKeyDown(LeftShift);
			ClearTarget = info.IsKeyPressed(controls[ControlKeys.ClearTarget]);
			TargetEnemy = info.IsKeyPressed(controls[ControlKeys.TargetEnemy])
							&& !info.IsKeyDown(LeftShift);
			TargetMouse = info.IsKeyPressed(controls[ControlKeys.TargetEnemy])
							&& info.IsKeyDown(LeftShift);
			NextWeapon = info.IsKeyPressed(controls[ControlKeys.NextWeapon]);
			FirePrimary = info.IsKeyPressed(controls[ControlKeys.FirePrimary]);
			AutoAim = info.IsKeyDown(controls[ControlKeys.AutoAim]);

			ToggleUI = info.IsKeyPressed(Tab);
			Gate = info.IsKeyPressed(controls[ControlKeys.Gate]);
			Autopilot = info.IsKeyPressed(controls[ControlKeys.Autopilot]);
			Dock = info.IsKeyPressed(controls[ControlKeys.Dock]);
			ShipMenu = info.IsKeyPressed(controls[ControlKeys.ShipMenu]);
		}
    }
}
