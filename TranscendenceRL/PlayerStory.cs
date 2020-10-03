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

                        Console Intro(int i = 0) {
                            var t =
@"Docking at the front entrance of the abbey, the great magenta
tower seems to reach into the oblivion above my head.
It looks much more massive from the view of the station platform.
The rows of stained glass windows glow warmly with orange light.

I feel strange walking into a station that looks nothing
like my home station. I've been to new places before.
At least, during my vacations to the virtual reality arcade.

But this is not a vacation.

I almost begin to question how I got here.".Replace("\r", null);
                            var sc = new TextScene(prev, t, new List<SceneOption>() {
                            new SceneOption() {
                                escape = true,
                                enter = true,
                                key = 'C',
                                name = "Continue",
                                next = Intro2
                                }});
                            sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                            return sc;

                            Console Intro2() {
                                var t =
@"Stumbling into the main hall, I see a great monolith of
sparkling crystal and glowing symbols. A low hum echoes
throughout the room. A stout man stands at a podium
by the entrance.

""Ah, hello! You must be new here. May I help you with anything?".Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                new SceneOption() {
                                    escape = true,
                                    enter = true,
                                    key = 'I',
                                    name = @"""I had a vision, and...""",
                                    next = Intro3
                                    }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;

                                Console Intro3() {
                                    var t =
@"""I- I had a vision, and...""

You find it quite difficult to describe.

""Ah, you must have had a vision of The Orator in a dream, right?
All followers of The Orator have met him once through a dream.
Some of us even remember what we saw and heard when it happened.
By any chance, do you have any memory of what you saw?"" The man asked.".Replace("\r", null);
                                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                        new SceneOption() {
                                            escape = true,
                                            enter = true,
                                            key = 'I',
                                            name = @"""I felt a stillness...""",
                                            next = () => Intro4(0)
                                            }});
                                    sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                    return sc;

                                    Console Intro4(int i) {
                                        string t =
@"""I...""

""I...""

It takes you a while to finally say it.";
                                        switch(i) {
                                            case 0: t =
@"""...I felt a sort of stillness as I watched centuries of human history pass
beyond the Earth...""

""It was dreadful, watching every civilization cycle
between war and peace in the most predictably repetitive manner...""

""I saw history crumble, not under earthquake or gravity or any other force of nature,
but under itself...""

""And the Orator told me, that if I listened to His words, then he would 
bring forth an ultimate peace...""


";
                                                break;
                                        }
                                        t = t.Replace("\r", null);
                                        var sc = new TextScene(prev, t, new List<SceneOption>() {
                                        new SceneOption() {
                                            escape = true,
                                            enter = true,
                                            key = 'I',
                                            name = @"",
                                            next = null
                                            }});
                                        sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                        return sc;

                                    }
                                }
                            }
                        }

                        var sc = Intro();
                        sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                        return sc;
                    }
                    break;
            }
            return null;
        }
    }
}
