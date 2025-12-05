using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NPC_Data_SO", menuName = "Midnight Line SOs / NPC Data SO")]

public class NPCDataSO : ScriptableObject
{
    public NPCBrain[] npcPrefabs;
    internal List<NPCBrain> npcsToPick;

    private void Awake()
    {
        npcsToPick = npcPrefabs.ToList();
    }

}
