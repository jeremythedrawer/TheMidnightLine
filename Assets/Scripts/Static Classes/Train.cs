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
        float maxDelta = accelSpeed * Time.deltaTime;
        return Mathf.MoveTowards(curVelocity, targetVelocity, maxDelta);
    }

    public static float DecreaseVelocity(float curVelocity, float targetVelocity, float initVelocity, float decelSpeed, float targetWorldPos)
    {
        float brakeDistance = GetBrakeDistance(curVelocity, decelSpeed);
        float distToTarget = targetWorldPos - TRAIN_WORLD_POS;
        float absDistToTarget = Mathf.Abs(distToTarget);

        float maxDelta = decelSpeed * Time.deltaTime;

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

    public static Vector2 GetElevateVelocityBezier(Vector2 curVelocity, Vector2 targetPos, ref float metersTravelledOnBezier)
    {
        metersTravelledOnBezier += curVelocity.x * Time.deltaTime;
        float t = metersTravelledOnBezier / targetPos.x;
        Vector2 halfTargPos = targetPos * 0.5f;

        Vector2 p0 = new Vector2(0, 0);
        Vector2 p1 = new Vector2(halfTargPos.x, 0);
        Vector2 p2 = new Vector2(halfTargPos.x, targetPos.y);
        Vector2 p3 = targetPos;

        float tSquared = Mathf.Pow(t, 2);

        float w0 = -3 * tSquared + 6 * t - 3;
        float w1 = 9 * tSquared - 12 * t + 3;
        float w2 = -9 * tSquared + 6 * t;
        float w3 = 3 * tSquared;

        return (p0 * w0) + (p1 * w1) + (p2 * w2) + (p3 * w3);
    }

    public static Vector2 GetEaseInOutBezierPos(float t, Vector2 targetPos)
    {
        Vector2 halfTargPos = targetPos * 0.5f;
        
        Vector2 p0 = new Vector2(0, 0);
        Vector2 p1 = new Vector2(halfTargPos.x, 0);
        Vector2 p2 = new Vector2(halfTargPos.x, targetPos.y);
        Vector2 p3 = targetPos;

        float tSquared = Mathf.Pow(t, 2);
        float tCubed = Mathf.Pow(t, 3);

        float w0 = -t + 3 * tSquared - 3 * t + 1;
        float w1 = 3 * tCubed - 6 * tSquared + 3 * t;
        float w2 = -3 * tCubed + 3 * tSquared;
        float w3 = tCubed;

        return (p0 * w0) + (p1 * w1) + (p2 * w2) + (p3 * tCubed);
    }
}
