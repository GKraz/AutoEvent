﻿using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Events;
using AutoEvent.API.Schematic.Objects;
using AutoEvent.Events.Handlers;
using AutoEvent.Games.Infection;
using AutoEvent.Interfaces;
using Mirror;
using Event = AutoEvent.Interfaces.Event;
using Random = UnityEngine.Random;

namespace AutoEvent.Games.DeathParty
{
    public class Plugin : Event, IEventMap, IEventSound, IInternalEvent
    {
        public override string Name { get; set; } = AutoEvent.Singleton.Translation.DeathTranslate.DeathName;
        public override string Description { get; set; } = AutoEvent.Singleton.Translation.DeathTranslate.DeathDescription;
        public override string Author { get; set; } = "KoT0XleB";
        public MapInfo MapInfo { get; set; } = new MapInfo()
            {MapName = "DeathParty", Position = new Vector3(10f, 1012f, -40f), };
        public SoundInfo SoundInfo { get; set; } = new SoundInfo()
            { SoundName = "DeathParty.ogg", Volume = 5, Loop = true };

        [EventConfig]
        public DeathPartyConfig Config { get; set; }
        public override string CommandName { get; set; } = "airstrike";
        protected override float PostRoundDelay { get; set; } = 5f;
        private EventHandler EventHandler { get; set; }
        private DeathTranslate Translation { get; set; }
        public bool RespawnWithGrenades { get; set; } = true;
        public int Stage { get; private set; }
        //private int _maxStage;
        private CoroutineHandle _grenadeCoroutineHandle;

        protected override void RegisterEvents()
        {
            Translation = new DeathTranslate();
            EventHandler = new EventHandler(this);

            EventManager.RegisterEvents(EventHandler);
            Servers.TeamRespawn += EventHandler.OnTeamRespawn;
            Servers.SpawnRagdoll += EventHandler.OnSpawnRagdoll;
            Servers.PlaceBullet += EventHandler.OnPlaceBullet;
            Servers.PlaceBlood += EventHandler.OnPlaceBlood;
            Players.DropItem += EventHandler.OnDropItem;
            Players.DropAmmo += EventHandler.OnDropAmmo;
            Players.PlayerDamage += EventHandler.OnPlayerDamage;
            Players.PlayerDying += EventHandler.OnPlayerDying;
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
            Players.PlayerDying -= EventHandler.OnPlayerDying;

            EventHandler = null;
        }

        protected override void OnStart()
        {
            Server.FriendlyFire = true;
            //_maxStage = Config.Rounds;
            //_maxStage = 5;

            foreach (Player player in Player.GetPlayers())
            {
                player.SetRole(RoleTypeId.ClassD, RoleChangeReason.None);
                player.Position = RandomClass.GetSpawnPosition(MapInfo.Map);
            }
        }

        protected override void OnStop()
        {
            Timing.CallDelayed(1.2f, () => {
                if (_grenadeCoroutineHandle.IsRunning)
                {
                    Timing.KillCoroutines(new CoroutineHandle[] { _grenadeCoroutineHandle });
                }
            });
        }

        protected override IEnumerator<float> BroadcastStartCountdown()
        {
            for (int _time = 10; _time > 0; _time--)
            {
                Extensions.Broadcast($"<size=100><color=red>{_time}</color></size>", 1);
                yield return Timing.WaitForSeconds(1f);
            }
        }

        protected override void CountdownFinished()
        { 
            _grenadeCoroutineHandle = Timing.RunCoroutine(GrenadeCoroutine(), "death_grenade");
        }

        protected override void ProcessFrame()
        {
            var count = Player.GetPlayers().Count(r => r.IsAlive).ToString();
            var cycleTime = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
            Extensions.Broadcast(Translation.DeathCycle.Replace("%count%", count).Replace("%time%", cycleTime), 1);
        }

