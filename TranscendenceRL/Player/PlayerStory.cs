using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ASECII;
using Common;
using SadConsole;
using SadRogue.Primitives;
using TranscendenceRL;
using Console = SadConsole.Console;

namespace TranscendenceRL {

    interface IPlayerInteraction {
        Console GetScene(Console prev, Dockable d, PlayerShip playerShip);
    }
    class IntroMeeting : IPlayerInteraction {
        PlayerStory story;
        public IntroMeeting(PlayerStory story) {
            this.story = story;
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if (d is Station s && s.type.codename == "station_daughters_outpost") {
                var heroImage = s.type.heroImage;
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
                    var sc = new TextScene(prev, t, new() {
                        new() { escape = false,
                            key = 'C', name = "Continue",
                            next = Intro2
                        }, new() {
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
                    var sc = new TextScene(prev, t, new() {
                                    new() { escape = true,
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
                    var sc = new TextScene(prev, t, new() {
                                    new() { escape = true,
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
                    var sc = new TextScene(prev, t, new() {
                                    new() { escape = true,
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
                    var sc = new TextScene(prev, t, new() {
                                    new() { escape = true,
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
                    var sc = new TextScene(prev, t, new() {
                                    new() { escape = true,
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
                    var sc = new TextScene(prev, t, new() {
                                    new() { escape = true,
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
                    var sc = new TextScene(prev, t, new() {
                                    new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
                            escape = true,
                            key = 'I',
                            name = @"""I intend to reach the Galactic Core.""",
                            next = Intro9a
                    }, new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
                            key = 'I', name = @"""I intend to reach the Galactic Core.""",
                            next = Intro10a
                        },new() {
                            key = 'U', name = @"""I intend to destroy the United Constellation.""",
                            next = Destroy1
                        },new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
                            escape = false,
                            key = 'I', name = @"I intend to reach the Galactic Core.",
                            next = Intro9a
                        }, new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
                            escape = false,
                            key = 'I', name = @"It's simple. I hate them a lot.",
                            next = Destroy2
                        }, new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
                            escape = false,
                            key = 'C', name = @"Continue",
                            next = BenjaminJoin
                        }
                    }) { background = heroImage };
                    return sc;
                }
                Console BenjaminJoin(Console prev) {
                    story.mainInteractions.Remove(this);

                    var w = playerShip.world;
                    var wingmateClass = w.types.Lookup<ShipClass>("ship_beowulf");
                    var wingmate = new AIShip(new BaseShip(w, wingmateClass, playerShip.sovereign, s.position), new EscortOrder(playerShip, new XY(-5, 0)));
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
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
                    var sc = new TextScene(prev, t, new() {
                        new() {
                            key = 'S', name = @"Start",
                            next = StartTraining
                        }
                    }) { background = heroImage };
                    return sc;
                }
                Console StartTraining(Console from) {
                    var m = new IntroTraining(story, s, playerShip);
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
    class IntroTraining : IPlayerInteraction {
        PlayerStory story;
        Station station;
        public AIShip[] drones;
        public int startTick;
        public IntroTraining(PlayerStory story, Station station, PlayerShip player) {
            this.story = story;
            this.station = station;
            var w = station.world;
            var shipClass = w.types.shipClass["ship_laser_drone"];
            var sovereign = Sovereign.SelfOnly;
            this.drones = new AIShip[3];
            var k = station.world.karma;
            for (int i = 0; i < 3; i++) {
                var d = new AIShip(new BaseShip(w, shipClass, sovereign, station.position + XY.Polar(k.NextDouble() * 2 * Math.PI, k.NextDouble() * 25 + 25)), new SnipeOrder(player));
                drones[i] = d;
            }
        }
        public void AddDrones() {
            foreach(var d in drones) {
                station.world.AddEntity(d);
                station.world.AddEffect(new Heading(d));
            }
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if (d == station) {
                var s = station;
                var heroImage = s.type.heroImage;
                var count = drones.Count(d => d.active);
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
                    var sc = new TextScene(prev, t, new() {
                        new() { escape = true,
                            key = 'C', name = "Continue",
                            next = null
                    }}) { background = heroImage };
                    return sc;
                }
                Console Complete() {
                    var sec = (station.world.tick - startTick) / 60;
                    var t =
@$"Benjamin meets you at the docking bay.

""You destroyed the drones in {sec} seconds.""

{(sec < 60 ? @"""I figured you were ready for that.""" : @"""So now you should know how to aim.""")}
".Replace("\r", null);
                    var sc = new TextScene(prev, t, new() {
                        new() { escape = true,
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
                    var sc = new TextScene(prev, t, new() {
                        new() { escape = true,
                            key = 'U', name = "Undock",
                            next = Undock
                    }}) { background = heroImage };
                    return sc;
                }
                Console Undock (Console prev) {
                    story.mainInteractions.Remove(this);
                    story.mainInteractions.Add(new IntroExploration(story, station, playerShip));
                    return null;
                }
            }
            return null;
        }
    }
    class IntroExploration : IPlayerInteraction {
        PlayerStory story;
        Station station;
        HashSet<Station> targets;
        public IntroExploration(PlayerStory story, Station station, PlayerShip playerShip) {
            this.story = story;
            this.station = station;
            var w = station.world;
            targets = new HashSet<Station>(w.entities.all.OfType<Station>().Where(
                s => s.sovereign.IsFriend(playerShip)));
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if (d == station) {
                var s = station;
                var heroImage = s.type.heroImage;
                
                if(targets.IsSubsetOf(playerShip.known)) {
                    
                    return new TextScene(prev,
@$"""You've found all the friendly stations in the system. Now that
you know what services each provide you can return to them when
needed.""",
                    new() {
                        new() { escape = true,
                            key = 'C', name = "Continue",
                            next = Continue
                    }}) { background = heroImage };
                    Console Continue(Console prev) {
                        return new TextScene(prev,
@$"""Now that you're learning to find what you need,
let me give you another goal.""

""Look around the system. Find all the warlords and pirates,
where they're hiding in this system.""

""Then, look out there, far out there. At the edge of the system
is an Errorist compound, where unhinged ""scientists"" commit
inhumane experiments involving radiation.""

""Fight back at the Orion Warlords and Iron Pirates.
And when you've gotten used to fighting warlords and pirates,
go and start destroying Errorists.""",
                        new() {
                            new() { escape = true,
                                key = 'C', name = "Continue",
                                next = Undock
                        }}) { background = heroImage };
                    }
                    Console Undock(Console prev) {
                        story.mainInteractions.Remove(this);
                        story.mainInteractions.Add(new IntroOuterEnemy(story, station, playerShip));
                        return null;
                    }
                } else {
                    int count = targets.Count - targets.Intersect(playerShip.known).Count();

                    if(count > 1) {
                        return new TextScene(prev,
@$"""You've found all but {count} friendly stations in this system.
Use your starship's megamap to look for them.""",
                    new() {
                        new() { escape = true,
                            key = 'U', name = "Undock",
                            next = null
                    }}) { background = heroImage };
                    } else {

                        return new TextScene(prev,
@$"""You've found all but one friendly station in this system.
Use your starship's megamap to look for it.""",
                    new() {
                        new() { escape = true,
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
    class IntroOuterEnemy : IPlayerInteraction {
        PlayerStory story;
        Station station;
        Station target;
        public IntroOuterEnemy(PlayerStory story, Station station, PlayerShip playerShip) {
            this.story = story;
            this.station = station;
            var w = station.world;

            target = new Station(w, w.types.Lookup<StationType>("station_errorist_compound"), XY.Polar(1, 800));
            target.CreateSegments();
            target.CreateGuards();
            w.AddEntity(target);
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if (d != station) {
                return null;
            }
            var s = station;
            var heroImage = s.type.heroImage;

            if (!target.active) {

                var sc = new TextScene(prev,
@$"""Well, congratulations. You've survived my final exam.
That's all the training I have for you. Hopefully now you
have at least a fighting chance when you leave this place.""

""Goodbye.""",
                new() {
                        new() { escape = true,
                            key = 'C', name = "Continue",
                            next = Undock
                    }}) { background = heroImage };
                Console Undock(Console prev) {
                    story.mainInteractions.Remove(this);
                    return null;
                }
                return sc;
            } else {
                return new TextScene(prev,
@$"""Go and destroy the Errorist compound when you're ready.""",
                new() {
                        new() { escape = true,
                            key = 'U', name = "Undock",
                            next = null
                    }}) { background = heroImage };
            }
        }
    }

    class PlayerStory {
        public HashSet<IPlayerInteraction> mainInteractions;
        public HashSet<IPlayerInteraction> secondaryInteractions;
        public HashSet<IPlayerInteraction> completedInteractions;


        public PlayerStory() {
            mainInteractions = new HashSet<IPlayerInteraction>();
            mainInteractions.Add(new IntroMeeting(this));
            secondaryInteractions = new HashSet<IPlayerInteraction>();
            completedInteractions = new HashSet<IPlayerInteraction>();
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            Console sc;
            sc = mainInteractions.Select(m => m.GetScene(prev, d, playerShip)).FirstOrDefault(s => s != null);
            if(sc != null) {
                return sc;
            } else {
                if (d is Station source) {
                    string codename = source.type.codename;


                    Dictionary<string, GetDockScreen> funcMap = new Dictionary<string, GetDockScreen> {
                        {"station_constellation_astra", ConstellationAstra},
                        {"station_constellation_habitat", ConstellationHabitat },
                        {"station_raisu", Raisu },
                        {"station_orion_warlords_camp", OrionWarlordsCamp }
                    };
                    if(funcMap.TryGetValue(codename, out var f)) {
                        return f(prev, source, playerShip);
                    }
                }
            }
            return null;
        }

        delegate Console GetDockScreen(Console prev, Station source, PlayerShip playerShip);
        public Console ConstellationHabitat(Console prev, Station source, PlayerShip playerShip) {
            Console Intro(Console prev) {
                return new TextScene(prev,
@"You are docked at a Constellation Habitat,
a residential station of the United Constellation.",
                    new() {
                        new() {
                            escape = false,
                            key = 'M', name = "Meeting Hall",
                            next = MeetingHall
                        },
                        new() {escape = true,
                            key = 'U', name = "Undock",
                            next = null
                        }
                    });
            }
            Console MeetingHall(Console prev) {
                var mission = mainInteractions.OfType<DestroyTarget>().FirstOrDefault(i => i.source == source);
                if (mission != null) {
                    return mission.GetScene(prev, source, playerShip);
                }
                var target = source.world.entities.all.OfType<Station>().Where(s => s.type.codename == "station_orion_warlords_camp").FirstOrDefault(other => (other.position - source.position).magnitude < 256);

                if (target == null) {
                    return new TextScene(prev,
@"The meeting hall is empty.",
                    new() {
                        new() {escape = true,
                            key = 'L', name = "Leave",
                            next = (s) => prev
                        }
                    });
                } else {
                    mission = mainInteractions.OfType<DestroyTarget>().FirstOrDefault(i => i.target == target);

                    if(mission != null) {

                        return new TextScene(prev,
    @"You aimlessly stand in the center of the empty Meeting Hall.
After 2 minutes, the station master approaches you.

""Hi, uh, we're currently dealing with a particularly annoying
Orion Warlords camp but I've been told that you're going to
destroy it for us. So, uh, thank you and good luck!""
",
                        new() {
                        new() {escape = true,
                            key = 'C', name = "Continue",
                            next = Intro
                        }
                        });
                    }
                    return new TextScene(prev,
@"You aimlessly stand in the center of the Meeting Hall.
After 10 minutes, the station master approaches you.

""Hi, uh, you seem to have a nice gunship. I'm currently
dealing with a nearby Orion Warlords outpost. They keep
sending us a lot of threats. We're not really worried
about being attacked so much as we just want them to
shut up. Even the health inspector is less asinine
than these idiots.""

""I'll pay you 400 cons to shut them up indefinitely.
What do you say?""
",
                    new() {
                        new() {escape = false,
                            key = 'A', name = "Accept",
                            next = Accept
                        }, new() {escape = true,
                            key = 'R', name = "Reject",
                            next = Reject
                        },
                    });
                    Console Accept(Console prev) {
                        return new TextScene(prev,
@"""Okay, thank you! Go destroy them and
then I'll see you back here.""",
                            new() {
                                new() {escape = false,
                                    key = 'U', name = "Undock",
                                    next = Accepted
                                }
                        });
                    }
                    Console Accepted(Console prev) {
                        DestroyTarget mission = null;
                        mission = new DestroyTarget(source, target) { inProgress = InProgress, debrief = Debrief };
                        mainInteractions.Add(mission);
                        return null;
                        Console InProgress(Console prev) {
                            return new TextScene(prev,
@"""Hey, you're going to destroy that station, right?""",
                                new() {
                                    new() {escape = true,
                                        key = 'U', name = "Undock",
                                        next = null
                                    }
                            });
                        }
                        Console Debrief(Console prev) {
                            return new TextScene(prev,
@"""Thank you very much for destroying those warlords for us!
As promised, here's your money - 400 cons""",
                                new() {
                                    new() {escape = false,
                                        key = 'U', name = "Undock",
                                        next = Debriefed
                                    }
                                });
                        }
                        Console Debriefed(Console prev) {
                            playerShip.player.money += 400;
                            mainInteractions.Remove(mission);
                            return null;
                        }
                    }
                    Console Reject(Console prev) {
                        return new TextScene(prev,
@"""Oh man, what the hell is it with you people?
Okay, fine, I'll just find someone else to do it then.""",
                            new() {
                                new() {escape = false,
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
                new() {
                    new() {
                        escape = false,
                        key = 'T', name = "Trade",
                        next = Trade
                    },
                    new() {escape = true,
                        key = 'U', name = "Undock",
                        next = null
                    }
                }) { background = source.type.heroImage };
            }
            Console Trade(Console from) => new TradeScene(from, playerShip, source);
            return Intro();
        }
        public bool raisuLiberated;
        public Console Raisu(Console prev, Station source, PlayerShip playerShip) {
            Console Intro() {
                var nearby = source.world.entities.all
                    .OfType<AIShip>()
                    .Where(s => s.order is PatrolOrbitOrder p 
                             && p.patrolTarget == source);
                if (nearby.Any()) {
                    return new TextScene(prev,
@"You are docked at Raisu station, though
nobody attends to the docking bay right now.",
                       new() {
                           new() {escape = true,
                               key = 'U', name = "Undock",
                               next = null
                           }
                   });
                } else {
                    return new TextScene(prev,
    @"You are docked at Raisu station.",
                        new() {
                            new() {escape = true,
                                key = 'M', name = "Meeting Hall",
                                next = MeetingHall
                            },
                            new() {escape = true,
                                key = 'U', name = "Undock",
                                next = null
                            }
                    });
                    Console MeetingHall(Console prev) {
                        var c = source.world.entities.all
                            .OfType<Station>()
                            .Where(s => s.type.codename == "station_orion_warlords_camp")
                            .Count();
                        if(c > 0) {
                            return new TextScene(prev,
@"The station master glares at you.

""Please get out of here before you screw this up.
We're in the middle of a hostage situation here.
You're gonna get us killed just sticking around here.
So if you really want to be a hero, come back when
you've hit the Orion Warlords where it hurts.
", new() {
                                new() {escape = true,
                                    key = 'C', name = "Continue",
                                    next = null
                                }
                            });
                        }
                        if(raisuLiberated) {
                            return new TextScene(prev,
@"Not much is happening around the station right now.
You feel a sense of relief.", new() {
                                new() {escape = true,
                                    key = 'C', name = "Continue",
                                    next = null
                                }
                            });
                        }
                        var target = playerShip.world.entities.all.OfType<AIShip>()
                                .FirstOrDefault(s => s.shipClass.codename == "ship_william_sulphin");
                        if(target != null) {
                            return new TextScene(prev,
@"The station master waits for you at the entrance.

""Is it true? Have you confronted the Orion Warlords? They have
given us a lifetime of suffering. I have one thing to ask of you. 
Give them eternity.""

""Destroy William Sulphin. And the Orion Warlords will fall.""

The station master brings out a modified Orion Warlords weapon.

""Take these missiles if you have to.""
", new() {
                                new() {escape = true,
                                    key = 'C', name = "Continue",
                                    next = Accept
                                }
                            });
                            Console Accept(Console prev) {
                                playerShip.cargo.Add(new Item(playerShip.world.types.Lookup<ItemType>("itTraitorLongbow")));
                                DestroyTarget mission = null;
                                mission = new DestroyTarget(source, target) { inProgress = InProgress, debrief = Debrief };
                                mainInteractions.Add(mission);
                                return null;
                                Console InProgress(Console prev) {
                                    return new TextScene(prev,
        @"""You made a promise. Destroy William Sulphin.""",
                                        new() {
                                    new() {escape = true,
                                        key = 'U', name = "Undock",
                                        next = null
                                    }
                                    });
                                }
                                Console Debrief(Console prev) {
                                    return new TextScene(prev,
@"""Thank you for destroying William Sulphin.""

""Now the real fight begins""",
                                        new() {
                                            new() {escape = false,
                                                key = 'U', name = "Undock",
                                                next = Debriefed
                                            }
                                        });
                                }
                                Console Debriefed(Console prev) {
                                    raisuLiberated = true;
                                    mainInteractions.Remove(mission);
                                    return null;
                                }
                            }
                        }
                        return new TextScene(prev,
@"Not much is happening around the station right now.
The mood here isn't particularly terrible, but it's
not particularly happy either.", new() {
                                new() {escape = true,
                                    key = 'C', name = "Continue",
                                    next = null
                                }
                        });
                    }
                }
            }
            return Intro();
        }
        public Console OrionWarlordsCamp(Console home, Station source, PlayerShip playerShip) {
            Console Intro(Console prev) {
                return new TextScene(prev,
@"You are docked at an Orion Warlords Camp.
Enemy soldiers glare at you from the windows
of the station.",
                    new() {
                        new() {
                            key='B', name="Break in",
                            next = BreakIn},
                        new() {escape = true,
                            key = 'U', name = "Undock",
                            next = null
                        }
                });
            }
            Console BreakIn(Console prev) {
                if(source.damageSystem.GetHP() < 50) {
                    return new TextScene(prev,
@"You bash down the entry gate with a lot of force.
You make your way to the bridge and destroy the
black box, shutting off the distress signal.

You leave the station in ruins.",
                        new() {
                            new() {
                                escape = true,
                                key = 'C',
                                name = "Continue",
                                next = Done
                            }
                        });
                    Console Done(Console prev) {
                        Wreck wreck = null;
                        var hook = new Container<Station.StationDestroyed>((s, d, w) => {
                            wreck = w;
                        });
                        source.onDestroyed.set.Add(hook);
                        source.Destroy(playerShip);
                        source.onDestroyed.set.Remove(hook);

                        return wreck.GetDockScene(home, playerShip);
                    }
                }

                return new TextScene(prev,
@"The entry gate refuses to budge...",
                    new() {
                        new() {
                            escape = true,
                            key = 'C',
                            name = "Continue",
                            next = Intro
                        }
                    });
            }
            return Intro(home);
        }
    }
    class DestroyTarget : IPlayerInteraction {
        public Station source;
        public SpaceObject target;
        public Func<Console, Console> inProgress, debrief;
        public DestroyTarget(Station source, SpaceObject target) {
            this.source = source;
            this.target = target;
        }
        public Console GetScene(Console prev, Dockable d, PlayerShip playerShip) {
            if(d != source) {
                return null;
            }
            if(target.active) {
                return inProgress(prev);
            } else {
                return debrief(prev);
            }
        }
    }
}

