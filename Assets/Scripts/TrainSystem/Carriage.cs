using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static NPC;
using static Train;

public class Carriage : MonoBehaviour
{
    const float QUEUE_TICK_RATE = 0.3f;
    public TrainStatsSO trainStats;
    public TrainSettingsSO trainSettings;
    public TripSO trip;
    public GameEventDataSO gameEventData;
    public LayerSettingsSO layerSettings;
    public MaterialIDSO materialIDs;

    public AtlasRenderer nextStationSignRenderer;
    public AtlasRenderer carriageWallRenderer;
    public AtlasRenderer[] exteriorRenderers;
    public AtlasRenderer[] seatRenderers;
    public AtlasRenderer[] grapPoleRenderers;

    public BoxCollider2D insideBoundsCollider;
    public BoxCollider2D[] smokingRoomColliders;

    public SlideDoors[] exteriorSlideDoors;
    public SlideDoors[] interiorSlideDoors;
    
    public CarriageMap map;

    [Header("Generated")]
    public float wheelCircumference;
    public float alpha;
    public SeatData seatData;
    public SmokersRoomData[] smokersRoomData;
    public Transform[] wheelTransforms;
    public CancellationTokenSource ctsFade;
    public float prevMeters;
    public int seatAmount;
    public NPCQueue seatQueue;
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
        string text = "Next Station is " + stationName;
        nextStationSignRenderer.SetText(text);
        nextStationSignRenderer.SetScrollingText();
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
    public void AddToSeatQueue(NPCBrain npc)
    {
        npc.seatQueueIndex = seatQueue.npcsCount;
        seatQueue.npcs[seatQueue.npcsCount] = npc;
        seatQueue.npcsCount++;
    }
    public void RemoveFromSeatQueue(NPCBrain npc)
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

        NPCBrain npc = seatQueue.npcs[seatQueue.npcsCount - 1];

        float npcX = npc.transform.position.x;
        float closestDist = float.PositiveInfinity;
        int bestIndex = int.MaxValue;

        for (int i = 0; i < seatAmount; i++)
        {
            if (seatData.filled[i]) continue;

            float dist = Mathf.Abs(npcX - seatData.xPos[i]);
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
            seatData.filled[bestIndex] = true;
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
            float firstSeatPos = seat.transform.position.x + (tileWidth * 0.5f);

            for (int j = 0; j < seatsPerRenderer[i]; j++)
            {
                seatData.xPos[seatIndex] = (firstSeatPos + (tileWidth * j)) + offset;
                seatIndex++;
            }
        }
        seatQueue = new NPCQueue();
        seatQueue.npcs = new NPCBrain[seatAmount];
    }
    private async UniTask MovingDown()
    {
        float elaspedTime = alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (elaspedTime < trainSettings.exteriorWallFadeTime)
            {
                elaspedTime += Time.deltaTime;

                alpha = elaspedTime / trainSettings.exteriorWallFadeTime;
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f; 
                for (int i = 0; i < exteriorRenderers.Length; i++)
                {
                    exteriorRenderers[i].custom.x = alpha;
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
        float elaspedTime = alpha * trainSettings.exteriorWallFadeTime;
        try
        {
            while (elaspedTime > 0)
            {
                elaspedTime -= Time.deltaTime;

                alpha = elaspedTime / trainSettings.exteriorWallFadeTime;
                alpha = alpha < 0.5 ? 16 * alpha * alpha * alpha * alpha * alpha : 1 - Mathf.Pow(-2 * alpha + 2, 5) * 0.5f;
                for (int i = 0; i < exteriorRenderers.Length; i++)
                {
                    exteriorRenderers[i].custom.x = alpha;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ctsFade.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
    private void OnDrawGizmos()
    {
        if (seatRenderers.Length < 0 || seatData.xPos.Length < 0) return;
        Gizmos.color = Color.red;

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
            Gizmos.DrawWireCube(center, size);
        }
    }
}
