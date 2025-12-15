using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class NPCsDataSO : ScriptableObject
{
    public NPCBrain[] npcPrefabs;
    public Color[] agentColors;
    public VisualEffect sleepingZs;
    public GameObject smoke;
    [Range(0, 1)] public float hoverColorOffet = 0.25f;
    [Range(0, 1)] public float fadeTime = 0.5f;

    [Serializable] public struct AgentData
    {
        public NPCBrain agent;
        public Color color;
    }
    internal int totalAgentCount;
    internal List<Color> colorsToPick;
    internal List<NPCBrain> npcsToPick;
    internal Queue<AgentData> agentPool = new Queue<AgentData>();
    internal Queue<NPCBrain> boardingNPCQueue = new Queue<NPCBrain>();

    [Serializable] internal struct AnimHashData
    {
        internal int sittingAboutToEat;
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
        internal int alphaID;
        internal int mainTexID;
    }
    internal MaterialData materialData;
}
