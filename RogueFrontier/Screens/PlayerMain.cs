using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using SadRogue.Primitives;
using static SadConsole.Input.Keys;
using SadConsole;
using SadConsole.Input;
using Console = SadConsole.Console;
using Helper = Common.Main;
using static UI;
using Newtonsoft.Json;
using ArchConsole;
using static RogueFrontier.PlayerShip;
using static RogueFrontier.Station;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using SFML.Audio;
using Microsoft.Win32.SafeHandles;

namespace RogueFrontier;
public class NotifyStationDestroyed : Ob<Station.Destroyed> {
    public PlayerShip playerShip;
    public Station source;
    public void Observe(Station.Destroyed ev) {
        var (s, d, w) = ev;
        playerShip?.AddMessage(new Transmission(source,
            $"{source.name} destroyed by {d?.name ?? "unknown forces"}!"
            ));
    }
    public NotifyStationDestroyed(PlayerShip playerShip, Station source) {
        this.playerShip = playerShip;
        this.source = source;
    }
}
public class Camera {
    public XY position;
    //For now we don't allow shearing
    public double rotation { get => Math.Atan2(right.y, right.x); set => right = XY.Polar(value, 1); }
    public XY up => right.Rotate(Math.PI / 2);
    public XY right;
    public Camera(XY position) {
        this.position = position;
        right = new XY(1, 0);
    }
    public Camera() {
        position = new XY();
        right = new XY(1, 0);
    }
    public void Rotate(double angle) {
        right = right.Rotate(angle);
    }
}
public class PlayerMain : ScreenSurface, Ob<PlayerShip.Destroyed> {
    public void Observe(PlayerShip.Destroyed ev) {
        var (p, d, w) = ev;
        OnPlayerDestroyed($"Destroyed by {d?.name ?? "unknown forces"}", w);
    }
    public int Width => Surface.Width;
    public int Height => Surface.Height;
    public System world => playerShip.world;
    public Camera camera { get; private set; }
    public Profile profile;
    public PlayerStory story;
    public PlayerShip playerShip;
    public PlayerControls playerControls;

    public Noisemaker audio;
    XY mouseWorldPos;
    Keyboard prevKeyboard=new();
    MouseScreenObjectState prevMouse=new(null, new());
    public bool sleepMouse = true;
    public BackdropConsole back;
    public Viewport viewport;
    public GateTransition transition;
    public Megamap uiMegamap;
    public Vignette vignette;
    public Console sceneContainer;
    public Readout uiMain;  //If this is visible, then all other ui Consoles are visible
    public Edgemap uiEdge;
    //public Minimap uiMinimap;
    public CommunicationsWidget communicationsWidget;
    public PowerWidget powerWidget;
    public ListWidget<Item> invokeWidget;
    public PauseScreen pauseScreen;
    public GalaxyMap networkMap;
    private TargetingMarker crosshair;
    private double targetCameraRotation;
    private double updateWait;
    public bool autopilotUpdate;
    //public bool frameRendered = true;
    public int updatesSinceRender = 0;
    private ListIndex<System> systems;

    public Sound music;
    //EventWaitHandle smooth = new(true, EventResetMode.AutoReset);
    public PlayerMain(int Width, int Height, Profile profile, PlayerShip playerShip) : base(Width, Height) {
        UseMouse = true;
        UseKeyboard = true;
        camera = new();
        this.profile = profile;
        this.story = new(playerShip);
        this.playerShip = playerShip;
        this.playerControls = new(playerShip, this);

        audio = new(playerShip);
        audio.Register(playerShip.world.universe);

        back = new(Width, Height, world.backdrop, camera);
        viewport = new(this, camera, world);
        uiMegamap = new(camera, playerShip, world.backdrop.layers.Last(), Width, Height);
        vignette = new(playerShip, Width, Height);
        sceneContainer = new(Width, Height);
        sceneContainer.Focused += (e, o) => this.IsFocused = true;
        uiMain = new(camera, playerShip, Width, Height);
        uiEdge = new(camera, playerShip, Width, Height);
        //uiMinimap = new(this, playerShip, 16, camera);
        communicationsWidget = new(63, 15, playerShip) { IsVisible = false, Position = new(3, 32) };
        powerWidget = new(31, 16, this) { IsVisible = false, Position = new(3, 32) };

        pauseScreen = new(this) { IsVisible = false };
        networkMap = new(this) { IsVisible = false };

        crosshair = new(playerShip, "Mouse Cursor", new());

        systems = new(new(playerShip.world.universe.systems.Values));


        //Don't allow anyone to get focus via mouse click
        FocusOnMouseClick = false;
    }

