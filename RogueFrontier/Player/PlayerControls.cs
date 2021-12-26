using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using static SadConsole.Input.Keys;
using static RogueFrontier.Control;
using Helper = Common.Main;

namespace RogueFrontier;

public enum Control {
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
    NextPrimary,
    FirePrimary,
    FireSecondary,
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
            playerMain?.SleepMouse();
        }
        if (input.TurnRight) {
            playerShip.SetRotating(Rotating.CW);
            playerMain?.SleepMouse();
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
            playerShip.ClearTarget();
        }
        if (input.TargetEnemy) {
            playerShip.NextTargetEnemy();
        }
        if (input.NextPrimary) {
            playerShip.NextPrimary();
        }
        if (input.NextSecondary) {
            playerShip.NextSecondary();
        }
        if (input.FirePrimary) {
            playerShip.SetFiringPrimary();
        }
        if (input.FireSecondary) {
            playerShip.SetFiringSecondary();
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
                if (playerShip.GetTarget(out var t) && (playerShip.position - t.position).magnitude < 24) {
                    if (t is not DockableObject d) {
                        playerShip.AddMessage(new Transmission(t, "Target is not dockable"));
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


    public static Dictionary<Control, Keys> standard => new() {
        { Thrust, Up },
        { TurnRight, Right },
        { TurnLeft, Left },
        { Brake, Down },
        { Autopilot, A },
        { Dock, D },
        { TargetFriendly, F },
        { ClearTarget, R },
        { Gate, G },
        { ShipMenu, S },
        { TargetEnemy, T },
        { Powers, P },
        { NextPrimary, W },
        { FirePrimary, X },
        { FireSecondary, LeftControl },
        { AutoAim, Z }
    };
}

public class PlayerInput {
    public bool Thrust, TurnLeft, TurnRight, Brake;
    public bool TargetFriendly, TargetMouse, TargetEnemy, ClearTarget, NextPrimary, NextSecondary, FirePrimary, FireSecondary, AutoAim;
    public bool ToggleUI, Gate, Autopilot, Dock, ShipMenu;
    public bool Escape, Powers;
    public PlayerInput() { }
    public PlayerInput(Dictionary<Control, Keys> controls, Keyboard info) {
        Thrust = info.IsKeyDown(controls[Control.Thrust]);
        TurnLeft = info.IsKeyDown(controls[Control.TurnLeft]);
        TurnRight = info.IsKeyDown(controls[Control.TurnRight]);
        Brake = info.IsKeyDown(controls[Control.Brake]);

        TargetFriendly = info.IsKeyPressed(controls[Control.TargetFriendly])
                        && !info.IsKeyDown(LeftShift);
        TargetMouse = info.IsKeyPressed(controls[Control.TargetFriendly])
                        && info.IsKeyDown(LeftShift);
        ClearTarget = info.IsKeyPressed(controls[Control.ClearTarget]);
        TargetEnemy = info.IsKeyPressed(controls[Control.TargetEnemy])
                        && !info.IsKeyDown(LeftShift);
        TargetMouse = info.IsKeyPressed(controls[Control.TargetEnemy])
                        && info.IsKeyDown(LeftShift);
        NextPrimary = info.IsKeyPressed(controls[Control.NextPrimary])
                        && !info.IsKeyDown(LeftShift);
        NextSecondary = info.IsKeyPressed(controls[Control.NextPrimary])
                        && info.IsKeyDown(LeftShift);
        FirePrimary = info.IsKeyDown(controls[Control.FirePrimary]);
        FireSecondary = info.IsKeyDown(controls[Control.FireSecondary]);
        AutoAim = info.IsKeyDown(controls[Control.AutoAim]);

        ToggleUI = info.IsKeyPressed(Tab);
        Gate = info.IsKeyPressed(controls[Control.Gate]);
        Autopilot = info.IsKeyPressed(controls[Control.Autopilot]);
        Dock = info.IsKeyPressed(controls[Control.Dock]);
        ShipMenu = info.IsKeyPressed(controls[Control.ShipMenu]);

        Escape = info.IsKeyPressed(Keys.Escape);
        Powers = info.IsKeyPressed(controls[Control.Powers]);
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
