using System;
using UnityEngine;

public static class Utils
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

    public static NPCBrain.Behaviours GetBehaviours()
    {
        int numBehaviours = Enum.GetValues(typeof(NPCBrain.Behaviours)).Length - 1;

        int firstBehave = 1 << UnityEngine.Random.Range(0, numBehaviours);

        int secondBehave;
        do
        {
            secondBehave = 1 << (UnityEngine.Random.Range(0, numBehaviours));
        }
        while (secondBehave == firstBehave);

        return (NPCBrain.Behaviours)(firstBehave | secondBehave);
    }
}
