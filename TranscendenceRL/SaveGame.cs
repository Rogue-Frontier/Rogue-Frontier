using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    interface SaveGame {
        public static readonly JsonSerializerSettings settings = new JsonSerializerSettings {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            TypeNameHandling = TypeNameHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }
    class LiveGame {
        public World world;
        public Player player;
        public PlayerShip playerShip;
    }
    class DeadGame {
        public World world;
        public Player player;
        public PlayerShip playerShip;
        public Epitaph epitaph;
    }
}
