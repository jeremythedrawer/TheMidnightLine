using System;
using UnityEngine;

public static class Train
{
    public const float KM_TO_MPS = 0.27777777778f;
    public const float CLOSE_TO_STOP_VELOCITY = 0.05f;
    public const float TRAIN_WORLD_POS = 300;
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

    public static float KMPHToVelocity(float kmph)
    {
        return kmph * KM_TO_MPS;
    }

    public static float GetBrakeDistance(float velocity, float decelSpeed)
    {
        return (velocity * velocity) / (2f * decelSpeed);
    }

    public static float IncreaseVelocity(float curVelocity, float targetVelocity, float accelSpeed)
    {
        float maxDelta = accelSpeed * Time.fixedDeltaTime;
        return Mathf.MoveTowards(curVelocity, targetVelocity, maxDelta);
    }

    public static float DecreaseVelocity(float curVelocity, float targetVelocity, float initVelocity, float decelSpeed, float targetWorldPos)
    {
        float brakeDistance = GetBrakeDistance(curVelocity, decelSpeed);
        float distToTarget = targetWorldPos - TRAIN_WORLD_POS;
        float absDistToTarget = Mathf.Abs(distToTarget);

        float maxDelta = decelSpeed * Time.fixedDeltaTime;

        if (absDistToTarget < CLOSE_TO_STOP_VELOCITY)
        {
            return 0;
        }
        else if (absDistToTarget < brakeDistance)
        {
            return Mathf.MoveTowards(curVelocity, targetVelocity, maxDelta);
        }
        else
        {
            return Mathf.MoveTowards(curVelocity, initVelocity, maxDelta);
        }
    }
}