    public void SleepMouse() => sleepMouse = true;
    public void HideUI() {
        uiMain.IsVisible = false;
    }
    public void ShowUI() {
        uiMain.IsVisible = true;
    }
    public void HideAll() {
        //Force exit any scenes
        sceneContainer.Children.Clear();
        //Force exit power menu
        powerWidget.IsVisible = false;
        communicationsWidget.IsVisible = false;
        uiMain.IsVisible = false;


        //Pretty sure this can't happen but make sure
        pauseScreen.IsVisible = false;
    }
    public void Jump() {
        var prevViewport = new Viewport(this, new(playerShip.position), world);
        var nextViewport = new Viewport(this, this.camera, world);

        back = new(nextViewport);
        viewport = nextViewport;
        transition = new GateTransition(prevViewport, nextViewport, () => {
            transition = null;
            if (playerShip.mortalTime <= 0) {
                vignette.powerAlpha = 0f;
            }
        });
    }
    public void Gate() {
        if (!playerShip.CheckGate(out Stargate gate)) {
            return;
        }
        var destGate = gate.destGate;
        if (destGate == null) {
            //OnPlayerLeft(true);
            world.entities.Remove(playerShip);
            transition = new GateTransition(new(this, new(playerShip.position), world), null, () => {
                transition = null;
                OnPlayerLeft(false);
            });
            return;
        }
        var prevViewport = new Viewport(this, new(playerShip.position), world);
        world.entities.Remove(playerShip);

        var nextWorld = destGate.world;
        playerShip.ship.world = nextWorld;
        playerShip.ship.position = destGate.position + (playerShip.ship.position - gate.position);
        nextWorld.entities.Add(playerShip);
        nextWorld.effects.Add(new Heading(playerShip));
        var nextViewport = new Viewport(this, this.camera, nextWorld);

        back = new(nextViewport);
        viewport = nextViewport;
        transition = new GateTransition(prevViewport, nextViewport, () => {
            transition = null;
            if (playerShip.mortalTime <= 0) {
                vignette.powerAlpha = 0f;
            }
        });
    }
    public void OnIntermission(Lis<LiveGame.LoadHook> hook = null) {
        HideAll();
        Game.Instance.Screen = new ExitTransition(this, EndCrawl()) { IsFocused = true };
        ScreenSurface EndCrawl() {
            SimpleCrawl ds = null;
            ds = new("Intermission\n\n", EndPause) {
                Position = new(Surface.Width / 4, 8), IsFocused = true
            };
            void EndPause() {
                Game.Instance.Screen = new Pause(ds, EndGame, 3) { IsFocused = true };
                void EndGame() {
                    Game.Instance.Screen = new IntermissionScreen(
                        this,
                        new(world, playerShip, hook),
                        $"Fate unknown") { IsFocused = true };
                }
            }
            return ds;
        }
    }
    public void OnPlayerLeft(bool transition) {
        HideAll();
        world.entities.Remove(playerShip);
        if (transition) {
            Game.Instance.Screen = new ExitTransition(this, EndCrawl()) { IsFocused = true };
        } else {
            Game.Instance.Screen = EndCrawl();
        }
        ScreenSurface EndCrawl() {
            SimpleCrawl ds = null;
            ds = new("You have left Human Space.\n\n", EndPause) { Position = new(Surface.Width / 4, 8), IsFocused = true };
            void EndPause() {
                Game.Instance.Screen = new Pause(ds, EndGame, 3) { IsFocused = true };
            }
            return ds;
        }
        void EndGame() {
            Game.Instance.Screen = new DeathScreen(this,
                new() {
                    desc = $"Left Human Space",
                    deathFrame = null,
                    wreck = null
                }) { IsFocused = true };
        }
    }
    public void OnPlayerDestroyed(string message, Wreck wreck) {
        //Clear mortal time so that we don't slow down after the player dies
        wreck.cargo.Clear();
        playerShip.mortalTime = 0;
        playerShip.ship.blindTicks = 0;
        HideAll();
        //Get a snapshot of the player
        var size = Surface.Height;
        var deathFrame = new ColoredGlyph[size, size];
        var center = new XY(size / 2, size / 2);
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                var tile = GetTile(camera.position - new XY(x, y) + center);
                deathFrame[x, y] = tile;
            }
        }
        ColoredGlyph GetTile(XY xy) {
            var back = world.backdrop.GetTile(xy, camera.position);
            //Round down to ensure we don't get duplicated tiles along the origin

            if (viewport.tiles.TryGetValue(xy.roundDown, out var g)) {
                g = g.Clone();          //Don't modify the source
                g.Background = back.Background.Premultiply().Blend(g.Background);
                return g;
            } else {
                return back;
            }
        }
        var ep = new Epitaph() {
            desc = message,
            deathFrame = deathFrame,
            wreck = wreck
        };
        playerShip.person.Epitaphs.Add(ep);

        playerShip.autopilot = false;
        //Bug: Background is not included because it is a separate console
        var ds = new DeathScreen(this, ep);
        var dt = new DeathTransition(this, ds);
        var dp = new DeathPause(this, dt) { IsFocused = true };
        SadConsole.Game.Instance.Screen = dp;
        Task.Run(() => {
            lock (world) {
                StreamWriter w = null;
                try {
#if false
                    new DeadGame(world, playerShip, ep).Save();
#else
                    //Task.Delay(2000);
                    Thread.Sleep(2000);
#endif

                } catch(Exception e) {
#if !DEBUG
                    throw;
#else
                    if (w == null) {
                        var name = $"[{DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}] Save Failed.txt";
                        w = new(new FileStream(name, FileMode.Create));
                    }
                    w.Write(e.Message);
#endif
                }
                w?.Close();
            }
            dp.done = true;
        });
    }
    public void UpdateClient(TimeSpan delta) {
        //if(!frameRendered) return;
        if (updatesSinceRender > 2) return;
        updatesSinceRender++;
        void UpdateUniverse() {
            world.UpdateActive(delta.TotalSeconds * (playerShip.autopilot ? 3 : 1));
            world.UpdatePresent();
            story.Update(playerShip);
        }
        if (true) {
            back.Update(delta);
            lock (world) {
                UpdateUniverse();
                PlaceTiles(delta);
                transition?.Update(delta);
            }
            if (playerShip.dock is { justDocked: true, Target: IDockable d } dock) {
                audio.PlayDocking(false);
                var scene = story.GetScene(this, playerShip, d) ?? d.GetDockScene(this, playerShip);
                if (scene != null) {
                    playerShip.DisengageAutopilot();
                    dock.Clear();
                    sceneContainer.Children.Add(new SceneScan(scene) { IsFocused = true });
                } else {
                    playerShip.AddMessage(new Message($"Stationed on {dock.Target.name}"));
                }
            }
        }
        camera.position = playerShip.position;
        //frameRendered = false;
        //Required to update children
        base.Update(delta);
    }
    public override void Update(TimeSpan delta) {
        //if(!frameRendered) return;
        if (updatesSinceRender > 2) return;
        updatesSinceRender++;
        if (pauseScreen.IsVisible) {
            pauseScreen.Update(delta);
            return;
        }
        if (networkMap.IsVisible) {
            networkMap.Update(delta);
            return;
        }
        var gameDelta = delta.TotalSeconds * (playerShip.autopilot ? 3 : 1);
        if(playerShip is { active:true, mortalTime: > 0 }) {
            playerShip.mortalTime -= gameDelta;
            gameDelta /= (1 + playerShip.mortalTime/2);
        }
        void UpdateUniverse() {

            world.UpdateActive(gameDelta);
            world.UpdatePresent();
            systems.GetNext(1).ForEach(s => {
                if (s != world) {
                    s.UpdateActive(gameDelta);
                    s.UpdatePresent();
                }
            });
        }

        if (gameDelta > 0) {
            back.Update(delta);
            lock (world) {

                if(!sceneContainer.Children.Any()) {
                    playerControls.ProcessAll();
                    playerControls.input = new();
                }

                audio.Update(delta.TotalSeconds);

                AddCrosshair();
                UpdateUniverse();
                
                PlaceTiles(delta);
                transition?.Update(delta);

                void AddCrosshair() {
                    if (playerShip.GetTarget(out var t) && t == crosshair) {
                        Heading.Crosshair(world, crosshair.position, Color.Yellow);
                    }
                }
            }

            if (playerShip.dock is {  justDocked:true, Target: IDockable d } dock) {
                audio.PlayDocking(false);
                var scene = story.GetScene(this, playerShip, d) ?? d.GetDockScene(this, playerShip);
                if (scene != null) {
                    playerShip.DisengageAutopilot();
                    dock.Clear();
                    sceneContainer.Children.Add(new SceneScan(scene) { IsFocused = true });
                } else {
                    playerShip.AddMessage(new Message($"Stationed on {dock.Target.name}"));
                }
            }
        }
        UpdateUI(delta);
        camera.position = playerShip.position;
        //frameRendered = false;
        //Required to update children
        base.Update(delta);
    }
    public void UpdateUI(TimeSpan delta) {
        var d = Main.AngleDiffRad(camera.rotation, targetCameraRotation);
        if (Math.Abs(d) < 0.01) {
            camera.rotation += d;
        } else {
            camera.rotation += d / 10;
        }
        if (sceneContainer.Children.Any()) {
            sceneContainer.Update(delta);
        } else {
            if (uiMain.IsVisible) {
                uiMegamap.Update(delta);

                vignette.Update(delta);

                uiMain.viewScale = uiMegamap.viewScale;
                uiMain.Update(delta);

                uiEdge.viewScale = uiMegamap.viewScale;
                uiEdge.Update(delta);

                //uiMinimap.alpha = (byte)(255 - uiMegamap.alpha);
                //uiMinimap.Update(delta);
            } else {
                uiMegamap.Update(delta);
                vignette.Update(delta);
            }
            if (powerWidget.IsVisible) {
                powerWidget.Update(delta);
            }
            if (communicationsWidget.IsVisible) {
                communicationsWidget.Update(delta);
            }
        }
    }
    public void PlaceTiles(TimeSpan delta) {
        if (playerShip.ship.blindTicks > 0) {
            viewport.UpdateBlind(delta, playerShip.GetVisibleDistanceLeft);
        } else {
            viewport.UpdateVisible(delta, playerShip.GetVisibleDistanceLeft);
        }
        /*
        foreach((var key, var value) in viewport.tiles) {
            viewport.tiles[key] = new(value.Foreground, value.Background, '?');
        }
        */
    }
    public void RenderWorld(TimeSpan delta) {
        viewport.Render(delta);
    }
    public override void Render(TimeSpan drawTime) {
        if (pauseScreen.IsVisible) {
            back.Render(drawTime);
            viewport.Render(drawTime);
            vignette.Render(drawTime);
            pauseScreen.Render(drawTime);
        } else if (networkMap.IsVisible) {
            networkMap.Render(drawTime);
        } else if (sceneContainer.Children.Any()) {
            back.Render(drawTime);
            viewport.Render(drawTime);
            vignette.Render(drawTime);
            sceneContainer.Render(drawTime);
        } else if(playerShip.active) {
            //viewport.UsePixelPositioning = true;
            //viewport.Position = (playerShip.position - playerShip.position.roundDown) * 8 * new XY(1, -1) * -1;
            if (uiMain.IsVisible) {
                //If the megamap is completely visible, then skip main render so we can fast travel
                if (uiMegamap.alpha < 255) {
                    if (transition != null) {
                        transition.Render(drawTime);
                    } else {
                        back.Render(drawTime);
                        viewport.Render(drawTime);
                    }
                    uiMegamap.Render(drawTime);

                    vignette.Render(drawTime);

                    uiMain.Render(drawTime);
                    uiEdge.Render(drawTime);
                    //uiMinimap.Render(drawTime);
                } else {
                    uiMegamap.Render(drawTime);
                    vignette.Render(drawTime);
                    uiMain.Render(drawTime);
                    uiEdge.Render(drawTime);
                }
            } else {
                /*
                if (transition != null) {
                    transition.Render(drawTime);
                } else {
                    back.Render(drawTime);
                    viewport.Render(drawTime);
                }
                vignette.Render(drawTime);
                */

                //If the megamap is completely visible, then skip main render so we can fast travel
                if (uiMegamap.alpha < 255) {
                    if (transition != null) {
                        transition.Render(drawTime);
                    } else {
                        back.Render(drawTime);
                        viewport.Render(drawTime);
                    }
                    uiMegamap.Render(drawTime);
                    vignette.Render(drawTime);
                } else {
                    uiMegamap.Render(drawTime);
                    vignette.Render(drawTime);
                }
            }
            if (powerWidget.IsVisible) {
                powerWidget.Render(drawTime);
            }
            if (communicationsWidget.IsVisible) {
                communicationsWidget.Render(drawTime);
            }
        } else {
            back.Render(drawTime);
            viewport.Render(drawTime);
        }
        //frameRendered = true;
        updatesSinceRender = 0;
    }
    public override bool ProcessKeyboard(Keyboard info) {
        if (sceneContainer.Children.Any()) {
            var children = new List<IScreenObject>(sceneContainer.Children);
            foreach (var c in children) {
                c.ProcessKeyboard(info);
            }
            return base.ProcessKeyboard(info);
        }
        uiMegamap.ProcessKeyboard(info);
        /*
        if (uiMain.IsVisible) {
            uiMegamap.ProcessKeyboard(info);
        }
        */
        prevKeyboard = info;
#if false
        if (info.IsKeyPressed(N)) {
            galaxyMap.IsVisible = !galaxyMap.IsVisible;
        }
#endif
        //Intercept the alphanumeric/Escape keys if the power menu is active
        if (pauseScreen.IsVisible) {
            pauseScreen.ProcessKeyboard(info);
        } else if (networkMap.IsVisible) {
            networkMap.ProcessKeyboard(info);
        } else if (powerWidget.IsVisible) {
            playerControls.UpdateInput(info);
            powerWidget.ProcessKeyboard(info);
        } else if (communicationsWidget.IsVisible) {
            playerControls.UpdateInput(info);
            communicationsWidget.ProcessKeyboard(info);
        } else {
            playerControls.UpdateInput(info);
            var p = (Keys k) => info.IsKeyPressed(k);
            var d = (Keys k) => info.IsKeyDown(k);
            if (d(LeftShift) || d(RightShift)) {
                if (p(OemOpenBrackets)) {
                    targetCameraRotation += Math.PI/2;
                }
                if (p(OemCloseBrackets)) {
                    targetCameraRotation -= Math.PI/2;
                }
            } else {
                if (d(OemOpenBrackets)) {
                    camera.rotation += 0.01;
                    targetCameraRotation = camera.rotation;
                }
                if (d(OemCloseBrackets)) {
                    camera.rotation -= 0.01;
                    targetCameraRotation = camera.rotation;
                }
            }
        }
        return base.ProcessKeyboard(info);
    }
    public void TargetMouse() {
        var targetList = new List<ActiveObject>(
                    world.entities.all
                    .OfType<ActiveObject>()
                    .OrderBy(e => (e.position - mouseWorldPos).magnitude)
                    .Distinct()
                    );

        //Set target to object closest to mouse cursor
        //If there is no target closer to the cursor than the playership, then we toggle aiming by crosshair
        //Using the crosshair, we can effectively force any omnidirectional weapons to point at the crosshair
        if (targetList.First() == playerShip) {
            if (playerShip.GetTarget(out var t) && t == crosshair) {
                crosshair.active = false;
                playerShip.ClearTarget();
            } else {
                crosshair.active = true;
                playerShip.SetTargetList(new() { crosshair });
            }
        } else {
            playerShip.targetList = targetList;
            playerShip.targetIndex = 0;
            playerShip.UpdateWeaponTargets();
        }
    }
    public override bool ProcessMouse(MouseScreenObjectState state) {
        if (pauseScreen.IsVisible) {
            pauseScreen.ProcessMouseTree(state.Mouse);
        } else if (networkMap.IsVisible) {
            networkMap.ProcessMouseTree(state.Mouse);
        } else if (sceneContainer.Children.Any()) {
            sceneContainer.ProcessMouseTree(state.Mouse);
        } else if (powerWidget.IsVisible
            && powerWidget.blockMouse
            && new MouseScreenObjectState(powerWidget, state.Mouse).IsOnScreenObject) {
            powerWidget.ProcessMouseTree(state.Mouse);
        } else if (state.IsOnScreenObject) {
            if(sleepMouse) {
                sleepMouse = state.SurfacePixelPosition.Equals(prevMouse.SurfacePixelPosition);
            }

            //bool moved = mouseScreenPos != state.SurfaceCellPosition;
            //var mouseScreenPos = state.SurfaceCellPosition;
            var mouseScreenPos = new XY(state.SurfacePixelPosition) / FontSize - new XY(0.5, 0.75);

            //(var a, var b) = (state.SurfaceCellPosition, state.SurfacePixelPosition);


            //Placeholder for mouse wheel-based weapon selection
            if (state.Mouse.ScrollWheelValueChange > 0) {
                if (playerControls.input.Shift) {
                    playerShip.NextSecondary();
                } else {
                    playerShip.NextPrimary();
                }

                playerControls.input.UsingMouse = true;
            } else if (state.Mouse.ScrollWheelValueChange < 0) {
                if (playerControls.input.Shift) {
                    playerShip.PrevSecondary();
                } else {
                    playerShip.PrevPrimary();
                }

                playerControls.input.UsingMouse = true;
            }

            var centerOffset = new XY(mouseScreenPos.x, Surface.Height - mouseScreenPos.y) - new XY(Surface.Width / 2, Surface.Height / 2);
            centerOffset *= uiMegamap.viewScale;
            mouseWorldPos = (centerOffset.Rotate(camera.rotation) + camera.position);
            ActiveObject t;
            if (state.Mouse.MiddleClicked && !playerControls.input.Shift) {
                TargetMouse();
            } else if(state.Mouse.MiddleButtonDown && playerControls.input.Shift) {
                playerControls.input.FirePrimary = true;
                playerControls.input.FireSecondary = true;
                playerControls.input.UsingMouse = true;
            }
            bool enableMouseTurn = !sleepMouse;
            //Update the crosshair if we're aiming with it
            if (playerShip.GetTarget(out t) && t == crosshair) {
                crosshair.position = mouseWorldPos;
                crosshair.velocity = playerShip.velocity;
                //If we set velocity to match player's velocity, then the weapon will aim directly at the crosshair
                //If we set the velocity to zero, then the weapon will aim to the lead angle of the crosshair
                //crosshair.Update();
                //Idea: Aiming with crosshair disables mouse turning
                enableMouseTurn = false;
            }
            //Also enable mouse turn with Power Menu
            if (enableMouseTurn && playerShip.ship.rotating == Rotating.None) {
                var playerOffset = mouseWorldPos - playerShip.position;
                if (playerOffset.xi != 0 || playerOffset.yi != 0) {
                    var radius = playerOffset.magnitude;
                    var facing = XY.Polar(playerShip.rotationRad, radius);
                    var aim = playerShip.position + facing;

                    var off = (mouseWorldPos - aim).magnitude;
                    var tolerance = Math.Sqrt(radius) / 3;
                    Color c = off < tolerance ? Color.White : Color.White.SetAlpha(255 * 3 / 5);

                    EffectParticle.DrawArrow(world, mouseWorldPos, playerOffset, c);

                    //EffectParticle.DrawArrow(World, aim, facing, Color.Yellow);

                    var mouseRads = playerOffset.angleRad;
                    playerShip.SetRotatingToFace(mouseRads);
                    playerControls.input.TurnRight = playerShip.ship.rotating == Rotating.CW;
                    playerControls.input.TurnLeft = playerShip.ship.rotating == Rotating.CCW;
                    playerControls.input.UsingMouse = true;
                }
            }
            if (state.Mouse.LeftButtonDown) {
                if (playerControls.input.Shift) {
                    playerControls.input.FireSecondary = true;
                } else {
                    playerControls.input.FirePrimary = true;
                }
                playerControls.input.UsingMouse = true;
            }
            if (state.Mouse.RightButtonDown) {
                playerControls.input.Thrust = true;
                playerControls.input.UsingMouse = true;
            }
        }
        prevMouse = state;
        return base.ProcessMouse(state);
    }
}
public class Noisemaker : Ob<EntityAdded>, IDestroyedListener, IDamagedListener, IWeaponListener, Ob<Projectile.Detonated> {
    PlayerShip player;
    public static readonly SoundBuffer
        generic_fire = Load(nameof(generic_fire)),
        generic_explosion = Load(nameof(generic_explosion)),
        generic_exhaust = Load(nameof(generic_exhaust)),
        generic_damage = Load(nameof(generic_damage)),
        generic_shield_damage = Load(nameof(generic_shield_damage)),
        target_set = Load(nameof(target_set)),
        target_clear = Load(nameof(target_clear)),
        generic_missile = Load(nameof(generic_missile)),
        autopilot_on = Load(nameof(autopilot_on)),
        autopilot_off=Load(nameof(autopilot_off)),
        dock_start = Load(nameof(dock_start)),
        dock_end = Load(nameof(dock_end));
    ListIndex<Sound> exhaust = new(new(Enumerable.Range(0, 16).Select(i => new Sound() { Volume = 10 })));
    ListIndex<Sound> gunfire = new(new(Enumerable.Range(0, 8).Select(i => new Sound() { Volume = 50 })));
    ListIndex<Sound> damage = new(new(Enumerable.Range(0, 8).Select(i => new Sound() { Volume = 50 })));
    ListIndex<Sound> explosion = new(new(Enumerable.Range(0, 4).Select(i => new Sound() { Volume = 75 })));
    Sound targeting = new() { Volume = 50 },
        autopilot = new() { Volume = 50 },
        dock = new() { Volume = 50 };
    public Sound button_press = new(new SoundBuffer("RogueFrontierContent/sounds/button_press.wav")) {
        Volume = 33 };
    
