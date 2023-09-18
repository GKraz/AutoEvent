﻿using CustomPlayerEffects;
using AutoEvent.API.Schematic.Objects;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AutoEvent.Events.Handlers;
using AutoEvent.Games.Infection;
using AutoEvent.Interfaces;
using Event = AutoEvent.Interfaces.Event;

namespace AutoEvent.Games.Survival
{
    public class Plugin : Event, IEventSound, IEventMap, IInternalEvent
    {
        public override string Name { get; set; } = AutoEvent.Singleton.Translation.SurvivalTranslate.SurvivalName;
        public override string Description { get; set; } = AutoEvent.Singleton.Translation.SurvivalTranslate.SurvivalDescription;
        public override string Author { get; set; } = "KoT0XleB";
        public override string CommandName { get; set; } = "zombie2";
        public MapInfo MapInfo { get; set; } = new MapInfo()
            {MapName = "Survival", Position = new Vector3(15f, 1030f, -43.68f), Rotation = Quaternion.identity};
        public SoundInfo SoundInfo { get; set; } = new SoundInfo()
            { SoundName = "DeathParty.ogg", Volume = 5, Loop = true };
        protected override float PostRoundDelay { get; set; } = 10f;
        private EventHandler EventHandler { get; set; }
        private SurvivalTranslate Translation { get; set; }
        internal Player FirstZombie { get; private set; }
        private GameObject _teleport;
        private GameObject _teleport1;
        private TimeSpan _remainingTime;

        protected override void RegisterEvents()
        {
            Translation = new SurvivalTranslate();

            EventHandler = new EventHandler(this);
            EventManager.RegisterEvents(EventHandler);
            Servers.TeamRespawn += EventHandler.OnTeamRespawn;
            Servers.SpawnRagdoll += EventHandler.OnSpawnRagdoll;
            Servers.PlaceBullet += EventHandler.OnPlaceBullet;
            Servers.PlaceBlood += EventHandler.OnPlaceBlood;
            Players.DropItem += EventHandler.OnDropItem;
            Players.DropAmmo += EventHandler.OnDropAmmo;
            Players.PlayerDamage += EventHandler.OnPlayerDamage;

        }

        protected override void UnregisterEvents()
        {
            EventManager.UnregisterEvents(EventHandler);
            Servers.TeamRespawn -= EventHandler.OnTeamRespawn;
            Servers.SpawnRagdoll -= EventHandler.OnSpawnRagdoll;
            Servers.PlaceBullet -= EventHandler.OnPlaceBullet;
            Servers.PlaceBlood -= EventHandler.OnPlaceBlood;
            Players.DropItem -= EventHandler.OnDropItem;
            Players.DropAmmo -= EventHandler.OnDropAmmo;
            Players.PlayerDamage -= EventHandler.OnPlayerDamage;

            EventHandler = null;
        }

        protected override void OnStart()
        {
            _remainingTime = new TimeSpan(0, 5, 0);
            Server.FriendlyFire = false;
            foreach (Player player in Player.GetPlayers())
            {
                Extensions.SetRole(player, RoleTypeId.NtfSergeant, RoleSpawnFlags.None);
                player.Position = RandomClass.GetSpawnPosition(MapInfo.Map);
                //Extensions.SetPlayerAhp(player, 100, 100, 0);

                var item = player.AddItem(RandomClass.GetRandomGun());
                player.AddItem(ItemType.GunCOM18);
                player.AddItem(ItemType.ArmorCombat);

                Timing.CallDelayed(0.1f, () =>
                {
                    player.CurrentItem = item;
                });
            }
        }

        protected override IEnumerator<float> BroadcastStartCountdown()
        {
            for (float _time = 20; _time > 0; _time--)
            {
                Extensions.Broadcast(Translation.SurvivalBeforeInfection.Replace("%name%", Name).Replace("%time%", $"{_time}"), 1);
                yield return Timing.WaitForSeconds(1f);
            }
        }

        protected override void CountdownFinished()
        {
            Extensions.StopAudio();
            Timing.CallDelayed(0.1f, () =>
            {
                Extensions.PlayAudio("Zombie2.ogg", 7, true, Name);
            });
            for (int i = 0; i <= Player.GetPlayers().Count() / 10; i++)
            {
                var player = Player.GetPlayers().Where(r => r.IsHuman).ToList().RandomItem();
                Extensions.SetRole(player, RoleTypeId.Scp0492, RoleSpawnFlags.AssignInventory);
                player.EffectsManager.EnableEffect<Disabled>();
                player.EffectsManager.EnableEffect<Scp1853>();
                player.Health = 5000;

                if (Player.GetPlayers().Count(r => r.IsSCP) == 1)
                {
                    FirstZombie = player;
                }
            }

            _teleport = MapInfo.Map.AttachedBlocks.First(x => x.name == "Teleport");
            _teleport1 = MapInfo.Map.AttachedBlocks.First(x => x.name == "Teleport1");
        }

        protected override bool IsRoundDone()
        {
            // At least 1 human player &&
            // At least 1 scp player &&
            // round time under 5 minutes (+ countdown)
            return Player.GetPlayers().Count(r => r.IsHuman) > 0 && Player.GetPlayers().Count(r => r.IsSCP) > 0 &&
                EventTime.TotalSeconds < 300 + 20;
        }

        protected override void ProcessFrame()
        {
            var text = Translation.SurvivalAfterInfection;
            
            text = text.Replace("%name%", Name);
            text = text.Replace("%humanCount%", Player.GetPlayers().Count(r => r.IsHuman).ToString());
            text = text.Replace("%time%", $"{_remainingTime.Minutes:00}:{_remainingTime.Seconds:00}");

            foreach (var player in Player.GetPlayers())
            {
                player.ClearBroadcasts();
                player.SendBroadcast(text, 1);

                if (Vector3.Distance(player.Position, _teleport.transform.position) < 1)
                {
                    player.Position = _teleport1.transform.position;
                }
            }

            _remainingTime -= TimeSpan.FromSeconds(FrameDelayInSeconds);
        }

        protected override void OnFinished()
        {
            if (Player.GetPlayers().Count(r => r.IsHuman) == 0)
            {
                Extensions.Broadcast(Translation.SurvivalZombieWin, 10);
                Extensions.PlayAudio("ZombieWin.ogg", 7, false, Name);
            }
            else if (Player.GetPlayers().Count(r => r.IsSCP) == 0)
            {
                Extensions.Broadcast(Translation.SurvivalHumanWin, 10);
                Extensions.PlayAudio("HumanWin.ogg", 7, false, Name);
            }
            else
            {
                Extensions.Broadcast(Translation.SurvivalHumanWinTime, 10);
                Extensions.PlayAudio("HumanWin.ogg", 7, false, Name);
            }
        }

        protected override void OnCleanup()
        {
            Server.FriendlyFire = AutoEvent.IsFriendlyFireEnabledByDefault;
        }
    }
}
