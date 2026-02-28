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

    [Header("To Set")]
    public NPCBrain[] npcPrefabs;
    public Color[] agentColors;
    public AtlasSpawner sleepingZs;
    public AtlasSpawner musicNotes;
    public AtlasSpawner speechBubble;
    public AtlasSpawner smoke;
    [Range(0, 1)] public float hoverColorOffet = 0.25f;
    [Range(0, 1)] public float fadeTime = 0.5f;


    [Header("Runtime")]
    public int totalAgentCount;
    public List<Color> colorsToPick;
    public List<NPCBrain> npcsToPick;
    public Queue<AgentData> agentPool = new Queue<AgentData>();
}
