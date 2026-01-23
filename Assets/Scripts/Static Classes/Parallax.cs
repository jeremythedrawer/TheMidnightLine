using System;
using UnityEngine;

public static class Parallax
{
    [Serializable] public struct ParallaxData
    {
        public float parallaxFactor;
    }
    public static ParallaxData GetParallaxData(CameraStatsSO camStats, TrainStatsSO trainStats, Transform transform)
    {
        ParallaxData parallaxData = new ParallaxData();
        parallaxData.parallaxFactor = Mathf.Abs(transform.position.z - camStats.worldFarClipPlane) / camStats.worldFarClipPlane;
        return parallaxData;
    }

    public static float UpdateParallax(TrainStatsSO trainStats, ParallaxData parallaxData)
    {
        return trainStats.curVelocity * parallaxData.parallaxFactor;
    }
}
