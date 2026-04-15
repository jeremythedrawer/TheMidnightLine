using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static NPC;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class NPCsDataSO : ScriptableObject
{
    [Header("To Set")]
    public VisualEffect sleepingZs_prefab;
    public VisualEffect musicNotes_prefab;
    public VisualEffect speechBubble_prefab;
    public VisualEffect smoke_prefab;
    [Range(0, 1)] public float hoverColorOffet = 0.25f;
    [Range(0, 1)] public float fadeTime = 0.5f;

    [Header("Generated")]
    public Dictionary<Behaviours, string> behaviourDescDict;
}
