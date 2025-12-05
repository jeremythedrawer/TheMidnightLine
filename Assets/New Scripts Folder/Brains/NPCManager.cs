using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public NPCDataSO npcData;
        public TrainSettingsSO trainSettings;
    }
    [SerializeField] SOData soData;

    [SerializeField] float waitingForSeatTickRate = 0.3f;
    public static Queue<NPCBrain> boardingNPCQueue = new Queue<NPCBrain>();
    public static Queue<NPCBrain> agentNPCPool = new Queue<NPCBrain>();
    bool npcFindingChair;

    private void Start()
    {
        CreateNPCAgents();
    }
    private void Update()
    {
        if (boardingNPCQueue.Count > 0 && npcFindingChair == false)
        {
            SeatingBoardingNPCs().Forget();
        }
    }

    private void CreateNPCAgents()
    {
        for (int i = 0; i < soData.trainSettings.stations.Length; i++)
        {
            for (int j = 0; j < soData.trainSettings.stations[i].agentSpawnAmount; j++)
            {
                int randNPCIndex = UnityEngine.Random.Range(0, soData.npcData.npcsToPick.Count - 1); // pick from list
                NPCBrain npc = Instantiate(soData.npcData.npcsToPick[randNPCIndex], transform.position, Quaternion.identity, transform);
                soData.npcData.npcsToPick.RemoveAt(randNPCIndex);
                npc.stats.type = NPCBrain.Type.Agent;

                Behaviours profilePageBehaviours = npc.stats.behaviours;
                Appearence profilePageAppearence = npc.soData.settings.appearence;
                agentNPCPool.Enqueue(npc);
                npc.gameObject.SetActive(false);
            }
        }
    }
    private async UniTask SeatingBoardingNPCs()
    {
        npcFindingChair = true;
        NPCBrain curNPC = boardingNPCQueue.Peek();
        curNPC.FindCarriageChair();
        boardingNPCQueue.Dequeue();
        await UniTask.WaitForSeconds(waitingForSeatTickRate);
        npcFindingChair = false;
    }

}
