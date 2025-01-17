﻿using AutoEvent.Events.EventArgs;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using System.Linq;

namespace AutoEvent.Games.HideAndSeek
{
    public class EventHandler
    {
        public void OnPlayerDamage(PlayerDamageArgs ev)
        {
            if (ev.DamageType == DeathTranslations.Falldown.Id)
            {
                ev.IsAllowed = false;
            }
            
            if (ev.Attacker != null)
            {
                bool isAttackerJailbird = ev.Attacker.Items.FirstOrDefault(r => r.ItemTypeId == ItemType.Jailbird);
                bool isTargetJailBird = ev.Target.Items.FirstOrDefault(r => r.ItemTypeId == ItemType.Jailbird);
                
                if (isAttackerJailbird == true && isTargetJailBird == false)
                {
                    ev.IsAllowed = false;

                    ev.Attacker.ClearInventory();

                    var jailbird = ev.Target.AddItem(ItemType.Jailbird);
                    Timing.CallDelayed(0.1f, () =>
                    {
                        ev.Target.CurrentItem = jailbird;
                    });
                }
            }
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnJoin(PlayerJoinedEvent ev)
        {
            ev.Player.SetRole(RoleTypeId.Spectator);
        }

        public void OnTeamRespawn(TeamRespawnArgs ev) => ev.IsAllowed = false;
        public void OnSpawnRagdoll(SpawnRagdollArgs ev) => ev.IsAllowed = false;
        public void OnPlaceBullet(PlaceBulletArgs ev) => ev.IsAllowed = false;
        public void OnPlaceBlood(PlaceBloodArgs ev) => ev.IsAllowed = false;
        public void OnDropItem(DropItemArgs ev) => ev.IsAllowed = false;
        public void OnDropAmmo(DropAmmoArgs ev) => ev.IsAllowed = false;
    }
}
