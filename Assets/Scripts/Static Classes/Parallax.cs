using System;
using UnityEngine;
using static Page;

public static class Parallax
{
    const float FAR_CLIP = 64;
    public static float GetParallaxFactor(float zPos)
    {
        return (FAR_CLIP - zPos) / FAR_CLIP;
    }

    public static float UpdateParallaxPosition(CameraStatsSO camStats, SpyStatsSO spyStats, TrainStatsSO trainStats, float parallaxFactor)
    {
        float xPos = camStats.curVelocity.x * (1 - parallaxFactor) * Time.deltaTime;

        if (spyStats.onTrain)
        {
            xPos += trainStats.curVelocity * parallaxFactor * Time.deltaTime;
        }

        return xPos;
    }

    public static float UpdatePositionNoParallax(TrainStatsSO trainStats)
    {
        return trainStats.curVelocity * Time.deltaTime;
    }
}
