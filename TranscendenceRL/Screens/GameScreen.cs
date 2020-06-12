using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.Xna.Framework;
using static Microsoft.Xna.Framework.Input.Keys;
using SadConsole;
using SadConsole.Input;
using SadConsole.Themes;
using ASECII;

namespace TranscendenceRL {
	static class Themes {
		public static WindowTheme Main = new SadConsole.Themes.WindowTheme() {
			ModalTint = Color.Transparent,
			FillStyle = new Cell(Color.White, Color.Black),
		};
		public static WindowTheme Sub = new WindowTheme() {
			ModalTint = Color.Transparent,
			FillStyle = new Cell(Color.Transparent, Color.Transparent)
		};
	}
	class GameConsole : Window {
		private PlayerMain main;
		public GameConsole(int Width, int Height, World World, ShipClass playerClass) : base(Width, Height) {
			Theme = new WindowTheme {
				ModalTint = Color.Transparent,
				FillStyle = new Cell(Color.White, Color.Black),
			};
			UseKeyboard = true;
			UseMouse = true;
			this.DebugInfo($"Width: {Width}", $"Height: {Height}");
			main = new PlayerMain(this, Width, Height, World, playerClass);
		}
		public override void Show(bool modal) {
			base.Show(modal);
			main.Show(true);
		}
		public override void Update(TimeSpan delta) {
		}
		private int HalfWidth { get => Width / 2; }
		private int HalfHeight { get => Height / 2; }
		public override void Draw(TimeSpan delta) {
			this.DebugInfo($"Draw({delta})");
			Clear();

			int ViewWidth = Width;
			int ViewHeight = Height;
			int HalfViewWidth = ViewWidth / 2;
			int HalfViewHeight = ViewHeight / 2;
			//var i = 0;
			for (int x = -HalfViewWidth; x < HalfViewWidth; x++) {
				for (int y = -HalfViewHeight; y < HalfViewHeight; y++) {
					XY location = main.camera + new XY(x, y);

					var xScreen = x + HalfViewWidth;
					//var xScreen = x;
					var yScreen = ViewHeight - (y + HalfViewHeight);
					Print(xScreen, yScreen, main.GetTile(location));
				}
				//i++;
				//Print(main.camera.xi + x + HalfViewWidth, 0, new ColoredGlyph('0' + i/10, Color.White, Color.Transparent));
				//Print(0, 0, new ColoredString(main.camera.xi.ToString(), new Cell(Color.White, Color.Black)));
			}
			base.Draw(delta);
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			return base.ProcessKeyboard(info);
		}
	}
	class PlayerMain : Window {
		public Window parent;
		public XY camera;
		public World world;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		public PlayerShip player;

