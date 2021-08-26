using SadConsole;
using Console = SadConsole.Console;
using System.IO;
using System.Collections.Generic;
using SadRogue.Primitives;
using System.Linq;
using SadConsole.Input;
using System;
namespace Life {
    public class Program {
        public static int TICKS_PER_SECOND = 60;
        static Program() {
            Height = 60;
            Width = Height * 5 / 3;
        }
        public static int Width, Height;
        public static string font = ("IBMCGA.font");
        static void Main(string[] args) {
            // Setup the engine and create the main window.
            Game.Create(Width, Height, font);
            // Hook the start event so we can add consoles to the system.
            Game.Instance.OnStart = Start;
            // Start the game.
            Game.Instance.Run();
            Game.Instance.Dispose();
        }
        public static void Start() {
            Game.Instance.Screen = new Life(Width, Height) { IsFocused = true };
        }
    }
    public class Grid {
        private HashSet<(int, int)> live = new HashSet<(int, int)>();
        public bool this[int x, int y] {
            get => live.Contains((x, y));
            set { 
                if (value) {
                    live.Add((x, y));
                } else {
                    live.Remove((x, y));
                }
            }
        }
    }
    public class Life : Console {
        bool[,] cells, next;
        public Life(int Width, int Height) : base(Width, Height) {
            cells = new bool[Width, Height];
            next = new bool[Width, Height];
            UseKeyboard = true;
        }
        public void UpdateLife() {
            foreach (var x in Enumerable.Range(0, Width)) {
                foreach (var y in Enumerable.Range(0, Height)) {
                    cells[x, y] = next[x, y];
                }
            }
            UpdateNext();
        }
        public void UpdateNext() {
            foreach (var x in Enumerable.Range(1, Width - 2)) {
                foreach (var y in Enumerable.Range(1, Height - 2)) {
                    int c = Count(
                        cells[x - 1, y + 1], cells[x, y + 1], cells[x + 1, y + 1],
                        cells[x - 1, y], cells[x + 1, y],
                        cells[x - 1, y - 1], cells[x, y - 1], cells[x + 1, y - 1]);
                    next[x, y] = (c == 3) || (cells[x, y] && c >= 2 && c <= 3);
                }
            }
        }
        public override void Render(TimeSpan delta) {
            foreach (var x in Enumerable.Range(0, Width)) {
                foreach (var y in Enumerable.Range(0, Height)) {
                    this.SetCellAppearance(x, y, new ColoredGlyph(Color.White, next[x, y] ? Color.DarkSlateGray : Color.Black, cells[x, y] ? '*' : ' '));
                }
            }
            base.Render(delta);
        }
        public override bool ProcessKeyboard(Keyboard keyboard) {
            if(keyboard.IsKeyPressed(Keys.A)) {
                UpdateLife();
                return true;
            }
            return base.ProcessKeyboard(keyboard);
        }
        public override bool ProcessMouse(MouseScreenObjectState state) {
            if(state.Mouse.IsOnScreen && state.Mouse.LeftClicked) {
                var p = state.SurfaceCellPosition;
                cells[p.X, p.Y] = !cells[p.X, p.Y];
                UpdateNext();
            }
            return base.ProcessMouse(state);
        }
        public int Count(params bool[] bb) {
            int i = 0;
            foreach(var b in bb) {
                if(b) {
                    i++;
                }
            }
            return i;
        }
    }
}
