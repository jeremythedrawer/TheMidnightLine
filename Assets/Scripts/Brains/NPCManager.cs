using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
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

    private void Start()
    {
        soData.npcData.npcsToPick = new List<NPCBrain>(soData.npcData.npcPrefabs);
        soData.npcData.colorsToPick = new List<Color>(soData.npcData.agentColors);
        CreateNPCAgents();
    }
    private void Update()
    {
        if (soData.npcData.boardingNPCQueue.Count > 0 && npcFindingChair == false)
        {
            SeatingBoardingNPCs().Forget();
        }
    }

    private void CreateNPCAgents()
    {
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
                soData.clipBoardStats.profilePageList.Add(profilePageData);

                NPCsDataSO.AgentData agentData = new NPCsDataSO.AgentData { agent = npc, color = agentColor };
                soData.npcData.agentPool.Enqueue(agentData);
                npc.gameObject.SetActive(false);
            }
        }
    }
    private async UniTask SeatingBoardingNPCs()
    {
        npcFindingChair = true;
        NPCBrain curNPC = soData.npcData.boardingNPCQueue.Dequeue();
        curNPC.FindCarriageChair();
        await UniTask.WaitForSeconds(waitingForSeatTickRate);
        npcFindingChair = false;
    }

}
