using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Newtonsoft.Json;
using Con = SadConsole.Console;

namespace RogueFrontier;

public interface IPlayerInteraction {
    Con GetScene(Con prev, Dockable d, PlayerShip playerShip);
}
public class IntroMeeting : IPlayerInteraction {
    PlayerStory story;
    public IntroMeeting(PlayerStory story) {
        this.story = story;
    }
    public Con GetScene(Con prev, Dockable d, PlayerShip playerShip) {
        if (d is Station s && s.type.codename == "station_daughters_outpost") {
            var heroImage = s.type.heroImage;
            /*
            var benedictPortrait = SScene.LoadImage("RogueFrontierContent/BenedictPortrait.asc.cg").Translate(new Point(heroImage.Max(p => p.Key.Item1), 4));
            var outpostLobby = SScene.LoadImage("RogueFrontierContent/DaughtersOutpostDock.asc.cg").Translate(new Point(benedictPortrait.Max(p => p.Key.Item1), 4));
            */
            Con Intro() {
                var t =
@"Docking at the front entrance of the abbey, the great
magenta tower seems to reach into the oblivion above
your head. It looks much more massive from the view of
the platform that juts out the side of the docking ring
The rows of stained glass windows glow warmly with
orange light. Nevertheless, you can't help but think...

You are a complete stranger here.".Replace("\r", null);
                var sc = new Dialog(prev, t, new() {
                    new("Continue", Intro2),
                    new("Leave", Intro2b, NavFlags.ESC)
                }) { background = heroImage };
                return sc;
            }

            Con Intro2(Con from) {

                var t =
@"Walking into the main hall, You see a great monolith of
sparkling crystals and glowing symbols. A low hum echoes
throughout the room. If you stand still, you can hear
some indistinct whispering from somewhere behind.

A stout man stands for reception duty near a wide door.

""Ah, hello. A meeting is in session right now.
You must be new here... May I help you with anything?""
".Replace("\r", null);
                var sc = new Dialog(prev, t, new() {
                    new(@"""I've been hearing a voice...""", Intro3)
                }) { background = heroImage };
                return sc;
            }


            Con Intro2b(Con from) {

                var t =
@"You decide to step away from the station,
much to the possible chagrin of some mysterious
entity and several possibly preferred timelines.".Replace("\r", null);
                var sc = new Dialog(prev, t, new() {
                    new("Undock")
                }) { background = heroImage };
                return sc;
            }

            Con Intro3(Con from) {
                var t =
@"""I've been hearing a voice. It calls itself...""

""The Orator.""

""And I thought you might know something about it.""

""Hmmm, yes, we are quite experienced with The Orator.
Though you are the first guest we've had in a while.
What did you hear?"" The man asked.".Replace("\r", null);
                var sc = new Dialog(prev, t, new() {
                    new(@"""The voice told me...""", Intro4)
                }) { background = heroImage };
                return sc;
            }
            Con Intro4(Con from) {
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
                var sc = new Dialog(prev, t, new() {
                    new("Continue", Intro5)
                }) { background = heroImage };
                return sc;
            }
            Con Intro5(Con from) {
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
                var sc = new Dialog(prev, t, new() {
                    new(@"""It does.""", Intro6, NavFlags.ESC)
                }) { background = heroImage };
                return sc;

            }
            Dialog Intro6(Con from) {
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
                return new(prev, t, new() {
                    new(@"""That is correct.""", Intro7, NavFlags.ESC)
                }) { background = heroImage }; ;
            }
            Dialog Intro7(Con from) {
                string t =
@"""That is correct.""

The man sighs.

""So you understand that this is not the first time that
The Orator has spoken, and told someone to just pack up,
leave, and look for Them somewhere out there?""

""Are you prepared to die?""
";
                t = t.Replace("\r", null);
                return new(prev, t, new() {
                    new(@"""Huh?!?!?!""", Intro8)
                }) { background = heroImage };
            }
            Dialog Intro8(Con from) {
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
                return new Dialog(prev, t, new() {
                    new(@"""I intend to reach the Galactic Core.""", Intro9a),
                    new("...", Intro9b)
                }) { background = heroImage }; ;
            }
            Dialog Intro9a(Con from) {
                story.mainInteractions.Remove(this);
                string t =
@"""Okay, I see you've already made your mind then.
I'll provide you with some combat training to start
your journey. That is all. Let's hope you make it.""";
                return new(prev, t, new() {
                    new("Continue", Intro10)
                }) { background = heroImage }; ;
            }


            Dialog Intro9b(Con from) {
                story.mainInteractions.Remove(this);
                string t =
@"You pause for a moment.";
                t = t.Replace("\r", null);
                return new(prev, t, new() {
                    new('I', @"""I intend to reach the Galactic Core.""", Intro10a),
                    new('C', @"""I intend to destroy the United Constellation.""", Destroy1),
                    new('D', @"""I don't know anymore.""", Intro10c)
                }) { background = heroImage }; ;
            }

            Dialog Intro10a(Con prev) {
                story.mainInteractions.Remove(this);
                string t =
@"""You sound uncertain there.
Do you truly intend to do that?""";
                t = t.Replace("\r", null);
                return new(prev, t, new() {
                    new('I', @"I intend to reach the Galactic Core.", Intro9a),
                    new('.', "...", Intro9b, NavFlags.ESC)
                }) { background = heroImage };
            }


            Dialog Destroy1(Con prev) {
                string t =
@"""I intend to destroy the United Constellation,"" you say.

""What?!?!"" the man says out loud.";
                t = t.Replace("\r", null);
                return new(prev, t, new() {
                    new('I', @"It's simple. I hate them a lot.", Destroy2),
                    new('G', @"It's for their own good.", Destroy2)
                }) { background = heroImage };
            }
            Dialog Destroy2(Con prev) {
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
                return new(prev, t, new() {
                    new("Continue", Destroy3)
                }) { background = heroImage };
            }
            //Placeholder dialogue
            //Should add more complicated stuff later
            Dialog Destroy3(Con prev) {
                string t =
@"""You know what, that sounds like a good idea.
Allow me to join you on your mission.""

""My name is Benjamin, by the way""";
                t = t.Replace("\r", null);
                return new(prev, t, new() {
                    new("Continue", BenjaminJoin)
                }) { background = heroImage };
            }
            Dialog BenjaminJoin(Con prev) {
                story.mainInteractions.Remove(this);

                var w = playerShip.world;
                var wingmateClass = w.types.Lookup<ShipClass>("ship_beowulf");
                var wingmate = new AIShip(new BaseShip(w, wingmateClass, playerShip.sovereign, s.position),
                    new Wingmate(playerShip) { order = new EscortOrder(playerShip, new XY(-5, 0)) }
                    );
                w.AddEntity(wingmate);
                w.AddEffect(new Heading(wingmate));

                playerShip.wingmates.Add(wingmate);

                return null;
            }
            Dialog Intro10c(Con prev) {
                story.mainInteractions.Remove(this);
                string t =
@"""I don't know anymore,"" you say.";
                return new(prev, t, new() {
                    new("Continue")
                }) { background = heroImage };
            }
            Dialog Intro10(Con from) {
                story.mainInteractions.Remove(this);
                string t =
@"""Let's start with some target practice.
I've sent some drones outside the station.
Destroy them as fast as you can""";
                return new(prev, t, new() {
                    new("Start", StartTraining)
                }) { background = heroImage };
            }
            Con StartTraining(Con from) {
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
        foreach (var d in drones) {
            station.world.AddEntity(d);
            station.world.AddEffect(new Heading(d));
        }
    }
    public Con GetScene(Con prev, Dockable d, PlayerShip playerShip) {
        if (d == station) {
            var s = station;
            var heroImage = s.type.heroImage;
            var count = drones.Count(d => d.active);
            if (count > 0) {
                return InProgress();
            } else {
                return Complete();
            }
            Dialog InProgress() {
                var t =
@$"Benjamin meets you at the docking bay.

""There's still {count} drones left.""
".Replace("\r", null);
                return new(prev, t, new() {
                    new("Continue")
                }) { background = heroImage };
            }
            Con Complete() {
                var sec = (station.world.tick - startTick) / 60;
                var t =
@$"Benjamin meets you at the docking bay.

""You destroyed the drones in {sec} seconds.""

{(sec < 60 ?
    @"""I figured you were ready for that.""" :
    @"""So now you should know how to aim.""")}";
                var sc = new Dialog(prev, t, new() {
                    new("Continue", Explore)
                }) { background = heroImage };
                return sc;
            }

            Con Explore(Con prev) {

                var t =
@$"""There are lots of people - friendly or unfriendly - out there.
In a place like this, you'll find plenty of  stations offering trade
and services for money. Some might have jobs that you can take.""

""Others will attack you on sight. Be careful around them.""

""Only time will tell who is who.""

""Take a look around this system and find out who can help you.""";
                var sc = new Dialog(prev, t, new() {
                    new("Undock", Undock)
                }) { background = heroImage };
                return sc;
            }
            Con Undock(Con prev) {
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
    public Con GetScene(Con prev, Dockable d, PlayerShip playerShip) {
        var s = station;
        if (d != s) return null;

        var heroImage = s.type.heroImage;
        int c = targets.Count - targets.Intersect(playerShip.known).Count();
        if (c > 0) {
            return new Dialog(prev,
@$"""You've found all but {c} friendly station{(c > 1 ? "s" : "")} in this system.
Use your starship's megamap to look for them.""",
                new() {
                    new("Undock")
                }) { background = heroImage };
        } else {

            return new Dialog(prev,
@$"""You've found all the friendly stations in the system. Now that
you know what services each provide you can return to them when
needed.""",
            new() {
                new("Continue", Continue)
            }) { background = heroImage };
            Con Continue(Con prev) {
                return new Dialog(prev,
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
                    new("Continue", Undock)
                }) { background = heroImage };
            }
            Con Undock(Con prev) {
                story.mainInteractions.Remove(this);
                story.mainInteractions.Add(new IntroOuterEnemy(story, station, playerShip));
                return null;
            }
        }
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
    public Con GetScene(Con prev, Dockable d, PlayerShip playerShip) {
        if (d != station) {
            return null;
        }
        var s = station;
        var heroImage = s.type.heroImage;

        if (target.active) {

            return new Dialog(prev,
@$"""Go and destroy the Errorist compound when you're ready.""",
                new() { new("Undock") }) { background = heroImage };
        } else {
            var sc = new Dialog(prev,
@$"""Well, congratulations. You've survived my final exam.
That's all the training I have for you. Hopefully now you
have at least a fighting chance when you leave this place.""

""Goodbye.""",
                new() { new("Continue", Undock) }) { background = heroImage };
            Con Undock(Con prev) {
                story.mainInteractions.Remove(this);
                return null;
            }
            return sc;
        }
    }
}
public class PlayerStory {
    public HashSet<IPlayerInteraction> mainInteractions;
    public HashSet<IPlayerInteraction> secondaryInteractions;
    public HashSet<IPlayerInteraction> completedInteractions;


    public PlayerStory() {
        mainInteractions = new HashSet<IPlayerInteraction>();
        mainInteractions.Add(new IntroMeeting(this));
        secondaryInteractions = new HashSet<IPlayerInteraction>();
        completedInteractions = new HashSet<IPlayerInteraction>();
    }
    delegate Con GetDockScreen(Con prev, Station source, PlayerShip playerShip);
    public Con GetScene(Con prev, Dockable d, PlayerShip playerShip) {
        Con sc;
        sc = mainInteractions.Select(m => m.GetScene(prev, d, playerShip)).FirstOrDefault(s => s != null);
        if (sc != null) {
            return sc;
        } else {
            if (d is Station source) {
                string codename = source.type.codename;
                Dictionary<string, GetDockScreen> funcMap = new Dictionary<string, GetDockScreen> {
                        {"station_constellation_astra", ConstellationAstra},
                        {"station_constellation_habitat", ConstellationHabitat },
                        {"station_armor_shop", TradeStation },
                        {"station_arms_dealer", TradeStation },
                        {"station_raisu", Raisu },
                        {"station_orion_warlords_camp", OrionWarlordsCamp }
                    };
                if (funcMap.TryGetValue(codename, out var f)) {
                    return f(prev, source, playerShip);
                }
            }
        }
        return null;
    }
    public Con TradeStation(Con prev, Station source, PlayerShip playerShip) {
        return new TradeScene(prev, null, playerShip, source);
    }
    public Con ConstellationArrest(Con prev, Station source, PlayerShip playerShip, ICrime c) {
        return new Dialog(prev,
@"Constellation armed soldiers approach your ship
as you dock.",
            new() {
                new("Continue docking", Arrest),
                new("Undock", Undock)
            });
        Con Arrest(Con prev) {
            return new Dialog(prev,
@$"""You are under immediate arrest for {c.name}.""

There will be no trial.",
            new() { new("Continue", Continue) }
            );
        }
        Con Undock(Con prev) {
            source.guards.ForEach(s => (s.behavior.GetOrder() as GuardOrder)?.Attack(playerShip, 900));
            return null;
        }
        Con Continue(Con prev) {
            playerShip.Destroy(source);
            return null;
        }
    }
    public IEnumerable<ICrime> GetConstellationCrimes(Station source, PlayerShip p) {
        return p.crimeRecord.Where(c => c is Destruction d
            && object.ReferenceEquals(d.station.sovereign, source.sovereign)
            && !d.resolved);
    }
    public Con ConstellationAstra(Con prev, Station source, PlayerShip playerShip) {
        var c = GetConstellationCrimes(source, playerShip);
        if (c.Any()) return ConstellationArrest(prev, source, playerShip, c.First());

        return Intro();
        Dialog Intro() {
            return new(prev,
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
                new("Trade", Trade),
                new("Undock")
            }) { background = source.type.heroImage };
        }
        Con Trade(Con from) => new TradeScene(from, playerShip, source);

    }
    public Con ConstellationHabitat(Con prev, Station source, PlayerShip playerShip) {
        var c = GetConstellationCrimes(source, playerShip);
        if (c.Any()) return ConstellationArrest(prev, source, playerShip, c.First());

        return Intro(prev);
        Con Intro(Con prev) {
            return new Dialog(prev,
@"You are docked at a Constellation Habitat,
a residential station of the United Constellation.",
                new() {
                    new("Meeting Hall", MeetingHall),
                    new("Undock")
                });
        }
        Con MeetingHall(Con prev) {
            var mission = mainInteractions.OfType<DestroyTarget>().FirstOrDefault(i => i.source == source);
            if (mission != null) {
                return mission.GetScene(prev, source, playerShip);
            }
            var target = source.world.entities.all.OfType<Station>().FirstOrDefault(other =>
                other.type.codename == "station_orion_warlords_camp"
                && (other.position - source.position).magnitude < 256
            );
            if (target == null) {
                return new Dialog(prev,
@"The meeting hall is empty.",
                new() {
                    new("Leave", Intro)
                });
            }
            if (mainInteractions.OfType<DestroyTarget>().Any(i => i.targets.Contains(target))) {
                return new Dialog(prev,
@"You aimlessly stand in the center of the empty Meeting Hall.
After 2 minutes, the station master approaches you.

""Hi, uh, we're currently dealing with a particularly annoying
Orion Warlords camp but I've been told that you're going to
destroy it for us. So, uh, thank you and good luck!""",
                new() {
                    new("Continue", Intro)
                });
            }
            return new Dialog(prev,
@"You aimlessly stand in the center of the Meeting Hall.
After 10 minutes, the station master approaches you.

""Hi, uh, you seem to have a nice gunship. I'm currently
dealing with a nearby Orion Warlords outpost. They keep
sending us a lot of threats. We're not really worried
about being attacked so much as we just want them to
shut up. Even the health inspector is less asinine
than these idiots.""

""I'll pay you 400 cons to shut them up indefinitely.
What do you say?""",
            new() {
                new("Accept", Accept),
                new("Decline", Reject)
            });
            Con Accept(Con prev) {
                return new Dialog(prev,
@"""Okay, thank you! Go destroy them and
then I'll see you back here.""",
                    new() {
                        new("Undock", Accepted)
                    });
            }
            Dialog Accepted(Con prev) {
                DestroyTarget mission = null;
                mission = new DestroyTarget(playerShip, source, target) { inProgress = InProgress, debrief = Debrief };
                mainInteractions.Add(mission);
                return null;
                Dialog InProgress(Con prev) {
                    return new(prev,
@"""Hey, you're going to destroy that station, right?""",
                        new() {
                            new("Undock")
                        });
                }
                Dialog Debrief(Con prev) {
                    return new(prev,
@"""Thank you very much for destroying those warlords for us!
As promised, here's your money - 400 cons""",
                        new() {
                            new("Undock", Debriefed)
                        });
                }
                Dialog Debriefed(Con prev) {
                    playerShip.player.money += 400;
                    mainInteractions.Remove(mission);
                    //completedInteractions.Add(mission);
                    return null;
                }
            }
            Dialog Reject(Con prev) {
                return new(prev,
@"""Oh man, what the hell is it with you people?
Okay, fine, I'll just find someone else to do it then.""",
                    new() {
                        new("Undock")
                    });
            }
        }
    }
    public bool raisuLiberated;
    public Con Raisu(Con prev, Station source, PlayerShip playerShip) {
        Dialog Intro() {
            var nearby = source.world.entities.all
                .OfType<AIShip>()
                .Where(s => s.behavior is BaseShipBehavior b
                         && b.current is PatrolOrbitOrder p
                         && p.patrolTarget == source);
            if (nearby.Any()) {
                return new(prev,
@"You are docked at Raisu station, though
nobody attends to the docking bay right now.",
                new() {
                    new("Undock")
                });
            }

            return new(prev,
@"You are docked at Raisu station.",
                new() {
                    new("Meeting Hall", MeetingHall),
                    new("Undock")
                });
            Dialog MeetingHall(Con prev) {
                var c = source.world.entities.all
                    .OfType<Station>()
                    .Where(s => s.type.codename == "station_orion_warlords_camp")
                    .Count();
                if (c > 0) {
                    return new(prev,
@"The station master glares at you.

""Please get out of here before you get us killed!""", new() {
new("Continue")
});
                }
                if (raisuLiberated) {
                    return new(prev,
@"Not much is happening around the station right now.
You feel a sense of relief.", new() {
new("Continue")
});
                }
                var target = playerShip.world.entities.all.OfType<AIShip>()
                        .FirstOrDefault(s => s.shipClass.codename == "ship_william_sulphin");
                if (target != null) {
                    return new(prev,
@"The station master waits for you at the entrance.

""Is it true? Have you confronted the Orion Warlords? They have
given us a lifetime of suffering. I have one thing to ask of you. 
Give them eternity.""

""Destroy William Sulphin. Let the Orion Warlords fall.""

The station master brings out a modified warlord weapon.

""Take these missiles if you have to.""",
                    new() {
                        new("Accept", Accept)
                    });
                    Dialog Accept(Con prev) {
                        playerShip.cargo.Add(new Item(playerShip.world.types.Lookup<ItemType>("itTraitorLongbow")));
                        DestroyTarget mission = null;
                        mission = new DestroyTarget(playerShip, source, target) { inProgress = InProgress, debrief = Debrief };
                        target.ship.onDestroyed += mission;
                        mainInteractions.Add(mission);
                        return null;
                        Dialog InProgress(Con prev) {
                            return new(prev,
@"""You made a promise. Destroy William Sulphin.""",
                                new() {
                                    new("Undock")
                                });
                        }
                        Dialog Debrief(Con prev) {
                            return new(prev,
@"""Thank you for destroying William Sulphin.""

""Now the real fight begins""",
                                new() {
                                    new("Undock", Debriefed)
                                });
                        }
                        Dialog Debriefed(Con prev) {
                            raisuLiberated = true;
                            mainInteractions.Remove(mission);
                            return null;
                        }
                    }
                }
                return new(prev,
@"Not much is happening around the station right now.", new() {
new("Continue")
});
            }
        }
        return Intro();
    }
    public Con OrionWarlordsCamp(Con home, Station source, PlayerShip playerShip) {
        Dialog Intro(Con prev) {

            return new(prev,
source.damageSystem.GetHP() >= 50 ?
@"You are docked at an Orion Warlords Camp.
Enemy soldiers glare at you from the windows
of the station." :
@"You are docked at an Orion Warlords Camp.
Your ship identifies a distress signal
originating from this station.",
                new() {
                    new("Break In", BreakIn),
                    new("Undock")
                });
        }
        Dialog BreakIn(Con prev) {
            if (source.damageSystem.GetHP() < 50) {

                Wreck wreck = null;
                Container<Station.Destroyed> hook = new((s, d, w) => wreck = w);
                source.onDestroyed.set.Add(hook);
                source.Destroy(playerShip);
                source.onDestroyed.set.Remove(hook);

                return new(prev,
@"You break down the entry gate with your primary weapon.
You make your way to the bridge and destroy the
black box, shutting off the distress signal.

You leave the station in ruins.",
                    new() {
                        new("Scavenge", Scavenge),
                        new("Undock")
                    });
                Con Scavenge(Con prev) => wreck.GetDockScene(home, playerShip);
            }
            return new(prev,
@"The entry gate refuses to budge...",
                new() {
                    new("Continue", Intro)
                });
        }
        return Intro(home);
    }
}
class DestroyTarget : IPlayerInteraction, IContainer<BaseShip.Destroyed>, IContainer<Station.Destroyed> {
    public PlayerShip attacker;
    public Station source;
    public HashSet<SpaceObject> targets;
    public bool complete => targets.Count == 0;
    [JsonIgnore]
    public Func<Con, Con> inProgress, debrief;
    public DestroyTarget(PlayerShip attacker, Station source, params SpaceObject[] targets) {
        this.attacker = attacker;
        this.source = source;
        this.targets = new(targets);
        foreach (var t in targets) {
            switch (t) {
                case AIShip s:
                    s.ship.onDestroyed += this;
                    break;
                case Station s:
                    s.onDestroyed += this;
                    break;
            }
        }
    }

    BaseShip.Destroyed IContainer<BaseShip.Destroyed>.Value => (s, d, w) => {
        if (targets.Remove(s) && targets.Count == 0) {
            attacker.AddMessage(new Message("Mission complete!"));
            s.onDestroyed -= this;
        }
    };

    Station.Destroyed IContainer<Station.Destroyed>.Value => (s, d, w) => {
        if (targets.Remove(s) && targets.Count == 0) {
            attacker.AddMessage(new Message("Mission complete!"));
            s.onDestroyed -= this;
        }
    };

    public Con GetScene(Con prev, Dockable d, PlayerShip playerShip) {
        if (d != source) {
            return null;
        }
        if (complete) {
            return debrief(prev);
        }

        return inProgress(prev);
    }
}

