using System;
using UnityEngine;

public static class Animations
{
    public static void SetAnimationEvent(AnimationClip clip, string inputFunction, float? inputTime = null, int? intParam = null)
    {
        AnimationEvent animationEvent = new AnimationEvent
        {
            time = inputTime ?? clip.length,
            intParameter = intParam ?? 0,
            functionName = inputFunction
        };
        clip.AddEvent(animationEvent);
    }
}
