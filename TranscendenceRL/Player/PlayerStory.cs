using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ASECII;
using Common;
using SadConsole;
using SadRogue.Primitives;
using Console = SadConsole.Console;

namespace TranscendenceRL {

    interface IPlayerInteraction {
        Console GetScene(Console prev, Dockable d, PlayerShip playerShip);
    }
    class DaughtersIntro : IPlayerInteraction {
        PlayerStory story;
        public DaughtersIntro(PlayerStory story) {
            this.story = story;
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if (d is Station s && s.StationType.codename == "station_daughters_outpost") {
                var heroImage = s.StationType.heroImage;
                /*
                var benedictPortrait = SScene.LoadImage("RogueFrontierContent/BenedictPortrait.asc.cg").Translate(new Point(heroImage.Max(p => p.Key.Item1), 4));
                var outpostLobby = SScene.LoadImage("RogueFrontierContent/DaughtersOutpostDock.asc.cg").Translate(new Point(benedictPortrait.Max(p => p.Key.Item1), 4));
                */
                Console Intro() {
                    var t =
@"Docking at the front entrance of the abbey, the great
magenta tower seems to reach into the oblivion above
your head. It looks much more massive from the view of
the platform that juts out the side of the docking ring
The rows of stained glass windows glow warmly with
orange light. Nevertheless, you can't help but think...

You are a complete stranger here.".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() { escape = false,
                            key = 'C', name = "Continue",
                            next = Intro2
                        }, new SceneOption() {
                            escape = true,
                            key = 'L', name = "Leave",
                            next = Intro2b
                        }
                    
                    }) { background = heroImage };
                    return sc;
                }