    List<IShip> exhaustList = new();
    const float distScale = 1 / 16f;
    public static SoundBuffer Load(string file) => new($"RogueFrontierContent/Sounds/{file}.wav");
    public Noisemaker(PlayerShip player) {
        this.player = player;

        player.onTargetChanged += new Container<PlayerShip.TargetChanged>(pl => {
            targeting.SoundBuffer = pl.targetIndex == -1 ? target_clear : target_set;
            targeting.Play();
        });
    }
    public void PlayAutopilot(bool active) {
        autopilot.SoundBuffer = active ? autopilot_on : autopilot_off;
        autopilot.Play();
    }
    public void PlayError() {
        targeting.SoundBuffer = target_clear;
        targeting.Play();
    }
    public void PlayDocking(bool start) {

        dock.SoundBuffer = start ? dock_start : dock_end;
        dock.Play();
    }
    double time;
    public void Update(double delta) {
        time += delta;
        if(time > 0.4) {
            time = 0;
            var s = player.world.entities.all.OfType<IShip>()
                .Where(s => s.thrusting)
                .OrderBy(sh => player.position.Dist(sh.position))
                .Zip(exhaust.list);
            foreach((var ship, var sound) in exhaustList.Zip(exhaust.list)) {
                sound.Stop();
            }
            exhaustList.Clear();
            foreach ((var ship, var sound) in s) {
                PlaySoundFrom(sound, ship, generic_exhaust);
                exhaustList.Add(ship);
            }
        } else {
            foreach((var ship, var sound) in exhaustList.Zip(exhaust.list)) {
                sound.Position = player.position.To(ship.position).Scale(distScale).ToVector3f();
            }
        }
    }
    public void Register(Universe u) {
        var obj = u.GetAllEntities().OfType<Entity>();
        foreach (var a in obj) {
            Register(a);
        }
        u.onEntityAdded += this;
    }
    private void Register(Entity e) {
        if (e is ActiveObject a) {
            ((IDestroyedListener)this).Register(a);
            ((IDamagedListener)this).Register(a);
            ((IWeaponListener)this).Register(a);
        }
    }
    public Sound GetNextChannel(ListIndex<Sound> i) => i.GetFirstOrNext(s => s.Status == SoundStatus.Stopped);
    public void Observe(EntityAdded ev) {
        Register(ev.e);
    }
    public void Observe(IDestroyedListener.Destroyed ev) {
        var e = ev.destroyed;
        if(e.world != player.world) {
            return;
        }
        PlaySoundFrom(explosion, e, generic_explosion);
    }
    public void Observe(IDamagedListener.Damaged ev) {
        var (e, p) = ev;
        if (e.world != player.world) {
            return;
        }
        PlaySoundFrom(damage, e,
            p.hitHull ? generic_damage : generic_shield_damage);
    }
    public void Observe(IWeaponListener.WeaponFired ev) {
        var (e, w, pr) = ev;
        if (e.world != player.world) {
            return;
        }
        foreach (var p in pr) {
            p.onDetonated += this;
        }

        PlaySoundFrom(gunfire, e,
            w.desc?.sound ?? generic_fire);
    }
    public void Observe(Projectile.Detonated d) {
        var sb = d.source.desc.detonateSound;
        if (sb == null) {
            return;
        }
        PlaySoundFrom(gunfire,
            d.source,
            sb);
    }
    private void PlaySoundFrom(ListIndex<Sound> s, Entity e, SoundBuffer sb) =>
        PlaySoundFrom(GetNextChannel(s), e, sb);
    private void PlaySoundFrom(Sound s, Entity e, SoundBuffer sb) {

        s.Position = player.position.To(e.position).Scale(distScale).ToVector3f();
        s.SoundBuffer = sb;
        s.Play();
    }
}
public class BackdropConsole : ScreenSurface {
    public int Width => Surface.Width;
    public int Height => Surface.Height;
    public Camera camera;
    private readonly XY screenCenter;
    private Backdrop backdrop;
    public BackdropConsole(Viewport view) : base(view.Width, view.Height) {
        this.camera = view.camera;
        this.backdrop = view.world.backdrop;
        screenCenter = new(Width / 2f, Height / 2f);
    }
    public BackdropConsole(int width, int height, Backdrop backdrop, Camera camera) : base(width, height) {
        this.camera = camera;
        this.backdrop = backdrop;
        screenCenter = new XY(Width / 2f, Height / 2f);
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
    }
    public override void Render(TimeSpan drawTime) {
        Surface.Clear();
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                //var g = this.GetGlyph(x, y);
                var offset = new XY(x, Height - y) - screenCenter;
                var location = camera.position + offset.Rotate(camera.rotation);
                Surface.SetCellAppearance(x, y, backdrop.GetTile(location, camera.position));
            }
        }
        base.Render(drawTime);
    }
    public ColoredGlyph GetTile(int x, int y) {
        var offset = new XY(x, Height - y) - screenCenter;
        var location = camera.position + offset.Rotate(camera.rotation);
        return backdrop.GetTile(location, camera.position);
    }
}
public class Megamap : ScreenSurface {
    int Width => Surface.Width;
    int Height => Surface.Height;
    Camera camera;
    PlayerShip player;
    GeneratedLayer background;
    public double targetViewScale = 1, viewScale = 1;
    double time;
    Dictionary<(int, int), List<(Entity entity, double distance)?>> scaledEntities;
    XY screenSize, screenCenter;
    public byte alpha;
    public Megamap(Camera camera, PlayerShip player, GeneratedLayer back, int width, int height) : base(width, height) {
        this.camera = camera;
        this.player = player;
        this.background = back;

        screenSize = new(Width, Height);
        screenCenter = screenSize / 2;
    }
    public double delta => Math.Min(targetViewScale / (2 * 30), 1);
    public override bool ProcessKeyboard(Keyboard info) {
        var p = (Keys k) => info.IsKeyPressed(k);
        var d = (Keys k) => info.IsKeyDown(k);
        if (d(LeftControl) || d(RightControl)) {
            if (p(OemMinus)) {
                targetViewScale *= 2;
            }
            if (p(OemPlus)) {
                targetViewScale /= 2;
                if (targetViewScale < 1) {
                    targetViewScale = 1;
                }
            }
            if (p(D0)) {
                targetViewScale = 1;
            }
        } else {
            if (d(OemMinus)) {
                viewScale += delta;
                targetViewScale = viewScale;
            }
            if (d(OemPlus)) {
                viewScale -= delta;
                if (viewScale < 1) {
                    viewScale = 1;
                }
                targetViewScale = viewScale;
            }
        }
        if (p(M)) {
            if(targetViewScale >= 16) {
                targetViewScale = 1.0;
            } else {
                targetViewScale = Math.Floor(targetViewScale)*4;
            }
        }
        return base.ProcessKeyboard(info);
    }
    public override void Update(TimeSpan delta) {
        var d = targetViewScale - viewScale;
        /*
        if(Math.Abs(d) < 0.1) {
            viewScale += d;
        } else {
            viewScale += d / 10;
        }
        */
        viewScale += Math.MaxMagnitude(Math.MinMagnitude(d, Math.Sign(d)*0.1), d / 10);
        alpha = (byte)(255 * Math.Min(1, viewScale - 1));
        time += delta.TotalSeconds;
#nullable enable
        scaledEntities = player.world.entities.TransformSelectList<(Entity entity, double distance)?>(
                e => (screenCenter + ((e.position - player.position) / viewScale).Rotate(-camera.rotation)).flipY + (0, Height),
                ((int x, int y) p) => p is (> -1, > -1) && p.x < Width && p.y < Height,
                ent => ent is { tile: not null } and not ISegment && player.GetVisibleDistanceLeft(ent) is var dist and > 0 ? (ent, dist) : null
            );
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        Surface.Clear();

        var alpha = this.alpha;
        if (alpha > 0) {
            if(alpha < 128) {
                alpha = (byte)(128 * Math.Sqrt(alpha / 128f));
            }
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    var offset = new XY((x - screenCenter.x) * viewScale, (y - screenCenter.y) * viewScale).Rotate(camera.rotation);
                    var pos = player.position + offset;
                    bool IsVisible(ColoredGlyph cg) =>
                        cg.GlyphCharacter != ' ' || cg.Background != Color.Transparent;
                    void Render(ColoredGlyph cg) {
                        var glyph = cg.Glyph;
                        var background = cg.Background.PremultiplySet(alpha);
                        var foreground = cg.Foreground.PremultiplySet(alpha);
                        Surface.SetCellAppearance(x, Height - y, new ColoredGlyph(foreground, background, glyph));
                    }
                    var environment = player.world.backdrop.planets.GetTile(pos.Snap(viewScale), XY.Zero);
                    if (IsVisible(environment)) {
                        Render(environment);
                        continue;
                    }
                    /*
                    environment = player.world.backdrop.orbits.GetTile(pos.Snap(viewScale), XY.Zero);
                    if (IsVisible(environment)) {
                        Render(environment);
                        continue;
                    }
                    */
                    environment = player.world.backdrop.nebulae.GetTile(pos.Snap(viewScale), XY.Zero);
                    if (IsVisible(environment)) {
                        Render(environment);
                        continue;
                    }
                    var starlight = player.world.backdrop.starlight.GetBackgroundFixed(pos).PremultiplySet(255);
                    var cg = this.background.GetTileFixed(new XY(x, y));
                    //Make sure to clone this so that we don't apply alpha changes to the original
                    var glyph = cg.Glyph;
                    var background = cg.Background.BlendPremultiply(starlight, alpha);
                    var foreground = cg.Foreground.PremultiplySet(alpha);
                    Surface.SetCellAppearance(x, Height - y, new ColoredGlyph(foreground, background, glyph));
                }
            }
            var visiblePerimeter = new Rectangle(new(Width / 2, Height / 2), (int)(Width / (viewScale * 2) - 1), (int)(Height / (viewScale * 2) - 1));
            foreach (var point in visiblePerimeter.PerimeterPositions()) {
                var b = Surface.GetBackground(point.X, point.Y);
                Surface.SetBackground(point.X, point.Y, b.BlendPremultiply(new Color(255, 255, 255, (int)(128/viewScale))));
            }
            
