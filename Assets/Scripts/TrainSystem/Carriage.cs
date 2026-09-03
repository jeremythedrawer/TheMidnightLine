using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using static Passenger;
using static Train;

public class Carriage : MonoBehaviour
{
    const float QUEUE_TICK_RATE = 0.3f;
    public AtlasRenderer[] exteriorRenderers;
    public AtlasRenderer[] seatRenderers;
    public AtlasRenderer[] grapPoleRenderers;

    public TrainData trainData;
    public TripData trip;
    public LayerData layerSettings;
    public AtlasSO graffitiAtlas;

    public AtlasTextRenderer nextStationSignRenderer;

    public AtlasRenderer carriageWallRenderer;

    public BoxCollider2D insideBoundsCollider;
    public BoxCollider2D[] smokingRoomColliders;

    public SlideDoors[] exteriorSlideDoors;
    public SlideDoors[] interiorSlideDoors;
    
    public CarriageMapProp[] maps;

    public RenderTexture graffitiRT;

    [Header("Generated")]
    public Transform[] wheelTransforms;
    
    public CancellationTokenSource ctsFade;

    public List<PassengerBrain> curNPCList;
    public PassengerBrain firstNPC;

    public SeatData seatData;
    
    public SmokersRoomData[] smokersRoomData;

    public NPCQueue seatQueue;
    
    public Bounds totalBounds;
    
    public int seatAmount;
    public int graffitiKernel;
    public int threadGroupX;
    public int threadGroupY;
    public int graffitiCount;

    public float wheelCircumference;
    public float alpha;
    public float prevMeters;

    private void Start()
    {
        curNPCList = new List<PassengerBrain>();

        for(int i = 0; i < exteriorSlideDoors.Length; i++)
        {
            exteriorSlideDoors[i].carriage = this;
            interiorSlideDoors[i].carriage = this;
        }
    }

