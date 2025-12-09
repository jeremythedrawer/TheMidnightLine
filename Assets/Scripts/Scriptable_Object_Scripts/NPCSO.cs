using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

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
    public AnimatorOverrideController animationOverrideController;
    public Dictionary<int, AnimationClip> animClipDict = new Dictionary<int, AnimationClip>();

    private void OnValidate()
    {
        AnimatorController editAnimController = animationOverrideController.runtimeAnimatorController as AnimatorController;

        foreach (ChildAnimatorState childState in editAnimController.layers[0].stateMachine.states)
        {
            int stateHash = childState.state.nameHash;
            AnimationClip clip = (AnimationClip)childState.state.motion;

            if (!animClipDict.ContainsKey(stateHash))
            {
                animClipDict.Add(stateHash, clip);
            }
        }
    }
}
