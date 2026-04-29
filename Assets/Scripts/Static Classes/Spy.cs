using System;
using UnityEngine;

public static class Spy
{
    public enum SpyState
    {
        None,
        Idle,
        Walk,
        Ticket,
        Notepad,
        CarriageMap,
    }

    public enum LocationState
    {
        None,
        Station,
        Carriage,
        Gangway,
    }

    [Serializable] public struct CollisionData
    {
        public Vector2 groundLeft;
        public Vector2 groundRight;

        public Vector2 stepTopLeft;
        public Vector2 stepTopRight;

        public Vector2 stepBottomLeft;
        public Vector2 stepBottomRight;

        public Vector2 wallTopLeft;
        public Vector2 wallTopRight;
        public Vector2 wallBottomLeft;
        public Vector2 wallBottomRight;

        public RaycastHit2D[] leftStepResults;
        public RaycastHit2D[] rightStepResults;
        public ContactFilter2D stepFilter;
    }
}
