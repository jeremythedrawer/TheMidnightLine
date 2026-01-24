using System;
using UnityEngine;

public static class Parallax
{
    public static float GetParallaxFactor(CameraStatsSO camStats, TrainStatsSO trainStats, float zPos)
    {
        return Mathf.Abs(zPos - camStats.worldFarClipPlane) / camStats.worldFarClipPlane;
    }

    public static float UpdateParallaxPosition(TrainStatsSO trainStats, float parallaxFactor)
    {
        return trainStats.curVelocity * parallaxFactor;
    }
}
