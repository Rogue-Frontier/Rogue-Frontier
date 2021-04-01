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
using System.IO;
using ArchConsole;
using static TranscendenceRL.PlayerShip;

namespace TranscendenceRL {

	public class EndGame : IContainer<PlayerDestroyed> {
		[JsonIgnore]
		private PlayerMain main;
		[JsonIgnore]
		public PlayerDestroyed Value => (p, d, w) => main.EndGame(d, w);
		public EndGame(PlayerMain main) {
			this.main = main;
		}
	}
	public class Camera {
		public XY position;
		//For now we don't allow shearing
		public double rotation { get => Math.Atan2(right.y, right.x); set => right = XY.Polar(value, 1); }
		public XY up => right.Rotate(Math.PI/2);
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
	public class PlayerMain : Console {
		private Camera _camera;
		public Camera camera { get => _camera; set {
				_camera = value;
				back.camera = value;
			} }
		public World World;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		private PlayerStory story = new PlayerStory();
		public PlayerShip playerShip;
		public PlayerControls playerControls;
		Point mousePos;

		Keyboard keyboard;
		MouseScreenObjectState mouse;

		public BackdropConsole back;
		Megamap uiMegamap;
		Vignette vignette;
		public Console sceneContainer;
		Readout uiMain;	//If this is visible, then all other ui Consoles are visible
		Edgemap uiEdge;
		Minimap uiMinimap;

		PowerMenu powerMenu;
		PauseMenu pauseMenu;

		TargetingMarker crosshair;

		double updateWait;

