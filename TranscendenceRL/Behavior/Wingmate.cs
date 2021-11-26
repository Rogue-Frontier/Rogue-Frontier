using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscendenceRL {
    public enum WingmateOrder {
        Escort,
        Wait,
        BreakAndAttack,
        Scout,
    }
    public class Wingmate : IShipBehavior {
        PlayerShip player;
        WingmateOrder order;

        //This class handles orders and communications
        public Wingmate(PlayerShip player) {
            this.player = player;
        }
        public void Update(IShip owner) {
            switch (order) {
                case WingmateOrder.Escort:
                    break;
                case WingmateOrder.Wait:
                    break;
                case WingmateOrder.BreakAndAttack:
                    break;
                case WingmateOrder.Scout:
                    break;
            }
        }
        public void SetOrder(IShip owner, WingmateOrder order) {
            switch(order) {
                case WingmateOrder.Escort:
                    break;
                case WingmateOrder.Wait:
                    break;
                case WingmateOrder.BreakAndAttack:
                    break;
                case WingmateOrder.Scout:
                    break;
            }
        }
    }
}
