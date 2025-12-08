using System;
using UnityEngine;

public static class Parallax
{
    [Serializable] public struct ParallaxData
    {
        internal float parallaxFactor;
        internal float initXPos;
        internal float startTrainDistTravelled;
    }
    public static ParallaxData GetParallaxData(CameraStatsSO camStats, TrainStatsSO trainStats, Transform transform)
    {
        ParallaxData parallaxData = new ParallaxData();
        float clipPlaneZ = camStats.curWorldPos.z + camStats.farClipPlane;
        parallaxData.parallaxFactor = Mathf.Abs(transform.position.z - clipPlaneZ) / clipPlaneZ;
        parallaxData.initXPos = transform.position.x;
        parallaxData.startTrainDistTravelled = trainStats.metersTravelled;
        return parallaxData;
    }

    public static float UpdateParallax(TrainStatsSO trainStats, ParallaxData parallaxData)
    {
        return parallaxData.initXPos - (trainStats.metersTravelled - parallaxData.startTrainDistTravelled) * parallaxData.parallaxFactor;
    }
}
