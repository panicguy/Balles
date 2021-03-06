﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;

namespace Bristana
{
    class Program
    {
        public static Spell.Active Q;
        public static Spell.Skillshot W;
        public static Spell.Targeted E;
        public static Spell.Targeted R;
        public static Item Botrk;
        public static Item Bil;
        
        public static Menu Menu,
        SpellMenu,
        JungleMenu,
        HarassMenu,
        LaneMenu,
        StealMenu,
        Misc,
        Items,
        Skin;

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
            {
                JungleClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                Flee();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            KillSteal();
            Item();
			
            if (_Player.SkinId != Skin["skin.Id"].Cast<ComboBox>().CurrentValue)
            {
                if (checkSkin())
                {
                    Player.SetSkinId(SkinId());
                }
            }

        }
		
        private static void JungleClear()
        {
            var monster = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, E.Range).OrderByDescending(a => a.MaxHealth).FirstOrDefault();
            if (monster != null)
            {
                if (Q.IsReady() && JungleMenu["jungleQ"].Cast<CheckBox>().CurrentValue && monster.IsValidTarget(E.Range))
                {
                    Q.Cast();
                }
                if (W.IsReady() && JungleMenu["jungleW"].Cast<CheckBox>().CurrentValue && monster.IsValidTarget(W.Range))
                {
                    W.Cast(monster.ServerPosition);
                }
                if (E.IsReady() && JungleMenu["jungleE"].Cast<CheckBox>().CurrentValue && monster.IsValidTarget(E.Range) && _Player.ManaPercent > JungleMenu["manaJung"].Cast<Slider>().CurrentValue)
                {
                    E.Cast(monster);
                }
            }
        }
		
        private static void Flee()
        {
            if (W.IsReady())
            {
                var cursorPos = Game.CursorPos;
                var castPos = Player.Instance.Position.Distance(cursorPos) <= W.Range ? cursorPos : Player.Instance.Position.Extend(cursorPos, W.Range).To3D();
                W.Cast(castPos);
			}
		}
		
        public static int SkinId()
        {
            return Skin["skin.Id"].Cast<ComboBox>().CurrentValue;
        }
        public static bool checkSkin()
        {
            return Skin["checkSkin"].Cast<CheckBox>().CurrentValue;
        }

        private static void Gapcloser_OnGapCloser(Obj_AI_Base sender, Gapcloser.GapcloserEventArgs args)
        {
            if(Misc["antiGap"].Cast<CheckBox>().CurrentValue && args.Sender.Distance(_Player)<300)
            {
                R.Cast(args.Sender);
            }
        }

        public static void Item()
        {

            var item = Items["BOTRK"].Cast<CheckBox>().CurrentValue;
            var Minhp = Items["ihp"].Cast<Slider>().CurrentValue;
            var Minhpp = Items["ihpp"].Cast<Slider>().CurrentValue;
            var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            if (target != null)
            {
                if (item && Bil.IsReady() && Bil.IsOwned() && target.IsValidTarget(550))
                {
                    Bil.Cast(target);
                }
                else if (item && Player.Instance.HealthPercent <= Minhp || target.HealthPercent < Minhpp && Botrk.IsReady() && Botrk.IsOwned() && target.IsValidTarget(550))
                {
                    Botrk.Cast(target);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target != null)
            {
                if (HarassMenu["HarassQ"].Cast<CheckBox>().CurrentValue && Q.IsReady() && target.IsValidTarget(E.Range))
                {
                    Q.Cast();
                }
                if (E.IsReady() && target.IsValidTarget(E.Range) && _Player.ManaPercent > HarassMenu["manaHarass"].Cast<Slider>().CurrentValue)
                {
                    if (HarassMenu["HarassE" + target.ChampionName].Cast<CheckBox>().CurrentValue)
                    {
                        E.Cast(target);
                    }
                }
            }
        }
		
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (target != null)
            {
                if (SpellMenu["ComboQ"].Cast<CheckBox>().CurrentValue && Q.IsReady() && target.IsValidTarget(E.Range))
                {
                    Q.Cast();
                }
                if (E.IsReady() && target.IsValidTarget(E.Range))
                {
                    if (SpellMenu["useECombo" + target.ChampionName].Cast<CheckBox>().CurrentValue)
                    {
                        E.Cast(target);
                    }
                }
            }
        }