    private void Update()
    {
        ProcessSeatQueue();
    }
    public void UnlockInteriorDoors()
    {
        for (int i = 0; i < interiorSlideDoors.Length; i++)
        {
            interiorSlideDoors[i].UnlockDoors();
        }
    }
    public void UnlockExteriorSlideDoors()
    {
        for (int i = 0; i < exteriorSlideDoors.Length; i++)
        {
            exteriorSlideDoors[i].UnlockDoors();
        }
    }
    public void CloseInteriorSlideDoors()
    {
        for (int i = 0; i < interiorSlideDoors.Length; i++)
        {
            interiorSlideDoors[i].CloseDoors();
        }
    }
    public void CloseExteriorSlideDoors()
    {
        for (int i = 0; i < exteriorSlideDoors.Length; i++)
        {
            exteriorSlideDoors[i].CloseDoors();
        }
    }
    public void SetSignToNextStation(string stationName)
    {
        string text = (trainData.curStationIndex < trip.stationsDataArray.Length ? "Next Station is " : "Terminating at ") + stationName;
        nextStationSignRenderer.SetText(text);
    }
    public void SetSignToCurrentStation(string stationName)
    {
        nextStationSignRenderer.SetText(stationName);
    }
    public void MoveUp()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        MovingUp().Forget();

    }
    public void MoveDown()
    {
        ctsFade?.Cancel();
        ctsFade?.Dispose();

        ctsFade = new CancellationTokenSource();

        MovingDown().Forget();
    }
    public void AddToSeatQueue(PassengerBrain npc)
    {
        if (seatQueue.npcsCount == seatAmount)
        {
            npc.FindStandingPosition();
            return;
        }
        npc.seatQueueIndex = seatQueue.npcsCount;
        seatQueue.npcs[seatQueue.npcsCount] = npc;
        seatQueue.npcsCount++;
    }
    public void RemoveFromSeatQueue(PassengerBrain npc)
    {
        if (seatQueue.npcsCount == 0) return;
        int lastIndex = seatQueue.npcsCount - 1;

        seatQueue.npcs[npc.seatQueueIndex] = seatQueue.npcs[lastIndex];
        seatQueue.npcs[lastIndex] = npc;
        seatQueue.npcsCount--;
    }
    private void ProcessSeatQueue()
    {
        if (seatQueue.npcsCount == 0) return;

        seatQueue.timer += Time.deltaTime;
        if (seatQueue.timer < QUEUE_TICK_RATE) return;

        PassengerBrain npc = seatQueue.npcs[seatQueue.npcsCount - 1];

        if (npc.seatPosIndex != int.MaxValue) return;

        float npcX = npc.transform.position.x;
        float closestDist = float.PositiveInfinity;
        int bestIndex = int.MaxValue;

        for (int i = 0; i < seatAmount; i++)
        {
            if (seatData.filled[i]) continue;
            float seatPosX = seatData.xPos[i];

            if (npc == firstNPC && seatPosX > insideBoundsCollider.bounds.center.x) continue;

            float dist = Mathf.Abs(npcX - seatPosX);
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
            npc.AssignSeat(bestIndex);
        }

        seatQueue.npcsCount--;
        seatQueue.timer = 0;
    }
    public void SetSmokerRoomData(float offset)
    {
        smokersRoomData = new SmokersRoomData[smokingRoomColliders.Length];

        for (int i = 0; i < smokersRoomData.Length; i++)
        {
            smokersRoomData[i].minXPos = smokingRoomColliders[i].bounds.min.x + offset;
            smokersRoomData[i].maxXPos = smokingRoomColliders[i].bounds.max.x + offset;
        }
    }
    public void SetSeatData(float offset)
    {
        AtlasRenderer seatRenderer = seatRenderers[0];
        float tileWidth = seatRenderer.atlas.slicedSprites[seatRenderer.spriteIndex].worldSlices.x;

        seatAmount = 0;
        int[] seatsPerRenderer = new int[seatRenderers.Length];
        for (int i = 0; i < seatRenderers.Length; i++)
        {
            AtlasRenderer seat = seatRenderers[i];

            float totalWidth = seat.bounds.size.x;
            int seats = Mathf.RoundToInt(totalWidth / tileWidth);
            seatAmount += seats;
            seatsPerRenderer[i] = seats;
        }
        seatData.xPos = new float[seatAmount];
        seatData.filled = new bool[seatAmount];
        int seatIndex = 0;

        for (int i = 0; i < seatRenderers.Length; i++)
        {
            AtlasRenderer seat = seatRenderers[i];
            float firstSeatPos = seat.transform.position.x + (tileWidth * 0.25f);

            for (int j = 0; j < seatsPerRenderer[i]; j++)
            {
                seatData.xPos[seatIndex] = (firstSeatPos + (tileWidth * j)) + offset;
                seatIndex++;
            }
        }
        seatQueue = new NPCQueue();
        seatQueue.npcs = new PassengerBrain[seatAmount];
    }
    public void SetTotalBounds(float offset)
    {
        totalBounds = insideBoundsCollider.bounds;

        for (int i = 0; i < smokingRoomColliders.Length; i++)
        {
            totalBounds.Encapsulate(smokingRoomColliders[i].bounds);
        }
        totalBounds.center = new Vector3(totalBounds.center.x + offset, totalBounds.center.y, totalBounds.center.z);
    }
    public void AddNPC(PassengerBrain npc)
    {
        if (firstNPC == null && npc.role != Role.Accomplice) firstNPC = npc;

        curNPCList.Add(npc);
    }
    public void RemoveNPC(PassengerBrain npc)
    {
        curNPCList.Remove(npc);

        if (npc == firstNPC) firstNPC = null;
    }
    private async UniTask MovingDown()
    {
        float elaspedTime = alpha * trainData.exteriorWallFadeTime;
        try
        {
            while (elaspedTime < trainData.exteriorWallFadeTime)
            {
                elaspedTime += Time.deltaTime;

                alpha = elaspedTime / trainData.exteriorWallFadeTime;
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f; 
                for (int i = 0; i < exteriorRenderers.Length; i++)
                {
                    exteriorRenderers[i].custom.z = alpha;
                }

                await UniTask.Yield(cancellationToken: ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private async UniTask MovingUp()
    {
        float elaspedTime = alpha * trainData.exteriorWallFadeTime;
        try
        {
            while (elaspedTime > 0)
            {
                elaspedTime -= Time.deltaTime;

                alpha = elaspedTime / trainData.exteriorWallFadeTime;
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f;
                for (int i = 0; i < exteriorRenderers.Length; i++)
                {
                    exteriorRenderers[i].custom.z = alpha;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (seatRenderers.Length < 0 || seatData.xPos.Length < 0) return;

        AtlasRenderer seatRenderer = seatRenderers[0];
        float yPos = seatRenderer.bounds.center.y;
        float zPos = seatRenderer.bounds.center.z;
        float ySize = seatRenderer.bounds.size.y;
        float tileWidth = seatRenderer.atlas.slicedSprites[seatRenderer.spriteIndex].worldSlices.x;
        for (int i = 0; i < seatData.xPos.Length; i++)
        {
            float xPos = seatData.xPos[i];
            Vector3 center = new Vector3(xPos, yPos, zPos);
            Vector3 size = new Vector3(tileWidth, ySize, 0);

            if (seatData.filled[i])
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.green;
            }
            Gizmos.DrawWireCube(center, size);
        }
    }
}
