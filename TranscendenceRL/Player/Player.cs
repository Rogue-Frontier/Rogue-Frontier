using System;
using System.Collections.Generic;
using System.Text;

namespace TranscendenceRL {
    public class Player {
        public string file;
        public Settings Settings;

        public string name;
        public GenomeType Genome;

        public List<Epitaph> Epitaphs = new List<Epitaph>();

        public int money;
    }
}