        protected override bool IsRoundDone()
        {
            // At least one player is alive &&
            // Stage hasn't yet hit the max stage.
            return !(Player.GetPlayers().Count(r => r.IsAlive && (!RespawnWithGrenades||r.Role != RoleTypeId.ChaosConscript)) > 0 && Stage <= Config.Rounds);
        }
        public IEnumerator<float> GrenadeCoroutine()
        { 
            Stage = 1;
            float fuse = 10f;
            float height = 20f;
            float count = 20;
            float timing = 1f;
            float scale = 4;
            float radius = MapInfo.Map.AttachedBlocks.First(x => x.name == "Arena").transform.localScale.x / 2 - 6f;

            while (Player.GetPlayers().Count(r => r.IsAlive) > 0 && Stage <= Config.Rounds)
            {
                if (KillLoop)
                {
                    yield break;
                }
                // Defaults: 
                // count += 30;     20,  50,  80,  110, [ignored last round] 1
                // timing -= 0.3f;  1.0, 0.7, 0.4, 0.1, [ignored last round] 10
                // height -= 5f;    20,  15,  10,  5,   [ignored last round] 20
                // fuse -= 2f;      10,  8,   6,   4,   [ignored last round] 10
                // scale -= 1;      4,   3,   2,   1,   [ignored last round] 75
                // radius += 7f;    4,   11,  18,  25   [ignored last round] 10
                radius = Config.GrenadeSpawnRadius.GetValue(Stage, Config.Rounds -1, 0.1f, 75);
                scale = Config.GrenadeScale.GetValue(Stage, Config.Rounds -1, 0.1f, 75);
                count = (int) Config.GrenadeCount.GetValue(Stage, Config.Rounds -1, 1, 300);
                timing = Config.GrenadeSpeed.GetValue(Stage, Config.Rounds -1, 0, 5);
                height = Config.GrenadeHeight.GetValue(Stage, Config.Rounds -1, 0,  30);
                fuse = Config.GrenadeFuseTime.GetValue(Stage, Config.Rounds -1, 2, 10);

                
                // Not the last round.
                if (Stage != Config.Rounds)
                {
                    int playerIndex = 0;
                    for (int i = 0; i < count; i++)
                    {

                        Vector3 pos = MapInfo.Map.Position + new Vector3(Random.Range(-radius, radius), height, Random.Range(-radius, radius));
                        // has to be re-iterated every run because a player could have been killed from the last one.
                        if (Config.TargetPlayers)
                        {
                            Player randomPlayer = Player.GetPlayers().Where(x => x.Role == RoleTypeId.ClassD).ToList().RandomItem();
                            pos = randomPlayer.Position;
                            pos.y = height;
                        }
                        Extensions.GrenadeSpawn(fuse, pos, scale);
                        yield return Timing.WaitForSeconds(timing);
                        playerIndex++;
                    }
                }
                else // last round.
                {
                    Vector3 pos = MapInfo.Map.Position + new Vector3(Random.Range(-10, 10), 20, Random.Range(-10, 10));
                    Extensions.GrenadeSpawn(10, pos, 75);
                }

                yield return Timing.WaitForSeconds(15f);
                Stage++;
            }

            yield break;
        }

        protected override void OnFinished()
        {
            if (_grenadeCoroutineHandle.IsRunning)
            {
                KillLoop = true;
                Timing.CallDelayed(1.2f, () => {
                    if (_grenadeCoroutineHandle.IsRunning)
                    {
                        Timing.KillCoroutines(new CoroutineHandle[] { _grenadeCoroutineHandle });
                    }
                });
            }
            var time = $"{EventTime.Minutes:00}:{EventTime.Seconds:00}";
            if (Player.GetPlayers().Count(r => r.IsAlive) > 1)
            {
                Extensions.Broadcast(Translation.DeathMorePlayer.Replace("%count%", $"{Player.GetPlayers().Count(r => r.IsAlive)}").Replace("%time%", time), 10);
            }
            else if (Player.GetPlayers().Count(r => r.IsAlive) == 1)
            {
                var player = Player.GetPlayers().First(r => r.IsAlive);
                player.Health = 1000;
                Extensions.Broadcast(Translation.DeathOnePlayer.Replace("%winner%", player.Nickname).Replace("%time%", time), 10);
            }
            else
            {
                Extensions.Broadcast(Translation.DeathAllDie.Replace("%time%", time), 10);
            }
        }

        protected override void OnCleanup()
        {
            Server.FriendlyFire = AutoEvent.IsFriendlyFireEnabledByDefault;
        }
        
}
}
