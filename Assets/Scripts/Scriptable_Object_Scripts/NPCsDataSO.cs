using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static NPC;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class NPCsDataSO : ScriptableObject
{
    public NPCBehaviourContextSO[] behaviourContexts;
    [Header("Generated")]
    public Dictionary<Behaviours, string> behaviourDescDict;
    public Dictionary<Behaviours, NPCBehaviourContextSO> behaviourContextDict;
}
