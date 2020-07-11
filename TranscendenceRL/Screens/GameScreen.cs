using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using SadRogue.Primitives;
using static SadConsole.Input.Keys;
using SadConsole;
using SadConsole.Input;
using ASECII;
using SadConsole.UI;
using Console = SadConsole.Console;
using Helper = Common.Helper;


namespace TranscendenceRL {
	public class PlayerMain : Console {
		public XY camera;
		public World world;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		public PlayerShip playerShip;
		GeneratedLayer backVoid;
		Point mousePos;

		MegaMap map;
		PlayerUI ui;
		PowerMenu powerMenu;

		TargetingMarker crosshair;

		double updateWait;

		public PlayerMain(int Width, int Height, World World, PlayerShip playerShip) : base(Width, Height) {
			UseMouse = true;
			UseKeyboard = true;
			camera = new XY();
			this.world = World;
			this.playerShip = playerShip;
			tiles = new Dictionary<(int, int), ColoredGlyph>();
			backVoid = new GeneratedLayer(1, new Random());

			map = new MegaMap(playerShip, Width, Height);
			ui = new PlayerUI(playerShip, tiles, Width, Height) {
			};
			powerMenu = new PowerMenu(Width, Height, playerShip) { IsVisible = false };
			map.Children.Add(ui);			//Set UI over the map since we don't want it to interfere with the vignette transparency
			ui.Children.Add(powerMenu);     //Set power menu as child of the UI so that it doesn't get covered by the vignette

			crosshair = new TargetingMarker(playerShip, "Mouse Cursor", new XY());

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
			playerShip.player.epitaphs.Add(epitaph);
			SadConsole.Game.Instance.Screen = new DeathTransition(this, new DeathScreen(this, world, playerShip, epitaph)) { IsFocused = true };
		}
		public void PlaceTiles() {
			tiles.Clear();
			foreach (var e in world.entities.all) {
				if (e.Tile != null && !tiles.ContainsKey(e.Position.RoundDown)) {
					tiles[e.Position.RoundDown] = e.Tile;
				}
			}
			foreach (var e in world.effects.all) {
				if (e.Tile != null && !tiles.ContainsKey(e.Position.RoundDown)) {
					tiles[e.Position.RoundDown] = e.Tile;
				}
			}
		}
		public void UpdateWorld() {
			world.UpdateAll();
		}
		public override void Update(TimeSpan delta) {
			/*
			//SM64-style camera: We smoothly point ahead of where the player is going
			var offset = playerShip.Velocity / 3;
			var dest = playerShip.Position + offset;
			if ((camera - dest).Magnitude < playerShip.Velocity.Magnitude / 15 + 1) {
				camera = dest;
			} else {
				var step = (dest - camera) / 15;
				if (step.Magnitude < 1) {
					step = step.Normal;
				}
				camera += step;
			
			}
			*/

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
				PlaceTiles();
				world.UpdatePresent();
			}

