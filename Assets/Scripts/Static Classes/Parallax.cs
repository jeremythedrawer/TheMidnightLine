using System;
using UnityEngine;

public static class Parallax
{
    [Serializable] public struct ParallaxData
    {
        public float parallaxFactor;
        public float startXPos;
        public float startTrainDistTravelled;
    }
    public static ParallaxData GetParallaxData(CameraStatsSO camStats, TrainStatsSO trainStats, Transform transform)
    {
        ParallaxData parallaxData = new ParallaxData();
        parallaxData.parallaxFactor = Mathf.Abs(transform.position.z - camStats.worldFarClipPlane) / camStats.worldFarClipPlane;
        parallaxData.startXPos = transform.position.x;
        parallaxData.startTrainDistTravelled = trainStats.metersTravelled;
        return parallaxData;
    }

    public static float UpdateParallax(TrainStatsSO trainStats, ParallaxData parallaxData)
    {
        return parallaxData.startXPos - (trainStats.metersTravelled - parallaxData.startTrainDistTravelled) * parallaxData.parallaxFactor;
    }
}
