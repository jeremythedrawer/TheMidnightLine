using System;
using UnityEngine;

public static class Spy
{
    public enum SpyState
    {
        None,
        Idle,
        Walk,
        TalkingToPassenger,
        Notepad,
        CarriageMap,
        Focus,
    }
    [Flags] public enum SpySubState
    {
        None = 0,
        CheckingCarriageMap = 1 << 0,
        IsFocusing = 1 << 1,
        IsTalkingToPassenger = 1 << 2,
    }

    public enum TargetType
    { 
        None,
        Passenger,
        SlideDoors,
        Gangway,
        CarriageMap,
    }


    public enum LocationState
    {
        None,
        Station,
        Carriage,
        Gangway,
        Menu,
        Title,
    }

    [Serializable] public struct CollisionData
    {
        public Vector2 groundLeft;
        public Vector2 groundRight;

        public Vector2 wallLeft;
        public Vector2 wallRight;
    }
}