        private static void KillSteal()
        {
            var target = EntityManager.Heroes.Enemies.Where(e => e.IsValidTarget(1200) && !e.IsDead && !e.IsZombie && e.HealthPercent <= 25);
            foreach (var target2 in target)
	    	{
                if (StealMenu["RKs"].Cast<CheckBox>().CurrentValue && R.IsReady() && target2.Health + target2.AttackShield < Player.Instance.GetSpellDamage(target2, SpellSlot.R))
                {
                    R.Cast(target2);
                }
            }
        }

        private static void LaneClear()
        {
            var minion = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, _Player.AttackRange).FirstOrDefault(a => !a.IsDead);
            var tower = EntityManager.Turrets.Enemies.FirstOrDefault(a => !a.IsDead && a.Distance(_Player) < _Player.AttackRange);
            if (tower != null)
            {
                if (LaneMenu["ClearTower"].Cast<CheckBox>().CurrentValue && E.IsReady() && tower.IsValidTarget(E.Range) && _Player.ManaPercent > LaneMenu["manaFarm"].Cast<Slider>().CurrentValue)
                {
                    E.Cast(tower);
                }
            }
            if (minion != null)
            {
                if (LaneMenu["ClearE"].Cast<CheckBox>().CurrentValue && E.IsReady() && !tower.IsValidTarget(E.Range) && minion.IsValidTarget(E.Range) && _Player.ManaPercent > LaneMenu["manaFarm"].Cast<Slider>().CurrentValue)
                {
                    E.Cast(minion);
                }
                if (LaneMenu["ClearQ"].Cast<CheckBox>().CurrentValue && Q.IsReady() && minion.IsValidTarget(E.Range))
                {
                    Q.Cast();
                }
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            var renga = EntityManager.Heroes.Enemies.Find(r => r.ChampionName.Equals("Rengar"));
            var khazix = EntityManager.Heroes.Enemies.Find(r => r.ChampionName.Equals("Khazix"));
            if (renga != null)
            {
                if (sender.Name == ("Rengar_LeapSound.troy") && Misc["antiRengar"].Cast<CheckBox>().CurrentValue && sender.Position.Distance(_Player) < R.Range)
                    R.Cast(renga);
            }
            if (khazix != null)
            {
                if (sender.Name == ("Khazix_Base_E_Tar.troy") && Misc["antiKZ"].Cast<CheckBox>().CurrentValue && sender.Position.Distance(_Player) <= 400)
                    R.Cast(khazix);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Misc["drawAA"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = E.Range }.Draw(_Player.Position);
            }
            if (Misc["drawW"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.GreenYellow, BorderWidth = 1, Radius = W.Range }.Draw(_Player.Position);
            }
        }
		
        private static void OnLoadingComplete(EventArgs args)
        {
            if (!_Player.ChampionName.Contains("Tristana")) return;
            Chat.Print("Bristana Loaded!", Color.GreenYellow);
            Chat.Print("Good Luck!", Color.GreenYellow);
            Bootstrap.Init(null);
            Q = new Spell.Active(SpellSlot.Q);
            W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 450, int.MaxValue, 180);
            E = new Spell.Targeted(SpellSlot.E, 550);
            R = new Spell.Targeted(SpellSlot.R, 550);
            Botrk = new Item( ItemId.Blade_of_the_Ruined_King);
            Bil = new Item(3144, 475f);

            Menu = MainMenu.AddMenu("Bristana", "Bristana");
            Menu.AddGroupLabel("Bristana");
            Menu.AddLabel(" Please Select E Before Play ! ");
            
