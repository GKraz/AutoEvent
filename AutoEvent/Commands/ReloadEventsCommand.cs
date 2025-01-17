﻿// <copyright file="Log.cs" company="Redforce04#4091">
// Copyright (c) Redforce04. All rights reserved.
// </copyright>
// -----------------------------------------
//    Solution:         AutoEvent
//    Project:          AutoEvent
//    FileName:         ReloadEventsCommand.cs
//    Author:           Redforce04#4091
//    Revision Date:    09/03/2023 7:44 PM
//    Created Date:     09/03/2023 7:44 PM
// -----------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AutoEvent.Interfaces;
using CommandSystem;
using Exiled.Permissions.Extensions;
using PluginAPI.Core;

namespace AutoEvent.Commands;

public class Reload : ICommand
{
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var config = AutoEvent.Singleton.Config;
        var player = Player.Get(sender);

        if (!((CommandSender)sender).CheckPermission("ev.run"))
        {
            response = "<color=red>You do not have permission to use this command!</color>";
            return false;
        }

        if (AutoEvent.ActiveEvent != null)
        {
            response = $"<color=red>The mini-game {AutoEvent.ActiveEvent.Name} is currently running!</color>";
            return false;
        }

        Event.Events = new List<Event>();
        Event.RegisterEvents();
        Loader.LoadEvents();
        Event.Events.AddRange(Loader.Events);
        
        response = $"Reloaded Events. {Event.Events.Count} events have been found.";
        return true;
    }

    public string Command { get; } = "Reload";
    public string[] Aliases { get; } = Array.Empty<string>();
    public string Description { get; } = "Reloads events.";
}