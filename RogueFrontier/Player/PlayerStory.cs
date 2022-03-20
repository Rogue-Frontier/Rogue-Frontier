using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Newtonsoft.Json;
using Con = SadConsole.ScreenSurface;

namespace RogueFrontier;

public interface IPlayerInteraction {
    Con GetScene(Con prev, PlayerShip playerShip, IDockable d);
}
public class IntroMeeting : IPlayerInteraction {
    PlayerStory story;
    public IntroMeeting(PlayerStory story) {
        this.story = story;
    }
    public Con GetScene(Con prev, PlayerShip playerShip, IDockable d) {
        var t = (d as Station)?.type;
        if(t?.codename != "station_daughters_outpost")
            return null;
        var heroImage = t.heroImage;
        return Intro();
        Dialog Intro() {
            var t =
@"Docking at the front entrance of the abbey, the great
magenta tower seems to reach into the oblivion above
your head. It looks much more massive from the view of
the platform that juts out the side of the docking ring
The rows of stained glass windows glow warmly with
orange light. Nevertheless, you can't help but think...

You are a complete stranger here.";
            var sc = new Dialog(prev, t, new() {
                new("Continue", Intro2),
                new("Leave", Intro2b, NavFlags.ESC)
            }) { background = heroImage };
            return sc;
        }
        Dialog Intro2(Con from) {
            var t =
@"Walking into the main hall, You see a great monolith of
sparkling crystals and glowing symbols. A low hum reflectes
throughout the room. If you stand still, you can hear
some indistinct whispering from somewhere behind.

A stout man stands for reception duty near a wide door.

""Ah, hello. A meeting is in session right now.
You must be new here... May I help you with anything?""";
            var sc = new Dialog(prev, t, new() {
                new(@"""I heard a voice...""", Intro3)
            }) { background = heroImage };
            return sc;
        }
        Dialog Intro2b(Con from) {
            var t =
@"You decide to step away from the station,
much to the possible chagrin of some mysterious
entity and several possibly preferred timelines.".Replace("\r", null);
            var sc = new Dialog(prev, t, new() {
                new("Undock")
            }) { background = heroImage };
            return sc;
        }
        Dialog Intro3(Con from) {
            var t =
@"""I heard a voice.
It calls itself...The Orator.
And I thought you might know
something about it,"" you say.

""...Yes, we are quite experienced with The Orator.
You are the first guest we've had in a while.
What did you hear?"" The man replied.";
            var sc = new Dialog(prev, t, new() {
                new(@"""The Orator told me...""", Intro4)
            }) { background = heroImage };
            return sc;
        }
        Dialog Intro4(Con from) {
            string t =
@"""The Orator told me...
that there is something terribly wrong
happening to us. All of us. Humanity.

Forces of conflict are emanating from the Celestial Center
and causing extremely deadly wars throughout our civilization.
A voice known as The Dictator seeks to control us in horrible ways.

I asked The Orator about what The Dictator's intentions were, and then
I heard a droning voice begin to speak loudly over The Orator, slowly
raising itself into a dreadful yell. My senses began falling apart.
One voice shouted ""SILENCE"" at the other Voice and both went quiet,
then The Orator spoke a final message.

The Orator told me, that They had an answer. And that
if I went to Them, taking a journey to the Celestial Center,
and I listened to Their words, and I wielded Their powers,
then They would bring forth an ultimate peace.

And...""

Wait, how are you saying all of this- Your mind blanks out.

""...And I... I witnessed all of this in a strange dream I had.""";
            var sc = new Dialog(prev, t, new() {
                new("Continue", Intro5)
            }) { background = heroImage };
            return sc;
        }
        Dialog Intro5(Con from) {
            string t =
@"The man replies, ""...I understand. That reminds me of
my own first encounter with The Orator.""

""In that dream, I could see everything - horrible conflicts
in distant star systems I could never even fathom visiting.
But I was terribly fearful of the Dictator - so fearful that
I vowed to see the Celestial Center and personally destroy
everything that I could find of The Dictator myself.""

""But now I know of older followers who discovered that
leaving for the Celestial Center was not the only answer.""

""The old survivors built this place to provide a shelter
for those who seek a different kind of answer - one that
values peace from within. Our bond with The Orator grants
us safety from The Dictator. I welcome you to reside here.""

""Unless, your answer rests..."" he points to a distant star
shining through the window, ""...far out there.""

""Does it?""";
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
around here... Not since the last war ended.

You really intend to see what's out there.""";
            return new(prev, t, new() {
                new(@"""That is correct.""", Intro7, NavFlags.ESC)
            }) { background = heroImage }; ;
        }
        Dialog Intro7(Con from) {
            string t =
@"He looks at you with doubt.

""So you understand that... this? This is not
the first time that The Orator has spoken,
and told someone to just pack up, leave,
and look for Them somewhere out there?

I don't care how ready you think you are
to go wherever you think you're going.

Are you prepared to die?""";
            return new(prev, t, new() {
                new(@"""Huh?!?!?!""", Intro8)
            }) { background = heroImage };
        }
        Dialog Intro8(Con from) {
            string t =
@"He looks somewhat tense.

""The Orator definitely calls to people. We know that this
happens occasionally but predictably. We see a new person
come in for their first time, and ask us about The Orator,
and, and,

It's only a matter of days until they leave this place
for the last time...

...And we never see that person again.

Until they show up in a news report in which
someone identifies them as an unwitting traveler
who got blown up in the middle of a war zone...

You'd have better chances of surviving if you joined
the Constellation Fleet. Not much better, mind you.
At least I got out when they were about to send us
through the gateway... before they'd shut the door
and lock it behind us.

So, tell me, what is it that you intend to do?""";
            return new Dialog(prev, t, new() {
                new(@"""I intend to reach the Celestial Center.""", Intro9a),
                new("...", Intro9b)
            }) { background = heroImage };
        }
        Dialog Intro9a(Con from) {
            story.mainInteractions.Remove(this);
            string t =
@"""So you do. Okay. Alright. I won't try to change
your mind.""

The man sighs and stares at the ground for a second.

""The Matriarch who runs this place said that we
need to give more training to those who decide to
seek the Celestial Center. I won't tell you what
to do, but here are the basics.""

The man takes out a script and reads from it.";
            return new(prev, t, new() {
                new("Continue", Intro11)
            }) { background = heroImage };
        }
        Dialog Intro9b(Con from) {
            story.mainInteractions.Remove(this);
            string t =
@"You pause for a moment.";
            t = t.Replace("\r", null);
            return new(prev, t, new() {
                new('I', @"""I intend to reach the Celestial Center.""", Intro10a),
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
            }) { background = heroImage };
        }
        Dialog Intro11(Con prev) {
            story.mainInteractions.Remove(this);
            string t =
@"""You will meet many different friends and foes
during your journey. Especially on the frontier,
you might be surprised at the kind of people that
you meet. And there are some who will want to
destroy you.

And with your starship, you may choose how much
trouble you want to involve yourself in. We, the
Daughters of the Orator, do not have any particular
opinion on how you ought to conduct yourself, but
we advise you to do only what feels right to you.

Please, please, do not abuse whatever magical powers
The Orator has granted you. We, the Daughters of the
Orator, have seen enough of that happen.

Take note of your complete surroundings as well as
yourself and your starship. Be sure to maintain your
ship's hull system, energy system, and weapon system
regularly to ensure your survival.""";
            t = t.Replace("\r", null);
            return new(prev, t, new() {
                new('C', @"Continue", Intro11),
            }) { background = heroImage };
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
        var shipClass = w.types.Lookup<ShipClass>("ship_laser_drone");
        var sovereign = Sovereign.SelfOnly;
        this.drones = new AIShip[3];
        var k = station.world.karma;
        for (int i = 0; i < 3; i++) {
            var d = new AIShip(new(w, shipClass, station.position + XY.Polar(k.NextDouble() * 2 * Math.PI, k.NextDouble() * 25 + 25)),
                sovereign, new SnipeOrder(player));
            drones[i] = d;
        }
    }
    public void AddDrones() {
        foreach (var d in drones) {
            station.world.AddEntity(d);
            station.world.AddEffect(new Heading(d));
        }
    }
    public Con GetScene(Con prev, PlayerShip playerShip, IDockable d) {
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
    public Con GetScene(Con prev, PlayerShip playerShip, IDockable d) {
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
    public Con GetScene(Con prev, PlayerShip playerShip, IDockable d) {
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

    Dictionary<ItemType, int> stdPrice;

    public int GetStdPrice(Item i) => stdPrice[i.type];
    public PlayerStory(PlayerShip playerShip) {
        var i = playerShip.world.types.GetDict<ItemType>();
        stdPrice = new Dictionary<string, int>() {
            { "item_amethyst_laser_i", 5000 },
            { "item_amethyst_laser_ii", 12000 },
            { "item_shimmer_shield_i", 400 },
            { "item_gemsteel_plate_i", 1500 },
            { "item_gemsteel_plate_ii", 4000 },
            { "item_radiant_plate", 0 },
            { "item_sand_cannon", 0 },
            { "item_sludge_cannon", 0 },
            { "item_iron_driver", 0 },
            { "item_iron_cannon", 0 },
            { "item_ironclad_plate", 0 },
            { "item_ironside_plate", 0 },
            { "item_grinder", 0 },
            { "item_deflect_device", 0 },
            { "item_lightning_cannon", 0 },
            { "item_orion_bolter", 400 },
            { "item_orion_longbow", 800 },
            { "item_traitor_longbow", 1200 },
            { "item_orion_skewer", 2000 },
            { "item_hunterscale_plate", 250 },
            { "item_skullhelm_plate", 600 },
            { "item_dark_cannon", 0 },
            { "item_thorn_missile", 0 },
            { "item_thorn_missile_system", 0 },
            { "item_simple_fuel_rod", 200 },
            { "item_armor_repair_patch", 500 },
            { "item_orator_charm_silence", 3000 },
            { "item_dictator_charm_silence", 3000 },
            { "item_emp_cannon", 0 },
            { "item_laser_pointer", 0 },
            { "item_beowulf_dual_laser_cannon", 3000 },
            { "item_beowulf_dual_laser_repeater", 3000 },
            { "item_beowulf_dual_laser_upgrade", 3000 },
            { "item_buckler_shield", 400 },
            { "item_klaw_missile", 8 },
            { "item_klaw_missile_launcher", 400 },
            { "item_musket_cannon", 3500 },
            { "item_missile_defender", 4000 },
            { "item_laser_drone", 1500 },
            { "item_flintlock", 1500 },
            { "item_sabre", 2500 },
            { "item_knightsteel_plate", 5000 },
            { "item_bumpersteel_plate", 4500 },
            { "item_dynamite_charge", 12 },
            { "item_dynamite_cannon", 2000 },
            { "item_amethyst_warranty_card", 200 },
            { "item_shield_bash", 3000 },
            { "item_20mw_generator", 0 },
            { "item_10mw_storage_battery", 0 },
            { "item_solar_panel", 0 },
        }.ToDictionary(pair => i[pair.Key], pair => pair.Value);

        mainInteractions = new HashSet<IPlayerInteraction>();
        mainInteractions.Add(new IntroMeeting(this));
        secondaryInteractions = new HashSet<IPlayerInteraction>();
        completedInteractions = new HashSet<IPlayerInteraction>();
    }
    delegate Con GetDockScreen(Con prev, PlayerShip playerShip, Station source);
    public Con GetScene(Con prev, PlayerShip playerShip, IDockable d) {
        if (mainInteractions.Select(m => m.GetScene(prev, playerShip, d)).FirstOrDefault(s => s != null) is Con c) {
            return c;
        }
        if (d is Station source) {
            GetDockScreen f = source.type.codename switch {
                "station_amethyst_store" => AmethystStore,
                "station_beowulf_club" => BeowulfClub,
                "station_camper_outpost" => CamperOutpost,
                "station_constellation_astra" => ConstellationAstra,
                "station_constellation_habitat" => ConstellationVillage,
                "station_armor_shop" => ArmorDealer,
                "station_arms_dealer" => ArmsDealer,
                "station_orion_warlords_camp" => OrionWarlordsCamp,
                _ => null
            };
            if (f != null) {
                return f(prev, playerShip, source);
            }
        }
        return null;
    }
    public Con AmethystStore(Con prev, PlayerShip playerShip, Station source) {
        var c = GetConstellationCrimes(playerShip, source);
        if (c.Any()) return ConstellationArrest(prev, playerShip, source, c.First());
        var discount = playerShip.cargo.Any(i => i.type.codename == "item_amethyst_warranty_card");
        var buyAdj = discount ? 0.8 : 1;
        return Intro();
        Dialog Intro() {
            return new(prev,
@"You are docked at The Amethyst Store,
one of several commercial stations
established by Amethyst, Inc to serve
all your Amethyst-related needs,
including but not limited to, product
purchases and repair services.",
            new() {
                new("Trade", Trade),
                new("Install Device", DeviceInstall),
                new("Remove Device", DeviceRemoval),
                new("Repair Armor", ArmorRepair),
                new("Replace Armor", ArmorReplace),
                new("Undock")
            });
        }
        int GetRepairPrice(Armor a) =>
            !a.source.type.attributes.Contains("Amethyst") ? -1 :
            discount ? 1 :
            3;
        Con Trade(Con from) => new TradeMenu(from, playerShip, source,
            i => (int)(GetStdPrice(i) * buyAdj),
            i => i.type.attributes.Contains("Amethyst") ? GetStdPrice(i) / 10 : -1);
        Con ArmorRepair(Con from) => SListScreen.ArmorRepairService(from, playerShip, GetRepairPrice, null);
        Con DeviceInstall(Con from) => SListScreen.DeviceInstallService(from, playerShip, GetInstallPrice, null);
        Con DeviceRemoval(Con from) => SListScreen.DeviceRemovalService(from, playerShip, GetRemovePrice, null);
        Con ArmorReplace(Con from) => SListScreen.ReplaceArmorService(from, playerShip, GetReplacePrice, null);

        int GetInstallPrice(Item i) =>
            !i.type.attributes.Contains("Amethyst") ? -1 : discount ? 80 : 100;
        int GetRemovePrice(Device i) =>
            !i.source.type.attributes.Contains("Amethyst") ? -1 : discount ? 80 : 100;
        int GetReplacePrice(Device i) =>
            !i.source.type.attributes.Contains("Amethyst") ? -1 : discount ? 80 : 100;
    }


    public Con BeowulfClub(Con prev, PlayerShip playerShip, Station source) {
        if (!playerShip.shipClass.attributes.Contains("BeowulfClub")) {
            return new Dialog(prev,
@"You are docked at an independent chapter
of the Beowulf Club, a galaxy-wide organization
serving civilian gunship pilots.

A heavily armored stationhand calls out to you.
""Hey! We only serve *gunships* around here!""", new() { new("Undock immediately") });
        }
        return Intro();
        Dialog Intro() {
            return new(prev,
@"You are docked at an independent chapter
of the Beowulf Club, a galaxy-wide organization
serving civilian gunship pilots.",
            new() {
                new("Trade", Trade),
                new("Repair Armor", ArmorServices),
                new("Undock")
            });
        }
        int GetPrice(Armor a) {
            if (a.source.type.attributes.Contains("Amethyst")) {
                return 6;
            }

            return 3;
        }
        Con Trade(Con from) => new TradeMenu(from, playerShip, source,
            i => GetStdPrice(i),
            i => GetStdPrice(i));
        Con ArmorServices(Con from) => SListScreen.ArmorRepairService(from, playerShip, GetPrice, null);
    }

    public Con CamperOutpost(Con prev, PlayerShip playerShip, Station source) {
        return Intro();
        Dialog Intro() {
            return new(prev,
@"You are docked at a Campers Outpost,
an independent enclave of tinkers,
craftspersons, and adventurers.",
            new() {
                new("Trade", Trade),
                new("Repair Armor", ArmorServices),
                new("Undock")
            });
        }
        int GetRepairPrice(Armor a) => a.source.type.attributes.Contains("Amethyst") ? 4 : 2;
        Con Trade(Con from) => new TradeMenu(from, playerShip, source,
            i => GetStdPrice(i),
            i => GetStdPrice(i));
        Con ArmorServices(Con from) => SListScreen.ArmorRepairService(from, playerShip, GetRepairPrice, null);
    }
    public TradeMenu TradeStation(Con prev, PlayerShip playerShip, Station source) =>
        new (prev, playerShip, source, GetStdPrice, i => GetStdPrice(i) / 2);
    public Con ConstellationArrest(Con prev, PlayerShip playerShip, Station source, ICrime c) {
        return new Dialog(prev,
@"Constellation armed soldiers approach your ship
as you dock.",
            new() {
                new("Continue docking", Arrest),
                new("Cancel", Cancel)
            });
        Con Arrest(Con prev) {
            return new Dialog(prev,
@$"""You are under immediate arrest for
{c.name}.""

There will be no trial.",
            new() { new("Continue", Surrender) }
            );
        }
        Con Cancel(Con prev) {
            source.guards.ForEach(s => (s.behavior.GetOrder() as GuardOrder)?.SetAttack(playerShip, 900));
            return null;
        }
        Con Surrender(Con prev) {
            playerShip.Destroy(source);
            return null;
        }
    }
    public IEnumerable<ICrime> GetConstellationCrimes(PlayerShip p, Station source) {
        return p.crimeRecord.Where(c => c is DestructionCrime d
            && ReferenceEquals(d.destroyed.sovereign, source.sovereign)
            && !d.resolved);
    }
    public Con ArmorDealer(Con prev, PlayerShip playerShip, Station source) {
        var c = GetConstellationCrimes(playerShip, source);
        if (c.Any()) return ConstellationArrest(prev, playerShip, source, c.First());
        return new Dialog(prev,
@"You are docked at an armor shop station.", new() {
            new("Armor", Trade),
            new("Undock")
        }) { background = source.type.heroImage };
        TradeMenu Trade(Con c) => new(c, playerShip, source, GetStdPrice, i => (int)(i.type.armor != null ? 0.8 * GetStdPrice(i) : -1));
    }
    public Con ArmsDealer(Con prev, PlayerShip playerShip, Station source) {
        var c = GetConstellationCrimes(playerShip, source);
        if (c.Any()) return ConstellationArrest(prev, playerShip, source, c.First());
        return new Dialog(prev,
@"You are docked at an arms dealer station", new() {
            new("Weapons", Trade),
            new("Undock")
        }) { background = source.type.heroImage };
        TradeMenu Trade(Con c) => new(c, playerShip, source, GetStdPrice, i => (int)(i.type.weapon != null ? 0.8 * GetStdPrice(i) : -1));
    }
    public Con ConstellationAstra(Con prev, PlayerShip playerShip, Station source) {
        var c = GetConstellationCrimes(playerShip, source);
        if (c.Any()) return ConstellationArrest(prev, playerShip, source, c.First());

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
                new("Install Devices", DeviceInstall),
                new("Remove Devices", DeviceRemoval),
                new("Repair Armor", ArmorRepair),
                new("Replace Armor", ArmorReplace),
                new("Undock")
            }) { background = source.type.heroImage };
        }
        Con Trade(Con from) => TradeStation(from, playerShip, source);

        Con ArmorRepair(Con from) => SListScreen.ArmorRepairService(from, playerShip, GetRepairPrice, null);
        Con DeviceInstall(Con from) => SListScreen.DeviceInstallService(from, playerShip, GetInstallPrice, null);
        Con DeviceRemoval(Con from) => SListScreen.DeviceRemovalService(from, playerShip, GetRemovePrice, null);
        Con ArmorReplace(Con from) => SListScreen.ReplaceArmorService(from, playerShip, GetReplacePrice, null);

        int GetRepairPrice(Armor a) =>
            a.source.type.attributes.Contains("Amethyst") ? 9 : 3;
        int GetInstallPrice(Item i) =>
            i.type.attributes.Contains("Amethyst") ? 300 : 100;
        int GetRemovePrice(Device i) =>
            i.source.type.attributes.Contains("Amethyst") ? 300 : 100;
        int GetReplacePrice(Device i) =>
            i.source.type.attributes.Contains("Amethyst") ? 300 : 100;
    }
    public Con ConstellationVillage(Con prev, PlayerShip playerShip, Station source) {
        var c = GetConstellationCrimes(playerShip, source);
        if (c.Any()) return ConstellationArrest(prev, playerShip, source, c.First());

        return Intro(prev);
        Con Intro(Con prev) {
            return new Dialog(prev,
@"You are docked at a Constellation Village,
a residential station assembled out of 
shipping containers and various spare parts.",
                new() {
                    new("Meeting Hall", MeetingHall),
                    new("Undock")
                });
        }
        Con MeetingHall(Con prev) {
            var mission = mainInteractions.OfType<DestroyTarget>().FirstOrDefault(i => i.source == source);
            if (mission != null) {
                return mission.GetScene(prev, playerShip, source);
            }
            var target = source.world.entities.all.OfType<Station>().FirstOrDefault(s =>
                s.type.codename is "station_orion_warlords_camp"
                && (s.position - source.position).magnitude < 256
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
                    playerShip.person.money += 400;
                    mainInteractions.Remove(mission);
                    //completedInteractions.Add(mission);
                    return null;
                }
            }
            Dialog Reject(Con prev) {
                return new(prev,
@"""Oh man, what the hell is it with you people?
Okay, fine, I'll just find someone else to do it.""",
                    new() {
                        new("Undock")
                    });
            }
        }
    }
    public Con OrionWarlordsCamp(Con home, PlayerShip playerShip, Station source) {
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
class DestroyTarget : IPlayerInteraction, IContainer<AIShip.Destroyed>, IContainer<Station.Destroyed> {
    public PlayerShip attacker;
    public Station source;
    public HashSet<ActiveObject> targets;
    public bool complete => targets.Count == 0;
    [JsonIgnore]
    public Func<Con, Con> inProgress, debrief;
    public DestroyTarget(PlayerShip attacker, Station source, params ActiveObject[] targets) {
        this.attacker = attacker;
        this.source = source;
        this.targets = new(targets);
        foreach (var t in targets) {
            switch (t) {
                case AIShip s:
                    s.onDestroyed += this;
                    break;
                case Station s:
                    s.onDestroyed += this;
                    break;
            }
        }
    }

    AIShip.Destroyed IContainer<AIShip.Destroyed>.Value => (s, d, w) => {
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

    public Con GetScene(Con prev, PlayerShip playerShip, IDockable d) {
        if (d != source) {
            return null;
        }
        if (complete) {
            return debrief(prev);
        }

        return inProgress(prev);
    }
}

