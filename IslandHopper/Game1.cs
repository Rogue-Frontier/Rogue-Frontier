using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using IslandHopper;

using SadConsole;
using System;
using System.Collections;
using System.Collections.Generic;
using SadConsole.Themes;
using SadConsole.Controls;

namespace IslandHopper {
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : SadConsole.Game {

		public Game1() : base("IBM.font", 240, 64, null) {
			Content.RootDirectory = "Content";
		}
		
		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize() {
			// Generally you don't want to hide the mouse from the user
			IsMouseVisible = true;
			// Finish the initialization of SadConsole    
			base.Initialize();
			//Settings.ToggleFullScreen();
			// Create your console    
			var firstConsole = new TitleConsole(240, 64);
			firstConsole.Position = new Point(0, 0);
			SadConsole.Global.CurrentScreen.Children.Add(firstConsole);
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent() {
			
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent() {
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime) {
			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime) {
			GraphicsDevice.Clear(Color.Black);
			base.Draw(gameTime);
		}
	}
	class TitleConsole : SadConsole.ControlsConsole {
		private double time = 0;

		private int waterLevel;
		private const int waterHeight = 10;
		private const double waterLineInterval = 0.2;
		private const double waterLineSpeed = 36.0;
		private List<Point2> waterLines = new List<Point2>();

		private double waterTrailInterval = 1.0 / waterLineSpeed;
		private const double waterTrailLifespan = 8;
		private List<WaterTrail> waterTrails = new List<WaterTrail>();

		const string PLANE =  @"<\_______ " + "\n"
							+ @" \\__>__O\";

		private const double planeInterval = 5;
		private const double planeSpeed = 10;
		private const int planeLevel = 18;
		private List<Point2> planes = new List<Point2>();

		/*
		const string player = @"/=\" + "\n"
							+ @"@&_";
		*/
		const string PLAYER = @" *" + "\n"
							+ @"@&";
		private const double playerInterval = Math.PI/2.5;
		private const double playerFallSpeed = 2;
		private List<Point2> players = new List<Point2>();

		private const double landSpeed = 10.0;
		private const double landSpawnTime = 2;
		List<Point2> land = new List<Point2>();
		bool[,] landGrid;

		private List<Timer> timers;

		private Random Random = Global.Random;

		ButtonTheme BUTTON_THEME = new SadConsole.Themes.ButtonTheme() {
			Normal = new SadConsole.Cell(Color.Yellow, Color.Transparent),
			Disabled = new Cell(Color.Gray, Color.Transparent),
			Focused = new Cell(Color.Red, Color.Transparent),
			MouseClicking = new Cell(Color.White, Color.Transparent),
			MouseOver = new Cell(Color.Red, Color.Transparent)
		};

		Button start, quit;

		public TitleConsole(int width, int height) : base(width, height) {
			landGrid = new bool[Width, Height];

			start = new SadConsole.Controls.Button(10) {
				Position = new Point(5, 5),
				Text = "START",
				Theme = BUTTON_THEME
			};
			start.Click += (btn, args) => {
				Global.CurrentScreen.Children.Remove(this);
				var game = new GameConsole(180, 60);
				Global.CurrentScreen.Children.Add(game);
				game.IsFocused = true;
			};
			start.IsFocused = true;
			Add(start);

			quit = new SadConsole.Controls.Button(10) {
				Position = new Point(5, 6),
				Text = "QUIT",
				Theme = BUTTON_THEME
			};
			quit.Click += (btn, args) => {
				Environment.Exit(0);
			};
			Add(quit);


			waterLevel = 50;

			timers = new List<Timer> {
				new Timer(waterLineInterval, () => {
					waterLines.Add(new Point2(0, waterLevel + Global.Random.Next(waterHeight)));
				}),
				new Timer(waterTrailInterval, () => {
					waterLines.ForEach(line => waterTrails.Add(new WaterTrail(line.x, line.y, waterTrailLifespan)));
				}),
				new Timer(planeInterval, () => {
					planes.Add(new Point2(0, planeLevel + Global.Random.Next(10)));
				}),
				new Timer(playerInterval, () => {
					planes.ForEach(plane => {
						if (Helper.InRange(plane.x + PLANE.LineLength(), Width/2, 30) && Global.Random.Next(2) < 1)
							players.Add(plane.clone() + new Point2(8, 1));
					});
				}),
				new Timer(0.05, () => {
					if(time < landSpawnTime)
						for(int i = 0; i < 25; i++)
							land.Add(new Point2(Width/2 + Random.Amplitude(15) + Random.Amplitude(15) + Random.Amplitude(15) + Random.Amplitude(15), planeLevel));
				})
			};
		}
		public override void Update(TimeSpan delta) {
			base.Update(delta);
			double sec = delta.TotalSeconds;
			time += sec;
			timers.ForEach(timer => timer.Update(sec));

			waterLines.ForEach(line => line.x += sec * waterLineSpeed);
			waterTrails.ForEach(trail => trail.lifetime -= sec);
			planes.ForEach(plane => plane.x += sec * planeSpeed);
			players.ForEach(player => player.y += sec * playerFallSpeed);


			//Clear grid for collision checking
			Array.Clear(landGrid, 0, landGrid.Length);
			land.ForEach(p => landGrid[p.xi, p.yi] = true);

			//Make the land points fall towards sea level and settle
			land.ForEach(l => {
				if(l.yi < waterLevel) {
					if(!landGrid[l.xi, l.yi+1]) {
						l.y += sec * landSpeed;
					}
				}
			});


			waterLines.RemoveAll(line => line.x > Width - 1);
			waterTrails.RemoveAll(trail => trail.lifetime < 0);
			planes.RemoveAll(plane => plane.x + PLANE.LineLength() > Width - 1);
			players.RemoveAll(player => player.y > waterLevel - 5);

		}
		public override void Draw(TimeSpan delta) {
			Clear();
			PrintTitle();
			PrintWater();
			PrintPlanes();
			PrintLand();
			PrintPlayers();
			base.Draw(delta);
		}
		private void PrintTitle() {
			string title = Properties.Resources.Title;
			int titleX = Width / 2 - title.LineLength() / 2;
			int titleY = 0;
			this.PrintLines(titleX, titleY, title, Color.Gold);
		}
		private void PrintWater() {

			foreach (var trail in waterTrails) {
				Print((int)trail.x, (int)trail.y, "=", new Color(Color.Blue, (int)(255 * trail.lifetime / trail.lifespan)));
			}
			foreach (var line in waterLines) {
				Print((int)line.x, (int)line.y, "=", Color.Blue);
			}
		}
		private void PrintPlanes() {
			foreach(var p in planes) {
				Color c = Color.Green;
				if(p.xi < 10) {
					c = new Color(c, 255 * p.xi / 10);
				} else if(p.xi > Width - (10 + PLANE.LineLength())) {
					c = new Color(c, 255 * (Width - p.xi - PLANE.LineLength()) / 10);
				}
				this.PrintLines(p.xi, p.yi, PLANE, c);

			}
		}
		private void PrintPlayers() {
			foreach(var p in players) {
				if(p.xi > 0) {
					Color c = Color.White;
					if(p.yi > waterLevel - 10) {
						c = new Color(c, 255 * (waterLevel - p.yi) / 10);
					}
					this.PrintLines(p.xi, p.yi, PLAYER, c);
				}
			}
		}
		private void PrintLand() {
			land.ForEach(l => this.Print(l.xi, l.yi, "=", Color.Green));
		}
	}
	class WaterTrail {
		public double x, y;
		public double lifetime;
		public double lifespan { get; private set; }
		public WaterTrail(double x, double y, double lifetime) {
			this.x = x;
			this.y = y;
			this.lifetime = lifetime;
			lifespan = lifetime;
		}
		public void Update(double time) {
			lifetime -= time;
		}
	}
	class Timer {
		private Action action;
		private double interval;
		private double time;
		public Timer(double interval, Action action) {
			this.action = action;
			this.interval = interval;
			this.time = interval;
		}
		public void Update(double passed) {
			time -= passed;
			if(time < 0) {
				time = interval;
				action.Invoke();
			}
		}
	}
	class RandomTimer {
		private Action action;
		Func<double> intervalSource;
		private double time;
		public RandomTimer(Func<double> intervalSource, Action action) {
			this.action = action;
			this.intervalSource = intervalSource;
			this.time = intervalSource.Invoke();
		}
		public void Update(double passed) {
			time -= passed;
			if(time < 0) {
				time = intervalSource.Invoke();
				action.Invoke();
			}
		}
	}
}

