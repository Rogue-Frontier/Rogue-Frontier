using System.Collections.Generic;
using System.Text;
using SadConsole;

namespace TranscendenceRL {

    interface IPlayerMission {
        Console GetScene(Console prev, Dockable d, PlayerShip playerShip);
    }
    class DaughtersIntro {

    }
    class PlayerStory {
        enum DaughtersArc {
            Intro
        }
        DaughtersArc daughtersArc = DaughtersArc.Intro;
        public PlayerStory() {

        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {

            switch(daughtersArc) {
                case DaughtersArc.Intro: {
                        if (d is Station s && s.StationType.codename == "station_daughters_outpost") {
                            Console Intro() {
                                var t =
    @"Docking at the front entrance of the abbey, the great magenta
tower seems to reach into the oblivion above my head.
It looks much more massive from the view of the station platform.
The rows of stained glass windows glow warmly with orange light.

You're a complete stranger here.".Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true, enter = true,
                                        key = 'C', name = "Continue",
                                        next = Intro2
                                }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;
                            }

                            Console Intro2(Console from) {
                                var t =
@"Stumbling into the main hall, I see a great monolith of
sparkling crystal and glowing symbols. A low hum echoes
throughout the room. A stout man stands at a podium
by the entrance.

""Ah, hello. A Communication is in session right now.
You must be new here... May I help you with anything?""

The man asks.".Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true, enter = true,
                                        key = 'I', name = @"""I've been hearing a voice...""",
                                        next = Intro3
                                }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;
                            }
                            Console Intro3(Console from) {
                                var t =
@"""I've been hearing a voice. It calls itself The Orator.
I thought you might know something about it.""

""Hmm, I understand. What did this voice tell you?"" The man asked.".Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true, enter = true,
                                        key = 'T', name = @"""The voice told me...""",
                                        next = Intro4
                                }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;
                            }
                            Console Intro4(Console from) {
                                string t =
@"""The voice told me...
that there is something terribly wrong happening to us. I had a vision...""

""...I felt a sort of stillness as I watched centuries of human history pass
beyond the Earth...""

""...It was dreadful, watching every civilization cycle
between war and peace in the most repetitive manner...""

""I saw history crumble, not under earthquake or gravity or any other force of nature,
but under itself...""

""And the Orator told me, that They had an answer. And that
if I went to Them, and I listened to Their words,
then They would bring forth an ultimate peace...""

""...and I heard all of this in a dream that I had last night.""
".Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true, enter = true,
                                        key = 'C', name = @"Continue",
                                        next = Intro5
                                }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;
                            }
                            Console Intro5(Console from) {
                                string t =
@"The man replies, ""...I understand. That reminds me of my own first encounter
with The Orator. The people here built this place to provide a shelter for those
who seek a kind of answer.""

""Unless, your answer rests..."" he points to a distant star shining through the window, ""...far out there.""";
                                t = t.Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true, enter = true,
                                        key = 'I', name = @"""It does.""",
                                        next = Intro6
                                }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;

                            }
                            Console Intro6(Console from) {
                                string t =
@"
After a long pause, you respond.

""It does.""

The man thinks for a minute.

""I figured. You have your own starship, fit for leaving this system
and exploring the stars beyond. We don't really see modern builds like
yours around here. Not since the last war.""

""You really intend to see what's out there.""";
                                t = t.Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true, enter = true,
                                        key = 'T', name = @"""That's correct.""",
                                        next = Intro7
                                }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;
                            }
                            Console Intro7(Console from) {
                                string t =
@"""That's correct.""

""And you understand that this is not the first time that The Orator has spoken,
and told someone to just pack up, leave, and look for Them somewhere out there.""

The man sighs.";
                                t = t.Replace("\r", null);
                                var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() {
                                        escape = true, enter = true,
                                        key = 'O', name = @"""Of course""",
                                        next = Intro8
                                }});
                                sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                return sc;

                                Console Intro8(Console from) {
                                    string t =
@"
""Of course,"" you reply.

The man paces around for a while.

""The Orator calls people on the regular. We see this happen about twice a year.
We see a new person come in first time, and ask us about The Orator.
It's only a matter of time until they leave this place for the last time
and we never see that person again. Until they show up in a news headline
about how they were a tourist that got blown up in the middle of a war zone...""

""...Anyway, I see you've already made your decision. I'll start you off
for your journey with some combat training. That is all. Let's hope you make it.""

""And my name is Benjamin, by the way.""";
                                    t = t.Replace("\r", null);
                                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                        new SceneOption() {
                                            escape = true,
                                            enter = true,
                                            key = 'I',
                                            name = @"""It's good to meet you, Benjamin.""",
                                            next = null
                                    }});
                                    sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                                    return sc;
                                }
                            }

                            var sc = Intro();
                            sc.Children.Add(new HeroImageScene(prev, s.StationType.heroImage, s.StationType.heroImageTint));
                            return sc;
                        }
                        break;
                    }
            }
            {
                if (d is Station s && s.StationType.codename == "station_constellation_astra") {
                    Console Intro() {
                        var sc = new TextScene(prev,
@"You are docked at a Constellation Astra,
a major residential and commercial station
of the United Constellation.

The station is a stack of housing units,
utility-facilities, entertainment districts,
business sectors, and trading rooms. The governing
tower protrudes out of the top of the station.
The rotator tower rests on the underside.
From a distance, the place looks like
a spinning pinwheel.

There is a modest degree of artificial gravity here.
".Replace("\r", null), new List<SceneOption>() {
                            new SceneOption() {
                                enter = true, escape = false,
                                key = 'T', name = "Trade",
                                next = Trade
                            },
                            new SceneOption() {
                                enter = false, escape = true,
                                key = 'U', name = "Undock",
                                next = null
                            }
                        });
                        return sc;
                    }
                    Console Trade(Console from) => new TradeScene(from, playerShip, s);
                    return Intro();
                }
            }
            return null;
        }
    }
}
