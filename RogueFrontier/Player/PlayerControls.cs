using SadConsole.Input;
using System.Collections.Generic;
using System.Linq;
using static SadConsole.Input.Keys;
using static RogueFrontier.Control;
using Helper = Common.Main;
using System;

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
    InvokePowers,
    NextPrimary,
    FirePrimary,
    FireSecondary,
    AutoAim
}
public class PlayerControls {
    public PlayerShip playerShip;
    private PlayerMain playerMain;
    public PlayerInput input=new();
    public PlayerControls(PlayerShip playerShip, PlayerMain playerMain) {
        this.playerShip = playerShip;
        this.playerMain = playerMain;
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
            if (playerShip.GetTarget(out var target) && playerShip.CanSee(target) && playerShip.GetPrimary(out Weapon w)) {
                playerShip.SetRotatingToFace(Helper.CalcFireAngle(target.position - playerShip.position, target.velocity - playerShip.velocity, w.projectileDesc.missileSpeed, out _));
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
                    playerShip.AddMessage(new Message("Docking canceled"));
                }
                playerShip.dock = null;
            } else {
                if (playerShip.GetTarget(out var t) && (playerShip.position - t.position).magnitude < 24) {
                    if (t is not IDockable d) {
                        playerShip.AddMessage(new Transmission(t, "Target is not dockable"));
                    } else {
                        Dock(d);
                    }
                } else {
                    var dest = playerShip.world.entities
                        .FilterKey(p => (playerShip.position - p).magnitude < 8)
                        .OfType<IDockable>()
                        .Where(s => s.GetDockPoint() != null)
                        .OrderBy(p => (p.position - playerShip.position).magnitude)
                        .FirstOrDefault();
                    if (dest != null) {
                        Dock(dest);
                    } else {
                        playerShip.AddMessage(new Message("No dock target in range"));
                    }
                }
                void Dock(IDockable dest) {
                    playerShip.AddMessage(new Transmission(dest, "Docking initiated"));
                    playerShip.dock = new(dest, dest.GetDockPoint());
                }
            }
        }
        if (input.ShipMenu) {
            playerShip.DisengageAutopilot();
            playerMain.sceneContainer?.Children.Add(new ShipScreen(playerMain, playerShip, playerMain.story) { IsFocused = true });
        }
    }
    public void ProcessAll() {
        ProcessArrows();
        ProcessTargeting();
        ProcessCommon();
    }
    public void UpdateInput(Keyboard info) =>
        input = new(playerShip.person.Settings.controls, info);
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
        if (input.Escape) {
            playerMain.pauseScreen.IsVisible = true;
        }
        if (input.InvokePowers) {
            if (playerMain.powerWidget is var m) {
                m.IsVisible = !m.IsVisible;
            }
        }
        if (info.IsKeyPressed(U)) {
            playerMain.sceneContainer?.Children.Add(SListScreen.UsableScreen(playerMain.sceneContainer, playerShip));
        }
        if (input.Communications) {
            if (playerMain.communicationsWidget is var m) {
                m.IsVisible = !m.IsVisible;
            }
        }
        if (input.NetworkMap) {
            if (playerMain.networkMap is var m) {
                m.IsVisible = !m.IsVisible;
            }
        }
        if (info.IsKeyPressed(F1)) {
            SadConsole.Game.Instance.Screen = new IdentityScreen(playerMain) { IsFocused = true };
            //playerMain.OnIntermission();
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
        { InvokePowers, I },
        { NextPrimary, W },
        { FirePrimary, X },
        { FireSecondary, LeftControl },
        { AutoAim, Z }
    };
}
public class PlayerInput {
    public bool Shift;
    public bool Thrust, TurnLeft, TurnRight, Brake;
    public bool TargetFriendly, TargetMouse, TargetEnemy, ClearTarget, NextPrimary, NextSecondary, FirePrimary, FireSecondary, AutoAim;
    public bool QuickZoom;
    public bool ToggleUI, Gate, Autopilot, Dock, ShipMenu;
    public bool Escape, InvokePowers, Communications, NetworkMap;
    public PlayerInput() { }
    public PlayerInput(Dictionary<Control, Keys> controls, Keyboard info) {
        var p = (Control c) => info.IsKeyPressed(controls[c]);
        var d = (Control c) => info.IsKeyDown(controls[c]);
        Shift = info.IsKeyDown(LeftShift) || info.IsKeyDown(RightShift);
        Thrust =        d(Control.Thrust);
        TurnLeft =      d(Control.TurnLeft);
        TurnRight =     d(Control.TurnRight);
        Brake =         d(Control.Brake);
        TargetFriendly =p(Control.TargetFriendly) && !Shift;
        TargetMouse =   p(Control.TargetFriendly) && Shift;
        ClearTarget =   p(Control.ClearTarget);
        TargetEnemy =   p(Control.TargetEnemy) && !Shift;
        TargetMouse =   p(Control.TargetEnemy) && Shift;
        NextPrimary =   p(Control.NextPrimary) && !Shift;
        NextSecondary = p(Control.NextPrimary) && Shift;
        FirePrimary =   d(Control.FirePrimary);
        FireSecondary = d(Control.FireSecondary);
        AutoAim =       d(Control.AutoAim);
        ToggleUI =      info.IsKeyPressed(Tab);
        Gate =          p(Control.Gate);
        Autopilot =     p(Control.Autopilot);
        Dock =          p(Control.Dock);
        ShipMenu =      p(Control.ShipMenu);
        Escape =        info.IsKeyPressed(Keys.Escape);
        InvokePowers =  p(Control.InvokePowers);
        Communications= info.IsKeyPressed(C);
        NetworkMap=     info.IsKeyPressed(N);
        QuickZoom =     info.IsKeyPressed(M);
    }
    public void ClientOnly() {
        Autopilot = false;
    }
    public void ServerOnly() {
        ToggleUI = false;
        Autopilot = false;
        ShipMenu = false;
        Escape = false;
        InvokePowers = false;
    }
}