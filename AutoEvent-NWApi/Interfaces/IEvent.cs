﻿namespace AutoEvent.Interfaces
{
    public interface IEvent
    {
        string Name { get; }
        string Description { get; }
        string Author { get; }
        string MapName { get; }
        string CommandName { get; }
        void OnStart();
        void OnStop();
        bool IsRoundDone();
    }
}
