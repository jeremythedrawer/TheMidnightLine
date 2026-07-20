using System;
using UnityEngine;

public static class Spy
{
    public enum SpyState
    {
        None,
        Idle,
        Walk,
        PickingNPCTicketCheck,
        TicketCheck,
        TalkingToAccomplice,
        Notepad,
        CarriageMap,
    }

    public enum LocationState
    {
        None,
        Station,
        Carriage,
        Gangway,
        MeetingRoom,
        Bunker
    }

    [Serializable] public struct CollisionData
    {
        public Vector2 groundLeft;
        public Vector2 groundRight;

        public Vector2 wallLeft;
        public Vector2 wallRight;
    }
}