                Console Intro2(Console from) {

                    var t =
@"Walking into the main hall, You see a great monolith of
sparkling crystals and glowing symbols. A low hum echoes
throughout the room. If you stand still, you can hear
some indistinct whispering from somewhere behind.

A stout man stands for reception duty near a wide door.

""Ah, hello. A meeting is in session right now.
You must be new here... May I help you with anything?""
".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true,
                                        key = 'I', name = @"""I've been hearing a voice...""",
                                        next = Intro3
                                }}) { background = heroImage };
                    return sc;
                }


                Console Intro2b(Console from) {

                    var t =
@"You decide to step away from the station,
much to the possible chagrin of some mysterious
entity and several possibly preferred timelines.".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true,
                                        key = 'U', name = @"Undock",
                                        next = null
                                }}) { background = heroImage };
                    return sc;
                }

                Console Intro3(Console from) {
                    var t =
@"""I've been hearing a voice. It calls itself...""

""The Orator.""

""And I thought you might know something about it.""

""Hmmm, yes, we are quite experienced with The Orator.
Though you are the first guest we've had in a while.
What did you hear?"" The man asked.".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true,
                                        key = 'T', name = @"""The voice told me...""",
                                        next = Intro4
                                }}) { background = heroImage };
                    return sc;
                }
                Console Intro4(Console from) {
                    string t =
@"""The voice told me...
that there is something terribly wrong
happening to us. With humanity. I had a vision...""

""...I felt a sort of stillness as I watched
centuries of human history pass
beyond the Earth...""

""...It was dreadful, watching every civilization cycle
between war and peace in the most repetitive manner...""

""I saw history crumble, not under earthquake or gravity
or any other force of nature, but under itself...""

""And the Orator told me, that They had an answer. And that
if I went to Them, and I found Them at the Galactic Core,
and I listened to Their words, and I wielded Their powers,
then They would bring forth an ultimate peace.""

""And...""

Wait, how are you saying all of this- Your mind blanks out.

""...And I... I witnessed all of this in a dream I had?""
".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true,
                                        key = 'C', name = @"Continue",
                                        next = Intro5
                                }}) { background = heroImage };
                    return sc;
                }
                Console Intro5(Console from) {
                    string t =
@"The man replies, ""...I understand. That reminds me of
my own first encounter with The Orator.""

""Experience has taught well-connected Followers that there
are other answers besides leaving for the Galactic Core.""

""The old survivors built this place to provide a shelter
for those who seek a certain kind of answer.""

""Unless, your answer rests..."" he points to a distant star
shining through the window, ""...far out there.""

""Does it?""
";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true,
                                        key = 'I', name = @"""It does.""",
                                        next = Intro6
                                }}) { background = heroImage };
                    return sc;

                }
                Console Intro6(Console from) {
                    string t =
@"
After a long pause, you respond.

""It does.""

The man thinks for a minute.

""I figured. You have your own starship, fit for
leaving this system and exploring the stars beyond.
We don't really see modern builds like yours
around here... Not since the last war.""

""You really intend to see what's out there.""";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() { escape = true,
                                        key = 'T', name = @"""That is correct.""",
                                        next = Intro7
                                }}) { background = heroImage };
                    return sc;
                }
                Console Intro7(Console from) {
                    string t =
@"""That is correct.""

The man sighs.

""So you understand that this is not the first time that
The Orator has spoken, and told someone to just pack up,
leave, and look for Them somewhere out there?""

""Are you prepared to die?""
";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                                    new SceneOption() {
                                        escape = true,
                                        key = 'H', name = @"""Huh?!?!?!""",
                                        next = Intro8
                                }}) { background = heroImage };
                    return sc;
                }
                Console Intro8(Console from) {
                    string t =
@"The man paces around for a while.

""The Orator calls people on the regular. We know that this
happens occasionally but predictably. We see a new person
come in first time, and ask us about The Orator. It's only
a matter of time until they leave this place for the last
time and we never see that person again-""

""-until they show up in a news headline and
someone identifies them as an unwitting traveler
who got blown up in the middle of a war zone...""

""...So tell me, what do you intend to do?""";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            escape = true,
                            key = 'I',
                            name = @"""I intend to reach the Galactic Core.""",
                            next = Intro9a
                    }, new SceneOption() {
                            key = '\0',
                            name = @"...",
                            next = Intro9b
                    }}) { background = heroImage };
                    return sc;
                }

                Console Intro9a(Console from) {
                    story.mainInteractions.Remove(this);
                    string t =
@"""Okay, I see you've already made your mind then.
I'll provide you with some combat training to start
your journey. That is all. Let's hope you make it.""";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            escape = true,
                            key = 'C', name = @"Continue",
                            next = Intro10
                    }}) { background = heroImage };
                    return sc;
                }


                Console Intro9b(Console from) {
                    story.mainInteractions.Remove(this);
                    string t =
@"You pause for a moment.";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            key = 'I', name = @"""I intend to reach the Galactic Core.""",
                            next = Intro10a
                        },new SceneOption() {
                            key = 'U', name = @"""I intend to destroy the United Constellation.""",
                            next = Destroy1
                        },new SceneOption() {
                            key = 'D', name = @"""I don't know anymore.""",
                            next = Intro10c
                        }
                    }) { background = heroImage };
                    return sc;
                }

                Console Intro10a(Console prev) {
                    story.mainInteractions.Remove(this);
                    string t =
@"""You sound uncertain there.
Do you truly intend to do that?""";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            escape = false,
                            key = 'I', name = @"I intend to reach the Galactic Core.",
                            next = Intro9a
                        }, new SceneOption() {
                            escape = true,
                            key = '\0', name = @"...",
                            next = Intro9b
                        }

                    }) { background = heroImage };
                    return sc;
                }


                Console Destroy1(Console prev) {
                    string t =
@"""I intend to destroy the United Constellation,"" you say.

""What?!?!"" the man says out loud.";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            escape = false,
                            key = 'I', name = @"It's simple. I hate them a lot.",
                            next = Destroy2
                        }, new SceneOption() {
                            escape = true,
                            key = 'G', name = @"It's for their own good.",
                            next = Destroy2
                        }
                    }) { background = heroImage };
                    return sc;
                }
                Console Destroy2(Console prev) {
                    string t =
@"You feel an energy welling up within you
as you speak.

""The United Constellation is a failed state!
They are built on monumental idiocy and inaction.
They cannot even protect their own people!""

""If the United Constellation cannot decide
their own wars, then maybe... I will.""

""Only then, can we begin reconstruction towards
a new era.""

You state your intentions firmly.";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            escape = false,
                            key = 'C', name = @"Continue",
                            next = Destroy3
                        }
                    }) { background = heroImage };
                    return sc;
                }
                //Placeholder dialogue
                //Should add more complicated stuff later
                Console Destroy3(Console prev) {
                    string t =
@"""You know what, that sounds like a good idea.
Allow me to join you on your mission.""

""My name is Benjamin, by the way""";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            escape = false,
                            key = 'C', name = @"Continue",
                            next = BenjaminJoin
                        }
                    }) { background = heroImage };
                    return sc;
                }
                Console BenjaminJoin(Console prev) {
                    story.mainInteractions.Remove(this);

                    var w = playerShip.World;
                    var wingmateClass = w.types.Lookup<ShipClass>("ship_beowulf");
                    var wingmate = new AIShip(new BaseShip(w, wingmateClass, playerShip.Sovereign, s.Position), new EscortOrder(playerShip, new XY(-5, 0)));
                    w.AddEntity(wingmate);
                    w.AddEffect(new Heading(wingmate));

                    return null;
                }
                Console Intro10c(Console prev) {
                    story.mainInteractions.Remove(this);
                    string t =
@"""I don't know anymore,"" you say.

";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            escape = false,
                            key = 'C', name = @"Continue",
                            next = null
                        }
                    }) { background = heroImage };
                    return sc;
                }
                Console Intro10(Console from) {
                    story.mainInteractions.Remove(this);
                    string t =
@"""Let's start with some target practice.
I've sent some drones outside the station.
Destroy them as fast as you can""";
                    t = t.Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() {
                            key = 'S', name = @"Start",
                            next = StartTraining
                        }
                    }) { background = heroImage };
                    return sc;
                }
                Console StartTraining(Console from) {
                    var m = new DaughtersTraining(story, s, playerShip);
                    story.mainInteractions.Add(m);
                    m.AddDrones();
                    return null;
                }
                var sc = Intro();
                return sc;
            } else {
                return null;
            }
        }
    }
    class DaughtersTraining : IPlayerInteraction {
        PlayerStory story;
        Station station;
        public AIShip[] drones;
        public int startTick;
        public DaughtersTraining(PlayerStory story, Station station, PlayerShip player) {
            this.story = story;
            this.station = station;
            var w = station.World;
            var shipClass = w.types.shipClass["ship_laser_drone"];
            var sovereign = Sovereign.SelfOnly;
            this.drones = new AIShip[3];
            var k = station.World.karma;
            for (int i = 0; i < 3; i++) {
                var d = new AIShip(new BaseShip(w, shipClass, sovereign, station.Position + XY.Polar(k.NextDouble() * 2 * Math.PI, k.NextDouble() * 25 + 25)), new SnipeOrder(player));
                drones[i] = d;
            }
        }
        public void AddDrones() {
            foreach(var d in drones) {
                station.World.AddEntity(d);
            }
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if (d == station) {
                var s = station;
                var heroImage = s.StationType.heroImage;
                var count = drones.Count(d => d.Active);
                if (count > 0) {
                    return InProgress();
                } else {
                    return Complete();
                }
                Console InProgress() {
                    var t =
@$"Benjamin meets you at the docking bay.

""There's still {count} drones left.""
".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() { escape = true,
                            key = 'C', name = "Continue",
                            next = null
                    }}) { background = heroImage };
                    return sc;
                }
                Console Complete() {
                    var sec = (station.World.tick - startTick) / 60;
                    var t =
@$"Benjamin meets you at the docking bay.

""You destroyed the drones in {sec} seconds.""

{(sec < 60 ? @"""I figured you were ready for that.""" : @"""So now you should know how to aim.""")}
".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() { escape = true,
                            key = 'C', name = "Continue",
                            next = Explore
                    }}) { background = heroImage };
                    return sc;
                }

                Console Explore(Console prev) {

                    var t =
@$"""There are lots of people - friendly or unfriendly - out there.
In a place like this, you'll find plenty of  stations offering trade
and services for money. Some might have jobs that you can take.""

""Others will attack you on sight. Be careful around them.""

""Only time will tell who is who.""

""Take a look around this system and find out who can help you.""
".Replace("\r", null);
                    var sc = new TextScene(prev, t, new List<SceneOption>() {
                        new SceneOption() { escape = true,
                            key = 'U', name = "Undock",
                            next = Undock
                    }}) { background = heroImage };
                    return sc;
                }
                Console Undock (Console prev) {
                    story.mainInteractions.Remove(this);
                    story.mainInteractions.Add(new BenjaminExploration(story, station, playerShip));
                    return null;
                }
            }
            return null;
        }
    }

    class BenjaminExploration : IPlayerInteraction {
        PlayerStory story;
        Station station;
        HashSet<Station> targets;
        public BenjaminExploration(PlayerStory story, Station station, PlayerShip playerShip) {
            this.story = story;
            this.station = station;
            var w = station.World;
            targets = new HashSet<Station>(w.entities.all.OfType<Station>().Where(
                s => s.Sovereign.IsFriend(playerShip)));
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if (d == station) {
                var s = station;
                var heroImage = s.StationType.heroImage;
                
                if(targets.IsSubsetOf(playerShip.Known)) {
                    
                    var sc = new TextScene(prev,
@$"""You've found all the friendly stations in the system. Now that
you know what services each provide you can return to them when
needed.""",
                    new List<SceneOption>() {
                        new SceneOption() { escape = true,
                            key = 'C', name = "Continue",
                            next = Undock
                    }}) { background = heroImage };
                    Console Undock(Console prev) {
                        story.mainInteractions.Remove(this);
                        return null;
                    }
                    return sc;
                } else {
                    int count = targets.Count - targets.Intersect(playerShip.Known).Count();

                    if(count > 1) {
                        return new TextScene(prev,
@$"""You've found all but {count} friendly stations in this system.
Use your starship's megamap to look for them.""",
                    new List<SceneOption>() {
                        new SceneOption() { escape = true,
                            key = 'U', name = "Undock",
                            next = null
                    }}) { background = heroImage };
                    } else {

                        return new TextScene(prev,
@$"""You've found all but one friendly station in this system.
Use your starship's megamap to look for it.""",
                    new List<SceneOption>() {
                        new SceneOption() { escape = true,
                            key = 'U', name = "Undock",
                            next = null
                    }}) { background = heroImage };
                    }
                }
            } else {
                

            }
            return null;
        }
    }

    class PlayerStory {
        public HashSet<IPlayerInteraction> mainInteractions;
        public HashSet<IPlayerInteraction> secondaryInteractions;

        public PlayerStory() {
            mainInteractions = new HashSet<IPlayerInteraction>();
            mainInteractions.Add(new DaughtersIntro(this));
            secondaryInteractions = new HashSet<IPlayerInteraction>();
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            Console sc;
            sc = mainInteractions.Select(m => m.GetScene(prev, d, playerShip)).FirstOrDefault(s => s != null);
            if(sc != null) {
                return sc;
            } else {
                if (d is Station source) {
                    string codename = source.StationType.codename;
                    if (codename == "station_constellation_astra") {
                        return ConstellationAstra(prev, source, playerShip);
                    } else if(codename == "station_constellation_habitat") {
                        return ConstellationHabitat(prev, source, playerShip);
                    }
                }
            }
            return null;
        }
        public Console ConstellationHabitat(Console prev, Station source, PlayerShip playerShip) {
            Console Intro(Console prev) {
                return new TextScene(prev,
@"You are docked at a Constellation Habitat,
a residential station of the United Constellation.",
                    new List<SceneOption>() {
                        new SceneOption() {
                            escape = false,
                            key = 'M', name = "Meeting Hall",
                            next = MeetingHall
                        },
                        new SceneOption() {escape = true,
                            key = 'U', name = "Undock",
                            next = null
                        }
                    });
            }
            Console MeetingHall(Console prev) {
                var mission = mainInteractions.OfType<DestroyStation>().FirstOrDefault(i => i.source == source);
                if (mission != null) {
                    return mission.GetScene(prev, source, playerShip);
                }
                
                var target = source.World.entities.all.OfType<Station>().Where(s => s.StationType.codename == "station_orion_warlords_camp").FirstOrDefault(other => (other.Position - source.Position).Magnitude < 256);

                if (target == null) {
                    return new TextScene(prev,
@"The meeting hall is empty.",
                    new List<SceneOption>() {
                        new SceneOption() {escape = true,
                            key = 'L', name = "Leave",
                            next = (s) => prev
                        }
                    });
                } else {
                    mission = mainInteractions.OfType<DestroyStation>().FirstOrDefault(i => i.target == target);

                    if(mission != null) {

                        return new TextScene(prev,
    @"You aimlessly stand in the center of the empty Meeting Hall.
After 2 minutes, the station master approaches you.

""Hi, uh, we're currently dealing with a particularly annoying
Orion Warlords camp but I've been told that you're going to
destroy it for us. So, uh, thank you and good luck!""
",
                        new List<SceneOption>() {
                        new SceneOption() {escape = true,
                            key = 'C', name = "Continue",
                            next = Intro
                        }
                        });
                    }


                    return new TextScene(prev,
@"You aimlessly stand in the center of the empty Meeting Hall.
After 10 minutes, the station master approaches you.

""Hi, uh, you seem to have a nice gunship. I'm currently dealing
with a nearby Orion Warlords outpost. They keep sending us a lot
of threats. We're not really worried about being attacked so much
as we just want them to shut the hell up. Even the health inspector
is less asinine than these idiots.""

""I'll pay you a few hundred to shut them up indefinitely.
What do you say?""
",
                    new List<SceneOption>() {
                        new SceneOption() {escape = false,
                            key = 'A', name = "Accept",
                            next = Accept
                        }, new SceneOption() {escape = true,
                            key = 'R', name = "Reject",
                            next = Reject
                        },
                    });

                    Console Accept(Console prev) {
                        return new TextScene(prev,
@"""Okay, thank you! Go destroy them and then I'll see you back here.""",
                            new List<SceneOption>() {
                                new SceneOption() {escape = false,
                                    key = 'U', name = "Undock",
                                    next = Accepted
                                }
                        });
                    }
                    Console Accepted(Console prev) {
                        DestroyStation mission = null;
                        mission = new DestroyStation(source, target) { inProgress = InProgress, debrief = Debrief };

                        mainInteractions.Add(mission);
                        return null;
                        Console InProgress(Console prev) {
                            var s = source;
                            var t = target;
                            return new TextScene(prev,
@"""Hey, you're going to destroy that station, right?""",
                                new List<SceneOption>() {
                                    new SceneOption() {escape = true,
                                        key = 'U', name = "Undock",
                                        next = null
                                    }
                            });
                        }
                        Console Debrief(Console prev) {
                            return new TextScene(prev,
@"""Thank you very much for destroying those warlords for us!
As promised, here's your money.""",
                                new List<SceneOption>() {
                                    new SceneOption() {escape = false,
                                        key = 'U', name = "Undock",
                                        next = Debriefed
                                    }
                                });
                        }
                        Console Debriefed(Console prev) {
                            mainInteractions.Remove(mission);
                            return null;
                        }
                    }
                    Console Reject(Console prev) {
                        return new TextScene(prev,
@"""Oh man, what the hell is it with you people?
Okay, fine, I'll just find someone else to do it then.""",
                            new List<SceneOption>() {
                                new SceneOption() {escape = false,
                                    key = 'U', name = "Undock",
                                    next = null
                                }
                            });
                    }
                }
            }
            return Intro(prev);
        }
        public Console ConstellationAstra(Console prev, Station source, PlayerShip playerShip) {
            Console Intro() {
                return new TextScene(prev,
@"You are docked at a Constellation Astra,
a major residential and commercial station
of the United Constellation.

The station is a stack of housing units,
utility-facilities, entertainment districts,
business sectors, and trading rooms. The governing
tower protrudes out the roofplate of the station.
The rotator tower rests on the underside.
From a distance, the place looks almost like
a spinning pinwheel.

There is a modest degree of artificial gravity here.",
                new List<SceneOption>() {
                    new SceneOption() {
                        escape = false,
                        key = 'T', name = "Trade",
                        next = Trade
                    },
                    new SceneOption() {escape = true,
                        key = 'U', name = "Undock",
                        next = null
                    }
                });
            }
            Console Trade(Console from) => new TradeScene(from, playerShip, source);
            return Intro();
        }
    }
    class DestroyStation : IPlayerInteraction {
        public Station source;
        public Station target;
        public Func<Console, Console> inProgress, debrief;
        public DestroyStation(Station source, Station target) {
            this.source = source;
            this.target = target;
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if(d == source) {
                return null;
            }
            if(target.Active) {
                return inProgress(prev);
            } else {
                return debrief(prev);
            }
        }
    }
}