            foreach ((var offset, var visible) in scaledEntities) {
                var (x, y) = offset;
                (var entity, var distance) = visible[(int)time % visible.Count].Value;
                var t = entity.tile;
                var f = t.Foreground;
                //Apply stealth
                const double threshold = 16;
                if (distance < threshold) {
                    f = f.SetAlpha((byte)(255 * distance / threshold));
                }
                t = new(f, Surface.GetBackground(x, y).Blend(t.Background), t.Glyph);
                Surface.SetCellAppearance(x, y, t);
            }
            /*
            Parallel.ForEach(scaledEntities.space, pair => {
                (var offset, var ent) = pair;
            });
            */
            /*
            var scaledEffects = player.world.effects.space.DownsampleSet(viewScale);
            foreach ((var p, HashSet<Effect> set) in scaledEffects.space) {
                var visible = set.Where(t => !(t is ISegment)).Where(t => t.tile != null);
                if (visible.Any()) {
                    var e = visible.ElementAt((int)time % visible.Count());
                    var offset = (e.position - player.position) / viewScale;
                    var (x, y) = screenCenter + offset.Rotate(-camera.rotation);
                    y = Height - y;
                    if (x > -1 && x < Width && y > -1 && y < Height) {
                        if (rendered.Contains((x, y))) {
                            continue;
                        }

                        var t = new ColoredGlyph(e.tile.Foreground, this.GetBackground(x, y), e.tile.Glyph);
                        this.SetCellAppearance(x, y, t);
                    }
                }
            }
            */
        }
        base.Render(delta);
    }
}
public class Vignette : ScreenSurface, Ob<PlayerShip.Damaged> {
    public int Width => Surface.Width;
    public int Height => Surface.Height;
    PlayerShip player;
    public float powerAlpha;
    public HashSet<EffectParticle> particles;
    public XY screenCenter;
    public int ticks;
    public Random r;
    public int[,] grid;
    public bool chargingUp;
    int recoveryTime;
    public int lightningHit;
    public int flash;
    public void Observe(PlayerShip.Damaged ev) {
        var (pl, pr) = ev;
        if (pr.desc.lightning) {
            lightningHit = 5;
        }
        if (pr.desc.blind?.Roll() is int db) {
            flash = Math.Max(db, flash);
        }
    }
    public Vignette(PlayerShip player, int width, int height) : base(width, height) {
        this.player = player;
        player.onDamaged += this;
        FocusOnMouseClick = false;
        powerAlpha = 0;
        particles = new();
        screenCenter = new(width / 2, height / 2);
        r = new();
        grid = new int[width, height];
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                grid[x, y] = r.Next(0, 240);
            }
        }
    }
    public override void Update(TimeSpan delta) {
        var charging = player.powers.Where(p => p.charging);
        if (charging.Any()) {
            var charge = Math.Min(1, charging.Max(p => p.invokeCharge / p.invokeDelay));
            if (powerAlpha < charge) {
                powerAlpha += (float)((charge - powerAlpha) / 10f);
            }
            if (recoveryTime < 360) {
                recoveryTime++;
            }
            chargingUp = true;
        } else {
            if (player.CheckGate(out Stargate gate)) {
                float targetAlpha = 1;
                if (powerAlpha < targetAlpha) {
                    powerAlpha += (targetAlpha - powerAlpha) / 30;
                }
            } else {
                chargingUp = false;
                powerAlpha -= (float)(powerAlpha * delta.TotalSeconds);
                if (powerAlpha < 0.01 && recoveryTime > 0) {
                    recoveryTime -= (int)(60 * delta.TotalSeconds);
                }
            }
        }
        ticks++;
        if (ticks % 5 == 0 && player.ship.disruption?.ticksLeft > 30) {
            int i = 0;
            var screenPerimeter = new Rectangle(i, i, Width - i * 2, Height - i * 2);
            foreach (var p in screenPerimeter.PerimeterPositions().Select(p => new XY(p))) {
                if (r.Next(0, 10) == 0) {
                    int speed = 15;
                    int lifetime = 60;
                    var v = new XY(p.xi == 0 ? speed : p.xi == screenPerimeter.Width - 1 ? -speed : 0,
                                    p.yi == 0 ? speed : p.yi == screenPerimeter.Height - 1 ? -speed : 0);
                    particles.Add(new EffectParticle(p, new(Color.Cyan, Color.Transparent, '#'), lifetime) { Velocity = v });
                }
            }
        }
        Parallel.ForEach(particles, p => {
            p.position += p.Velocity / Program.TICKS_PER_SECOND;
            p.lifetime--;
            p.Velocity -= p.Velocity / 15;

            p.tile.Foreground = p.tile.Foreground.SetAlpha(
                (byte)(255 * Math.Min(p.lifetime / 30f, 1))
                );
        });
        particles.RemoveWhere(p => !p.active);
        lightningHit--;
        flash--;
        base.Update(delta);
    }
    public override void Render(TimeSpan delta) {
        Surface.Clear();
        //XY screenSize = new XY(Width, Height);
        //Set the color of the vignette
        Color borderColor = Color.Black;
        int borderSize = 2;
        if (powerAlpha > 0) {
            var v = new Color(204, 153, 255, (int)(255 * (float)Math.Min(1, powerAlpha * 1.5)));
            borderColor = borderColor.Blend(v).Premultiply();
            borderSize += (int)(12 * powerAlpha);
        }
        Mortal();
        void Mortal() {
            if (player.mortalTime > 0) {
                borderColor = borderColor.Blend(Color.Red.SetAlpha((byte)(Math.Min(1, player.mortalTime / 4.5) * 255)));
                var fraction = (player.mortalTime - Math.Truncate(player.mortalTime));
                borderSize += (int)(6 * fraction);
            }
        }
        if(flash > 0) {
            borderSize += Math.Min(5, 5 * flash / 30);
            borderColor = borderColor.Blend(Color.White.SetAlpha((byte)Math.Min(255, 255 * flash / 30)));
        } else if (player.ship.disruption?.ticksLeft > 0) {
            var ticks = player.ship.disruption.ticksLeft;
            var strength = Math.Min(ticks / 60f, 1);
            borderSize += (int)(5 * strength);
            borderColor = borderColor.Blend(Color.Cyan.SetAlpha((byte)(128 * strength)));
        } else {
            var b = player.world.backdrop.starlight.GetBackgroundFixed(player.position);
            var br = 255 * b.GetBrightness();
            borderSize += (int)(0*5f * Math.Pow(br / 255, 1.4));
            borderColor = borderColor.Blend(b.SetAlpha((byte)(br)));
        }
        if(player.ship.blindTicks > 0) {
            for (int i = 0; i < borderSize; i++) {
                var d = 1d * i / borderSize;
                d = Math.Pow(d, 1.4);
                byte alpha = (byte)(255 - 255 * d);
                var screenPerimeter = new Rectangle(i, i, Width - i * 2, Height - i * 2);
                foreach (var point in screenPerimeter.PerimeterPositions()) {
                    //var back = this.GetBackground(point.X, point.Y).Premultiply();
                    var (x, y) = point;

                    var inc = r.Next(102);
                    var c = borderColor.Add(inc, inc, inc).Gray().SetAlpha(alpha);
                    Surface.SetBackground(x, y, c);
                }
            }
        } else {
            for (int i = 0; i < borderSize; i++) {
                var d = 1d * i / borderSize;
                d = Math.Pow(d, 1.4);
                byte alpha = (byte)(255 - 255 * d);
                var c = borderColor.SetAlpha(alpha);
                var screenPerimeter = new Rectangle(i, i, Width - i * 2, Height - i * 2);
                foreach (var point in screenPerimeter.PerimeterPositions()) {
                    //var back = this.GetBackground(point.X, point.Y).Premultiply();
                    var (x, y) = point;
                    Surface.SetBackground(x, y, c);
                }
            }
        }
        if (lightningHit > 0) {
            var i = 2;
            var c = new Color(255, 0, 0, 200 * lightningHit/5);
            foreach(var p in new Rectangle(i, i, Width - i * 2, Height - i * 2).PerimeterPositions()) {
                var (x, y) = p;
                Surface.SetBackground(x, y, c);
            }
        }
        foreach (var p in particles) {
            var (x, y) = p.position;
            var (fore, glyph) = (p.tile.Foreground, p.tile.Glyph);
            Surface.SetCellAppearance(x, y, new ColoredGlyph(fore, Surface.GetBackground(x, y), glyph));
        }

        base.Render(delta);
    }
}
public class Readout : ScreenSurface {
    /*
    struct Snow {
        public char c;
        public double factor;
    }
    Snow[,] snow;
    */
    Camera camera;
    PlayerShip player;
    public double viewScale;