		public PlayerMain(int Width, int Height, World World, PlayerShip playerShip) : base(Width, Height) {
			DefaultBackground = Color.Transparent;
			DefaultForeground = Color.Transparent;
			UseMouse = true;
			UseKeyboard = true;
			_camera = new Camera();
			this.World = World;
			this.playerShip = playerShip;
			tiles = new Dictionary<(int, int), ColoredGlyph>();

			back = new BackdropConsole(Width, Height, World.backdrop, camera);
			uiMegamap = new Megamap(camera, playerShip, Width, Height);
			vignette = new Vignette(playerShip, Width, Height);
			sceneContainer = new Console(Width, Height);
			sceneContainer.Focused += (e, o) => this.IsFocused = true;
			uiMain = new Readout(camera, playerShip, tiles, Width, Height);
			uiEdge = new Edgemap(camera, playerShip, Width, Height);
			uiMinimap = new Minimap(this, playerShip, 16);
			powerMenu = new PowerMenu(Width, Height, playerShip) { IsVisible = false };
			pauseMenu = new PauseMenu(this) { IsVisible = false };
			crosshair = new TargetingMarker(playerShip, "Mouse Cursor", new XY());

			this.playerControls = new PlayerControls(playerShip, this, powerMenu, pauseMenu, sceneContainer);

			//Don't allow anyone to get focus via mouse click
			FocusOnMouseClick = false;
		}
		public void HideUI() {
			uiMain.IsVisible = false;
        }
		public void ShowUI() {
			uiMain.IsVisible = true;
        }
		public void EndGame(SpaceObject destroyer, Wreck wreck) {
			//Clear mortal time so that we don't slow down after the player dies
			playerShip.mortalTime = 0;

			//Force exit any scenes
			sceneContainer.Children.Clear();

			//Force exit power menu
			powerMenu.IsVisible = false;

			//Pretty sure this can't happen but make sure
			pauseMenu.IsVisible = false;

			uiMain.IsVisible = false;
			
			//Get a snapshot of the player
			var size = Height;
			var deathFrame = new ColoredGlyph[size, size];
			XY center = new XY(size / 2, size / 2);
			for (int y = 0; y < size; y++) {
				for(int x = 0; x < size; x++) {
					var tile = GetTile(camera.position - new XY(x, y) + center);
					deathFrame[x, y] = tile;
				}
            }
			ColoredGlyph GetTile(XY xy) {
				var back = World.backdrop.GetTile(xy, camera.position);
				//Round down to ensure we don't get duplicated tiles along the origin
				if (tiles.TryGetValue(xy.RoundDown, out ColoredGlyph g)) {
					g = g.Clone();          //Don't modify the source
					g.Background = back.Background.Premultiply().Blend(g.Background);
					return g;
				} else {
					return back;
					//return GetBackTile(xy);
				}
			}
			var epitaph = new Epitaph() {
				desc = $"Destroyed by {destroyer.Name}",
				deathFrame = deathFrame,
				wreck = wreck
			};
			playerShip.player.Epitaphs.Add(epitaph);
			new DeadGame(World, playerShip.player, playerShip, epitaph);
			//Bug: Background is not included because it is a separate console
			var ds = new DeathScreen(this, epitaph);
			SadConsole.Game.Instance.Screen = new DeathTransition(this, ds) { IsFocused = true };
		}
		public void UpdateWorld() {
			World.UpdateActive();
		}
		public void PlaceTiles() {
			tiles.Clear();
			World.PlaceTiles(tiles);
		}
		public override void Update(TimeSpan delta) {
			if(sceneContainer.Children.Any()) {
				sceneContainer.Update(delta);
            }

			//If the player is in mortality, then slow down time
			bool passTime = true;

			if(pauseMenu.IsVisible) {
				passTime = false;
            }else if(playerShip.mortalTime > 0) {

				//Note that while the world updates are slowed down, the game window actually updates faster since there's less work per frame
				var timePassed = delta.TotalSeconds;
				updateWait += timePassed;

				var interval = Math.Max(2, playerShip.mortalTime);
				if (updateWait < interval / 60) {
					passTime = false;
				} else {
					updateWait = 0;
                }
				playerShip.mortalTime -= timePassed;
			}
			if(passTime) {
				UpdateWorld();
				if(playerShip.autopilot) {
					ProcessKeyboard(keyboard);
					ProcessMouse(mouse);
					UpdateWorld();
					ProcessKeyboard(keyboard);
					ProcessMouse(mouse);
					UpdateWorld();
                }
				PlaceTiles();
				World.UpdatePresent();
			}

			camera.position = playerShip.Position;
			if (playerShip.Dock?.justDocked == true && playerShip.Dock.target is Dockable d) {

				Console scene = story.GetScene(this, d, playerShip) ?? d.GetScene(this, playerShip);
				
				if (scene != null) {
					playerShip.Dock = null;
					sceneContainer.Children.Add(new SceneScan(scene) { IsFocused = true });
				} else {
					playerShip.AddMessage(new InfoMessage($"Stationed on {d.Name}"));
                }
			}
			if (uiMain.IsVisible) {
				uiMegamap.Update(delta);

				vignette.Update(delta);

				uiMain.Update(delta);

				uiEdge.viewScale = uiMegamap.viewScale;
				uiEdge.Update(delta);

				uiMinimap.alpha = (byte)(255 - uiMegamap.alpha);
				uiMinimap.Update(delta);
			} else {

				vignette.Update(delta);
			}

			//Need to update powermenu while it is hidden
			powerMenu.Update(delta);

			if (pauseMenu.IsVisible) {
				pauseMenu.Update(delta);
			}

			//Required to update children
			base.Update(delta);
		}
		public override void Render(TimeSpan drawTime) {
			back.Render(drawTime);

			this.Clear();
			DrawWorld();
			base.Render(drawTime);
			if(pauseMenu.IsVisible) {
				vignette.Render(drawTime);
				pauseMenu.Render(drawTime);
			} else if (sceneContainer.Children.Count > 0) {
				vignette.Render(drawTime);
				sceneContainer.Render(drawTime);
			} else {
				if (uiMain.IsVisible) {
					uiMegamap.Render(drawTime);

					vignette.Render(drawTime);

					uiMain.Render(drawTime);
					uiEdge.Render(drawTime);
					uiMinimap.Render(drawTime);
				} else {

					vignette.Render(drawTime);
				}
				if (powerMenu.IsVisible) {
					powerMenu.Render(drawTime);
				}
			}
		}
		public void DrawWorld() {
			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;

			for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
				for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
					XY location = camera.position + new XY(x, y).Rotate(camera.rotation);

					if (tiles.TryGetValue(location.RoundDown, out var tile)) {
						var xScreen = x + HalfViewWidth;
						var yScreen = HalfViewHeight - y;
						this.SetCellAppearance(xScreen, yScreen, tile);
					}
				}
			}
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
			keyboard = info;

