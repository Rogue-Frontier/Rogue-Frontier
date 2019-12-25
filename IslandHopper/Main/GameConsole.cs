using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using static IslandHopper.Constants;

namespace IslandHopper {
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
        Island World;
		public GameConsole(int Width, int Height) : base(Width, Height) {
            Theme = new WindowTheme {
                ModalTint = Color.Transparent,
                FillStyle = new Cell(Color.White, Color.Black),
            };
            UseKeyboard = true;
			UseMouse = true;
			this.DebugInfo($"Width: {Width}", $"Height: {Height}");

            int size = 300;
            int height = 30;
            World = new Island() {
                karma = new Random(0),
                entities = new Space<Entity>(size, size, height, e => e.Position),
                effects = new Space<Effect>(size, size, height, e => e.Position),
                voxels = new ArraySpace<Voxel>(size, size, height, new Air()),
                camera = new XYZ(0, 0, 0),
                types = new TypeCollection(XElement.Parse(Properties.Resources.Items))
			};
			World.player = new Player(World, new XYZ(29, 29, 1));
			//World.AddEntity(new Player(World, new Point3(85, 85, 20)));

			for (int x = 0; x < World.voxels.Width; x++) {
				for(int y = 0; y < World.voxels.Height; y++) {
					World.voxels[new XYZ(x, y, 0)] = new Grass();
				}
			}

			for(int i = 0; i < 1; i++) {

				World.entities.Place(World.types.Lookup<ItemType>("itSevenShooter").GetItem(World, new XYZ(28, 28, 1)));
            }
			
			World.entities.Place(World.player);
		}
		public override void Show(bool modal) {
			base.Show(modal);
			new PlayerMain(Width, Height, World) {
				IsFocused = true
			}.Show(true);
		}
		public override void Update(TimeSpan delta) {
		}
		private int HalfWidth { get => Width / 2; }
		private int HalfHeight { get => Height / 2; }
		public override void Draw(TimeSpan delta) {
            this.DebugInfo($"Draw({delta})");
            Clear();

            int HalfViewWidth = 90;
            int HalfViewHeight = 30;
			for(int x = -HalfViewWidth; x < HalfViewWidth; x++) {
				for(int y = -HalfViewHeight; y < HalfViewHeight; y++) {
					XYZ location = World.camera + new XYZ(x, y, 0);
					
					Print(x + HalfWidth, y + HalfHeight, World.GetGlyph(location));
				}
			}
            base.Draw(delta);
		}
		public override bool ProcessKeyboard(SadConsole.Input.Keyboard info) {
			return base.ProcessKeyboard(info);
		}
	}
	interface GameMenu {
		bool Done { get; }
		void Update(TimeSpan delta);
		void Draw(TimeSpan delta);
		
		void ProcessKeyboard(SadConsole.Input.Keyboard info);
	}
	static class Help {
		public static void Transparent(this SadConsole.Console c) {
			c.DefaultBackground = Color.Transparent;
			c.DefaultForeground = Color.Transparent;
			c.Clear();
		}
	}
}
