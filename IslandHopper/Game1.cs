using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using IslandHopper;

using SadConsole;
using System;
using System.Collections;
using System.Collections.Generic;

namespace IslandHopper {
	public static class Helper {
		public static int LineLength(this string lines) {
			return lines.IndexOf('\n');
		}
		public static void PrintLines(this SadConsole.Console console, int x, int y, string lines, Color? foreground = null, Color? background = null, SpriteEffects? mirror = null) {
			foreach (var line in lines.Replace("\r\n", "\n").Split('\n')) {
				console.Print(x, y, line, foreground, background, mirror);
				y++;
			}
		}
	}
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class Game1 : SadConsole.Game {

		public Game1() : base("IBM.font", 240, 63, null) {
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
			var firstConsole = new TitleConsole(180, 180);
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
	class TitleConsole : SadConsole.Console {
		private const double waterLineInterval = 0.4;
		private double waterLineNext = 0;
		private const double waterLineSpeed = 36.0;
		private List<PointD> waterLines = new List<PointD>();

		private double waterTrailNext = 0;
		private const double waterTrailLifespan = 2;
		private List<WaterTrail> waterTrails = new List<WaterTrail>();

		//List<Timer> timers = new List<Timer>();

		public TitleConsole(int width, int height) : base(width, height) {
		}
		public override void Update(TimeSpan delta) {
			base.Update(delta);
			UpdateWater(delta);
			UpdatePlanes();
			
			Clear();
			PrintWater();
			PrintTitle();
		}
		public void UpdateWater(TimeSpan delta) {
			waterLineNext -= delta.TotalSeconds;
			waterTrailNext -= delta.TotalSeconds;
			while(waterLineNext < 0) {
				waterLineNext += waterLineInterval;
				waterLines.Add(new PointD(0, 30 + Global.Random.Next(10)));
			}
			while(waterTrailNext < 0) {
				waterTrailNext += 1.0 / waterLineSpeed;
				waterLines.ForEach(line => waterTrails.Add(new WaterTrail(line.x, line.y, waterTrailLifespan)));
			}
			waterLines.ForEach(water => water.x += delta.TotalSeconds * waterLineSpeed);
			waterTrails.ForEach(water => water.lifetime -= delta.TotalSeconds);
		}
		
		private void UpdatePlanes() {

		}
		private void PrintWater() {
			waterLines.RemoveAll(line => line.x > Width - 1);
			waterTrails.RemoveAll(trail => trail.lifetime < 0);

			foreach (var trail in waterTrails) {
				Print((int)trail.x, (int)trail.y, "=", new Color(Color.Blue, (int) (255 * trail.lifetime / trail.lifespan)));
			}
			foreach (var line in waterLines) {
				Print((int)line.x, (int)line.y, "=", Color.Blue);
			}
		}
		private void PrintTitle() {
			string title = Properties.Resources.Title;
			int titleX = Width / 2 - title.LineLength() / 2;
			int titleY = 0;
			this.PrintLines(titleX, titleY, title, Color.Gold);
		}
	}
	class PointD {
		public double x;
		public double y;
		public PointD(double x, double y) {
			this.x = x;
			this.y = y;
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
			while(time < 0) {
				time += interval;
				action.Invoke();
			}
		}
	}
}

