using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;


#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

[CreateAssetMenu(fileName = "NPCSO", menuName = "Midnight Line SOs / NPC SO")]
public class NPCSO : ScriptableObject
{
    [Header("Components")]
    public AnimatorOverrideController overrideAnimationController;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundAccelation = 12f;

    [Header("Station")]
    public float maxDistanceDetection = 6.0f;

    public NPCTraits.Appearence appearence;
    public NPCTraits.Behaviours behaviours;

    [Header("Difficulty")]
    public Vector2 pickBehaviourDurationRange = new Vector2(10, 30);
   
    [Serializable] public struct AnimEventPosData
    {
        public Vector2 position;
        public float time;
    }
    public AnimEventPosData[] smokeAnimPosData;

    [Serializable] public struct AnimStateClipPair
    {
        public int stateHash;
        public AnimationClip clip;
    }
    [Header("Baked Animation Data")]
    [SerializeField] private AnimStateClipPair[] animClips;

    public Dictionary<int, AnimationClip> animClipDict;

    private void OnEnable()
    {
        BuildRuntimeDictionary();
    }
    public void BuildRuntimeDictionary()
    {
        animClipDict = new Dictionary<int, AnimationClip>();

        if (animClips == null) return;

        foreach (AnimStateClipPair pair in animClips)
        {
            if (pair.clip == null) continue;

            if (!animClipDict.ContainsKey(pair.stateHash)) animClipDict.Add(pair.stateHash, pair.clip);
        }
    }
#if UNITY_EDITOR
    public void BakeAnimationData()
    {
        if (overrideAnimationController == null)
        {
            Debug.LogWarning($"{name}: No AnimatorOverrideController assigned.");
            return;
        }

        var overridePairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideAnimationController.GetOverrides(overridePairs);

        var baseController =
            overrideAnimationController.runtimeAnimatorController as AnimatorController;

        if (baseController == null)
        {
            Debug.LogError($"{name}: Runtime controller is not an AnimatorController.");
            return;
        }

        var bakedList = new List<AnimStateClipPair>();

        foreach (var childState in baseController.layers[0].stateMachine.states)
        {
            var baseClip = childState.state.motion as AnimationClip;
            if (baseClip == null)
                continue;

            AnimationClip finalClip = baseClip;

            foreach (var pair in overridePairs)
            {
                if (pair.Key == baseClip && pair.Value != null)
                {
                    finalClip = pair.Value;
                    break;
                }
            }

            bakedList.Add(new AnimStateClipPair
            {
                stateHash = childState.state.nameHash,
                clip = finalClip
            });
        }

        animClips = bakedList.ToArray();
        EditorUtility.SetDirty(this);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBakeAllNPCSOs()
    {
        NPCSO[] all = Resources.FindObjectsOfTypeAll<NPCSO>();
        foreach (NPCSO so in all)
        {
            so.BakeAnimationData();
        }
    }
#endif
}
