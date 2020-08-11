using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranscendenceRL.RogueFrontierContent {
    class PlayerTutorial : Event {
        PlayerMain playerMain;
        int delay;
        public PlayerTutorial(PlayerMain playerMain) {
            this.playerMain = playerMain;
            delay = 300;
        }
        public void Update() {
            if(delay > 0) {
                delay--;
            } else {
                if(playerMain.sceneContainer.Children.Count == 0) {
                    playerMain.sceneContainer.Children.Add(new SceneScan(new TextScene(playerMain, $"Today has been a long time in the making. From the day you received the Orator's call to the day you earned your pilot's license, so much has changed in you. Having just registered a new {playerMain.playerShip.ShipClass.name} without thinking twice, the euphoria of taking your first steps into space has left you wondering where to go next. Maybe the Daughters of the Orator will know something.", new List<SceneOption>() { })));
                }
                playerMain.World.RemoveEvent(this);
            }
        }
    }
}
