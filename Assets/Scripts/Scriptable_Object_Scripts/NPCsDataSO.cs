using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class NPCsDataSO : ScriptableObject
{
    public NPCBrain[] npcPrefabs;

    internal List<NPCBrain> npcsToPick = new List<NPCBrain>();
    internal Queue<NPCBrain> agentPool = new Queue<NPCBrain>();
    internal Queue<NPCBrain> boardingNPCQueue = new Queue<NPCBrain>();


    [Serializable] internal struct AnimHashData
    {
        internal int sittingBlinking;
        internal int sittingBreathing;
        internal int sittingEating;
        internal int sittingSick;
        internal int sittingSleeping;
        internal int smoking;
        internal int standingAboutToEat;
        internal int standingBlinking;
        internal int standingBreathing;
        internal int standingEating;
        internal int standingSick;
        internal int standingSleeping;
        internal int walking;
    }
    internal AnimHashData animHashData;

    [Serializable] internal struct MaterialData
    {
        internal int colorID;
        internal int zPosID;
        internal int mainTexID;
    }
    internal MaterialData materialData;

    private void OnValidate()
    {        
        animHashData.sittingBlinking = Animator.StringToHash("SittingBlinking");
        animHashData.sittingBreathing = Animator.StringToHash("SittingBreathing");
        animHashData.sittingEating = Animator.StringToHash("SittingEating");
        animHashData.sittingSick = Animator.StringToHash("SittingSick");
        animHashData.sittingSleeping = Animator.StringToHash("SittingSleeping");
        animHashData.smoking = Animator.StringToHash("Smoking");
        animHashData.standingAboutToEat = Animator.StringToHash("StandingAboutToEat");
        animHashData.standingBlinking = Animator.StringToHash("StandingBlinking");
        animHashData.standingBreathing = Animator.StringToHash("StandingBreathing");
        animHashData.standingEating = Animator.StringToHash("StandingEating");
        animHashData.standingSick = Animator.StringToHash("StandingSick");
        animHashData.standingSleeping = Animator.StringToHash("StandingSleeping");
        animHashData.walking = Animator.StringToHash("Walking");

        materialData.colorID = Shader.PropertyToID("_Color");
        materialData.zPosID = Shader.PropertyToID("_ZPos");
        materialData.mainTexID = Shader.PropertyToID("_MainTex");
    }

}
