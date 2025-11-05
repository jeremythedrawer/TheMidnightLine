using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    [SerializeField] float waitingForSeatTickRate = 0.3f;

    public static Queue<NPCBrain> boardingNPCQueue = new Queue<NPCBrain>();

    bool npcFindingChair;
    private void Update()
    {
        if (boardingNPCQueue.Count > 0 && npcFindingChair == false)
        {
            SeatingBoardingNPCs().Forget();
        }
    }

    private async UniTaskVoid SeatingBoardingNPCs()
    {
        npcFindingChair = true;
        NPCBrain curNPC = boardingNPCQueue.Peek();
        curNPC.FindCarriageChair();
        boardingNPCQueue.Dequeue();
        await UniTask.WaitForSeconds(waitingForSeatTickRate);
        npcFindingChair = false;
    }

}
