using Common;
using static SadConsole.Input.Keys;
using SadRogue.Primitives;
using SadConsole;
using SadConsole.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using static IslandHopper.Constants;
using SadConsole.Input;
using System.IO;
using Console = SadConsole.Console;
using ArchConsole;
using static IslandHopper.ItemType.GunType;

namespace IslandHopper {
    public class ShopConsole : Console {
        ItemType preview;
        DictCounter<ItemType> items = new DictCounter<ItemType>();
        public ShopConsole(int Width, int Height) : base(Width, Height) {
            DefaultBackground = Color.Black;
            DefaultForeground = Color.White;

            int x = 4;
            int y = 4;

            Children.Add(new Label("Shop") { Position = new Point(x, y) });
            y += 2;
            foreach(var s in StandardTypes.stdWeapons) {

                var it = s;
                var name = s.name;

                var label = new Label(name) { Position = new Point(x, y) };

                label.MouseEnter += Row_MouseEnter;
                void Row_MouseEnter(object sender, MouseScreenObjectState e) {
                    preview = it;
                }
                Children.Add(label);

                int x2 = 24;
                Label count = null;
                Children.Add(new LabelButton("-", () => {
                    items.Decrement(it);
                    UpdateCount();
                }) { Position = new Point(x2,y ) });

                count = new Label("0") { Position = new Point(x2 + 2, y) };
                Children.Add(count);

                Children.Add(new LabelButton("+", () => {
                    items.Increment(it);
                    UpdateCount();
                }) { Position = new Point(x2 + 2 + 2 + 2, y) });
                void UpdateCount() => count.text = new ColoredString(items[it].ToString(), Color.White, Color.Black);

                y++;
            }
            y++;
            Children.Add(new LabelButton("Start", () => {


                //TO DO: Create a multi-tiled plane structure to introduce the player
                int size = 128;
                int height = 30;
                var World = new Island() {
                    karma = new Rand(0),
                    entities = new LocatorDict<Entity, (int, int, int)>(new EntityPosition()),
                    effects = new LocatorDict<Effect, (int, int, int)>(new EffectPosition()),
                    voxels = new ArraySpace<Voxel>(size, size, height, new Air()),
                    camera = new XYZ(0, 0, 0),
                    types = null
                    //types = new TypeCollection(XElement.Parse(File.ReadAllText("IslandHopperContent/Items.xml")))
                };
                var player = new Player(World, new XYZ(28.5, 29.5, 1));

                foreach ((var w, var c) in items.dict) {
                    for (int i = 0; i < c; i++) {
                        player.Inventory.Add(w.GetItem(World, player.Position));
                    }
                }
                World.player = player;

                //World.AddEntity(new Player(World, new Point3(85, 85, 20)));

                for (int x = 0; x < World.voxels.Width; x++) {
                    for (int y = 0; y < World.voxels.Height; y++) {
                        World.voxels[new XYZ(x, y, 0)] = new Grass(World);
                    }
                }
                var r = World.karma;

                for (int i = 0; i < 50; i++) {
                    //World.entities.Place(World.types.Lookup<ItemType>("itHotRod").GetItem(World, new XYZ(28.5, 29.5, 1)));
                    //World.entities.Place(StandardTypes.itStoppedClock.GetItem(World, new XYZ(28.5, 29.5, 1)));
                    var s = StandardTypes.stdWeapons;

                    Func<int, int> next = r.NextInteger;

                    World.entities.PlaceNew(s[r.NextInteger(s.Length)].GetItem(World, new XYZ(next(World.voxels.Width), next(World.voxels.Height), 1)));
                    World.entities.PlaceNew(new Enemy(World, new XYZ(next(World.voxels.Width), next(World.voxels.Height), 1)));
                }
                World.entities.PlaceNew(World.player);
                //            World.entities.Place(new Enemy(World, new XYZ(35, 35, 1)));

                var plane = new Plane(World, new XYZ(60, 20, 1), new XYZ(0, 0, 0));
                World.AddEntity(plane);
                plane.OnAdded();

                GameHost.Instance.Screen = new GameConsole(Width, Height, World) { IsFocused = true };
            }) { Position = new Point(x, y) });
        }

        public override void Render(TimeSpan delta) {
            this.Clear();
            if(preview != null) {
                int x = 64;
                int y = 0;
                this.Print(x, y++, preview.name);
                y++;
                switch (preview.gun.projectile) {
                    case BulletDesc b:
                        this.Print(x, y++, $"Projectile:  Bullet");
                        this.Print(x, y++, $"Speed:       {b.speed}");
                        this.Print(x, y++, $"Damage:      {b.damage}");
                        this.Print(x, y++, $"Lifetime:    {b.lifetime}");
                        break;
                    case FlameDesc f:
                        this.Print(x, y++, $"Projectile:  Flame");
                        this.Print(x, y++, $"Speed:       {f.speed}");
                        this.Print(x, y++, $"Damage:      {f.damage}");
                        this.Print(x, y++, $"Lifetime:    {f.lifetime}");
                        break;
                    case GrenadeDesc g:
                        this.Print(x, y++, $"Projectile:  Grenade");
                        this.Print(x, y++, $"Speed:       {g.speed}");
                        this.Print(x, y++, $"Fuse time:   {g.grenadeType.fuseTime}");
                        this.Print(x, y++, $"Blast Radius:{g.grenadeType.explosionRadius}");
                        this.Print(x, y++, $"Blast Power: {g.grenadeType.explosionDamage}");
                        break;
                }
                y = 8;
                this.Print(x, y++, $"Clip size:   {preview.gun.clipSize}");
                this.Print(x, y++, $"Max ammo:    {preview.gun.maxAmmo}");

                this.Print(x, y++, $"Fire time:   {preview.gun.fireTime}");
                this.Print(x, y++, $"Reload time: {preview.gun.reloadTime}");
                y++;
                foreach(var l in preview.desc.Replace("\r", null).Split("\n")) {
                    this.Print(x, y++, l);
                }
                
                y = 20;
                preview.image?.Render(this, new Point(x, y));
            }
            base.Render(delta);
        }
    }
}
