using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public NPCsDataSO npcData;
        public StationsDataSO stationsData;
        public ClipboardStatsSO clipBoardStats;
    }
    [SerializeField] SOData soData;

    [SerializeField] float waitingForSeatTickRate = 0.3f;
    bool npcFindingChair;

    internal static List<NPCBrain> npcChairList = new List<NPCBrain>();

    private void Awake()
    {
        soData.npcData.totalAgentCount = 0;
        for (int i = 0; i < soData.stationsData.stations.Length; i++)
        {
            for (int j = 0; j < soData.stationsData.stations[i].agentSpawnAmount; j++)
            {
                soData.npcData.totalAgentCount++;
            }
        }

        soData.npcData.animHashData.sittingAboutToEat = Animator.StringToHash("SittingAboutToEat");
        soData.npcData.animHashData.sittingBlinking = Animator.StringToHash("SittingBlinking");
        soData.npcData.animHashData.sittingBreathing = Animator.StringToHash("SittingBreathing");
        soData.npcData.animHashData.sittingEating = Animator.StringToHash("SittingEating");
        soData.npcData.animHashData.sittingSick = Animator.StringToHash("SittingSick");
        soData.npcData.animHashData.sittingSleeping = Animator.StringToHash("SittingSleeping");
        soData.npcData.animHashData.smoking = Animator.StringToHash("Smoking");
        soData.npcData.animHashData.standingAboutToEat = Animator.StringToHash("StandingAboutToEat");
        soData.npcData.animHashData.standingBlinking = Animator.StringToHash("StandingBlinking");
        soData.npcData.animHashData.standingBreathing = Animator.StringToHash("StandingBreathing");
        soData.npcData.animHashData.standingEating = Animator.StringToHash("StandingEating");
        soData.npcData.animHashData.standingSick = Animator.StringToHash("StandingSick");
        soData.npcData.animHashData.standingSleeping = Animator.StringToHash("StandingSleeping");
        soData.npcData.animHashData.walking = Animator.StringToHash("Walking");

        soData.npcData.materialData.colorID = Shader.PropertyToID("_Color");
        soData.npcData.materialData.zPosID = Shader.PropertyToID("_ZPos");
        soData.npcData.materialData.alphaID = Shader.PropertyToID("_Alpha");
        soData.npcData.materialData.mainTexID = Shader.PropertyToID("_MainTex");
    }

    private void Start()
    {
        soData.npcData.npcsToPick = new List<NPCBrain>(soData.npcData.npcPrefabs);
        soData.npcData.colorsToPick = new List<Color>(soData.npcData.agentColors);
        CreateNPCAgents();
    }
    private void Update()
    {
        if (npcChairList.Count > 0 && npcFindingChair == false)
        {
            SeatingBoardingNPCs().Forget();
        }
    }

    private void CreateNPCAgents()
    {
        soData.clipBoardStats.profilePageArray = new ClipboardStatsSO.ProfilePageData[soData.npcData.totalAgentCount];
        int profilePageIndex = 0;
        for (int i = 0; i < soData.stationsData.stations.Length; i++)
        {
            for (int j = 0; j < soData.stationsData.stations[i].agentSpawnAmount; j++)
            {
                int randNPCIndex = UnityEngine.Random.Range(0, soData.npcData.npcsToPick.Count); // pick from list

                NPCBrain npc = Instantiate(soData.npcData.npcsToPick[randNPCIndex], transform.position, Quaternion.identity, transform);
                Color agentColor = soData.npcData.colorsToPick[randNPCIndex];
                soData.npcData.npcsToPick.RemoveAt(randNPCIndex);
                soData.npcData.colorsToPick.RemoveAt(randNPCIndex);
                npc.stats.type = NPCBrain.Type.Agent;

                NPCTraits.Behaviours profilePageBehaviours = npc.stats.behaviours;
                NPCTraits.Appearence profilePageAppearence = NPCTraits.GetRandomAppearence(npc.soData.npc.appearence);
                ClipboardStatsSO.ProfilePageData profilePageData = new ClipboardStatsSO.ProfilePageData { behaviours = profilePageBehaviours, appearence = profilePageAppearence, color = agentColor };
                soData.clipBoardStats.profilePageArray[profilePageIndex] = (profilePageData);

                NPCsDataSO.AgentData agentData = new NPCsDataSO.AgentData { agent = npc, color = agentColor };
                soData.npcData.agentPool.Enqueue(agentData);
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
        await UniTask.WaitForSeconds(waitingForSeatTickRate);
        npcFindingChair = false;
    }

    public void AssignNextNPCPosition(NPCBrain npc)
    {

        float npcX = npc.transform.position.x;
        float closestDist = float.PositiveInfinity;
        int bestIndex = -1;

        for (int i = 0; i < npc.componentData.curCarriage.chairData.Length; i++)
        {
            if (npc.componentData.curCarriage.chairData[i].filled) continue;

            float dist = Mathf.Abs(npcX - npc.componentData.curCarriage.chairData[i].xPos);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestIndex = i;
            }
        }

        if (bestIndex == -1)
        {
            npc.FindStandingPosition();
        }
        else
        {
            npc.componentData.curCarriage.chairData[bestIndex].filled = true;
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
