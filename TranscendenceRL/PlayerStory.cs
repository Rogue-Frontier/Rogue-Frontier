using System.Collections.Generic;
using System.Text;
using SadConsole;

namespace TranscendenceRL {
    class PlayerStory {
        enum DaughtersArc {
            Intro
        }
        DaughtersArc daughtersArc = DaughtersArc.Intro;
        public PlayerStory() {

        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            switch(daughtersArc) {
                case DaughtersArc.Intro:
                    if (d is Station s && s.StationType.codename == "station_daughters_outpost") {
                        var t =
@"Docking at the main entrance of the abbey, the great magenta
tower seems to reach into the oblivion above your head.
It looks much more massive from the view of the station platform.
The rows of stained glass windows glow warmly with orange light.

You feel strange walking into a station that looks nothing
like your home station. You've been to new places before.
At least, during your vacations to the virtual reality arcade.

But this is not a vacation.

You almost begin to question how you got here.".Replace("\r", "");
                        var sc = new TextScene(prev, t, new List<SceneOption>() {
                            new SceneOption() {
                                escape = true,
                                enter = true,
                                key = 'C',
                                name = "Continue",
                                next = null
                            }
                        });
                        sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                        return sc;
                    }
                    break;
            }
            return null;
        }
    }
}