			//Intercept the alphanumeric/Escape keys if the power menu is active
			if(pauseMenu.IsVisible) {
				pauseMenu.ProcessKeyboard(info);
            } else if (powerMenu.IsVisible) {
				playerControls.ProcessPowerMenu(info);
				powerMenu.ProcessKeyboard(info);
			} else {
				playerControls.ProcessKeyboard(info);

				if(info.IsKeyDown(OemOpenBrackets)) {
					camera.rotation += 0.01;
				}
				if(info.IsKeyDown(OemCloseBrackets)) {
					camera.rotation -= 0.01;
				}
            }
			return base.ProcessKeyboard(info);
		}
		public override bool ProcessMouse(MouseScreenObjectState state) {
			if(pauseMenu.IsVisible) {
				pauseMenu.ProcessMouseTree(state.Mouse);
            } else if (powerMenu.IsVisible) {
				powerMenu.ProcessMouseTree(state.Mouse);
            } else if(sceneContainer.Children.Any()) {
				sceneContainer.ProcessMouseTree(state.Mouse);
            } else if(state.IsOnScreenObject) {

				//Placeholder for mouse wheel-based weapon selection
				if(state.Mouse.ScrollWheelValueChange > 100) {
					playerShip.NextWeapon();
                } else if(state.Mouse.ScrollWheelValueChange < -100) {
					playerShip.PrevWeapon();
                }

				mousePos = state.SurfaceCellPosition;
				var centerOffset = new XY(mousePos.X, Height - mousePos.Y) - new XY(Width / 2, Height / 2);

				var worldPos = centerOffset.Rotate(camera.rotation) + camera.position;
				SpaceObject t;
				if (state.Mouse.MiddleClicked) {
					var targetList = new List<SpaceObject>(
						World.entities.all
						.OfType<SpaceObject>()
						.OrderBy(e => (e.Position - worldPos).Magnitude)
						.Select(s => s is Segment seg ? seg.Parent : s)
						.Distinct()
						);

					//Set target to object closest to mouse cursor
					//If there is no target closer to the cursor than the playership, then we toggle aiming by crosshair
					//Using the crosshair, we can effectively force any omnidirectional weapons to point at the crosshair
					if (targetList.First() == playerShip) {
						if (playerShip.GetTarget(out t) && t == crosshair) {
							playerShip.ClearTarget();
						} else {
							playerShip.SetTargetList(new List<SpaceObject>() { crosshair });
						}
					} else {
						playerShip.targetList = targetList;
						playerShip.targetIndex = 0;
						playerShip.UpdateAutoAim();
					}
				}


				bool enableMouseTurn = true;
				//Update the crosshair if we're aiming with it
				if (playerShip.GetTarget(out t) && t == crosshair) {
					crosshair.Position = worldPos;
					crosshair.Velocity = playerShip.Velocity;
					//If we set velocity to match player's velocity, then the weapon will aim directly at the crosshair
					//If we set the velocity to zero, then the weapon will aim to the lead angle of the crosshair

					crosshair.Update();

					Heading.Crosshair(World, crosshair.Position);

					//Idea: Aiming with crosshair disables mouse turning
					enableMouseTurn = false;
				}

				if(enableMouseTurn) {
					var playerOffset = worldPos - playerShip.Position;

					if (playerOffset.xi != 0 && playerOffset.yi != 0) {

						EffectParticle.DrawArrow(World, worldPos, playerOffset, Color.White);

						var mouseRads = playerOffset.Angle;
						playerShip.SetRotatingToFace(mouseRads);
					}
				}
				if (state.Mouse.LeftButtonDown) {
					playerShip.SetFiringPrimary();
				}
				if (state.Mouse.RightButtonDown) {
					playerShip.SetThrusting();
				}
			}
			mouse = state;
			return base.ProcessMouse(state);
		}
	}

	public class BackdropConsole : Console {
		public Camera camera;

		private readonly XY screenCenter;
		private Backdrop backdrop;
		public BackdropConsole(int width, int height, Backdrop backdrop, Camera camera) : base(width, height) {
			this.camera = camera;

			this.backdrop = backdrop;
			screenCenter = new XY(Width / 2f, Height / 2f);
		}
		public override void Render(TimeSpan drawTime) {
			this.Clear();
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					var g = this.GetGlyph(x, y);

					var offset = new XY(x, Height - y) - screenCenter;
					var location = camera.position + offset.Rotate(camera.rotation);
					this.SetCellAppearance(x, y, backdrop.GetTile(location, camera.position));
				}
			}
			base.Render(drawTime);
		}
	}
	class Megamap : Console {
		Camera camera;
		PlayerShip player;
		GeneratedLayer BackVoid;
		public double viewScale;
		double time;

		public byte alpha;
		public Megamap(Camera camera, PlayerShip player, int width, int height) : base(width, height) {
			this.camera = camera;
			this.player = player;
			BackVoid = new GeneratedLayer(1, new Rand());
			viewScale = 1;
			time = 0;
		}
		public override bool ProcessKeyboard(Keyboard info) {
			if (info.IsKeyDown(OemMinus)) {
				viewScale += Math.Min(viewScale / (2 * 30), 1);
			}
			if (info.IsKeyDown(OemPlus)) {
				viewScale -= Math.Min(viewScale / (2 * 30), 1);
				if (viewScale < 1) {
					viewScale = 1;
				}
			}
			return base.ProcessKeyboard(info);
		}
        public override void Update(TimeSpan delta) {
			alpha = (byte)(255 * Math.Min(1, (viewScale - 1)));
			time += delta.TotalSeconds;
			base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
			this.Clear();

			if (viewScale > 1) {
				XY screenSize = new XY(Width, Height);
				XY screenCenter = screenSize / 2;
				for (int x = 0; x < Width; x++) {
					for (int y = 0; y < Height; y++) {
						var offset = new XY((x - screenCenter.x) * viewScale, (y - screenCenter.y) * viewScale).Rotate(-camera.rotation);

						var pos = player.Position + offset;

						bool IsVisible(ColoredGlyph cg) {
							return cg.GlyphCharacter != ' ' || cg.Background != Color.Transparent;

						}
						void Render(ColoredGlyph cg) {
							var glyph = cg.Glyph;
							var background = cg.Background.PremultiplySet(alpha);
							var foreground = cg.Foreground.PremultiplySet(alpha);
							this.SetCellAppearance(x, Height - y, new ColoredGlyph(foreground, background, glyph));
						}

						var environment = player.World.backdrop.planets.GetTile(pos.Snap(viewScale), XY.Zero);
						if (IsVisible(environment)) {
							Render(environment);
							continue;
						}

						environment = player.World.backdrop.nebulae.GetTile(pos.Snap(viewScale), XY.Zero);
						if (IsVisible(environment)) {
							Render(environment);
							continue;
						}

						var starlight = player.World.backdrop.starlight.GetTile(pos).PremultiplySet(255);

						var cg = BackVoid.GetTileFixed(new XY(x, y));
						//Make sure to clone this so that we don't apply alpha changes to the original
						var glyph = cg.Glyph;
						var background = cg.Background.BlendPremultiply(starlight, alpha);
						var foreground = cg.Foreground.PremultiplySet(alpha);
						this.SetCellAppearance(x, Height - y, new ColoredGlyph(foreground, background, glyph));
					}
				}

				var visiblePerimeter = new Rectangle((int)(Width / 2 - Width / (2 * viewScale)) + 1, (int)(Height / 2 - Height / (2 * viewScale)) + 1, (int)(Width / viewScale), (int)(Height / viewScale));
				foreach (var point in visiblePerimeter.PerimeterPositions()) {
					var b = this.GetBackground(point.X, point.Y);
					this.SetBackground(point.X, point.Y, b.BlendPremultiply(new Color(255, 255, 255, 128)));
				}

				var scaledMap = player.World.entities.space.DownsampleSet(viewScale);
				foreach ((var p, HashSet<Entity> set) in scaledMap.space) {
					var visible = set.Where(t => !(t is Segment)).Where(t => t.Tile != null);
					if (visible.Any()) {
						var e = visible.ElementAt((int)time % visible.Count());
						var offset = (e.Position - player.Position) / viewScale;
						var (x, y) = screenCenter + offset.Rotate(-camera.rotation);

						y = Height - y;
						if(x > -1 && x < Width && y > -1 && y < Height) {

							var t = new ColoredGlyph(e.Tile.Foreground, this.GetBackground(x, y), e.Tile.Glyph);
							this.SetCellAppearance(x, y, t);
						}
					}
				}
			}
			base.Render(delta);
        }
    }
	class Vignette : Console {
		PlayerShip player;
		public float powerAlpha;
		public HashSet<EffectParticle> particles;

		public XY screenCenter;
		public int ticks;
		public Random r;
		public Vignette(PlayerShip player, int width, int height) : base(width, height) {
			this.player = player;
			FocusOnMouseClick = false;

			powerAlpha = 0;
			particles = new HashSet<EffectParticle>();
			screenCenter = new XY(width / 2, height / 2);
			r = new Random();
		}
        public override void Update(TimeSpan delta) {
			var charging = player.Powers.Where(p => p.charging);
			if(charging.Any()) {
				var charge = Math.Min(1, charging.Max(p => (float) p.invokeCharge / p.invokeDelay));
				if(powerAlpha < charge) {
					powerAlpha += (charge - powerAlpha) / 10f;
                }
			} else {
				powerAlpha -= powerAlpha / 120;
			}
			ticks++;
			if(ticks % 5 == 0 && player.Ship.ControlHijack != null) {
				int i = 0;
				var screenPerimeter = new Rectangle(i, i, Width - i * 2, Height - i * 2);
				foreach(var p in screenPerimeter.PerimeterPositions().Select(p => new XY(p))) {
					if(r.Next(0, 10) == 0) {
						int speed = 30;
						int lifetime = 60;
						var v = new XY( p.xi == 0 ? speed : p.xi == screenPerimeter.Width - 1 ? -speed : 0,
										p.yi == 0 ? speed : p.yi == screenPerimeter.Height - 1 ? -speed : 0);
						particles.Add(new EffectParticle(p, new ColoredGlyph(Color.Cyan, Color.Transparent, '#'), lifetime) { Velocity = v });
					}
                }
			}

			foreach(var p in particles) {
				p.Position += p.Velocity / TranscendenceRL.TICKS_PER_SECOND;
				p.Lifetime--;
				p.Velocity -= p.Velocity / 15;
            }
			particles.RemoveWhere(p => !p.Active);

			base.Update(delta);
        }
        public override void Render(TimeSpan delta) {
			this.Clear();

			XY screenSize = new XY(Width, Height);

			//Set the color of the vignette

			Color borderColor = Color.Black;
			int borderSize = 2;
			if (powerAlpha > 0) {
				borderColor = borderColor.Blend(new Color(204, 153, 255, 255) * (float)Math.Min(1, powerAlpha * 1.5)).Premultiply();

				borderSize += (int)(12 * powerAlpha);
			}
			if (player.mortalTime > 0) {
				//Vignette is red when the player is mortal
				/*
				borderColor = borderColor.SetRed(Math.Min((byte)255, (byte)(Math.Min(2, mortalOpacity) * 128)));
				borderColor = borderColor.SetBlue(Math.Min((byte)255, (byte)((3 - Math.Min(3, mortalOpacity)) * 25)));
				borderColor = borderColor.SetAlpha(Math.Min((byte)255, (byte)Math.Min(255, 255 * mortalOpacity / 3)));
				*/
				borderColor = borderColor.Blend(Color.Red.SetAlpha((byte)(Math.Min(1, player.mortalTime / 4.5) * 255)));
				

				var fraction = (player.mortalTime - Math.Truncate(player.mortalTime));

				borderSize += (int)(6 * fraction);
			}
			if (player.Ship.ControlHijack != null) {
			} else {
				var b = player.World.backdrop.starlight.GetBackgroundFixed(player.Position);
				borderColor = borderColor.Blend(b.SetAlpha((byte)(255 * b.GetBrightness())));
			}
			int dec = 255 / borderSize;

			for (int i = 0; i < borderSize; i++) {
				var decrease = i * dec;
				byte alpha = (byte)Math.Max(0, 255 - decrease);

				var screenPerimeter = new Rectangle(i, i, Width - i * 2, Height - i * 2);

				var cg = new ColoredGlyph(Color.Transparent, borderColor.SetAlpha(alpha).Premultiply(), ' ');
				foreach (var point in screenPerimeter.PerimeterPositions()) {
					//var back = this.GetBackground(point.X, point.Y).Premultiply();
					var (x, y) = point;
					this.SetCellAppearance(x, y, cg);
				}
			}

			foreach(var p in particles) {
				var (x, y) = p.Position;
				var (fore, glyph) = (p.Tile.Foreground, p.Tile.Glyph);
				this.SetCellAppearance(x, y, new ColoredGlyph(fore, this.GetBackground(x,y), glyph));
            }
			base.Render(delta);
        }
    }
	class Readout : Console {
		/*
		struct Snow {
			public char c;
			public double factor;
        }
		Snow[,] snow;
		*/
		Camera camera;
		PlayerShip player;
		Dictionary<(int, int), ColoredGlyph> tiles;
		public int borderBound;

		public int arrowDistance;

		public double time;

		public Readout(Camera camera, PlayerShip player, Dictionary<(int, int), ColoredGlyph> tiles, int width, int height) : base(width, height) {
			this.camera = camera;
			this.player = player;
			this.tiles = tiles;
			borderBound = Math.Max(Width, Height)/2;

			//arrowDistance = Math.Min(Width, Height)/2 - 6;
			arrowDistance = 32;
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
            base.Update(delta);
			time += delta.TotalSeconds;
        }
        public override void Render(TimeSpan drawTime) {
			this.Clear();
			XY screenSize = new XY(Width - 2, Height - 2);
			XY screenCenter = screenSize / 2;
			var messageY = Height * 3 / 5;

			int targetX = 48, targetY = 1;
			if (player.GetTarget(out SpaceObject playerTarget)) {
				this.Print(targetX, targetY++, "[Target]");
				this.Print(targetX, targetY++, playerTarget.Name);
				PrintTarget(targetX, targetY, playerTarget);


				var offset = playerTarget.Position - player.Position;
				if (Math.Abs(offset.x) > Width / 2 || Math.Abs(offset.y) > Height / 2) {

					var offsetNormal = offset.Normal.FlipY;
					var p = screenCenter + offsetNormal * arrowDistance;

					this.SetCellAppearance(p.xi, p.yi, new ColoredGlyph(Color.Yellow, Color.Transparent, '+'));

					var trailLength = Math.Min(3, offset.Magnitude / 4) + 1;
					for (int i = 1; i < trailLength; i++) {
						p -= offsetNormal;
						this.SetCellAppearance(p.xi, p.yi, new ColoredGlyph(Color.Yellow, Color.Transparent, '.'));
					}
				}

				targetX += 32;
				targetY = 1;
			}
			var autoTarget = player.Devices.Weapons.Select(w => w.target).FirstOrDefault();
			if(autoTarget != null && autoTarget != playerTarget) {
				this.Print(targetX, targetY++, "[Auto]");
				this.Print(targetX, targetY++, autoTarget.Name);
				PrintTarget(targetX, targetY, autoTarget);
			}

			void PrintTarget(int x, int y, SpaceObject target) {
				if (target is AIShip ai) {
					PrintDamageSystem(ai.DamageSystem);
					PrintDeviceSystem(ai.Devices);
				} else if(target is Station s) {
					PrintDamageSystem(s.DamageSystem);

					if (s.weapons?.Any() == true) {
						foreach (var w in s.weapons) {
							this.Print(x, y++, $"{w.source.type.name}{w.GetBar()}");
						}
					}
				}

				void PrintDamageSystem(HullSystem s) {
					if (s is HPSystem hp) {
						this.Print(x, y++, $"Hull: {hp.hp} hp");
					} else if (s is LayeredArmorSystem las) {
						this.Print(x, y++, $"[Armor]");
						foreach (var layer in las.layers) {
							this.Print(x, y++, $"{layer.source.type.name}{new string('>', (16 * layer.hp) / layer.desc.maxHP)}");
						}
					}
				}

				void PrintDeviceSystem(DeviceSystem ds) {
					if (ds.Installed.Any()) {
						this.Print(x, y++, $"[Devices]");
						foreach (var d in ds.Installed) {
							if (d is Weapon w) {
								this.Print(x, y++, $"{d.source.type.name}{w.GetBar()}");
							} else {
								this.Print(x, y++, $"{d.source.type.name}");
							}

						}
					}
				}
			}

			for (int i = 0; i < player.Messages.Count; i++) {
				var message = player.Messages[i];
				var line = message.Draw();
				var x = Width * 3 / 4 - line.Count();
				this.Print(x, messageY, line);
				if (message is Transmission t) {
					//Draw a line from message to source

					var screenCenterOffset = new XY(Width * 3 / 4, Height - messageY) - screenCenter;
					var messagePos = (player.Position + screenCenterOffset).RoundDown;

					var sourcePos = t.source.Position.RoundDown;
					sourcePos = player.Position + (sourcePos - player.Position).Rotate(-camera.rotation);
					if (messagePos.yi == sourcePos.yi) {
						continue;
					}

					int screenX = Width * 3 / 4;
					int screenY = messageY;

					var (f, b) = line.Any() ? (line[0].Foreground, line[0].Background) : (Color.White, Color.Transparent);

					screenX++;
					messagePos.x++;
					this.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
						e = Line.Double,
						n = Line.Single,
						s = Line.Single
					}]));
					screenX++;
					messagePos.x++;

					for (int j = 0; j < i; j++) {
						this.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
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

					var offset = sourcePos - player.Position;

					var offsetLeft = new XY(0, 0);
					bool truncateX = Math.Abs(offset.x) > Width / 2 - 3;
					bool truncateY = Math.Abs(offset.y) > Height / 2 - 3;
					if (truncateX || truncateY) {
						var sourcePosEdge = Helper.GetBoundaryPoint(screenSize, offset.Angle) - screenSize / 2 + player.Position;
						offset = sourcePosEdge - player.Position;
						if (truncateX) { offset.x -= Math.Sign(offset.x) * (i + 2); }
						if (truncateY) { offset.y -= Math.Sign(offset.y) * (i + 2); }
						offsetLeft = sourcePos - sourcePosEdge;
					}
					offset += player.Position - messagePos;

					int screenLineY = offset.yi + (offset.yi < 0 ? 0 : 1);
					int screenLineX = offset.xi;

					if (screenLineY != 0) {
						this.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
							n = offset.y > 0 ? Line.Double : Line.None,
							s = offset.y < 0 ? Line.Double : Line.None,
							w = Line.Double
						}]));
						screenY -= Math.Sign(screenLineY);
						screenLineY -= Math.Sign(screenLineY);

						while (screenLineY != 0) {
							this.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
								n = Line.Double,
								s = Line.Double
							}]));
							screenY -= Math.Sign(screenLineY);
							screenLineY -= Math.Sign(screenLineY);
						}
					}

					if (screenLineX != 0) {
						this.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
							n = offset.y < 0 ? Line.Double : Line.None,
							s = offset.y > 0 ? Line.Double : Line.None,

							e = offset.x > 0 ? Line.Double : Line.None,
							w = offset.x < 0 ? Line.Double : Line.None
						}]));
						screenX += Math.Sign(screenLineX);
						screenLineX -= Math.Sign(screenLineX);

						while (screenLineX != 0) {
							this.SetCellAppearance(screenX, screenY, new ColoredGlyph(f, b, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
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
			{
				int x = 3;
				int y = 3;
				if (player.Energy.totalMaxOutput > 0) {
					this.Print(x, y, $"[{new string(' ', 16)}]", Color.White, Color.Transparent);
					this.Print(x + 1, y, $"{new string('=', 16)}", Color.Gray, Color.Transparent);
					if (player.Energy.totalUsedOutput > 0) {
						this.Print(x + 1, y,
							new ColoredString(new string('=', 16 * player.Energy.totalUsedOutput / player.Energy.totalMaxOutput),
								Color.Yellow, Color.Transparent));
					}
					this.Print(x + 1 + 16 + 2, y,
							new ColoredString("Total Output", Color.White, Color.Transparent));

					y++;
					foreach (var reactor in player.Ship.Devices.Reactors) {
						this.Print(x, y,
							new ColoredString("[", Color.White, Color.Transparent)
							+ new ColoredString(new string(' ', 16), Color.Transparent, Color.Transparent)
							+ new ColoredString("]", Color.White, Color.Transparent));

						if (reactor.energy > 0) {
							Color bar = Color.White;
							char end = '=';
							if (reactor.energyDelta < 0) {
								bar = Color.Yellow;
								end = '<';
							} else if (reactor.energyDelta > 0) {
								end = '>';
								bar = Color.Cyan;
							}

							int length = (int)(15 * reactor.energy / reactor.desc.capacity);
							this.Print(x + 1, y,
								new ColoredString($"{new string('=', length)}{end}", bar, Color.Transparent)
								+ new ColoredString(new string('=', 15 - length), Color.Gray, Color.Transparent)
								);
						} else {
							this.Print(x + 1, y, $"[{new string('=', 16)}]", Color.Gray, Color.Transparent);
						}
						this.Print(x + 1 + 16 + 2, y,
									new ColoredString(reactor.source.type.name, Color.White, Color.Transparent));
						y++;
					}
				}
				y++;
				if (player.Ship.Devices.Weapons.Any()) {
					int i = 0;
					foreach (var w in player.Ship.Devices.Weapons) {
						string tag = $"{(i == player.selectedPrimary ? "->" : "  ")}{w.source.type.name}";
						Color foreground;
						if (player.Energy.disabled.Contains(w)) {
							foreground = Color.Gray;
						} else if (w.firing || w.fireTime > 0) {
							foreground = Color.Yellow;
						} else {
							foreground = Color.White;
						}

						this.Print(x, y, "[", Color.White, Color.Transparent);
						this.Print(x + 1, y, w.GetBar());
						this.Print(x + 1 + 16, y, tag, foreground, Color.Transparent);
						y++;
						i++;
					}
				}
				y++;
				switch (player.Ship.DamageSystem) {
					case LayeredArmorSystem las:
						foreach (var armor in las.layers) {
							this.Print(x, y, "[", Color.White, Color.Transparent);
							this.Print(x + 1, y, new string('>', 16 ), Color.Gray, Color.Transparent);
							this.Print(x + 1, y, new string('>', 16 * armor.hp / armor.desc.maxHP), Color.White, Color.Transparent);
							this.Print(x + 1 + 16, y, $"-[{armor.source.type.name}", Color.White, Color.Transparent);
							y++;
						}
						break;
					case HPSystem hp:
						this.Print(x, y, $"HP: {hp.hp}");
						break;
				}
			}
			base.Render(drawTime);
        }
    }
	public class Edgemap : Console {
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
			this.Clear();
			var screenSize = new XY(Width - 2, Height - 2);
			var screenCenter = screenSize / 2;

			var halfWidth = Width / 2;
			var halfHeight = Height / 2;

			var range = 192;

			var nearby = player.World.entities.GetAll(((int, int) p) => (player.Position - p).MaxCoord < range);
			foreach (var entity in nearby) {
				var offset = (entity.Position - player.Position).Rotate(-camera.rotation);
				var (x, y) = offset / viewScale;
				(x, y) = (Math.Abs(x), Math.Abs(y));

				if (x > halfWidth || y > halfHeight) {

					if (entity is Segment) {
						continue;
					}


					(x, y) = Helper.GetBoundaryPoint(screenSize, offset.Angle);

					Color c = Color.Transparent;
					if (entity is SpaceObject so) {
						c = so.Tile.Foreground;
					} else if (entity is Projectile p) {
						c = p.Tile.Foreground;
					}
					this.Print(x, Height - y - 1, new ColoredGlyph(c, Color.Transparent, '#'));
				} else if (x > halfWidth - 4 || y > halfHeight - 4) {
					(x, y) = ((screenCenter + offset) + new XY(1, 1));

					Color c = Color.Transparent;
					if (entity is SpaceObject so) {
						c = so.Tile.Foreground;
					} else if (entity is Projectile p) {
						c = p.Tile.Foreground;
					}
					this.Print(x, Height - y - 1, new ColoredGlyph(c, Color.Transparent, '#'));
				}
			}

			base.Render(drawTime);
		}
	}
	public class Minimap : Console {
		PlayerShip playerShip;
		public int size;
		public double time;
		public byte alpha;
		public Minimap(Console parent, PlayerShip playerShip, int size) : base(size, size) {
			this.Position = new Point(parent.Width - size, 0);
			this.playerShip = playerShip;
			this.size = size;

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

			var mapSample = playerShip.World.entities.space.DownsampleSet(mapScale);
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					var entities = mapSample[(
						(x - halfSize + playerShip.Position.xi / mapScale),
						(halfSize - y + playerShip.Position.yi / mapScale))]
						.Where(e => !(e is Segment))
						.Where(e => e.Tile != null);

					if (entities.Any()) {
						var t = entities.ElementAt((int)time % entities.Count()).Tile;

						this.SetCellAppearance(x, y,
							new ColoredGlyph(t.Foreground, Color.Black, t.Glyph)
								.PremultiplySet(alpha)
							);
					} else {
						var foreground = new Color(
									255, 255, 255,
									51 + ((x + y) % 2 == 0 ? 0 : 12));
						this.SetCellAppearance(x, y,
							new ColoredGlyph(foreground, Color.Black, '#')
								.PremultiplySet(alpha)
							);
					}
				}
			}
			base.Render(delta);
        }
    }



	public class PowerMenu : Console {
		PlayerShip playerShip;
		int ticks;
		public PowerMenu(int width, int height, PlayerShip playerShip) : base(width, height) {
			this.playerShip = playerShip;
			FocusOnMouseClick = false;
        }
        public override void Update(TimeSpan delta) {
			ticks++;
			foreach(var p in playerShip.Powers) {
				if(p.charging) {
					//We don't need to check ready because we already do that before we set charging
					//Charging up
					p.invokeCharge++;

					if (ticks % 3 == 0) {
						p.charging = false;
					}
				} else if(p.cooldownLeft > 0) {
					p.cooldownLeft--;
                } else if(p.invokeCharge > 0) {
					if (p.invokeCharge >= p.invokeDelay) {
						//Invoke now!
						p.cooldownLeft = p.cooldownPeriod;
						p.type.Effect.Invoke(playerShip);

						//Reset charge
						p.invokeCharge = 0;
						p.charging = false;
					} else {
						p.invokeCharge--;
					}
				}
            }
            base.Update(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
			foreach(var k in keyboard.KeysDown) {
				var ch = k.Character;
				//If we're pressing a digit/letter, then we're charging up a power
				int powerIndex = keyToIndex(ch);
				//Find the power
				if(powerIndex > -1 && powerIndex < playerShip.Powers.Count) {
					var power = playerShip.Powers[powerIndex];
					//Make sure this power is available
					if(power.ready) {
						//Enable charging
						power.charging = true;
					}
                }
            }
			if(keyboard.IsKeyPressed(Keys.Escape)) {
				//Set charge for all powers back to 0
				foreach(var p in playerShip.Powers) {
					p.invokeCharge = 0;
					p.charging = false;
                }

				//Hide menu
				IsVisible = false;
            }
            return base.ProcessKeyboard(keyboard);
        }
		public override void Render(TimeSpan delta) {
			int x = 3;
			int y = 32;
			int index = 0;

			this.Clear();

			Color foreground = Color.White;
			if(ticks%60 < 30) {
				foreground = Color.Yellow;
            }
			var back = Color.Black;
			this.Print(x, y++, "[Powers]", foreground, back);
			this.Print(x, y++, "[Ship control locked]", foreground, back);
			this.Print(x, y++, "[Press ESC to cancel]", foreground, back);
			this.Print(x, y++, "[Press key to invoke]", foreground, back);
			y++;
			foreach (var p in playerShip.Powers) {
				char key = indexToKey(index);
				if(p.cooldownLeft > 0) {
					this.Print(x, y++, $"[{key}] {p.type.name}{new string('>', 16 - 16 * p.cooldownLeft / p.cooldownPeriod)}", Color.Gray);
                } else if(p.invokeCharge > 0) {
					var chargeMeter = Math.Min(16, 16 * p.invokeCharge / p.invokeDelay);

					var c = Color.Yellow;
					if(p.invokeCharge >= p.invokeDelay && ticks%30 < 15) {
						c = Color.Orange;
                    }
					this.Print(x, y++,
						new ColoredString($"[{key}] {p.type.name}{new string('>', chargeMeter)}", c, Color.Black)
						+ new ColoredString(new string('>', 16 - chargeMeter), Color.Gray, Color.Black)
						);
				} else {
					this.Print(x, y++, new ColoredString($"[{key}] {p.type.name}", Color.White, Color.Transparent) + new ColoredString($"{new string('>', 16)}", Color.Gray, Color.Transparent));
				}
				index++;
            }

			//this.SetCellAppearance(Width/2, Height/2, new ColoredGlyph(Color.White, Color.White, 'X'));

            base.Render(delta);
        }

    }
}
