using System.Collections.Generic;

namespace RogueFrontier;

public class Player {
    public string file;
    public Settings Settings;

    public string name;
    public GenomeType Genome;

    public List<Epitaph> Epitaphs = new List<Epitaph>();

    public int money;
    public Player() { }
    public Player(Settings settings) {
        this.Settings = settings;
    }
}
