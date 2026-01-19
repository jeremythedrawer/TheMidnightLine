using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class NPCsDataSO : ScriptableObject
{
    [Serializable] public struct AgentData
    {
        public NPCBrain agent;
        public Color color;
    }
    [Serializable] public struct AnimHashData
    {
        public int sittingAboutToEat;
        public int sittingAboutToRead;
        public int sittingBlinking;
        public int sittingBreathing;
        public int sittingCalling;
        public int sittingEating;
        public int sittingMusic;
        public int sittingReading;
        public int sittingSick;
        public int sittingSleeping;
        public int smoking;
        public int standingAboutToEat;
        public int standingBlinking;
        public int standingBreathing;
        public int standingCalling;
        public int standingEating;
        public int standingMusic;
        public int standingReading;
        public int standingSick;
        public int standingSleeping;
        public int walking;
    }

    [Header("To Set")]
    public NPCBrain[] npcPrefabs;
    public Color[] agentColors;
    public VisualEffect sleepingZs;
    public VisualEffect musicNotes;
    public VisualEffect speechBubble;
    public GameObject smoke;
    [Range(0, 1)] public float hoverColorOffet = 0.25f;
    [Range(0, 1)] public float fadeTime = 0.5f;


    [Header("Runtime")]
    public int totalAgentCount;
    public List<Color> colorsToPick;
    public List<NPCBrain> npcsToPick;
    public Queue<AgentData> agentPool = new Queue<AgentData>();

    public AnimHashData animHashData;
}
