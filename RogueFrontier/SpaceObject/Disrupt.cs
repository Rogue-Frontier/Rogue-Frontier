namespace RogueFrontier;

public enum DisruptMode {
    NONE = 0, FORCE_OFF, FORCE_ON, //FORCE_CCW, FORCE_CW
}
public class Disrupt {
    public int ticksLeft;
    public bool active => ticksLeft > 0;
    public DisruptMode thrustMode;
    public DisruptMode turnMode;
    public DisruptMode brakeMode;
    public DisruptMode fireMode;

    public void Update() {
        ticksLeft--;
    }
}
