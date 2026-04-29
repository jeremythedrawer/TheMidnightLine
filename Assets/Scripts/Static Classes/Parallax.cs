using System;
using UnityEngine;
using static Spy;
public static class Parallax
{
    public const float FAR_CLIP = 128;

    public enum RepeatType
    {
        OneShot,
        Repeat,
    }
    public static float GetParallaxFactor(float zPos)
    {
        return (FAR_CLIP - zPos) / FAR_CLIP;
    }

    public static float UpdateParallaxPosition(CameraStatsSO camStats, SpyStatsSO spyStats, TrainStatsSO trainStats, float parallaxFactor)
    {
        float xPos = camStats.curVelocity.x * (1 - parallaxFactor) * Time.fixedDeltaTime;

        if (spyStats.curLocationState != LocationState.Station)
        {
            xPos += trainStats.curVelocity * parallaxFactor * Time.fixedDeltaTime;
        }

        return xPos;
    }

    public static float UpdatePositionNoParallax(TrainStatsSO trainStats)
    {
        return trainStats.curVelocity * Time.fixedDeltaTime;
    }
}