    public int arrowDistance;
    public int Width => Surface.Width;
    public int Height => Surface.Height;
    XY screenSize => new XY(Width, Height);
    XY screenCenter => screenSize / 2;

    public Readout(Camera camera, PlayerShip player, int width, int height) : base(width, height) {
        this.camera = camera;
        this.player = player;

        //arrowDistance = Math.Min(Width, Height)/2 - 6;
        arrowDistance = 24;
        /*
        char[] particles = {
            '%', '&', '?', '~'
        };
        snow = new Snow[width, height];
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                snow[x, y] = new Snow() {
                    c = particles.GetRandom(r),
                    factor = r.NextDouble()
                };
            }
        }
        */
        FocusOnMouseClick = false;
    }
    public override void Update(TimeSpan delta) {
        if (player.GetTarget(out var playerTarget)) {
            DrawTargetArrow(playerTarget, Color.Yellow);
        }
        foreach (var t in player.tracking.Keys) {
            DrawTargetArrow(t, Color.SpringGreen);
        }
        //var autoTarget = player.devices.Weapons.Select(w => w.target).FirstOrDefault();
        foreach (var autoTarget in player.devices.Weapon.Select(w => w.target).Except(new ActiveObject[] { null, playerTarget })) {
            DrawTargetArrow(autoTarget, Color.Yellow);
        }
        void DrawTargetArrow(ActiveObject target, Color c) {
            var o = (target.position - player.position);
            var offset = o / viewScale;
            if (Math.Abs(offset.x) > Width / 2 - 6 || Math.Abs(offset.y) > Height / 2 - 6) {
                var offsetNormal = offset.normal.flipY;
                //var p = screenCenter + offsetNormal * arrowDistance;
                var smallScreen = screenSize - (20, 20);
                var smallCenter = smallScreen / 2;
                var p = Main.GetBoundaryPoint(smallScreen, offsetNormal.angleRad);
                var centerOffset = (p - smallCenter).flipY;

                var loc = player.position + centerOffset;
                EffectParticle.DrawArrow(player.world, loc, offset, c);
            }
            if(target is Station st) {
                Heading.Box(st, c);
            } else {
                Heading.Crosshair(target.world, target.position, c);
            }
        }
        base.Update(delta);
    }
    public override void Render(TimeSpan drawTime) {
        Surface.Clear();
        var messageY = Height * 3 / 5;
        int targetX = 48, targetY = 1;
        int tick = player.world.tick;
        for (int i = 0; i < player.messages.Count; i++) {
            var message = player.messages[i];
            var line = message.Draw();
            var x = Width * 3 / 4 - line.Count();
            Surface.Print(x, messageY, line);
            if (message is Transmission t) {
                //Draw a line from message to source

                var screenCenterOffset = new XY(Width * 3 / 4, Height - messageY) - screenCenter;
                var messagePos = (player.position + screenCenterOffset).roundDown;

                var sourcePos = t.source.position.roundDown;
                sourcePos = player.position + (sourcePos - player.position).Rotate(-camera.rotation) / viewScale;
                if (messagePos.yi == sourcePos.yi) {
                    continue;
                }

                int screenX = Width * 3 / 4;
                int screenY = messageY;

                var (f, b) = line.Any() ? (line[0].Foreground, line[0].Background) : (Color.White, Color.Transparent);

                screenX++;
                messagePos.x++;
                Surface.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
                    e = Line.Double,
                    n = Line.Single,
                    s = Line.Single
                }]));
                screenX++;
                messagePos.x++;

                for (int j = 0; j < i; j++) {
                    Surface.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
                        e = Line.Double,
                        w = Line.Double
                    }]));
                    screenX++;
                    messagePos.x++;
                }
                /*
                var offset = sourcePos - messagePos;
                int screenLineY = Math.Max(-(Height - screenY - 2), Math.Min(screenY - 2, offset.yi < 0 ? offset.yi - 1 : offset.yi));
                int screenLineX = Math.Max(-(screenX - 2), Math.Min(Width - screenX - 2, offset.xi));
                */
                var offset = sourcePos - player.position;
                var offsetLeft = new XY(0, 0);
                bool truncateX = Math.Abs(offset.x) > Width / 2 - 3;
                bool truncateY = Math.Abs(offset.y) > Height / 2 - 3;
                if (truncateX || truncateY) {
                    var sourcePosEdge = Helper.GetBoundaryPoint(screenSize, offset.angleRad) - screenSize / 2 + player.position;
                    offset = sourcePosEdge - player.position;
                    if (truncateX) { offset.x -= Math.Sign(offset.x) * (i + 2); }
                    if (truncateY) { offset.y -= Math.Sign(offset.y) * (i + 2); }
                    offsetLeft = sourcePos - sourcePosEdge;
                }
                offset += player.position - messagePos;
                int screenLineY = offset.yi + (offset.yi < 0 ? 0 : 1);
                int screenLineX = offset.xi;
                if (screenLineY != 0) {
                    Surface.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
                        n = offset.y > 0 ? Line.Double : Line.None,
                        s = offset.y < 0 ? Line.Double : Line.None,
                        w = Line.Double,
                        e = offset.y == 0 ? Line.Double : Line.None
                    }]));
                    screenY -= Math.Sign(screenLineY);
                    screenLineY -= Math.Sign(screenLineY);

                    while (screenLineY != 0) {
                        Surface.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
                            n = Line.Double,
                            s = Line.Double
                        }]));
                        screenY -= Math.Sign(screenLineY);
                        screenLineY -= Math.Sign(screenLineY);
                    }
                }
                if (screenLineX != 0) {
                    Surface.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
                        n = offset.y < 0 ? Line.Double : offset.y > 0 ? Line.None : Line.Single,
                        s = offset.y > 0 ? Line.Double : offset.y < 0 ? Line.None : Line.Single,

                        e = offset.x > 0 ? Line.Double : offset.x < 0 ? Line.None : Line.Single,
                        w = offset.x < 0 ? Line.Double : offset.x > 0 ? Line.None : Line.Single
                    }]));
                    screenX += Math.Sign(screenLineX);
                    screenLineX -= Math.Sign(screenLineX);

                    while (screenLineX != 0) {
                        Surface.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
                            e = Line.Double,
                            w = Line.Double
                        }]));
                        screenX += Math.Sign(screenLineX);
                        screenLineX -= Math.Sign(screenLineX);
                    }
                }
                /*
                screenX += Math.Sign(offsetLeft.x);
                screenY -= Math.Sign(offsetLeft.y);
                this.SetCellAppearance(screenX, screenY, new ColoredGlyph('*', f, b));
                */
            }
            messageY++;
        }
        const int BAR = 8;


        ActiveObject target;
        if (player.GetTarget(out target)) {
            Surface.Print(targetX, targetY++, "[Target]", Color.White, Color.Black);
        } else if(player.devices.Weapon.Select(w => w.target).Except(new ActiveObject[] { null }).FirstOrDefault() is ActiveObject found) {
            target = found;
            Surface.Print(targetX, targetY++, "[Auto]", Color.White, Color.Black);
        } else {
            goto SkipTarget;
        }
        Surface.Print(targetX, targetY++, target.name, player.tracking.ContainsKey(target) ? Color.SpringGreen : Color.White, Color.Black);
        PrintTarget(targetX, targetY, target);
    SkipTarget:
        
        void PrintTarget(int x, int y, ActiveObject target) {
            var b = Color.Black;
            switch (target) {
                case AIShip ai:
                    Print(ai.devices);
                    PrintHull(ai.hull);
                    break;
                case Station s:
                    PrintHull(s.damageSystem);
                    break;
            }
            void Print(Circuit devices) {
                var solars = devices.Solar;
                var reactors = devices.Reactor;
                var weapons = devices.Weapon;
                var shields = devices.Shield;
                var misc = devices.Installed.OfType<Service>();
                foreach (var reactor in reactors) {
                    ColoredString bar;
                    if (reactor.energy > 0) {
                        (var arrow, var f) = reactor.energyDelta switch {
                            < 0 => ('<', Color.Yellow),
                            > 0 => ('>', Color.Cyan),
                            _   => ('=', Color.White)
                        };
                        int length = (int)Math.Ceiling(BAR * reactor.energy / reactor.desc.capacity);
                        bar = new ColoredString(new string('=', length - 1) + arrow, f, b)
                            + new ColoredString(new string('=', BAR - length), Color.Gray, b);
                    } else {
                        bar = new(new string('=', BAR), Color.Gray, b);
                    }

                    int l = (int)Math.Ceiling(-BAR * (double)reactor.energyDelta / reactor.maxOutput);
                    for (int i = 0; i < l; i++) {
                        bar[i].Background = Color.DarkKhaki;
                    }

                    Surface.Print(x, y,
                        new ColoredString("[", Color.White, b)
                        + bar
                        + new ColoredString("]", Color.White, b)
                        + " "
                        + new ColoredString($"{reactor.source.type.name}", Color.White, b)
                        );
                    y++;
                }
                if (solars.Any()) {
                    foreach (var s in solars) {
                        int length = (int)Math.Ceiling(BAR * (double)s.maxOutput / s.desc.maxOutput);
                        int sublength = s.maxOutput > 0 ? (int)Math.Ceiling(length * (-s.energyDelta) / s.maxOutput) : 0;
                        var bar = new ColoredString(new string('=', sublength), Color.Yellow, Color.DarkKhaki)
                            + new ColoredString(new string('=', length - sublength), Color.Cyan, b)
                            + new ColoredString(new string('=', BAR - length), Color.Gray, b);
                        /*
                        int l = (int)Math.Ceiling(-16f * s.maxOutput / s.desc.maxOutput);
                        for (int i = 0; i < l; i++) {
                            bar[i].Background = Color.DarkKhaki;
                            bar[i].Foreground = Color.Yellow;
                        }
                        */
                        Surface.Print(x, y,
                            new ColoredString("[", Color.White, b)
                            + bar
                            + new ColoredString("]", Color.White, b)
                            + " "
                            + new ColoredString($"{s.source.type.name}", Color.White, b)
                            );
                        y++;
                    }
                    y++;
                }
                if (weapons.Any()) {
                    int i = 0;
                    foreach (var w in weapons) {
                        string enhancement;
                        if (w.source.mod?.empty == false) {
                            enhancement = "[+]";
                        } else {
                            enhancement = "";
                        }
                        string tag = $"] {w.source.type.name} {enhancement}";
                        Color foreground =
                            false ?
                                Color.Gray :
                            w.firing || w.delay > 0 ?
                                Color.Yellow :
                            Color.White;
                        Surface.Print(x, y,
                            new ColoredString("[", Color.White, b)
                            + w.GetBar(BAR)
                            + new ColoredString(tag, foreground, b));
                        y++;
                        i++;
                    }
                    y++;
                }
                if (misc.Any()) {
                    foreach (var m in misc) {
                        string tag = m.source.type.name;
                        var f = Color.White;
                        Surface.Print(x, y, $"{tag}", f, b);
                        y++;
                    }
                    y++;
                }
                if (shields.Any()) {
                    foreach (var s in shields.Reverse<Shield>()) {
                        string name = s.source.type.name;
                        var f =
                            false ?
                                Color.Gray :
                            s.hp == 0 || s.delay > 0 ?
                                Color.Yellow :
                            s.hp < s.desc.maxHP ?
                                Color.Cyan :
                            Color.White;
                        int l = BAR * s.hp / s.desc.maxHP;
                        Surface.Print(x, y, "[", f, b);
                        Surface.Print(x + 1, y, new('=', BAR), Color.Gray, b);
                        Surface.Print(x + 1, y, new('=', l), f, b);
                        Surface.Print(x + 1 + BAR, y, $"] {name}", f, b);
                        y++;
                    }
                    y++;
                }
            }
            void PrintHull(HullSystem hull) {
                switch (hull) {
                    case LayeredArmor las: {
                            foreach (var armor in las.layers.Reverse<Armor>()) {
                                var f =
                                    (tick - armor.lastDamageTick) < 15 ?
                                        Color.Yellow :
                                        (armor.hp > 0 ?
                                            Color.White :
                                            (armor.canAbsorb ?
                                                Color.Orange :
                                                Color.Gray)
                                            );
                                var bb =
                                    armor.decay.Any() ?
                                        Color.Black.Blend(Color.Red.SetAlpha(128)) :
                                    tick - armor.lastRegenTick < 15 ?
                                        Color.Black.Blend(Color.Cyan.SetAlpha(128)) :
                                    !armor.allowSpecial ?
                                        Color.Black.Blend(Color.Yellow.SetAlpha(128)) :
                                        b;
                                int l = BAR * armor.hp / Math.Max(1, armor.maxHP);
                                Surface.Print(x, y, Main.Concat(
                                    ("[", f, b),
                                    (new('=', l), f, b),
                                    (new('=', BAR - l), Color.Gray, b),
                                    ($"] [{armor.hp,3}/{armor.maxHP,3}] ", f, b),
                                    ($"{armor.source.type.name}", f, bb)
                                ));
                                y++;
                            }
                            break;
                        }
                    case HP hp: {
                            var f = Color.White;
                            Surface.Print(x, y, "[", f, b);
                            Surface.Print(x + 1, y, new('=', BAR), Color.Gray, b);
                            Surface.Print(x + 1, y, new('=', BAR * hp.hp / hp.maxHP), f, b);
                            Surface.Print(x + 1 + BAR, y, $"] HP: {hp.hp}", f, b);
                            break;
                        }
                }
            }
        }
        //Print Player
        {
            int x = 3;
            int y = 3;
            var b = Color.Black;
            var ship = player.ship;
            var devices = ship.devices;
            var solars = devices.Solar;
            var reactors = devices.Reactor;
            var weapons = devices.Weapon;
            var shields = devices.Shield;
            var misc = devices.Installed.OfType<Service>();

            {
                double totalFuel = reactors.Sum(r => r.energy),
                       maxFuel = reactors.Sum(r => r.desc.capacity),
                       netDelta = reactors.Sum(r => r.energyDelta),
                       totalSolar = solars.Sum(s => s.maxOutput);
                ColoredString bar;

                if (totalFuel > 0) {
                    (var arrow, var f) = netDelta switch {
                        < 0 => ('<', Color.Yellow),
                        > 0 => ('>', Color.Cyan),
                        _   => ('=', Color.White),
                    };

                    int length = (int)Math.Ceiling(BAR * totalFuel / maxFuel);
                    bar = new ColoredString(new string('=', length - 1) + arrow, f, b)
                        + new ColoredString(new string('=', BAR - length), Color.Gray, b);
                } else {
                    bar = new ColoredString(new string('=', BAR), Color.Gray, b);
                }

                int totalUsed = player.energy.totalOutputUsed,
                    totalMax = player.energy.totalOutputMax;
                int l;

                l = (int)Math.Ceiling(BAR * (double)totalUsed / totalMax);
                for (int i = 0; i < l; i++) {
                    bar[i].Background = Color.DarkKhaki;
                    //bar[i].Foreground = Color.Yellow;
                }
                l = (int)Math.Ceiling(BAR * (double)totalSolar / totalMax);
                for (int i = 0; i < l; i++) {
                    bar[i].Background = Color.DarkCyan;
                }
                Surface.Print(x, y++,
                      new ColoredString("[", Color.White, b)
                    + bar
                    + new ColoredString("]", Color.White, b)
                    + " "
                    + new ColoredString($"[{totalUsed,3}/{totalMax,3}] Total Power", Color.White, b)
                    );
            }

            if (reactors.Any()) {
                foreach (var reactor in reactors) {
                    ColoredString bar;
                    if (reactor.energy > 0) {
                        (var arrow, var f) = reactor.energyDelta switch {
                            < 0 => ('<', Color.Yellow),
                            > 0 => ('>', Color.Cyan),
                            _ => ('=', Color.White)
                        };

                        int length = (int)Math.Ceiling(BAR * reactor.energy / reactor.desc.capacity);
                        bar = new ColoredString(new string('=', length - 1) + arrow, f, b)
                            + new ColoredString(new string('=', BAR - length), Color.Gray, b);
                    } else {
                        bar = new ColoredString(new string('=', BAR), Color.Gray, b);
                    }

                    int l = (int)Math.Ceiling(-BAR * (double)reactor.energyDelta / reactor.maxOutput);
                    for (int i = 0; i < l; i++) {
                        bar[i].Background = Color.DarkKhaki;
                    }

                    Surface.Print(x, y,
                        new ColoredString("[", Color.White, b)
                        + bar
                        + new ColoredString("]", Color.White, b)
                        + " "
                        + new ColoredString($"[{Math.Abs(reactor.energyDelta),3}/{reactor.maxOutput,3}] {reactor.source.type.name}", reactor.energy > 0 ? Color.White : Color.Gray, b)
                        );
                    y++;
                }
                y++;
            }
            if (solars.Any()) {
                foreach (var s in solars) {
                    int length = (int)Math.Ceiling(BAR * (double)s.maxOutput / s.desc.maxOutput);
                    int sublength = s.maxOutput > 0 ? (int)Math.Ceiling(length * (-s.energyDelta) / s.maxOutput) : 0;
                    var bar = new ColoredString(new string('=', sublength), Color.Yellow, Color.DarkKhaki)
                            + new ColoredString(new string('=', length - sublength), Color.Cyan, b)
                            + new ColoredString(new string('=', BAR - length), Color.Gray, b);
                    /*
                    int l = (int)Math.Ceiling(-16f * s.maxOutput / s.desc.maxOutput);
                    for (int i = 0; i < l; i++) {
                        bar[i].Background = Color.DarkKhaki;
                        bar[i].Foreground = Color.Yellow;
                    }
                    */
                    Surface.Print(x, y,
                        new ColoredString("[", Color.White, b)
                        + bar
                        + new ColoredString("]", Color.White, b)
                        + " "
                        + new ColoredString($"[{Math.Abs(s.energyDelta),3}/{s.maxOutput,3}] {s.source.type.name}", Color.White, b)
                        );
                    y++;
                }
                y++;
            }
            if (weapons.Any()) {
                int i = 0;
                foreach (var w in weapons) {
                    string enhancement;
                    if (w.source.mod?.empty == false) {
                        enhancement = "[+]";
                    } else {
                        enhancement = "";
                    }
                    var tag = $"{(i == player.primary.index ? "->" : i == player.secondary.index ? "=>" : "  ")}{w.GetReadoutName()} {enhancement}";
                    var foreground =
                        player.energy.off.Contains(w) ?
                            Color.Gray :
                        w.firing || w.delay > 0 ?
                            Color.Yellow :
                        Color.White;
                    Surface.Print(x, y,
                        new ColoredString("[", Color.White, b)
                        + w.GetBar(BAR)
                        + new ColoredString(tag, foreground, b));
                    y++;
                    i++;
                }
                y++;
            }
            if (misc.Any()) {
                foreach (var m in misc) {
                    var tag = m.source.type.name;
                    var f = Color.White;
                    Surface.Print(x, y, $"[{new string('-', BAR)}] {tag}", f, b);
                    y++;
                }
                y++;
            }
            if (shields.Any()) {
                foreach (var s in shields.Reverse<Shield>()) {
                    var name = s.source.type.name;
                    var f = player.energy.off.Contains(s) ? Color.Gray :
                        s.hp == 0 || s.delay > 0 ? Color.Yellow :
                        s.hp < s.desc.maxHP ? Color.Cyan :
                        Color.White;
                    var l = BAR * s.hp / s.desc.maxHP;
                    Surface.Print(x, y, Main.Concat(
                        ("[", f, b),
                        (new('=', l), f, b),
                        (new('=', BAR - l), Color.Gray, b),
                        ($"] [{s.hp,3}/{s.desc.maxHP,3}] {name}", f, b)
                    ));
                    y++;
                }
            }
            switch (player.hull) {
                case LayeredArmor las: {
                        foreach (var armor in las.layers.Reverse<Armor>()) {
                            var f =
                                    (tick - armor.lastDamageTick) < 15 ?
                                        Color.Yellow :
                                        (armor.hp > 0 ?
                                            Color.White :
                                            (armor.canAbsorb ?
                                                Color.Orange :
                                                Color.Gray)
                                            );
                            var bb =
                                armor.decay.Any() ?
                                    Color.Black.Blend(Color.Red.SetAlpha(128)) :
                                tick - armor.lastRegenTick < 15 ?
                                    Color.Black.Blend(Color.Cyan.SetAlpha(128)) :
                                !armor.allowSpecial ?
                                    Color.Black.Blend(Color.Yellow.SetAlpha(128)) :
                                    
                                    b;
                            int l = BAR * armor.hp / Math.Max(1, armor.maxHP);
                            Surface.Print(x, y, Main.Concat(
                                ("[", f, b),
                                (new('=', l), f, b),
                                (new('=', BAR - l), Color.Gray, b),
                                ($"] [{armor.hp,3}/{armor.maxHP,3}] ", f, b),
                                ($"{armor.source.type.name}", f, bb)
                            ));
                            y++;
                        }
                        break;
                    }
                case HP hp: {
                        var f = Color.White;
                        var l = BAR * hp.hp / hp.maxHP;
                        Surface.Print(x, y, Main.Concat(
                            ("[", f, b),
                            (new('=', l), f, b),
                            (new('=', BAR - l), Color.Gray, b),
                            ($"] HP: {hp.hp}", f, b)
                        ));
                        y++;
                        break;
                    }
            }
            y++;
            Surface.Print(x, y++, $"Stealth: {ship.stealth:0.00}");
            Surface.Print(x, y++, $"Visibility: {SStealth.GetVisibleRangeOf(player):0.00}");
        }
        base.Render(drawTime);
    }
}
public class Edgemap : ScreenSurface {
    public int Width => Surface.Width;
    public int Height => Surface.Height;
    Camera camera;
    PlayerShip player;
    public double viewScale;
    public Edgemap(Camera camera, PlayerShip player, int width, int height) : base(width, height) {
        this.camera = camera;
        this.player = player;
        FocusOnMouseClick = false;
        viewScale = 1;
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
    }
    public override void Render(TimeSpan drawTime) {
        Surface.Clear();
        var screenSize = new XY(Width - 2, Height - 2);
        var screenCenter = screenSize / 2;
        var halfWidth = Width / 2;
        var halfHeight = Height / 2;
        var range = 192;
        player.world.entities.FilterKeySelect<(Entity entity, double dist)?>(
            ((int, int) p) => (player.position - p).maxCoord < range,
            entity => entity is { tile:not null } and not ISegment && player.GetVisibleDistanceLeft(entity) is var d and > 0 ? (entity, d) : null,
            v => v != null)
            .ToList()
            .ForEach(pair => {
                (var entity, var dist) = pair.Value;
                var offset = (entity.position - player.position).Rotate(-camera.rotation);
                var (x, y) = (offset / viewScale).abs;
                if (x > halfWidth || y > halfHeight) {
                    (x, y) = Helper.GetBoundaryPoint(screenSize, offset.angleRad);
                    PrintTile(x, y, dist, entity);
                } else if (x > halfWidth - 4 || y > halfHeight - 4) {
                    (x, y) = screenCenter + offset + new XY(1, 1);
                    PrintTile(x, y, dist, entity);
                }
            });
        void PrintTile(int x, int y, double distance, Entity e) {
            switch(e) {
                case ActiveObject:
                case Projectile:
                case Wreck:
                    var c = e.tile.Foreground;
                    const int threshold = 16;
                    if (distance < threshold) {
                        c = c.SetAlpha((byte)(255 * distance / threshold));
                    }
                    Surface.SetCellAppearance(x, Height - y - 1, new ColoredGlyph(c, Color.Transparent, '#'));
                    break;
                default: return;
            }
        }
        base.Render(drawTime);
    }
}
public class Minimap : ScreenSurface {
    PlayerShip player;
    public int size;
    Camera camera;
    public double time;
    public byte alpha;

