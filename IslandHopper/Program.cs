using System;
using SadConsole;
namespace IslandHopper {
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			Settings.ResizeMode = Settings.WindowResizeOptions.Scale;
			
			using (var game = new Game1())
                game.Run();
        }
    }
#endif
}