		bool showLargeMap = false;
		public PlayerMain(Window parent, int Width, int Height, World World, ShipClass playerClass) : base(Width, Height) {
			this.parent = parent;
			camera = new XY();
			this.world = World;
			tiles = new Dictionary<(int, int), ColoredGlyph>();
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
			player = new PlayerShip(new Ship(world, playerClass, playerSovereign, playerStart));
			world.AddEntity(player);
			/*
			var daughters = new Station(world, world.types.Lookup<StationType>("stDaughtersOutpost"), new XY(5, 5));
			world.AddEntity(daughters);
			*/
			player.messages.Add(new InfoMessage("Welcome to Transcendence: Rogue Frontier!"));
		}
		public override void Update(TimeSpan delta) {
			tiles.Clear();
			//Place everything in the grid
			world.entities.UpdateSpace();
			world.effects.UpdateSpace();

			//Update everything
			foreach (var e in world.entities.all) {
				e.Update();
				if (e.Tile != null && !tiles.ContainsKey(e.Position.RoundDown)) {
					tiles[e.Position.RoundDown] = e.Tile;
				}
			}
			foreach (var e in world.effects.all) {
				e.Update();
				if (e.Tile != null && !tiles.ContainsKey(e.Position.RoundDown)) {
					tiles[e.Position.RoundDown] = e.Tile;
				}
			}

			camera = player.Position.RoundDown;

			world.UpdatePresent();

			if(player.docking?.docked == true && player.docking.target is Dockable d) {
				player.docking = null;
				new Dockscreen(Width, Height, d.MainView, this, player, d).Show(true); ;
			}
		}
		public override void Draw(TimeSpan drawTime) {
			var messageY = Height * 3 / 5;
			Clear();
			XY screenSize = new XY(Width, Height);
			XY screenCenter = screenSize/2;

			if(showLargeMap) {
				int n = (int)Math.Pow(Math.Min(Width, Height) / 2, 2);

				double scale = 2;
				var downscaled = tiles.Downsample(scale);


				XY direction = new XY(0, 1);
				XY point = screenCenter;
				int i = 0;
				int interval = 1;

				//Draw system map with spiral animation
				bool active = i < n;
				StartDraw:
				for (int j = 0; j < 2; j++) {
					for (int k = 0; k < interval; k++) {
						if (active) {
							point += direction;
							var offset = point - screenCenter;
							var tiles = downscaled[camera / scale + offset];
							if(tiles.Any()) {
								this.Print(point.xi, Height - point.yi, tiles.First());
							}
						/*
						for(int x = location.xi; x != (int)(location.xi * scale) + 1; x += location.xi < 0 ? -1 : 1) {
							for (int y = location.yi; y != (int)(location.yi * scale) + 1; y += location.yi < 0 ? -1 : 1) {
								if(GetForegroundTile(new XY(x, y), out ColoredGlyph tile)) {
									visible = tile;
									goto DrawTile;
								}
							}
						}
						goto Done;
						DrawTile:
						*/
						Done:

							i++;
							active = i < n;
						} else {
							goto EndDraw;
                        }
					}
					direction = new XY(direction.y, -direction.x);
				}
				interval++;

				if (active) {
					goto StartDraw;
				}

			EndDraw:
				int a;
			}

			if(player.GetTarget(out SpaceObject target)) {
				var screenPos = (target.Position - player.Position) + screenSize / 2;
				screenPos = screenPos.RoundDown;
				screenPos.y += 1;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					e = Line.Single,
					w = Line.Single,
					n = Line.Double,
				}], Color.White, Color.Transparent));
				for(int i = 0; i < 3; i++) {
					screenPos.y++;
					this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
						n = Line.Double,
						s = Line.Double,
					}], Color.White, Color.Transparent));
				}
				screenPos.y++;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					e = Line.Double,
					s = Line.Double,
				}], Color.White, Color.Transparent));
				screenPos.x++;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					w = Line.Double,
					n = Line.Single,
					s = Line.Single
				}], Color.White, Color.Transparent));
				screenPos.x++;
				this.Print(screenPos.xi, Height - screenPos.yi, target.Name);
				/*
				Helper.CalcFireAngle(target.Position - player.Position, target.Velocity - player.Velocity, player.GetPrimary().desc.missileSpeed, out double timeToHit);

				screenPos = (target.Position - player.Position) + screen / 2 + target.Velocity * timeToHit;
				this.SetCellAppearance(screenPos.xi, Height - screenPos.yi, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
					e = Line.Double,
					w = Line.Double,
					n = Line.Double,
					s = Line.Double
				}], Color.White, Color.Transparent));
				*/
			}

			for (int i = 0; i < player.messages.Count;i++) {
				var message = player.messages[i];
				var line = message.Draw();
				var x = Width * 3 / 4 - line.Count;
				Print(x, messageY, line);
				if(message is Transmission t) {
					//Draw a line from message to source

					var screenCenterOffset = new XY(Width * 3 / 4, Height - messageY) - screenCenter;
					var messagePos = camera + screenCenterOffset;

					var sourcePos = t.source.Position.RoundDown;
					if (messagePos.RoundDown.yi == sourcePos.RoundDown.yi) {
						continue;
                    }

					int screenX = Width * 3 / 4;
					int screenY = messageY;

					var (f, b) = line.Any() ? (line[0].Foreground, line[0].Background) : (Color.White, Color.Transparent);

					screenX++;
					messagePos.x++;
					this.SetCellAppearance(screenX, screenY, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
						e = Line.Double,
						n = Line.Single,
						s = Line.Single
					}], f, b));
					screenX++;
					messagePos.x++;
					
					for (int j = 0; j < i; j++) {
						this.SetCellAppearance(screenX, screenY, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
							e = Line.Double,
							w = Line.Double
						}], f, b));
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
						var sourcePosEdge = Helper.GetBoundaryPoint(screenSize, offset.Angle) - screenSize/2 + camera;
						offset = sourcePosEdge - player.Position;
						if (truncateX) { offset.x -= Math.Sign(offset.x) * (i+2); }
						if (truncateY) { offset.y -= Math.Sign(offset.y) * (i+2); }
						offsetLeft = sourcePos - sourcePosEdge;
					}
					offset += player.Position - messagePos;

					int screenLineY = offset.yi + (offset.yi < 0 ? -1 : 0);
					int screenLineX = offset.xi;

					if (screenLineY != 0) {
						this.SetCellAppearance(screenX, screenY, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
							n = offset.y > 0 ? Line.Double : Line.None,
							s = offset.y < 0 ? Line.Double : Line.None,
							w = Line.Double
						}], f, b));
						screenY -= Math.Sign(screenLineY);
						screenLineY -= Math.Sign(screenLineY);

						while (screenLineY != 0) {
							this.SetCellAppearance(screenX, screenY, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
								n = Line.Double,
								s = Line.Double
							}], f, b));
							screenY -= Math.Sign(screenLineY);
							screenLineY -= Math.Sign(screenLineY);
						}
					}

					if (screenLineX != 0) {
						this.SetCellAppearance(screenX, screenY, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
							n = offset.y < 0 ? Line.Double : Line.None,
							s = offset.y > 0 ? Line.Double : Line.None,

							e = offset.x > 0 ? Line.Double : Line.None,
							w = offset.x < 0 ? Line.Double : Line.None
						}], f, b));
						screenX += Math.Sign(screenLineX);
						screenLineX -= Math.Sign(screenLineX);

						while (screenLineX != 0) {
							this.SetCellAppearance(screenX, screenY, new ColoredGlyph(BoxInfo.IBMCGA.glyphFromInfo[new BoxGlyph {
								e = Line.Double,
								w = Line.Double
							}], f, b));
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
					Print(x, y, $"[{new string('=', 16)}]", Color.White, Color.Transparent);
					if (player.power.totalUsedOutput > 0) {
						Print(x, y, $"[{new string('=', 16 * player.power.totalUsedOutput / player.power.totalMaxOutput)}", Color.Yellow, Color.Transparent);
					}
					y++;
					foreach (var reactor in player.Ship.Devices.Reactors) {
						Print(x, y, new ColoredString("[", Color.White, Color.Transparent) + new ColoredString(new string(' ', 16)) + new ColoredString("]", Color.White, Color.Transparent));
						if (reactor.energy > 0) {
							Color bar = Color.White;
							char end = '=';
							if(reactor.energyDelta < 0) {
								bar = Color.Yellow;
								end = '<';
                            } else if(reactor.energyDelta > 0) {
								end = '>';
								bar = Color.Cyan;
                            }
							Print(x, y, $"[{new string('=', (int)(15 * reactor.energy / reactor.desc.capacity))}{end}", bar, Color.Transparent);
						}
						y++;
					}
				}
				if (player.Ship.Devices.Weapons.Any()) {
					foreach (var weapon in player.Ship.Devices.Weapons) {
						Color foreground = Color.White;
						if(player.power.disabled.Contains(weapon)) {
							foreground = Color.Gray;
                        } else if (weapon.firing || weapon.fireTime > 0) {
							foreground = Color.Yellow;
						}
						Print(x, y, $"{weapon.source.type.name}{new string('>', weapon.fireTime / 3)}", foreground, Color.Transparent);
						y++;
					}
				}
				switch(player.Ship.DamageSystem) {
					case LayeredArmorSystem las:
						foreach(var armor in las.layers) {
							Print(x, y, $"{armor.source.type.name}{new string('>', armor.hp/3)}", Color.White, Color.Transparent);
							y++;
						}
						break;
					case HPSystem hp:
						Print(x, y, $"HP: {hp.hp}");
						break;
                }
			}

			var halfWidth = Width / 2;
			var halfHeight = Height / 2;


			var range = 128;
			var nearby = world.entities.GetAll(((int, int) p) => (player.Position - p).MaxCoord < range);
			foreach (var entity in nearby) {
				var offset = (entity.Position - player.Position);
				if (Math.Abs(offset.x) > halfWidth || Math.Abs(offset.y) > halfHeight) {

					(int x, int y) = Helper.GetBoundaryPoint(screenSize, offset.Angle);

					Color c = Color.Transparent;
					if(entity is SpaceObject so) {
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
					} else if(entity is Projectile) {
						//Draw projectiles as yellow
						c = new Color(204, 204, 51);
                    }
					if (y == 0) {
						y = 1;
					}
					Print(x, Height - y, "#", c, Color.Transparent);

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
			for (int x = -mapWidth/2; x < mapWidth/2; x++) {
				for(int y = -mapHeight/2; y < mapHeight/2; y++) {
					var tiles = mapSample[((x + player.Position.xi / mapScale), (y + player.Position.yi / mapScale))];
					if (tiles.Any()) {
						Print(mapCenterX + x, mapCenterY - y, tiles.First());
					} else {
						Print(mapCenterX + x, mapCenterY - y, "=", new Color(255, 255, 255, 204), Color.Transparent);
					}
				}
			}

			base.Draw(drawTime);
		}
		public bool GetForegroundTile(XY xy, out ColoredGlyph result) {
			if (tiles.TryGetValue(xy, out result)) {
				result = result.Clone();	//Don't modify the source
				var back = GetBackTile(xy);
				if (result.Background == Color.Transparent) {
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
				if(g.Background == Color.Transparent) {
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
			var back = world.backdrop.GetTile(xy, camera);
			return back;
		}
		public override bool ProcessKeyboard(Keyboard info) {
			if(info.IsKeyDown(Up)) {
				player.SetThrusting();
			}
			if (info.IsKeyDown(Left)) {
				player.SetRotating(Rotating.CCW);
			}
			if (info.IsKeyDown(Right)) {
				player.SetRotating(Rotating.CW);
			}
			if(info.IsKeyDown(Down)) {
				player.SetDecelerating();
			}
			if(info.IsKeyPressed(Escape)) {
				Hide();
				parent.Hide();
				new TitleConsole(Width, Height).Show(true);
			}
			if(info.IsKeyPressed(S)) {
				Hide();
				new ShipScreen(this, player).Show(true);
			}
			if(info.IsKeyPressed(T)) {
				player.NextTargetEnemy();
				//Note: Show a label on the target after select
            }
			if (info.IsKeyPressed(F)) {
				player.NextTargetFriendly();
				//Note: Show a label on the target after select
			}
			if (info.IsKeyPressed(R)) {
				player.ClearTarget();
				//Note: Show a label on the target after select
			}
			if (info.IsKeyPressed(D)) {
				if(player.docking != null) {
					if(player.docking.docked) {
						player.AddMessage(new InfoMessage("Undocked"));
					} else {
						player.AddMessage(new InfoMessage("Docking sequence canceled"));
					}
					
					player.docking = null;
				} else {
					var dest = world.entities.GetAll(p => (player.Position - p).Magnitude < 8).OfType<Dockable>().OrderBy(p => (p.Position - player.Position).Magnitude).FirstOrDefault();
					if(dest != null) {
						player.AddMessage(new InfoMessage("Docking sequence engaged"));
						player.docking = new Docking(player.Ship, dest);
					}
					
				}
			}
			if(info.IsKeyDown(X)) {
				player.SetFiringPrimary();
			}
			return base.ProcessKeyboard(info);
		}
		public override bool ProcessMouse(MouseConsoleState state) {
			if(state.IsOnConsole) {


				var cell = state.ConsoleCellPosition;
				var offset = new XY(cell.X, Height - cell.Y) - new XY(Width / 2, Height / 2);
				if(offset.xi != 0 && offset.yi != 0) {

					var mouseRads = offset.Angle;
					var facingRads = player.Ship.stoppingRotation * Math.PI / 180;

					var ccw = (XY.Polar(facingRads + 3 * Math.PI / 180) - XY.Polar(mouseRads)).Magnitude;
					var cw = (XY.Polar(facingRads - 3 * Math.PI / 180) - XY.Polar(mouseRads)).Magnitude;
					if(ccw < cw) {
						player.SetRotating(Rotating.CCW);
					} else if(cw < ccw) {
						player.SetRotating(Rotating.CW);
					}
					/*
					var mouseDegrees = offset.Angle * 180 / Math.PI;
					var facingDegrees = player.ship.stoppingRotation;
					var diff = Helper.AngleDiff(facingDegrees, mouseDegrees);
					if (diff > 1) {
						player.SetRotating(Rotating.CCW);
					} else if(diff < -1) {
						player.SetRotating(Rotating.CW);
					}
					*/
				}

				if(state.Mouse.LeftButtonDown) {
					player.SetFiringPrimary();
				}
				if(state.Mouse.RightButtonDown) {
					player.SetThrusting();
				}
			}
			return base.ProcessMouse(state);
		}
	}
}
