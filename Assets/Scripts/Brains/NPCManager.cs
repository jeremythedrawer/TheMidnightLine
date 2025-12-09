using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.OnScreen.OnScreenStick;

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

    private void Awake()
    {
        soData.npcData.npcsToPick = new List<NPCBrain>(soData.npcData.npcPrefabs);
    }
    private void Start()
    {
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
                soData.npcData.npcsToPick.RemoveAt(randNPCIndex);
                npc.stats.type = NPCBrain.Type.Agent;

                NPCTraits.Behaviours profilePageBehaviours = npc.stats.behaviours;
                NPCTraits.Appearence profilePageAppearence = NPCTraits.GetRandomAppearence(npc.soData.npc.appearence);
                ClipboardStatsSO.ProfilePageData profilePageData = new ClipboardStatsSO.ProfilePageData { behaviours = profilePageBehaviours, appearence = profilePageAppearence };
                soData.clipBoardStats.profilePageData.Add(profilePageData);

                Debug.Log(profilePageAppearence.ToString() + " | " + profilePageBehaviours.ToString() + " | " + npc.gameObject.name);
                soData.npcData.agentPool.Enqueue(npc);
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
