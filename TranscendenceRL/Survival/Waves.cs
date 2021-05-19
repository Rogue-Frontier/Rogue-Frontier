using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TranscendenceRL.BaseShip;

namespace TranscendenceRL {
    class Waves : Event {
        public PlayerShip playerShip;
        public int ticks;

        public int difficulty = 90;
        public Waves(PlayerShip playerShip) {

            this.playerShip = playerShip;


            playerShip.messages.Add(new InfoMessage("Wave incoming!"));
            CreateWave();
        }
        HashSet<AIShip> ships;
        public void CreateWave() {
            ships = new HashSet<AIShip>();
            World world = playerShip.world;
            difficulty += 90;

            Dictionary<string, int> map = new Dictionary<string, int> {
                {"ship_amethyst", 90},
            {"ship_beowulf", 180 },
            {"ship_chemotoxin", 150 },
{"ship_constellation_marksman", 150 },
{"ship_constellation_marshal", 210 },
{"ship_errant", 180 },
{"ship_hyperego", 240 },
{"ship_iron_embargo", 300 },
{"ship_gunboat", 180 },
{"ship_privateer", 240 },
{"ship_laser_drone", 30 },
{"ship_orion_raider", 90 },
{"ship_huntsman", 150 },

            };

            

            void createShip() {
                var ship = new AIShip(new BaseShip(world,
                    world.types.shipClass.Values.GetRandom(world.karma),
                    Sovereign.Gladiator,
                    XY.Polar(0, 100)), new AttackOrder(playerShip));

                world.AddEntity(ship);
                world.AddEffect(new Heading(ship));
                ships.Add(ship);
            }
        }
        public void Update() {
            ticks++;
            if(ticks%60 == 0) {
                if(ships.Any(s => s.active)) {
                    return;
                }

                CreateWave();
            }
        }
    }
}
