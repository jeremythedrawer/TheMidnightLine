using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static NPC;

public class NPCManager : MonoBehaviour
{
    const float WAITING_FOR_SEAT_TICK_RATE = 0.3f;

    public TripSO trip;

    public NPCsDataSO npcsData;

    [Header("Generated")]
    public bool npcFindingChair;
    public int totalAgentCount;


    public static List<NPCBrain> npcChairList = new List<NPCBrain>();


    private void Awake()
    {
    }
    private void Update()
    {
        if (npcChairList.Count > 0 && npcFindingChair == false)
        {
            SeatingBoardingNPCs().Forget();
        }
    }
    private async UniTask SeatingBoardingNPCs()
    {
        npcFindingChair = true;
        NPCBrain curNPC = npcChairList[0];
        npcChairList.RemoveAt(0);
        AssignNextNPCPosition(curNPC);
        await UniTask.WaitForSeconds(WAITING_FOR_SEAT_TICK_RATE);
        npcFindingChair = false;
    }
    public void AssignNextNPCPosition(NPCBrain npc)
    {
        float npcX = npc.transform.position.x;
        float closestDist = float.PositiveInfinity;
        int bestIndex = int.MaxValue;

        for (int i = 0; i < npc.curCarriage.chairData.Length; i++)
        {
            if (npc.curCarriage.chairData[i].filled) continue;

            float dist = Mathf.Abs(npcX - npc.curCarriage.chairData[i].xPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestIndex = i;
            }
        }

        if (bestIndex == int.MaxValue)
        {
            npc.FindStandingPosition();
        }
        else
        {
            npc.AssignChair(bestIndex);
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(NPCManager))]
public class NPCManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Chair List");

        foreach (NPCBrain item in NPCManager.npcChairList)
        {
            EditorGUILayout.LabelField(item.ToString());
        }
    }
}
#endif
