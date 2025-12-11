using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    //Settings

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    public float groundAccelation = 12f;
    public float groundDecelaration = 14f;

    [Header("Station")]
    public float maxDistanceDetection = 6.0f;

    [Header("Appearence")]
    public NPCTraits.Appearence appearence;

    [Header("Components")]
    public AnimatorOverrideController overrideAnimationController;

    public Dictionary<int, AnimationClip> animClipDict = new Dictionary<int, AnimationClip>();
    private void SetAnimationEventDictionary()
    {
        List<KeyValuePair<AnimationClip, AnimationClip>> overrideClipPairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideAnimationController.GetOverrides(overrideClipPairs);

        animClipDict.Clear();

        AnimatorController baseAnimController = overrideAnimationController.runtimeAnimatorController as AnimatorController;

        foreach (ChildAnimatorState childState in baseAnimController.layers[0].stateMachine.states)
        {
            AnimationClip baseClip = (AnimationClip)childState.state.motion;
            if (baseClip == null) continue;

            AnimationClip overrideClip = baseClip;

            foreach (KeyValuePair<AnimationClip, AnimationClip> overrideClipPair in overrideClipPairs)
            {
                if (overrideClipPair.Key == baseClip)
                {
                    overrideClip = overrideClipPair.Value;
                    break;
                }
            }

            int stateHash = childState.state.nameHash;

            if (!animClipDict.ContainsKey(stateHash))
            {
                animClipDict.Add(stateHash, overrideClip);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitAllNPCSO()
    {
        // Find all NPCSO assets in Resources and update their dictionaries
        NPCSO[] allSO = Resources.FindObjectsOfTypeAll<NPCSO>();
        foreach (var so in allSO)
        {
            so.SetAnimationEventDictionary();
        }
    }
}
