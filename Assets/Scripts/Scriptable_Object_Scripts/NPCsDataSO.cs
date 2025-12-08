using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCs_Data_SO", menuName = "Midnight Line SOs / NPCs Data SO")]
public class NPCsDataSO : ScriptableObject
{
    public NPCBrain[] npcPrefabs;
    internal List<NPCBrain> npcsToPick = new List<NPCBrain>();
    internal Queue<NPCBrain> agentPool = new Queue<NPCBrain>();
    internal Queue<NPCBrain> boardingNPCQueue = new Queue<NPCBrain>();

}
