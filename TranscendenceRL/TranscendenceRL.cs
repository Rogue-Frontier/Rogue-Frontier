using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Themes;
using System.Xml.Linq;

namespace TranscendenceRL {
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class TranscendenceRL : SadConsole.Game {
		const int width = 200;
		const int height = 60;
		public TranscendenceRL() : base("IBM.font", width, height, null) {
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

			var def = Library.Default.Colors;
			def.ControlBack = Color.Transparent;
			def.ControlHostBack = Color.Transparent;
			def.ModalBackground = Color.Transparent;

			//var types = new TypeCollection(XElement.Parse(Properties.Resources.Items));

			var title = new TitleConsole(width, height, SadConsole.Global.FontDefault.Master.GetFont(Font.FontSizes.One));
			SadConsole.Global.FontDefault = SadConsole.Global.FontDefault.Master.GetFont(Font.FontSizes.Two);
			title.Position = new Point(0, 0);
			title.Show(true);
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
}
