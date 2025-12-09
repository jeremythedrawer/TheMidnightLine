using System;
using UnityEngine;

public static class AnimationUtilities
{
    public static void SetAnimationEvent(AnimationClip clip, string inputFunction, float? inputTime = null)
    {
        AnimationEvent animationEvent = new AnimationEvent
        {
            time = inputTime ?? clip.length,
            functionName = inputFunction
        };
        clip.AddEvent(animationEvent);
    }
}
