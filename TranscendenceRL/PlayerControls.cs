using Common;
using SadConsole;
using SadConsole.Input;
using System.Linq;
using static SadConsole.Input.Keys;
namespace TranscendenceRL {
    public class PlayerControls {
		PlayerShip playerShip;
		Console console;
		Console powerMenu;
		Console sceneContainer;
		public PlayerControls(PlayerShip playerShip, Console console, Console powerMenu, Console sceneContainer) {
			this.playerShip = playerShip;
			this.console = console;
			this.powerMenu = powerMenu;
			this.sceneContainer = sceneContainer;
        }
        public void ProcessKeyboard(Keyboard info) {
			//Move the player
			if (info.IsKeyDown(Up)) {
				playerShip.SetThrusting();
			}
			if (info.IsKeyDown(Left)) {
				playerShip.SetRotating(Rotating.CCW);
			}
			if (info.IsKeyDown(Right)) {
				playerShip.SetRotating(Rotating.CW);
			}
			if (info.IsKeyDown(Down)) {
				playerShip.SetDecelerating();
			}

			if (info.KeysDown.Select(d => d.Key).Intersect<Keys>(new Keys[] { Keys.LeftControl, Keys.LeftShift, Keys.Enter }).Count() == 3) {
				playerShip.Destroy(playerShip);
			}
			if (info.IsKeyPressed(D)) {
				if (playerShip.Dock != null) {
					if (playerShip.Dock.docked) {
						playerShip.AddMessage(new InfoMessage("Undocked"));
					} else {
						playerShip.AddMessage(new InfoMessage("Docking sequence canceled"));
					}

					playerShip.Dock = null;
				} else {
					var dest = playerShip.World.entities.GetAll(p => (playerShip.Position - p).Magnitude < 8).OfType<Dockable>().OrderBy(p => (p.Position - playerShip.Position).Magnitude).FirstOrDefault();
					if (dest != null) {
						playerShip.AddMessage(new InfoMessage("Docking sequence engaged"));
						playerShip.Dock = new Docking(dest);
					}

				}
			}
			if (info.IsKeyPressed(F)) {
				playerShip.NextTargetFriendly();
			}
			if (info.IsKeyPressed(R)) {
				if (playerShip.targetIndex > -1) {
					playerShip.ClearTarget();
				}
			}
			if (info.IsKeyPressed(S)) {
				sceneContainer?.Children.Add(new SceneScan(new ShipScreen(console, playerShip)) { IsFocused = true });
			}
			if (info.IsKeyPressed(T)) {
				playerShip.NextTargetEnemy();
			}
			if (info.IsKeyPressed(V)) {
				if(powerMenu != null)
					powerMenu.IsVisible = true;
			}
			if (info.IsKeyPressed(W)) {
				playerShip.NextWeapon();
			}
			if (info.IsKeyDown(X)) {
				playerShip.SetFiringPrimary();
			}
			if (info.IsKeyDown(Z)) {
				if (playerShip.GetTarget(out SpaceObject target) && playerShip.GetPrimary(out Weapon w)) {
					playerShip.SetRotatingToFace(Helper.CalcFireAngle(target.Position - playerShip.Position, target.Velocity - playerShip.Velocity, w.missileSpeed, out _));
				}
			}
			if (info.IsKeyPressed(C)) {
				if(info.IsKeyDown(LeftShift)) {
					playerShip.Destroy(playerShip);
                } else {
					playerShip.Damage(playerShip, playerShip.Ship.DamageSystem.GetHP() - 5);
				}
			}
		}
    }
}
