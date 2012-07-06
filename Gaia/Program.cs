using System;

namespace Gaia
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using (GameCore game = new GameCore())
            {
                game.Run();
            }
        }
    }
}

