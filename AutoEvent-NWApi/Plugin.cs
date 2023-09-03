﻿using System.IO;
using AutoEvent.CedModIntegration;
using AutoEvent.Interfaces;
using HarmonyLib;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Helpers;
using PluginAPI.Events;
using AutoEvent.Events.Handlers;
using GameCore;
using MEC;
using Event = AutoEvent.Interfaces.Event;

namespace AutoEvent
{
    public class AutoEvent
    {
        public static IEvent ActiveEvent;
        public static AutoEvent Singleton;
        public static Harmony HarmonyPatch;
        EventHandler eventHandler;

        [PluginConfig("configs/autoevent.yml")]
        public Config Config;

        [PluginConfig("configs/translation.yml")]
        public Translation Translation;

        [PluginPriority(LoadPriority.Low)]
        [PluginEntryPoint("AutoEvent-NWApi", "8.2.7", "A plugin that allows you to run mini-games.", "KoT0XleB")]
        void OnEnabled()
        {
            CosturaUtility.Initialize();
            _onEnabled();
        }
        void _onEnabled()
        {
            if (!Config.IsEnabled) return;

            Singleton = this;
            HarmonyPatch = new Harmony("autoevent-nwapi");
            HarmonyPatch.PatchAll();

           
            Timing.CallDelayed(2, () =>
            {
                var cedModEventLoader = new CedModEventLoader();
            });
            EventManager.RegisterEvents(this);
            SCPSLAudioApi.Startup.SetupDependencies();

            eventHandler = new EventHandler();
            Servers.RemoteAdmin += eventHandler.OnRemoteAdmin;

            Event.RegisterEvents();
            
            // Load External Events.
            if (!Directory.Exists(Path.Combine(Paths.GlobalPlugins.Plugins, "Events")))
            {
                Directory.CreateDirectory(Path.Combine(Paths.GlobalPlugins.Plugins, "Events"));
            }
            Loader.LoadEvents();
            Event.Events.AddRange(Loader.Events);
            
            PluginAPI.Core.Log.Info(Loader.Events.Count > 0 ? $"[ExternalEventLoader] Loaded {Loader.Events.Count} external event{(Loader.Events.Count > 1 ? "s" : "")}." : "No external events were found.");
            if (!Directory.Exists(Path.Combine(Paths.GlobalPlugins.Plugins, "Music")))
            {
                Directory.CreateDirectory(Path.Combine(Paths.GlobalPlugins.Plugins, "Music"));
            }

            if (!Directory.Exists(Path.Combine(Paths.GlobalPlugins.Plugins, "Schematics")))
            {
                Directory.CreateDirectory(Path.Combine(Paths.GlobalPlugins.Plugins, "Schematics"));
            }
        }

        [PluginUnload]
        void OnDisabled()
        {
            Servers.RemoteAdmin -= eventHandler.OnRemoteAdmin;
            eventHandler = null;

            EventManager.UnregisterEvents(this);
            HarmonyPatch.UnpatchAll();
            Singleton = null;
        }
    }
}
