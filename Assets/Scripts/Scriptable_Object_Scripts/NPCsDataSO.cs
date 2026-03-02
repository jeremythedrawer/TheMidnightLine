using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static NPC;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class NPCsDataSO : ScriptableObject
{
    [Header("To Set")]
    public NPCBrain[] npc_prefab;
    public Color[] agentColors;
    public VisualEffect sleepingZs_prefab;
    public VisualEffect musicNotes_prefab;
    public VisualEffect speechBubble_prefab;
    public VisualEffect smoke_prefab;
    [Range(0, 1)] public float hoverColorOffet = 0.25f;
    [Range(0, 1)] public float fadeTime = 0.5f;


    [Header("Generated")]
    public int totalAgentCount;
    public List<Color> colorsToPick;
    public List<NPCBrain> npcsToPick;
    public Queue<AgentData> agentPool = new Queue<AgentData>();


}
