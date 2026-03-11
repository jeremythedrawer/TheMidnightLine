using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static NPC;

public class NPCManager : MonoBehaviour
{
    const float WAITING_FOR_SEAT_TICK_RATE = 0.3f;

    public NPCsDataSO npcData;
    public StationsDataSO stationsData;
    public ClipboardStatsSO clipBoardStats;

    [Header("Generated")]
    public bool npcFindingChair;
    public static List<NPCBrain> npcChairList = new List<NPCBrain>();

    private void Awake()
    {
        npcData.npcsToPick = new List<NPCBrain>(npcData.npc_prefab);
        npcData.colorsToPick = new List<Color>(npcData.agentColors);

        CreateNPCTraitors();
        NPC.InitializeDescriptions();
    }
    private void Update()
    {
        if (npcChairList.Count > 0 && npcFindingChair == false)
        {
            SeatingBoardingNPCs().Forget();
        }
    }

    private void CreateNPCTraitors()
    {
        npcData.totalAgentCount = 0;
        for (int i = 0; i < stationsData.stations.Length; i++)
        {
            for (int j = 0; j < stationsData.stations[i].traitorSpawnAmount; j++)
            {
                npcData.totalAgentCount++;
            }
        }
        clipBoardStats.profilePageArray = new ClipboardStatsSO.ProfilePageData[npcData.totalAgentCount];
        int profilePageIndex = 0;
        for (int i = 0; i < stationsData.stations.Length; i++)
        {
            for (int j = 0; j < stationsData.stations[i].traitorSpawnAmount; j++)
            {
                int randNPCIndex = Random.Range(0, npcData.npcsToPick.Count); // pick from list

                NPCBrain npc = Instantiate(npcData.npcsToPick[randNPCIndex], transform.position, Quaternion.identity, transform);
                Color agentColor = npcData.colorsToPick[randNPCIndex];
                npcData.npcsToPick.RemoveAt(randNPCIndex);
                npcData.colorsToPick.RemoveAt(randNPCIndex);
                npc.stats.role = Role.Traitor;

                Behaviours profilePageBehaviours = npc.stats.behaviours;
                Appearence profilePageAppearence = GetRandomAppearence(npc.npc.appearence);
                ClipboardStatsSO.ProfilePageData profilePageData = new ClipboardStatsSO.ProfilePageData { behaviours = profilePageBehaviours, appearence = profilePageAppearence, color = agentColor };
                clipBoardStats.profilePageArray[profilePageIndex] = (profilePageData);

                TraitorData agentData = new TraitorData { traitor_prefab = npc, color = agentColor };
                npcData.agentPool.Enqueue(agentData);
                npc.gameObject.SetActive(false);

                profilePageIndex++;
            }
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
        uint bestIndex = int.MaxValue;

        for (uint i = 0; i < npc.curCarriage.chairData.Length; i++)
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
public class MyManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Static List");

        foreach (NPCBrain item in NPCManager.npcChairList)
        {
            EditorGUILayout.LabelField(item.ToString());
        }
    }
}
#endif