			camera = playerShip.Position;
			if (playerShip.Dock?.docked == true && playerShip.Dock.target is Dockable d) {
				playerShip.Dock = null;
				this.Children.Add(new SceneScreen(Width, Height, d.MainView, playerShip, d) { IsFocused = true });
			}
			map.Update(delta);
		}
		public override void Draw(TimeSpan drawTime) {
			base.Draw(drawTime);
			DrawWorld();
			map.Draw(drawTime);
		}
		public void DrawWorld() {
			this.Clear();
			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;

			for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
				for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
					XY location = camera + new XY(x, y);

					var xScreen = x + HalfViewWidth;
					var yScreen = ViewHeight - (y + HalfViewHeight);
					this.SetCellAppearance(xScreen, yScreen, GetTile(location));
				}
			}
		}
		public bool GetForegroundTile(XY xy, out ColoredGlyph result) {
			//Round down to ensure we don't get duplicated tiles along the origin
			if (tiles.TryGetValue(xy.RoundDown, out result)) {
				result = result.Clone();	//Don't modify the source
				var back = GetBackTile(xy);
				if (result.Background.A == 0) {
					result.Background = back.Background;
				}
				return true;
			} else {
				result = null;
				return false;
				//return GetBackTile(xy);
			}
		}
		public ColoredGlyph GetTile(XY xy) {
			var back = GetBackTile(xy);
			//Round down to ensure we don't get duplicated tiles along the origin
			if (tiles.TryGetValue(xy.RoundDown, out ColoredGlyph g)) {
				g = g.Clone();			//Don't modify the source
				if(g.Background.A == 0) {
					g.Background = back.Background;
				}
				return g;
			} else {
				return back;
				//return GetBackTile(xy);
			}
		}
		public ColoredGlyph GetBackTile(XY xy) {
			//Don't round down since this function does it for us
			var back = world.backdrop.GetTile(xy, camera);
			return back;
		}
		public override bool ProcessKeyboard(Keyboard info) {


			map.ProcessKeyboard(info);

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

			//Intercept the alphanumeric/Escape keys if the power menu is active
			if (powerMenu.IsVisible) {
				powerMenu.ProcessKeyboard(info);
            } else {
				if (info.IsKeyPressed(Escape)) {
					//SadConsole.Game.Instance.Screen = new TitleConsole(Width, Height) { IsFocused = true };
				}
				if (info.KeysDown.Select(d => d.Key).Intersect<Keys>(new Keys[] { Keys.LeftControl, Keys.LeftShift, Keys.Enter }).Count() == 3) {
					playerShip.Destroy(playerShip);
				}
				if (info.IsKeyPressed(S)) {
					SadConsole.Game.Instance.Screen = new ShipScreen(this, playerShip) { IsFocused = true };
				}
				if (info.IsKeyPressed(V)) {
					powerMenu.IsVisible = true;
				}
				if (info.IsKeyPressed(W)) {
					playerShip.NextWeapon();
				}
				if (info.IsKeyPressed(T)) {
					playerShip.NextTargetEnemy();
				}
				if (info.IsKeyPressed(F)) {
					playerShip.NextTargetFriendly();
				}
				if (info.IsKeyPressed(R)) {
					if (playerShip.targetIndex > -1) {
						playerShip.ClearTarget();
					}
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
						var dest = world.entities.GetAll(p => (playerShip.Position - p).Magnitude < 8).OfType<Dockable>().OrderBy(p => (p.Position - playerShip.Position).Magnitude).FirstOrDefault();
						if (dest != null) {
							playerShip.AddMessage(new InfoMessage("Docking sequence engaged"));
							playerShip.Dock = new Docking(dest);
						}

					}
				}
				if (info.IsKeyDown(X)) {
					playerShip.SetFiringPrimary();
				}
				if(info.IsKeyPressed(C)) {
					playerShip.Damage(playerShip, playerShip.Ship.DamageSystem.GetHP() - 5);
                }
			}
			return base.ProcessKeyboard(info);
		}
		public override bool ProcessMouse(MouseScreenObjectState state) {
			if(state.IsOnScreenObject) {

				//Placeholder for mouse wheel-based weapon selection
				if(state.Mouse.ScrollWheelValueChange > 100) {
					playerShip.NextWeapon();
                } else if(state.Mouse.ScrollWheelValueChange < -100) {
					playerShip.PrevWeapon();
                }

				mousePos = state.SurfaceCellPosition;
				var centerOffset = new XY(mousePos.X, Height - mousePos.Y) - new XY(Width / 2, Height / 2);

				var worldPos = centerOffset + camera;

				if (state.Mouse.MiddleClicked) {
					//Set target to object closest to mouse cursor
					//If there is no target closer to the cursor than the playership, then we toggle aiming by crosshair

					var targetList = new List<SpaceObject>(world.entities.all.OfType<SpaceObject>().OrderBy(e => (e.Position - worldPos).Magnitude));
					if (targetList.First() == playerShip) {
						if (playerShip.GetTarget(out var t) && t == crosshair) {
							playerShip.ClearTarget();
						} else {
							playerShip.SetTargetList(new List<SpaceObject>() { crosshair });
						}
					} else {
						playerShip.targetList = targetList;
						playerShip.targetIndex = 0;
						playerShip.UpdateAutoAim();
					}



					/*
					//Attempt to skip the beginning items of both lists that already match
					int i = 0;
					for(i = 0; i < Math.Min(playerShip.targetIndex + 1, Math.Min(playerShip.targetList.Count, targetList.Count)); i++) {
						if(targetList[i] == playerShip.targetList[i]) {
							i++;
						}
					}
					if(i < targetList.Count) {
						playerShip.targetIndex = i;
					}
					*/
				}

				//Aiming with crosshair disables mouse turning
				if (playerShip.GetTarget(out var target) && target == crosshair) {
					crosshair.Position = worldPos;
					crosshair.Velocity = playerShip.Velocity;
					Heading.Crosshair(world, worldPos);
				} else {
					var playerOffset = worldPos - playerShip.Position;

					if (playerOffset.xi != 0 && playerOffset.yi != 0) {

						//Draw an effect for the cursor
						world.AddEffect(new EffectParticle(worldPos, new ColoredGlyph(Color.White, Color.Transparent, '+'), 1));
						{
							//Draw a trail leading back to the player
							var norm = playerOffset.Normal;
							var trailLength = Math.Min(3, playerOffset.Magnitude / 4);
							for (int i = 1; i < trailLength; i++) {
								world.AddEffect(new EffectParticle(worldPos - norm * i, new ColoredGlyph(Color.White, Color.Transparent, '.'), 1));
							}
						}

						var mouseRads = playerOffset.Angle;
						var facingRads = playerShip.Ship.stoppingRotationWithCounterTurn * Math.PI / 180;

						var ccw = (XY.Polar(facingRads + 3 * Math.PI / 180) - XY.Polar(mouseRads)).Magnitude;
						var cw = (XY.Polar(facingRads - 3 * Math.PI / 180) - XY.Polar(mouseRads)).Magnitude;
						if (ccw < cw) {
							playerShip.SetRotating(Rotating.CCW);
						} else if (cw < ccw) {
							playerShip.SetRotating(Rotating.CW);
						} else {
							if (playerShip.Ship.rotatingVel > 0) {
								playerShip.SetRotating(Rotating.CW);
							} else {
								playerShip.SetRotating(Rotating.CCW);
							}
						}
					}
				}


				if (state.Mouse.LeftButtonDown) {
					playerShip.SetFiringPrimary();
				}
				if (state.Mouse.RightButtonDown) {
					playerShip.SetThrusting();
				}
			}
			return base.ProcessMouse(state);
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
        public override void Draw(TimeSpan delta) {
			this.Clear();
			if (viewScale > 1) {
				XY screenSize = new XY(Width, Height);
				XY screenCenter = screenSize / 2;
				for (int x = 0; x < Width; x++) {
					for (int y = 0; y < Height; y++) {
						var back = BackVoid.GetTileFixed(new XY(x, y));
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
			base.Draw(delta);
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
		public double mortalOpacity;

		public PlayerUI(PlayerShip player, Dictionary<(int, int), ColoredGlyph> tiles, int width, int height) : base(width, height) {
			Random r = new Random();
			this.player = player;
			this.tiles = tiles;
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
        public override void Draw(TimeSpan drawTime) {
			this.Clear();
			XY screenSize = new XY(Width, Height);
			XY screenCenter = screenSize / 2;
			var messageY = Height * 3 / 5;

			//Set the color of the vignette
			Color borderColor = Color.Black;
			int borderSize = 8;

			if (player.mortalTime > 0) {
				//Vignette is red when the player is mortal
				if(mortalOpacity < player.mortalTime) {
					mortalOpacity += player.mortalTime / 30;
                } else {
					mortalOpacity = player.mortalTime;
                }
				/*
				borderColor = borderColor.SetRed(Math.Min((byte)255, (byte)(Math.Min(2, mortalOpacity) * 128)));
				borderColor = borderColor.SetBlue(Math.Min((byte)255, (byte)((3 - Math.Min(3, mortalOpacity)) * 25)));
				borderColor = borderColor.SetAlpha(Math.Min((byte)255, (byte)Math.Min(255, 255 * mortalOpacity / 3)));
				*/
				borderColor = borderColor.SetRed(Math.Min((byte)255, (byte)(Math.Min(3, mortalOpacity) * 255 / 3f)));
				borderColor = borderColor.Premultiply();

				var fraction = (player.mortalTime - Math.Truncate(player.mortalTime));

				borderSize += (int) (2 * borderSize * fraction);
            }
			int dec = 255 / borderSize;
			for(int i = 0; i < borderSize; i++) {
				byte alpha = (byte)Math.Max(0, 255 - i * dec);
				var screenPerimeter = new Rectangle(i, i, Width - i*2, Height - i*2);
				foreach(var point in screenPerimeter.PerimeterPositions()) {
					var color = borderColor.SetAlpha(alpha);
					//var back = this.GetBackground(point.X, point.Y).Premultiply();

					this.SetBackground(point.X, point.Y, color);
                }
            }

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
						string tag = $"{(i == player.selectedPrimary ? ">" : "")}{weapon.source.type.name}{new string('>', 16 * weapon.fireTime / weapon.desc.fireCooldown).PadRight(16)}";
						Color foreground = Color.White;
						if (player.Energy.disabled.Contains(weapon)) {
							foreground = Color.Gray;
						} else if (weapon.firing || weapon.fireTime > 0) {
							foreground = Color.Yellow;
						}

						this.Print(x, y, tag, foreground, Color.Transparent);
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

			var range = 128;
			var nearby = player.World.entities.GetAll(((int, int) p) => (player.Position - p).MaxCoord < range);
			foreach (var entity in nearby) {
				var offset = (entity.Position - player.Position);
				if (Math.Abs(offset.x) > halfWidth || Math.Abs(offset.y) > halfHeight) {

					(int x, int y) = Helper.GetBoundaryPoint(screenSize, offset.Angle);

					Color c = Color.Transparent;
					if (entity is SpaceObject so) {
						switch (player.Sovereign.GetDisposition(so)) {
							case Disposition.Enemy:
								c = new Color(255, 51, 51);
								break;
							case Disposition.Neutral:
								c = new Color(204, 102, 51);
								break;
							case Disposition.Friend:
								c = new Color(51, 255, 51);
								break;
						}
					} else if (entity is Projectile) {
						//Draw projectiles as yellow
						c = new Color(204, 204, 51);
					}
					if (y == 0) {
						y = 1;
					}
					this.Print(x, Height - y, "#", c, Color.Transparent);

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
						this.Print(mapCenterX + x, mapCenterY - y, "#", new Color(255, 255, 255, 128), Color.Black);
					}
				}
			}
			base.Draw(drawTime);
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
				if (ch == 0 || ch == ' ') {
					continue;
                }

				ch = char.ToLower(ch);
				//If we're pressing a digit/letter, then we're charging up a power
				int powerIndex = -1;
				if (char.IsDigit(ch)) {
					powerIndex = ch - '0';
                } else if(char.IsLetter(ch)) {
					powerIndex = 10 + ch - 'a';
                } else {
					continue;
                }

				//Find the power
				if(powerIndex < playerShip.Powers.Count) {
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
		public static char indexToKey(int index) {
			if (index < 10) {
				return (char)('0' + index);
			} else {
				index -= 10;
				if (index < 26) {
					return (char)('a' + index);
				} else {
					throw new IndexOutOfRangeException();
				}
			}
		}
        public override void Draw(TimeSpan delta) {
			int x = 3;
			int y = 32;
			int index = 0;

			this.Clear();

			Color foreground = Color.White;
			if(ticks%30 < 15) {
				foreground = Color.Yellow;
            }
			this.Print(x, y++, "[Powers]", foreground);
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

            base.Draw(delta);
        }

    }
}
