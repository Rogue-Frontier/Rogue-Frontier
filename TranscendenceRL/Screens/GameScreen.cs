﻿using System;
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
			main = new PlayerMain(Width, Height, World, playerClass);
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
		public XY camera;
		public World world;
		public Dictionary<(int, int), ColoredGlyph> tiles;
		public PlayerShip player;
		public PlayerMain(int Width, int Height, World World, ShipClass playerClass) : base(Width, Height) {
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
			var playerSovereign = world.types.Lookup<Sovereign>("svPlayer");
			player = new PlayerShip(new Ship(world, playerClass, playerSovereign, new XY(0, 0)));
			world.AddEntity(player);
			var daughters = new Station(world, world.types.Lookup<StationType>("stDaughtersOutpost"), new XY(5, 5));
			world.AddEntity(daughters);

			player.messages.Add(new PlayerMessage("Welcome to Transcendence: Rogue Frontier!"));
		}
		public override void Update(TimeSpan delta) {
			tiles.Clear();
			//Place everything in the grid
			world.entities.UpdateSpace();
			world.effects.UpdateSpace();

			//Update everything
			foreach (var e in world.entities.all) {
				e.Update();
				if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
					tiles[e.Position] = e.Tile;
				}
			}
			foreach (var e in world.effects.all) {
				e.Update();
				if (e.Tile != null && !tiles.ContainsKey(e.Position)) {
					tiles[e.Position] = e.Tile;
				}
			}

			camera = player.Position;

			world.entities.all.UnionWith(world.entitiesAdded);
			world.effects.all.UnionWith(world.effectsAdded);
			world.entitiesAdded.Clear();
			world.effectsAdded.Clear();
			world.entities.all.RemoveWhere(e => !e.Active);
			world.effects.all.RemoveWhere(e => !e.Active);
		}
		public override void Draw(TimeSpan drawTime) {
			var y = Height * 3 / 5;
			Clear();
			foreach (var message in player.messages) {
				var line = message.Draw();
				var x = Width * 3 / 4 - line.Count;
				Print(x, y, line);
				y++;
			}
			base.Draw(drawTime);
		}
		public ColoredGlyph GetTile(XY xy) {
			var back = GetBackTile(xy);
			if (tiles.TryGetValue(xy, out ColoredGlyph g)) {
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
			if(info.IsKeyPressed(S)) {
				Hide();
				new ShipScreen(this, player).Show(true);
			}
			if(info.IsKeyPressed(D)) {
				if(player.docking != null) {
					player.docking = null;
				} else {
					var dest = world.entities.GetAll(p => (player.Position - p).Magnitude < 8).OfType<Station>().OrderBy(p => (p.Position - player.Position).Magnitude).FirstOrDefault();
					if(dest != null) {
						player.docking = new Docking(player.ship, dest);
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
					var facingRads = player.ship.stoppingRotation * Math.PI / 180;

					var ccw = (XY.Polar(facingRads + 1 * Math.PI / 180) - XY.Polar(mouseRads)).Magnitude;
					var cw = (XY.Polar(facingRads - 1 * Math.PI / 180) - XY.Polar(mouseRads)).Magnitude;
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