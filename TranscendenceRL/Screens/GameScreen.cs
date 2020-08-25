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

namespace TranscendenceRL {
    public class PlayerMain : Console {
		public XY camera;
		public World World;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		private PlayerStory story = new PlayerStory();
		public PlayerShip playerShip;
		public PlayerControls playerControls;
		GeneratedLayer backVoid;
		Point mousePos;

		Keyboard keyboard;
		MouseScreenObjectState mouse;

		BackdropConsole back;
		MegaMap map;
		PlayerBorder vignette;
		public Console sceneContainer;
		PlayerUI ui;
		PowerMenu powerMenu;

		TargetingMarker crosshair;

		double updateWait;

		public PlayerMain(int Width, int Height, World World, PlayerShip playerShip) : base(Width, Height) {
			DefaultBackground = Color.Transparent;
			DefaultForeground = Color.Transparent;
			UseMouse = true;
			UseKeyboard = true;
			camera = new XY();
			this.World = World;
			this.playerShip = playerShip;
			tiles = new Dictionary<(int, int), ColoredGlyph>();
			backVoid = new GeneratedLayer(1, new Random());

			back = new BackdropConsole(Width, Height, World.backdrop, () => camera);
			map = new MegaMap(playerShip, Width, Height);
			vignette = new PlayerBorder(playerShip, Width, Height);
			sceneContainer = new Console(Width, Height);
			sceneContainer.Focused += (e, o) => this.IsFocused = true;
			ui = new PlayerUI(playerShip, tiles, Width, Height);
			powerMenu = new PowerMenu(Width, Height, playerShip) { IsVisible = false };
			crosshair = new TargetingMarker(playerShip, "Mouse Cursor", new XY());

			this.playerControls = new PlayerControls(playerShip, this, powerMenu, sceneContainer);

			//Don't allow anyone to get focus via mouse click
			FocusOnMouseClick = false;
		}
		public void EndGame(SpaceObject destroyer, Wreck wreck) {

			//Get a snapshot of the player
			var size = 60;
			var deathFrame = new ColoredGlyph[size, size];
			var halfWidth = Width / 2;
			var halfHeight = Height / 2;
			XY center = new XY(size / 2, size / 2);
			for (int y = 0; y < size; y++) {
				for(int x = 0; x < size; x++) {
					var tile = GetTile(camera - new XY(x, y) + center);
					tile.Foreground = Helper.Gray((int)(255 * tile.Foreground.Premultiply().GetBrightness()));
					tile.Background = Helper.Gray((int)(255 * tile.Background.Premultiply().GetBrightness()));
					deathFrame[x, y] = tile;

				}
            }
			var epitaph = new Epitaph() {
				desc = $"Destroyed by {destroyer.Name}",
				deathFrame = deathFrame,
				wreck = wreck
			};
			playerShip.player.Epitaphs.Add(epitaph);

			//Bug: Background is not included because it is a separate console
			SadConsole.Game.Instance.Screen = new DeathTransition(this, new DeathScreen(this, World, playerShip, epitaph)) { IsFocused = true };


			ColoredGlyph GetTile(XY xy) {
				var back = World.backdrop.GetTile(xy, camera);
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
		}
		public void PlaceTiles() {
			tiles.Clear();
			foreach (var e in World.entities.all) {
				if (e.Tile != null) {
					tiles[e.Position.RoundDown] = e.Tile;
				}
			}
			foreach (var e in World.effects.all) {
				if (e.Tile != null) {
					tiles[e.Position.RoundDown] = e.Tile;
				}
			}
		}
		public void UpdateWorld() {
			World.UpdateActive();
		}
		public override void Update(TimeSpan delta) {
			if(sceneContainer.Children.Count > 0) {
				sceneContainer.Update(delta);
            }

			//If the player is in mortality, then slow down time
			bool passTime = true;
			if(playerShip.mortalTime > 0) {

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

			camera = playerShip.Position;
			if (playerShip.Dock?.justDocked == true && playerShip.Dock.target is Dockable d) {

				Console scene = story.GetScene(this, d, playerShip) ?? d.GetScene(this, playerShip);
				if (scene != null) {
					playerShip.Dock = null;
					sceneContainer.Children.Add(new SceneScan(scene) { IsFocused = true });
				} else {
					playerShip.AddMessage(new InfoMessage($"Stationed on {d.Name}"));
                }
			}
			map.Update(delta);
			vignette.Update(delta);
			ui.Update(delta);
			powerMenu.Update(delta);

			//Required to update children
			base.Update(delta);
		}
		public override void Render(TimeSpan drawTime) {
			back.Render(drawTime);

			this.Clear();
			DrawWorld();
			base.Render(drawTime);
			if (sceneContainer.Children.Count > 0) {
				vignette.Render(drawTime);
				sceneContainer.Render(drawTime);
			} else {
				if (map.IsVisible) {
					map.Render(drawTime);
				}
				vignette.Render(drawTime);
				ui.Render(drawTime);
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
					XY location = camera + new XY(x, y);

					if (tiles.TryGetValue(location.RoundDown, out var tile)) {
						var xScreen = x + HalfViewWidth;
						var yScreen = HalfViewHeight - y;
						this.SetCellAppearance(xScreen, yScreen, tile);
					}
				}
			}
		}
		public override bool ProcessKeyboard(Keyboard info) {
			map.ProcessKeyboard(info);
			keyboard = info;

			if(info.IsKeyPressed(S) && info.IsKeyDown(LeftControl)) {
				var s = JsonConvert.SerializeObject(this);
				File.WriteAllText(playerShip.player.file, s);
            }

			//Intercept the alphanumeric/Escape keys if the power menu is active
			if (powerMenu.IsVisible) {
				powerMenu.ProcessKeyboard(info);
			} else {
				playerControls.ProcessKeyboard(info);
            }
			return base.ProcessKeyboard(info);
		}
		public override bool ProcessMouse(MouseScreenObjectState state) {
			if(sceneContainer.Children.Count > 0) {
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

				var worldPos = centerOffset + camera;
				SpaceObject t;
				if (state.Mouse.MiddleClicked) {
					var targetList = new List<SpaceObject>(World.entities.all.OfType<SpaceObject>().OrderBy(e => (e.Position - worldPos).Magnitude));

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

						//Draw an effect for the cursor
						World.AddEffect(new EffectParticle(worldPos, new ColoredGlyph(Color.White, Color.Transparent, '+'), 1));

						//Draw a trail leading back to the player
						var trailNorm = playerOffset.Normal;
						var trailLength = Math.Min(3, playerOffset.Magnitude / 4) + 1;
						for (int i = 1; i < trailLength; i++) {
							World.AddEffect(new EffectParticle(worldPos - trailNorm * i, new ColoredGlyph(Color.White, Color.Transparent, '.'), 1));
						}


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

	class BackdropConsole : Console {
		public Func<XY> camera;
		XY screenCenter;
		Backdrop backdrop;
		public BackdropConsole(int width, int height, Backdrop backdrop, Func<XY> camera) : base(width, height) {
			this.backdrop = backdrop;
			screenCenter = new XY(Width / 2f, Height / 2f);
			this.camera = camera;
		}
		public override void Render(TimeSpan drawTime) {
			this.Clear();
			XY camera = this.camera();

			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					var g = this.GetGlyph(x, y);

					var offset = new XY(x, Height - y) - screenCenter;
					var location = camera + offset;
					this.SetCellAppearance(x, y, backdrop.GetTile(location, camera));
				}
			}
			base.Render(drawTime);
		}
	}
	class MegaMap : Console {
		PlayerShip player;
		GeneratedLayer BackVoid;
		public double viewScale;
		double time;
		public MegaMap(PlayerShip player, int width, int height) : base(width, height) {
			this.player = player;
			BackVoid = new GeneratedLayer(1, new Random());
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
						var back = BackVoid.GetTileFixed(new XY(x * 2, y * 2));
						//Make sure to clone this so that we don't apply alpha changes to the original
						back = back.Clone();
						var glyph = back.Glyph;
						var alpha = (byte)(255 * Math.Min(1, (viewScale - 1)));
						back.Background = back.Background.Premultiply().SetAlpha(alpha);
						back.Foreground = back.Foreground.Premultiply().SetAlpha(alpha);
						this.SetCellAppearance(x, y, back);
					}
				}

				var visiblePerimeter = new Rectangle((int)(Width / 2 - Width / (2 * viewScale)), (int)(Height / 2 - Height / (2 * viewScale)), (int)(Width / viewScale), (int)(Height / viewScale));
				foreach (var point in visiblePerimeter.PerimeterPositions()) {
					this.SetBackground(point.X, point.Y, this.GetBackground(point.X, point.Y).Premultiply().Blend(new Color(255, 255, 255, 128)));
				}

				var scaledMap = player.World.entities.space.DownsampleSet(viewScale);
				foreach ((var p, HashSet<Entity> set) in scaledMap.space) {
					var visible = set.Where(t => t.Tile != null).Where(t => !(t is Segment));
					if (visible.Any()) {
						var e = visible.ElementAt((int)time % visible.Count());
						var (x, y) = (e.Position - player.Position) / viewScale + screenCenter;

						this.SetCellAppearance(x, Height - y, e.Tile);
					}
				}
			}
			base.Render(delta);
        }
    }
	class PlayerBorder : Console {
		PlayerShip player;
		public char[,] mortalBorder;
		public float powerAlpha;
		public HashSet<EffectParticle> particles;

