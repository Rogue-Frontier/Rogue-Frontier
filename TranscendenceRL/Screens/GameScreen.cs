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
using TranscendenceRL.Screens;

namespace TranscendenceRL {
	public class PlayerMain : Console {
		public XY camera;
		public World world;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		public PlayerShip playerShip;
		public bool active;
		double viewScale = 1;
		GeneratedLayer backVoid;
		Point mousePos;

		PlayerUI ui;

		public PlayerMain(int Width, int Height, World World, Player player, ShipClass playerClass) : base(Width, Height) {
			UseMouse = true;
			UseKeyboard = true;

			camera = new XY();
			this.world = World;
			tiles = new Dictionary<(int, int), ColoredGlyph>();

			backVoid = new GeneratedLayer(1, new Random());
			/*
			var shipClass =  new ShipClass() { thrust = 0.5, maxSpeed = 20, rotationAccel = 4, rotationDecel = 2, rotationMaxSpeed = 3 };
			world.AddEntity(player = new PlayerShip(new Ship(world, shipClass, new XY(0, 0))));
			world.AddEffect(new Heading(player));

			
			
			StationType stDaughters = new StationType() {
				name = "Daughters of the Orator",
				segments = new List<StationType.SegmentDesc> {
					new StationType.SegmentDesc(new XY(-1, 0), new StaticTile('<')),
					new StationType.SegmentDesc(new XY(1, 0), new StaticTile('>')),
				},
				tile = new StaticTile(new ColoredGlyph('S', Color.Pink, Color.Transparent))
			};
			var daughters = new Station(world, stDaughters, new XY(5, 5));
			world.AddEntity(daughters);
			*/
			/*
			player = new PlayerShip(new Ship(world, world.types.Lookup<ShipClass>("scAmethyst"), new XY(0, 0)));
			world.AddEntity(player);
			*/
			world.entities.all.Clear();
			world.effects.all.Clear();
			/*
			var shipClasses = World.types.shipClass.Values;
			var shipClass = shipClasses.ElementAt(new Random().Next(shipClasses.Count));
			var ship = new Ship(World, shipClass, Sovereign.Gladiator, new XY(-10, -10));
			var enemy = new AIShip(ship, new AttackAllOrder(ship));
			World.AddEntity(enemy);
			*/

			world.types.Lookup<SystemType>("ssOrion").Generate(world);
			World.UpdatePresent();
			var playerStart = world.entities.all.First(e => e is Marker m && m.Name == "Start").Position;
			var playerSovereign = world.types.Lookup<Sovereign>("svPlayer");
			playerShip = new PlayerShip(player, new BaseShip(world, playerClass, playerSovereign, playerStart));
            World.AddEffect(new Heading(playerShip));
			playerShip.OnDestroyed += (p, d, wreck) => EndGame(d, wreck);
			active = true;
			world.AddEntity(playerShip);
			/*
			var daughters = new Station(world, world.types.Lookup<StationType>("stDaughtersOutpost"), new XY(5, 5));
			world.AddEntity(daughters);
			*/
			playerShip.messages.Add(new InfoMessage("Welcome to Transcendence: Rogue Frontier!"));

			ui = new PlayerUI(playerShip, tiles, Width, Height) {
			IsVisible = true
			};
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
			UpdateWorld();
			PlaceTiles();
			camera = playerShip.Position.RoundDown;
			world.UpdatePresent();
			if (playerShip.docking?.docked == true && playerShip.docking.target is Dockable d) {
				playerShip.docking = null;
				this.Children.Add(new Dockscreen(Width, Height, d.MainView, this, playerShip, d));
			}
			ui.Update(delta);
		}
		public override void Draw(TimeSpan drawTime) {
			base.Draw(drawTime);
			DrawWorld();
			ui.Draw(drawTime);
		}
		public void DrawWorld() {
			this.Clear();
			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;

			if (viewScale > 1) {
				var scaled = tiles.Downsample(viewScale);
				for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
					for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
						var screenCenterOffset = new XY(x, y);
						XY location = camera / viewScale + screenCenterOffset;

						var xScreen = x + HalfViewWidth;
						var yScreen = ViewHeight - (y + HalfViewHeight);
						var visible = scaled[location];
						if (visible.Any()) {
							this.SetCellAppearance(xScreen, yScreen, visible.First());
							visible.Remove(visible.First());
						} else {
							var backX = Math.Abs(screenCenterOffset.xi * viewScale);
							var backY = Math.Abs(screenCenterOffset.yi * viewScale);
							if (backX < HalfViewWidth && backY < HalfViewHeight) {


								var back = world.backdrop.GetTile(camera + (screenCenterOffset + new XY(0.5, 0.5)) * viewScale, camera / viewScale).Clone();

								//back.Background = (backVoid.GetTile(screenCenterOffset * Math.Sqrt(viewScale), camera).Background).Blend(back.Background * (float)(1 / viewScale));

								this.SetCellAppearance(xScreen, yScreen, back);   //Reduce parallax on zoom out)
							} else {

								//var value = (int)(51 * Math.Sqrt(HalfViewWidth * HalfViewWidth + HalfViewHeight * HalfViewHeight) / Math.Sqrt(backX * backX + backY * backY));

								//var value = 51 / (int)Math.Sqrt(1+ Math.Pow(HalfViewWidth - backX, 2) + Math.Pow(HalfViewHeight - backY, 2));
								//var c = new Color(value, value, value);

								var back = backVoid.GetTile(screenCenterOffset * Math.Sqrt(viewScale), camera).Clone();
								back.Background = back.Background * 2;
								this.SetCellAppearance(xScreen, yScreen, back);
							}
						}
					}
				}
			} else {
				for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
					for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
						XY location = camera + new XY(x, y);

						var xScreen = x + HalfViewWidth;
						var yScreen = ViewHeight - (y + HalfViewHeight);
						this.SetCellAppearance(xScreen, yScreen, GetTile(location));
					}
				}
			}
		}
		public bool GetForegroundTile(XY xy, out ColoredGlyph result) {
			if (tiles.TryGetValue(xy, out result)) {
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
			if (tiles.TryGetValue(xy, out ColoredGlyph g)) {
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
			//var value = backSpace.Get(xy - (camera * 3) / 4);
			var back = world.backdrop.GetTile(xy, camera.RoundDown);
			return back;
		}
		public override bool ProcessKeyboard(Keyboard info) {
			if(info.IsKeyDown(Up)) {
				playerShip.SetThrusting();
			}
			if (info.IsKeyDown(Left)) {
				playerShip.SetRotating(Rotating.CCW);
			}
			if (info.IsKeyDown(Right)) {
				playerShip.SetRotating(Rotating.CW);
			}
			if(info.IsKeyDown(Down)) {
				playerShip.SetDecelerating();
			}
			if(info.IsKeyPressed(Escape)) {
				//SadConsole.Game.Instance.Screen = new TitleConsole(Width, Height) { IsFocused = true };
			}
			if(info.KeysDown.Select(d => d.Key).Intersect<Keys>(new Keys[] { Keys.LeftControl, Keys.LeftShift, Keys.Enter }).Count() == 3) {
				playerShip.Destroy(playerShip);
            }
			if(info.IsKeyPressed(S)) {
				SadConsole.Game.Instance.Screen = new ShipScreen(this, playerShip) { IsFocused = true };
			}
			if(info.IsKeyPressed(W)) {
				playerShip.NextWeapon();
            }
			if(info.IsKeyPressed(T)) {
				playerShip.NextTargetEnemy();
            }
			if (info.IsKeyPressed(F)) {
				playerShip.NextTargetFriendly();
			}
			if (info.IsKeyPressed(R)) {
				if(playerShip.targetIndex > -1) {
					playerShip.ClearTarget();
				}
			}

			if(info.IsKeyDown(OemMinus)) {
				viewScale += Math.Min(viewScale / (2 * 30), 1);
				if(viewScale < 1) {
					viewScale = 1;
                }
            }
			if(info.IsKeyDown(OemPlus)) {
				viewScale -= Math.Min(viewScale / (2 * 30), 1);
				if(viewScale < 1) {
					viewScale = 1;
                }
            }
			if (info.IsKeyPressed(D)) {
				if(playerShip.docking != null) {
					if(playerShip.docking.docked) {
						playerShip.AddMessage(new InfoMessage("Undocked"));
					} else {
						playerShip.AddMessage(new InfoMessage("Docking sequence canceled"));
					}
					
					playerShip.docking = null;
				} else {
					var dest = world.entities.GetAll(p => (playerShip.Position - p).Magnitude < 8).OfType<Dockable>().OrderBy(p => (p.Position - playerShip.Position).Magnitude).FirstOrDefault();
					if(dest != null) {
						playerShip.AddMessage(new InfoMessage("Docking sequence engaged"));
						playerShip.docking = new Docking(dest);
					}
					
				}
			}
			if(info.IsKeyDown(X)) {
				playerShip.SetFiringPrimary();
			}
			return base.ProcessKeyboard(info);
		}
		public override bool ProcessMouse(MouseScreenObjectState state) {
			mousePos = state.SurfaceCellPosition;
			var offset = new XY(mousePos.X, Height - mousePos.Y) - new XY(Width / 2, Height / 2);

			var worldPos = offset + camera;
			if(state.Mouse.MiddleClicked) {
				var targetList = new List<SpaceObject>(world.entities.all.OfType<SpaceObject>().OrderBy(e => (e.Position - worldPos).Magnitude));
				playerShip.targetList = targetList;
				playerShip.targetIndex = 0;
				playerShip.UpdateAutoAim();

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

			if (offset.xi != 0 && offset.yi != 0) {
				var mouseRads = offset.Angle;
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

			if (state.Mouse.LeftButtonDown) {
				playerShip.SetFiringPrimary();
			}
			if (state.Mouse.RightButtonDown) {
				playerShip.SetThrusting();
			}
			return base.ProcessMouse(state);
		}
	}
	class PlayerUI : Console {
		PlayerShip player;
		Dictionary<(int, int), ColoredGlyph> tiles;
		public PlayerUI(PlayerShip player, Dictionary<(int, int), ColoredGlyph> tiles, int width, int height) : base(width, height) {
			this.player = player;
			this.tiles = tiles;
        }
        public override void Draw(TimeSpan drawTime) {
			this.Clear();
			XY screenSize = new XY(Width, Height);
			var messageY = Height * 3 / 5;
			XY screenCenter = screenSize / 2;

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

			for (int i = 0; i < player.messages.Count; i++) {
				var message = player.messages[i];
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
				if (player.power.totalMaxOutput > 0) {
					this.Print(x, y, $"[{new string('=', 16)}]", Color.White, Color.Transparent);
					if (player.power.totalUsedOutput > 0) {
						this.Print(x, y, $"[{new string('=', 16 * player.power.totalUsedOutput / player.power.totalMaxOutput)}", Color.Yellow, Color.Transparent);
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
						if (player.power.disabled.Contains(weapon)) {
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
						this.Print(mapCenterX + x, mapCenterY - y, "=", new Color(255, 255, 255, 204), Color.Transparent);
					}
				}
			}
			base.Draw(drawTime);
        }
    }
}
