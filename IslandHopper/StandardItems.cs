using Common;
using System;
using System.Collections.Generic;
using System.Text;
using static IslandHopper.ItemType;
using static IslandHopper.ItemType.GunType;

namespace IslandHopper {
    public static class StandardTypes {
        public static ItemType itStoppedClock = new ItemType() {
            name = "The Stopped Clock",
            desc = "Time flies when you're blowing up strangers into smithereens.",
            image = ColorImage.FromFile("IslandHopperContent/StoppedClock.asc.cg"),
            gun = new GunType() {
                clipSize = 12,
                difficulty = (int)WeaponDifficulty.medium,
                critOnLastShot = false,
                fireTime = 30,
                initialAmmo = 24,
                maxAmmo = 36,
                initialClip = 12,
                knockback = 0,
                noiseRange = 24,
                projectile = new GrenadeDesc() {
                    speed = 30,
                    grenadeType = new GrenadeType() {
                        canArm = false,
                        detonateOnDamage = true,
                        detonateOnImpact = true,
                        explosionDamage = 20,
                        explosionForce = 10,
                        explosionRadius = 20,
                        fuseTime = 45
                    }
                },
                projectileCount = 1,
                reloadTime = 30,
            }
        }, itHotRod = new ItemType() {
            name = "The Hot Rod",
            desc = "This metal rod is so hot that it learned how to breathe fire. Wait, what?",
            image = ColorImage.FromFile("IslandHopperContent/TheHotRod.asc.cg"),
            gun = new GunType() {
                projectileCount = 8,
                clipSize = 50,
                initialClip = 50,
                maxAmmo = 150,
                initialAmmo = 150,
                reloadTime = 120,
                fireTime = 4,
                projectile = new FlameDesc() {
                    damage = 3,
                    lifetime = 30,
                    speed = 20
                }
            }
        }, itSeventhStriker = new ItemType() {
            name = "The Seventh Striker",
            desc = "This seven-shooter revolver hits different on the last shot.",
            image = ColorImage.FromFile("IslandHopperContent/TheSeventhStriker.asc.cg"),
            gun = new GunType() {
                projectileCount=1,
                initialClip=7,
                clipSize=7,
                initialAmmo=28,
                maxAmmo=28,
                critOnLastShot=true,
                difficulty=0,
                reloadTime=60,
                fireTime=20,
                projectile = new BulletDesc() {
                    damage=12,
                }
            },
        }, itSameOldShotgun = new ItemType() {
            name="Same Old Shotgun",
            desc = "This is the same shotgun that your grandpa used back when this whole war started.",
            image = ColorImage.FromFile("IslandHopperContent/SameOldShotgun.asc.cg"),
            gun = new GunType() {
                projectileCount=6,
                initialClip=4,
                clipSize=4,
                initialAmmo=24,
                maxAmmo=24,
                difficulty=0,
                reloadTime=90,
                fireTime=30,
                projectile = new BulletDesc() {
                    damage=8
                },
                spread = 10
            }
        };
        public static ItemType[] stdWeapons = new ItemType[] { itHotRod, itSeventhStriker, itStoppedClock, itSameOldShotgun };
    }
}
/*
		<Gun
			projectileCount="8"
			clipSize=		"50"
			maxAmmo=		"150"
			reloadTime=		"120"
			fireTime=		"4"
			>
			<Flame damage="3" lifetime="30" speed="20"/>
*/
