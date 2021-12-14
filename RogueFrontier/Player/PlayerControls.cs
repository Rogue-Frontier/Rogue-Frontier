using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using static SadConsole.Input.Keys;
using Helper = Common.Main;
namespace RogueFrontier;

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
    public PlayerShip playerShip;
    private PlayerMain playerMain;
    public PlayerInput input;
    public PlayerControls(PlayerShip playerShip, PlayerMain playerMain) {
        this.playerShip = playerShip;
        this.playerMain = playerMain;
        this.input = new PlayerInput();
    }
    public void ProcessArrows() {
        if (input.Thrust) {
            playerShip.SetThrusting();
        }
        if (input.TurnLeft) {
            playerShip.SetRotating(Rotating.CCW);
        }
        if (input.TurnRight) {
            playerShip.SetRotating(Rotating.CW);
        }
        if (input.Brake) {
            playerShip.SetDecelerating();
        }
    }
    public void ProcessTargeting() {
        if (input.TargetFriendly) {
            playerShip.NextTargetFriendly();
        }
        if (input.TargetMouse) {
            playerMain.TargetMouse();
        }
        if (input.ClearTarget) {
            if (playerShip.targetIndex > -1) {
                playerShip.ClearTarget();
            }
        }
        if (input.TargetEnemy) {
            playerShip.NextTargetEnemy();
        }
        if (input.NextWeapon) {
            playerShip.NextWeapon();
        }
        if (input.FirePrimary) {
            playerShip.SetFiringPrimary();
        }
        if (input.AutoAim) {
            if (playerShip.GetTarget(out SpaceObject target) && playerShip.GetPrimary(out Weapon w)) {
                playerShip.SetRotatingToFace(Helper.CalcFireAngle(target.position - playerShip.position, target.velocity - playerShip.velocity, w.missileSpeed, out _));
            }
        }
    }

    public void ProcessCommon() {
        if (input.ToggleUI) {
            playerMain.uiMain.IsVisible = !playerMain.uiMain.IsVisible;
        }
        if (input.Gate) {
            playerShip.DisengageAutopilot();
            playerMain.Gate();
        }
        if (input.Autopilot && !playerMain.autopilotUpdate) {
            playerShip.autopilot = !playerShip.autopilot;
            playerShip.AddMessage(new Message($"Autopilot {(playerShip.autopilot ? "engaged" : "disengaged")}"));
        }

        if (input.Dock) {
            if (playerShip.dock != null) {
                if (playerShip.dock.docked) {
                    playerShip.AddMessage(new Message("Undocked"));
                } else {
                    playerShip.AddMessage(new Message("Docking sequence canceled"));
                }

                playerShip.dock = null;
            } else {
                if (playerShip.GetTarget(out var t)) {
                    if (!(t is DockableObject d)) {
                        playerShip.AddMessage(new Transmission(t, "Target is not dockable"));
                    } else if (!((playerShip.position - t.position).magnitude < 24)) {
                        playerShip.AddMessage(new Transmission(t, "Target is out of docking range"));
                    } else {
                        Dock(d);
                    }
                } else {
                    var dest = playerShip.world.entities
                        .GetAll(p => (playerShip.position - p).magnitude < 8)
                        .OfType<DockableObject>()
                        .Where(s => s.dockable)
                        .OrderBy(p => (p.position - playerShip.position).magnitude)
                        .FirstOrDefault();
                    if (dest != null) {
                        Dock(dest);
                    } else {
                        playerShip.AddMessage(new Message("No dock target in range"));
                    }
                }

                void Dock(DockableObject dest) {
                    playerShip.AddMessage(new Transmission(dest, "Docking sequence engaged"));
                    playerShip.dock = new Docking(dest, dest.GetDockPoint());
                }
            }
        }
        if (input.ShipMenu) {
            playerShip.DisengageAutopilot();
            playerMain.sceneContainer?.Children.Add(new ShipScreen(playerMain, playerShip, playerMain.story) { IsFocused = true });
        }
    }
    public void ProcessOther() {
        if (input.Escape) {
            playerMain.pauseMenu.IsVisible = true;
        }
        if (input.Powers) {
            if (playerMain.powerMenu != null) {
                playerMain.powerMenu.IsVisible = !playerMain.powerMenu.IsVisible;
            }
        }
    }
    public void ProcessAll() {
        ProcessArrows();
        ProcessTargeting();
        ProcessCommon();
        ProcessOther();
    }
    public void UpdateInput(Keyboard info) {
        input = new PlayerInput(playerShip.player.Settings.controls, info);
    }
    public void ProcessWithMenu(Keyboard info) {
        UpdateInput(info);
        ProcessArrows();
        ProcessTargeting();
        ProcessCommon();
    }
    public void ProcessKeyboard(Keyboard info) {
        UpdateInput(info);
        ProcessArrows();
        ProcessTargeting();
        ProcessCommon();
        ProcessOther();

        if (info.IsKeyPressed(C)) {
            if (playerMain.communicationsMenu != null) {
                playerMain.communicationsMenu.IsVisible = !playerMain.communicationsMenu.IsVisible;
            }
        }
#if DEBUG
        /*
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
        */
#endif
    }
}

public class PlayerInput {
    public bool Thrust, TurnLeft, TurnRight, Brake;
    public bool TargetFriendly, TargetMouse, TargetEnemy, ClearTarget, NextWeapon, FirePrimary, AutoAim;
    public bool ToggleUI, Gate, Autopilot, Dock, ShipMenu;
    public bool Escape, Powers;
    public PlayerInput() { }
    public PlayerInput(Dictionary<ControlKeys, Keys> controls, Keyboard info) {
        Thrust = info.IsKeyDown(controls[ControlKeys.Thrust]);
        TurnLeft = info.IsKeyDown(controls[ControlKeys.TurnLeft]);
        TurnRight = info.IsKeyDown(controls[ControlKeys.TurnRight]);
        Brake = info.IsKeyDown(controls[ControlKeys.Brake]);

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
        FirePrimary = info.IsKeyDown(controls[ControlKeys.FirePrimary]);
        AutoAim = info.IsKeyDown(controls[ControlKeys.AutoAim]);

        ToggleUI = info.IsKeyPressed(Tab);
        Gate = info.IsKeyPressed(controls[ControlKeys.Gate]);
        Autopilot = info.IsKeyPressed(controls[ControlKeys.Autopilot]);
        Dock = info.IsKeyPressed(controls[ControlKeys.Dock]);
        ShipMenu = info.IsKeyPressed(controls[ControlKeys.ShipMenu]);

        Escape = info.IsKeyPressed(Keys.Escape);
        Powers = info.IsKeyPressed(controls[ControlKeys.Powers]);
    }
    public void ClientOnly() {
        Autopilot = false;
    }
    public void ServerOnly() {
        ToggleUI = false;
        Autopilot = false;
        ShipMenu = false;
        Escape = false;
        Powers = false;
    }
}