		public XY screenCenter;
		public int ticks;
		public Random r;
		public PlayerBorder(PlayerShip player, int width, int height) : base(width, height) {
			this.player = player;
			FocusOnMouseClick = false;

			char[] letters = new char[] { 'A', 'E', 'I', 'H', 'B', 'M', 'V' };
			mortalBorder = new char[width, height];

			for (int x = 0; x < width; x++) {
				for(int y = 0; y < height; y++) {
					mortalBorder[x, y] = letters[x % 2 + (x / 2) % 2 + (x / 4) % 2 + y % 2 + (y / 2) % 2 + (y / 4) % 2];
				}
            }
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

			Color borderFront = Color.Black;
			int borderSize = 8;
			if (powerAlpha > 0) {
				borderFront = borderFront.Blend(new Color(204, 153, 255, 255) * (float)Math.Min(1, powerAlpha * 1.5)).Premultiply();

				borderSize += (int)(12 * powerAlpha);
			}
			if (player.mortalTime > 0) {
				//Vignette is red when the player is mortal
				/*
				borderColor = borderColor.SetRed(Math.Min((byte)255, (byte)(Math.Min(2, mortalOpacity) * 128)));
				borderColor = borderColor.SetBlue(Math.Min((byte)255, (byte)((3 - Math.Min(3, mortalOpacity)) * 25)));
				borderColor = borderColor.SetAlpha(Math.Min((byte)255, (byte)Math.Min(255, 255 * mortalOpacity / 3)));
				*/
				borderFront = borderFront.SetRed(Math.Min((byte)255, (byte)(Math.Min(3, player.mortalTime / 1.5) * 255 / 3f)));
				borderFront = borderFront.Premultiply();

				var fraction = (player.mortalTime - Math.Truncate(player.mortalTime));

				borderSize += (int)(12 * fraction);
			}
			Color borderBack = Color.Black;
			if (player.Ship.ControlHijack != null) {
				borderBack = Color.Cyan;
			} else {
				var b = player.World.backdrop.starlight.GetBackground(player.Position, XY.Zero);
				borderBack = b.Premultiply();
			}
			int dec = 255 / borderSize;

			for (int i = 0; i < borderSize; i++) {
				var decrease = i * dec;
				byte backAlpha = (byte)Math.Max(0, 255 - decrease);
				byte frontAlpha = (byte)Math.Min(255, 4 * backAlpha / 3);

				var back = borderBack.SetAlpha(backAlpha);
				var front = borderFront.SetAlpha(frontAlpha);

				var screenPerimeter = new Rectangle(i, i, Width - i * 2, Height - i * 2);
				foreach (var point in screenPerimeter.PerimeterPositions()) {
					//var back = this.GetBackground(point.X, point.Y).Premultiply();
					var (x, y) = point;
					this.SetCellAppearance(x, y, new ColoredGlyph(front, back, mortalBorder[x, y]));
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
	class PlayerUI : Console {
		/*
		struct Snow {
			public char c;
			public double factor;
        }
		Snow[,] snow;
		*/
		PlayerShip player;
		Dictionary<(int, int), ColoredGlyph> tiles;
		public int borderBound;

		public PlayerUI(PlayerShip player, Dictionary<(int, int), ColoredGlyph> tiles, int width, int height) : base(width, height) {
			this.player = player;
			this.tiles = tiles;
			borderBound = Math.Max(Width, Height)/2;
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
        public override void Render(TimeSpan drawTime) {
			this.Clear();
			XY screenSize = new XY(Width - 2, Height - 2);
			XY screenCenter = screenSize / 2;
			var messageY = Height * 3 / 5;

			if (player.GetTarget(out SpaceObject target)) {

				/*
				var screenPos = (target.Position - player.Position) + screenSize / 2;
				screenPos = screenPos.RoundDown;
				screenPos.y += 1;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(Color.White, Color.Transparent, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					e = Line.Single,
					w = Line.Single,
					n = Line.Double,
				}]));
				for (int i = 0; i < 3; i++) {
					screenPos.y++;
					this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(Color.White, Color.Transparent, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
						n = Line.Double,
						s = Line.Double,
					}]));
				}
				screenPos.y++;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(Color.White, Color.Transparent, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					e = Line.Double,
					s = Line.Double,
				}]));
				screenPos.x++;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(Color.White, Color.Transparent, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					w = Line.Double,
					n = Line.Single,
					s = Line.Single
				}]));
				screenPos.x++;


				foreach (var cc in target.Name) {
					this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(Color.White, Color.Transparent, cc));
					screenPos.xi++;
				}
				*/
				/*
				Helper.CalcFireAngle(target.Position - player.Position, target.Velocity - player.Velocity, player.GetPrimary().desc.missileSpeed, out double timeToHit);

				var screenPos = (target.Position - player.Position) + new XY(Width/2, Height/2) / 2 + target.Velocity * timeToHit;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(Color.White, Color.Transparent, BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					e = Line.Double,
					w = Line.Double,
					n = Line.Double,
					s = Line.Double
				}]));
				*/

				var x = Width / 3;
				var y = 1;

				this.Print(x, y++, "[Targeting]");
				this.Print(x, y++, target.Name);
				if(target is AIShip ai) {
					if(ai.DamageSystem is HPSystem hp) {
						this.Print(x, y++, $"HP: {hp.hp}");
                    } else if(ai.DamageSystem is LayeredArmorSystem las) {
						this.Print(x, y++, $"[Armor]");
						foreach(var layer in las.layers) {
							this.Print(x, y++, $"{layer.source.type.name}{new string('>', (16 * layer.hp) / layer.desc.maxHP)}");
                        }
					}
					if(ai.Devices.Installed.Any()) {
						this.Print(x, y++, $"[Devices]");
						foreach (var d in ai.Devices.Installed) {
							if (d is Weapon w) {
								this.Print(x, y++, $"{d.source.type.name}{new string('>', (16 * w.fireTime) / w.desc.fireCooldown)}");
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
				var x = Width * 3 / 4 - line.Count;
				this.Print(x, messageY, line);
				if (message is Transmission t) {
					//Draw a line from message to source

					var screenCenterOffset = new XY(Width * 3 / 4, Height - messageY) - screenCenter;
					var messagePos = player.Position + screenCenterOffset;

					var sourcePos = t.source.Position.RoundDown;
					if (messagePos.RoundDown.yi == sourcePos.RoundDown.yi) {
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
					this.Print(x, y, $"[{new string('=', 16)}]", Color.White, Color.Transparent);
					if (player.Energy.totalUsedOutput > 0) {
						this.Print(x, y, $"[{new string('=', 16 * player.Energy.totalUsedOutput / player.Energy.totalMaxOutput)}", Color.Yellow, Color.Transparent);
					}
					y++;
					foreach (var reactor in player.Ship.Devices.Reactors) {
						this.Print(x, y, new ColoredString("[", Color.White, Color.Transparent) + new ColoredString(new string(' ', 16)) + new ColoredString("]", Color.White, Color.Transparent));
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
							this.Print(x, y, $"[{new string('=', (int)(15 * reactor.energy / reactor.desc.capacity))}{end}", bar, Color.Transparent);
						}
						y++;
					}
				}
				if (player.Ship.Devices.Weapons.Any()) {
					int i = 0;
					foreach (var weapon in player.Ship.Devices.Weapons) {
						string tag = $"{(i == player.selectedPrimary ? ">" : "")}{weapon.source.type.name}";
						Color foreground = Color.White;
						if (player.Energy.disabled.Contains(weapon)) {
							foreground = Color.Gray;
						} else if (weapon.firing || weapon.fireTime > 0) {
							foreground = Color.Yellow;
						}
						this.Print(x, y, tag, foreground, Color.Transparent);

						var xBar = x + tag.Length;

						ColoredString bar;
						if(weapon.fireTime > 0) {
							bar = new ColoredString(new string('>', 16 - (int)(16f * weapon.fireTime / weapon.desc.fireCooldown)),
													Color.Gray, Color.Transparent
													);
						} else {
							bar = new ColoredString(new string('>', 16),
													Color.White, Color.Transparent);
						}
						if (weapon.capacitor != null) {
							var n = 16 * weapon.capacitor.charge / weapon.capacitor.desc.maxCharge;
							for (int j = 0; j < n; j++) {
								bar[j].Foreground = bar[j].Foreground.Blend(Color.Cyan.SetAlpha(128));
							}
						}

						this.Print(xBar, y, bar);
						y++;
						i++;
					}
				}
				switch (player.Ship.DamageSystem) {
					case LayeredArmorSystem las:
						foreach (var armor in las.layers) {
							this.Print(x, y, $"{armor.source.type.name}{new string('>', 16 * armor.hp / armor.desc.maxHP)}", Color.White, Color.Transparent);
							y++;
						}
						break;
					case HPSystem hp:
						this.Print(x, y, $"HP: {hp.hp}");
						break;
				}
			}

			var halfWidth = Width / 2;
			var halfHeight = Height / 2;

			var range = 192;

			var nearby = player.World.entities.GetAll(((int, int) p) => (player.Position - p).MaxCoord < range);
			foreach (var entity in nearby) {
				var offset = (entity.Position - player.Position);
				var (x, y) = offset;
				Func<int, int> abs = Math.Abs;
				(x, y) = (Math.Abs(x), Math.Abs(y));

				if (x > halfWidth || y > halfHeight) {

					(x, y) = Helper.GetBoundaryPoint(screenSize, offset.Angle);

					Color c = Color.Transparent;
					if (entity is SpaceObject so) {
						c = so.Tile.Foreground;
					} else if (entity is Projectile p) {
						c = p.Tile.Foreground;
					}
					this.Print(x, Height - y - 1, "#", c, Color.Transparent);
				} else if(x > halfWidth - 8 || y > halfHeight - 8) {
					(x, y) = (screenCenter + offset).RoundDown;

					Color c = Color.Transparent;
					if (entity is SpaceObject so) {
						c = so.Tile.Foreground;
					} else if (entity is Projectile p) {
						c = p.Tile.Foreground;
					}
					this.Print(x, Height - y - 1, "#", c, Color.Transparent);
				}
			}

			var mapWidth = 24;
			var mapHeight = 24;
			var mapScale = (range / (mapWidth / 2));

			var mapX = Width - mapWidth;
			var mapY = 0;
			var mapCenterX = mapX + mapWidth / 2;
			var mapCenterY = mapY + mapHeight / 2;

			var mapSample = tiles.Downsample(mapScale);
			for (int x = -mapWidth / 2; x < mapWidth / 2; x++) {
				for (int y = -mapHeight / 2; y < mapHeight / 2; y++) {
					var tiles = mapSample[((x + player.Position.xi / mapScale), (y + player.Position.yi / mapScale))];
					if (tiles.Any()) {
						this.Print(mapCenterX + x, mapCenterY - y, tiles.First());
					} else {
						this.Print(mapCenterX + x, mapCenterY - y, "#", new Color(255, 255, 255, 102), Color.Black);
					}
				}
			}
			base.Render(drawTime);
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
					if(p.invokeCharge < p.invokeDelay) {
						p.invokeCharge++;
                    } else {
						//Invoke now!
						p.cooldownLeft = p.cooldownPeriod;
						p.type.Effect.Invoke(playerShip);

						//Reset charge
						p.invokeCharge = 0;
						p.charging = false;
                    }

					p.charging = false;
                } else if(p.cooldownLeft > 0) {
					p.cooldownLeft--;
                } else if(p.invokeCharge > 0) {
					p.invokeCharge--;
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
			this.Print(x, y++, "[Powers]", foreground);
			this.Print(x, y++, "[Ship control locked]", foreground);
			this.Print(x, y++, "[Press ESC to cancel]", foreground);
			this.Print(x, y++, "[Press key to invoke]", foreground);
			y++;
			foreach (var p in playerShip.Powers) {
				char key = indexToKey(index);
				if(p.cooldownLeft > 0) {
					this.Print(x, y++, $"[{key}] {p.type.name}{new string('>', 16 - 16 * p.cooldownLeft / p.cooldownPeriod)}", Color.Gray);
                } else if(p.invokeCharge > 0) {
					var chargeMeter = 16 * p.invokeCharge / p.invokeDelay;
					this.Print(x, y++,
						new ColoredString($"[{key}] {p.type.name}{new string('>', chargeMeter)}", Color.Yellow, Color.Transparent)
						+ new ColoredString(new string('>', 16 - chargeMeter), Color.Gray, Color.Transparent)
						);
				} else {
					this.Print(x, y++, new ColoredString($"[{key}] {p.type.name}", Color.White, Color.Transparent) + new ColoredString($"{new string('>', 16)}", Color.Gray, Color.Transparent));
				}
				index++;
            }

            base.Render(delta);
        }

    }
}
