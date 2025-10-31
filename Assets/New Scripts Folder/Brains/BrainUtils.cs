using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public static class BrainUtils
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