            SpellMenu = Menu.AddSubMenu("Combo Settings", "Combo");
            SpellMenu.AddGroupLabel("Combo Settings");
            SpellMenu.Add("ComboQ", new CheckBox("Spell [Q]"));
            SpellMenu.Add("ComboR", new CheckBox("Spell [R]"));
            SpellMenu.Add("ComboER", new CheckBox("Spell [ER]"));
            SpellMenu.AddGroupLabel("Combo [E] On");
            foreach (var target in EntityManager.Heroes.Enemies)
            {
                SpellMenu.Add("useECombo" + target.ChampionName, new CheckBox("" + target.ChampionName));
            }

            HarassMenu = Menu.AddSubMenu("Harass Settings", "Harass");
            HarassMenu.AddGroupLabel("Harass Settings");
            HarassMenu.Add("HarassQ", new CheckBox("Spell [Q]", false));
            HarassMenu.AddGroupLabel("Spell [E] on");
            foreach (var target in EntityManager.Heroes.Enemies)
            {
                HarassMenu.Add("HarassE" + target.ChampionName, new CheckBox("" + target.ChampionName));
            }
            HarassMenu.Add("manaHarass", new Slider("Min Mana For Harass", 50, 0, 100));

            LaneMenu = Menu.AddSubMenu("Laneclear Settings", "Clear");
            LaneMenu.AddGroupLabel("Laneclear Settings");
            LaneMenu.Add("ClearQ", new CheckBox("Spell [Q]", false));
            LaneMenu.Add("ClearE", new CheckBox("Spell [E]", false));
            LaneMenu.Add("ClearTower", new CheckBox("Spell [E] Turret", false));
            LaneMenu.Add("manaFarm", new Slider("Min Mana For LaneClear", 50, 0, 100));
			
            JungleMenu = Menu.AddSubMenu("JungleClear Settings", "JungleClear");
            JungleMenu.AddGroupLabel("JungleClear Settings");
            JungleMenu.Add("jungleQ", new CheckBox("Spell [Q]"));
            JungleMenu.Add("jungleE", new CheckBox("Spell [E]"));
            JungleMenu.Add("jungleW", new CheckBox("Spell [W]", false));
            JungleMenu.Add("manaJung", new Slider("Min Mana For JungleClear", 50, 0, 100));

            StealMenu = Menu.AddSubMenu("KillSteal Settings", "KS");
            StealMenu.AddGroupLabel("Killsteal Settings");
            StealMenu.Add("RKs", new CheckBox("Spell [R]"));

            Items = Menu.AddSubMenu("Items Settings", "Items");
            Items.AddGroupLabel("Items Settings");
            Items.Add("BOTRK", new CheckBox("Use [Botrk]"));
            Items.Add("ihp", new Slider("My HP Use BOTRK <=", 50));
            Items.Add("ihpp", new Slider("Enemy HP Use BOTRK <=", 50));

            Misc = Menu.AddSubMenu("Misc Settings", "Draw");
            Misc.AddGroupLabel("Anti Gapcloser");
            Misc.Add("antiGap", new CheckBox("Anti Gapcloser"));
            Misc.Add("antiRengar", new CheckBox("Anti Rengar"));
            Misc.Add("antiKZ", new CheckBox("Anti Kha'Zix"));
            Misc.AddGroupLabel("Drawings Settings");
            Misc.Add("drawAA", new CheckBox("Draw E"));
            Misc.Add("drawW", new CheckBox("Draw W", false));
			
            Skin = Menu.AddSubMenu("Skin Changer", "SkinChanger");
            Skin.Add("checkSkin", new CheckBox("Use Skin Changer"));
            Skin.Add("skin.Id", new ComboBox("Skin Mode", 0, "Classic", "Riot Tristana", "Earnest Elf Tristana", "Firefighter Tristana", "Guerilla Tristana", "Rocket Tristana", "Color Tristana", "Color Tristana", "Color Tristana", "Color Tristana", "Dragon Trainer Tristana"));


            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapCloser;
            GameObject.OnCreate += GameObject_OnCreate;

		}
    }
}