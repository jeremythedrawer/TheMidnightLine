using System;
using UnityEngine;

public static class Train
{
    public const float KM_TO_MPS = 0.27777777778f;
    public const float CLOSE_TO_STATION_DISTANCE = 0.05f;
    public enum TrainStates
    { 
        Accelerating,
        Decelerating,
        Stopped,
        AtMaxSpeed,
    }
    [Serializable] public struct SeatData
    {
        public float[] xPos;
        public bool[] filled;
    }
    [Serializable] public struct SmokersRoomData
    {
        public float minXPos;
        public float maxXPos;
        public int npcCount;
    }
    [Serializable] public struct DepthSections
    {
        public int min;
        public int max;

        public int frontMin;
        public int frontMax;
        public int backMin;
        public int backMax;

        public int carriageSeat;
    }

    public static float GetVelocity(float kmph)
    {
        return kmph * KM_TO_MPS;
    }
}