    public int Width => Surface.Width;
    public int Height => Surface.Height;
    XY screenSize, screenCenter;
    public Minimap(ScreenSurface parent, PlayerShip playerShip, int size, Camera camera) : base(size, size) {
        this.Position = new Point(parent.Surface.Width - size, 0);
        this.player = playerShip;
        this.size = size;
        this.camera = camera;

        screenSize = new(Width, Height);
        screenCenter = screenSize / 2;

        alpha = 255;
    }
    public override void Update(TimeSpan delta) {
        base.Update(delta);
        time += delta.TotalSeconds;
    }

    public override void Render(TimeSpan delta) {
        var halfSize = size / 2;

        var range = 192;
        var mapScale = (range / halfSize);

        var mapSample = player.world.entities.space.DownsampleSet(mapScale);


        var scaledEntities = player.world.entities.TransformSelectList<(Entity entity, double distance)?>(
            e => (screenCenter + ((e.position - player.position) / mapScale).Rotate(-camera.rotation)).flipY + (0, Height),
            ((int x, int y) p) => p is (> -1, > -1) && p.x < Width && p.y < Height,
            ent => ent is { tile:not null } and not ISegment && player.GetVisibleDistanceLeft(ent) is var dist && dist > 0 ? (ent, dist) : null
        );


        for (int x = 0; x < Surface.Width; x++) {

            for (int y = 0; y < Surface.Height; y++) {
                if (scaledEntities.TryGetValue((x, y), out var entities)) {
                    (var entity, var distance) = entities[(int)time % entities.Count()].Value;

                    var g = entity.tile.Glyph;
                    var f = entity.tile.Foreground;

                    const double threshold = 16;
                    if(distance < threshold) {
                        f = f.SetAlpha((byte)(255 * distance / threshold));
                    }

                    Surface.SetCellAppearance(x, y, new ColoredGlyph(f, Color.Black, g).PremultiplySet(alpha));
                } else {
                    var foreground = new Color(
                                255, 255, 255,
                                51 + ((x + y) % 2 == 0 ? 0 : 12));
                    Surface.SetCellAppearance(x, y,
                        new ColoredGlyph(foreground, Color.Black, '#')
                            .PremultiplySet(alpha)
                        );
                    
                }
            }
        }
        /*
        Parallel.For(0, Width, x => {
        });
        */
        base.Render(delta);
    }
}
public class CommunicationsWidget : ScreenSurface {
    PlayerShip playerShip;
    int ticks;
    CommandMenu? menu;
    public CommunicationsWidget(int width, int height, PlayerShip playerShip) : base(width, height) {
        this.playerShip = playerShip;
        menu = null;
    }
    public override void Update(TimeSpan delta) {
        if (menu?.IsVisible == true) {
            menu.Update(delta);
            return;
        }
        if (ticks % 30 == 0) {
            playerShip.wingmates.RemoveAll(w => !w.active);
        }
        base.Update(delta);
        ticks++;
    }
    public override bool ProcessKeyboard(Keyboard info) {
        if (menu?.IsVisible == true) {
            return menu.ProcessKeyboard(info);
        }
        foreach (var k in info.KeysPressed) {
            int index = keyToIndex(k.Character);
            if (index > -1 && index < 10 && index < playerShip.wingmates.Count) {
                var w = playerShip.wingmates[index];
                menu = new(this, playerShip, w) { Position = Position };
            }
        }
        return base.ProcessKeyboard(info);
    }
    public override void Render(TimeSpan delta) {
        if (menu?.IsVisible == true) {
            menu.Render(delta);
            return;
        }
        int x = 0;
        int y = 0;

        Surface.Clear();

        Color foreground = Color.White;
        if (ticks % 60 < 30) {
            foreground = Color.Yellow;
        }
        var back = Color.Black;
        Surface.Print(x, y++, "[Communications]", foreground, back);
        //this.Print(x, y++, "[Ship control locked]", foreground, back);
        Surface.Print(x, y++, "[ESC     -> cancel]", foreground, back);
        y++;
        /*
        if (playerShip.wingmates.Count(w => w.active) == 0) {
            playerShip.wingmates.AddRange(playerShip.world.entities.all.OfType<AIShip>());
            foreach (var w in playerShip.wingmates) {
                w.ship.sovereign = playerShip.sovereign;
            }
        }
        */

        int index = 0;
        foreach (var w in playerShip.wingmates.Take(10)) {
            char key = indexToKey(index++);
            Surface.Print(x, y++, $"[{key}] {w.name}: {w.behavior.GetOrderName()}", Color.White, Color.Black);
        }

        //this.SetCellAppearance(Width/2, Height/2, new ColoredGlyph(Color.White, Color.White, 'X'));

        base.Render(delta);
    }
    public class CommandMenu : ScreenSurface {
        //PlayerShip player;
        AIShip subject;
        public int ticks = 0;
        private Dictionary<string, Action> commands;
        public CommandMenu(ScreenSurface prev, PlayerShip player, AIShip subject) : base(prev.Surface.Width, prev.Surface.Height) {
            //this.player = player;
            this.subject = subject;
            EscortShip GetEscortOrder(int i) {
                int root = (int)Math.Sqrt(i);
                int lower = root * root;
                int upper = (root + 1) * (root + 1);
                int range = upper - lower;
                int index = i - lower;
                return new EscortShip(player, XY.Polar(
                        -(Math.PI * index / range), root * 2));
            }
            commands = new();
            switch(subject.behavior) {
                case Wingmate w:
                    commands["Form Up"] = () => {
                        player.AddMessage(new Transmission(subject, $"Ordered {subject.name} to Form Up"));
                        w.order = GetEscortOrder(0);
                    };

                    if(subject.devices.Weapon.FirstOrDefault(w => w.projectileDesc.tracker != 0) is Weapon weapon) {
                        commands["Fire Tracker"] = () => {
                            if (!player.GetTarget(out ActiveObject target)) {
                                player.AddMessage(new Transmission(subject, $"{subject.name}: Firing tracker at nearby enemies"));
                                w.order = new FireTrackerNearby(weapon);
                                return;
                            }
                            player.AddMessage(new Transmission(subject, $"{subject.name}: Firing tracker at target"));
                            w.order = new FireTrackerAt(weapon, target);
                        };
                    }
                    commands["Attack Target"] = () => {
                        if (player.GetTarget(out ActiveObject target)) {
                            w.order = new AttackTarget(target);
                            player.AddMessage(new Transmission(subject, $"{subject.name}: Attacking target"));
                        } else {
                            player.AddMessage(new Transmission(subject, $"No target selected"));
                        }
                    };
                    commands["Wait"] = () => {
                        w.order = new GuardAt(new TargetingMarker(player, "Wait", subject.position));
                        player.AddMessage(new Transmission(subject, $"Ordered {subject.name} to Wait"));
                    };
                    break;
                default:
                    commands["Form Up"] = () => {
                        player.AddMessage(new Message($"Ordered {subject.name} to Form Up"));
                        subject.behavior = GetEscortOrder(0);
                    };
                    commands["Attack Target"] = () => {
                        if (player.GetTarget(out ActiveObject target)) {
                            var attack = new AttackTarget(target);
                            var escort = GetEscortOrder(0);
                            subject.behavior = attack;
                            OrderOnDestroy.Register(subject, attack, escort, target);
                            player.AddMessage(new Message($"Ordered {subject.name} to Attack Target"));
                        } else {
                            player.AddMessage(new Message($"No target selected"));
                        }
                    };
                    break;
            }
        }
        public override void Update(TimeSpan delta) {
            ticks++;
            base.Update(delta);
        }
        public override bool ProcessKeyboard(Keyboard info) {
            foreach (var k in info.KeysPressed) {
                int index = keyToIndex(k.Character);
                if (index > -1 && index < commands.Count) {
                    commands.Values.ElementAt(index)();
                }
            }
            if (info.IsKeyPressed(Keys.Escape)) {
                IsVisible = false;
            }
            return base.ProcessKeyboard(info);
        }
        public override void Render(TimeSpan delta) {
            int x = 0;
            int y = 0;

            Surface.Clear();

            Color foreground = Color.White;
            if (ticks % 60 < 30) {
                foreground = Color.Yellow;
            }
            var back = Color.Black;
            Surface.Print(x, y++, "[Command]", foreground, back);
            //this.Print(x, y++, "[Ship control locked]", foreground, back);
            Surface.Print(x, y++, "[ESC     -> cancel]", foreground, back);
            y++;
            Surface.Print(x, y++, $"{subject.name}:{subject.behavior.GetOrderName()}", Color.White, Color.Black);
            y++;
            int index = 0;
            foreach (var w in commands.Keys) {
                char key = indexToKey(index++);
                Surface.Print(x, y++, $"[{key}] {w}", Color.White, Color.Black);
            }

            base.Render(delta);
        }
    }
}
public class PowerWidget : ScreenSurface {
    PlayerShip playerShip;
    PlayerMain main;
    int ticks;
    private bool _blockMouse;
    public bool blockMouse {
        set {
            _blockMouse = value;

            Surface.DefaultBackground = value ? new Color(0, 0, 0, 127) : Color.Transparent;
        }
        get => _blockMouse;
    }
    public PowerWidget(int width, int height, PlayerMain main) : base(width, height) {
        this.playerShip = main.playerShip;
        this.main = main;
        FocusOnMouseClick = false;
        InitButtons();
    }
    protected override void OnVisibleChanged() {
        if (IsVisible) {
            InitButtons();
        }
        base.OnVisibleChanged();
    }
    public void InitButtons() {
        int x = 4;
        int y = 6;
        this.Children.Clear();
        foreach (var p in playerShip.powers) {
            this.Children.Add(new LabelButton(p.type.name) {
                Position = new Point(x, y++),
                leftHold = () => {
                    if (p.ready) {
                        //Enable charging
                        p.charging = true;
                    }
                }
            });
        }
    }
    public override void Update(TimeSpan delta) {
        ticks++;
        foreach (var p in playerShip.powers) {
            if (p.charging) {
                //We don't need to check ready because we already do that before we set charging
                //Charging up
                p.invokeCharge += (int)(60 * delta.TotalSeconds);

                if (ticks % 3 == 0) {
                    p.charging = false;
                }
            } else if (p.invokeCharge > 0) {
                if (p.invokeCharge < p.invokeDelay) {
                    p.invokeCharge -= (int)(60 * delta.TotalSeconds);
                } else {
                    //Invoke now!
                    p.cooldownLeft = p.cooldownPeriod;

                    p.type.Effect.ForEach(e => {
                        if (e is PowerJump j) {
                            j.Invoke(main);
                        } else {
                            e.Invoke(playerShip);
                        }
                    });
                    if (p.type.message != null) {
                        playerShip.AddMessage(new Message(p.type.message));
                    }
                    //Reset charge
                    p.invokeCharge = 0;
                    p.charging = false;
                }
            }
        }
        base.Update(delta);
    }
    public override bool ProcessKeyboard(Keyboard keyboard) {
        foreach (var k in keyboard.KeysDown) {
            var ch = k.Character;
            //If we're pressing a digit/letter, then we're charging up a power
            int powerIndex = keyToIndex(ch);
            //Find the power
            if (powerIndex > -1 && powerIndex < playerShip.powers.Count) {
                var power = playerShip.powers[powerIndex];
                //Make sure this power is available
                if (power.ready) {
                    //Enable charging
                    power.charging = true;
                }
            }
        }
        if (keyboard.IsKeyPressed(Escape)) {
            //Set charge for all powers back to 0
            foreach (var p in playerShip.powers) {
                p.invokeCharge = 0;
                p.charging = false;
            }
            //Hide menu
            //IsVisible = false;
        }
        if (keyboard.IsKeyPressed(I)) {
            //Set charge for all powers back to 0
            foreach (var p in playerShip.powers) {
                if (p.invokeCharge < p.invokeDelay) {
                    p.invokeCharge = 0;
                    p.charging = false;
                }
            }
            //Hide menu
            //IsVisible = false;
        }

        return base.ProcessKeyboard(keyboard);
    }
    public override void Render(TimeSpan delta) {
        int x = 0;
        int y = 0;
        int index = 0;
        Surface.Clear();
        var foreground = Color.White;
        if (ticks % 60 < 30) {
            foreground = Color.Yellow;
        }
        var back = Color.Black;
        Surface.Print(x, y++, "[Powers]", foreground, back);
        //this.Print(x, y++, "[Ship control locked]", foreground, back);
        Surface.Print(x, y++, "[ESC     -> cancel]", foreground, back);
        Surface.Print(x, y++, "[P       -> close ]", foreground, back);
        Surface.Print(x, y++, "[Hold    -> charge]", foreground, back);
        Surface.Print(x, y++, "[Release -> invoke]", foreground, back);
        y++;

        var bl = Color.Black;
        var gr = Color.Gray;
        var wh = Color.White;
        foreach (var p in playerShip.powers) {
            char key = indexToKey(index);
            if (p.cooldownLeft > 0) {
                int chargeBar = (int)(16 * p.cooldownLeft / p.cooldownPeriod);
                Surface.Print(x, y++,
                    new ColoredString($"[{key}] {p.type.name,-8} ", gr, bl) +
                    new ColoredString("[", wh, bl) +
                    new ColoredString(new string('>', 16 - chargeBar), wh, bl) +
                    new ColoredString(new string('>', chargeBar), gr, bl) +
                    new ColoredString("]", wh, bl)
                    );
            } else if (p.invokeCharge > 0) {
                var chargeMeter = (int)Math.Min(16, 16 * p.invokeCharge / p.invokeDelay);

                var c = Color.Yellow;
                if (p.invokeCharge >= p.invokeDelay && ticks % 30 < 15) {
                    c = Color.Orange;
                }
                Surface.Print(x, y++,
                    new ColoredString($"[{key}] {p.type.name,-8} ", c, bl) +
                    new ColoredString("[", c, bl) +
                    new ColoredString(new string('>', chargeMeter), c, bl) +
                    new ColoredString(new string('>', 16 - chargeMeter), wh, bl) +
                    new ColoredString("]", c, bl)
                    );
            } else {
                Surface.Print(x, y++,
                    new ColoredString($"[{key}] {p.type.name,-8} ", wh, bl) +
                    new ColoredString($"[{new string('>', 16)}]", wh, bl));
            }
            index++;
        }

        //this.SetCellAppearance(Width/2, Height/2, new ColoredGlyph(Color.White, Color.White, 'X'));

        base.Render(delta);
    }

}
