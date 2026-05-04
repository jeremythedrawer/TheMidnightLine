using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static AtlasSpawn;
using static Spy;
using static Train;
[ExecuteAlways]
public class SpawnMaster : MonoBehaviour
{
    public SpawnSO spawner;
    public CameraSettingsSO camSettings;
    public CameraStatsSO camStats;
    public TripSO trip;
    public TrainStatsSO trainStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;

    public ZoneSpawner[] zoneSpawners;

    public Queue<Scroller> scrollRendererPool;
    public Scroller[] activeScrollers;

    public ComputeBuffer scrollOutputBuffer;
    
    public int nextSpawnIndex;
    public int activeSpawnerCount;


    private void OnEnable()
    {
        InitBoundParameters();
        
        InitZoneCompute();
        InitScrollCompute();

        InitZoneSpawners();
        InitScrollers();

        gameEventData.OnTicketInspect.RegisterListener(SetScrollers);
    }
    private void OnDisable()
    {
        ResetScrollerSpawner();
        gameEventData.OnTicketInspect.UnregisterListener(SetScrollers);

    }
    private void Update()
    {
        spawner.zoneCompute.SetFloat("_CamVelocity", camStats.curVelocity.x);
        spawner.scrollCompute.SetFloat("_CamVelocity", camStats.curVelocity.x);
        if (spyStats.curLocationState != LocationState.Station)
        {
            spawner.zoneCompute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
            spawner.scrollCompute.SetFloat("_TrainVelocity", (trainStats.curVelocity * Time.deltaTime));
        }

        spawner.scrollCompute.Dispatch(spawner.scrollKernelUpdate, spawner.scrollComputeGroupSize, 1, 1);
    }
    private void InitBoundParameters()
    {
        spawner.bounds.center = new Vector3(TRAIN_WORLD_POS, 0, FAR_CLIP * 0.5f);

        spawner.bounds.size = new Vector3(trip.stationsDataArray[0].station_prefab.frontPlatformRenderer.bounds.size.x + camStats.camBounds.size.x, trainStats.totalBounds.size.y + camStats.camBounds.size.y, FAR_CLIP);

        transform.position = spawner.bounds.min;
    }
    private void InitZoneCompute()
    {
        spawner.zoneCompute.SetVector("_SpawnerMinPos", spawner.bounds.min);
        spawner.zoneCompute.SetVector("_SpawnerMaxPos", spawner.bounds.max);
        spawner.zoneCompute.SetVector("_SpawnerSize", spawner.bounds.size);
        spawner.zoneCompute.SetInt("_ForegroundParticleCount", FORE_PARTICLE_COUNT);
        spawner.zoneCompute.SetInt("_MiddlegroundParticleCount", MID_PARTICLE_COUNT);
        spawner.zoneCompute.SetInt("_BackgroundParticleCount", BACK_PARTICLE_COUNT);
        spawner.zoneCompute.SetFloat("_TrainVelocity", 0);
    }
    private void InitScrollCompute()
    {
        spawner.scrollCompute.SetVector("_SpawnerMinPos", spawner.bounds.min);
        spawner.scrollCompute.SetVector("_SpawnerMaxPos", spawner.bounds.max);
        spawner.scrollCompute.SetVector("_SpawnerSize", spawner.bounds.size);
        spawner.scrollCompute.SetInt("_ParticleCount", SCROLL_PARTICLE_COUNT);
        spawner.scrollCompute.SetFloat("_TrainVelocity", 0);

        spawner.scrollKernelUpdate = spawner.scrollCompute.FindKernel("_ScrollUpdate");
        spawner.scrollKernelInit = spawner.scrollCompute.FindKernel("_ScrollInit");

        spawner.scrollComputeGroupSize = Mathf.CeilToInt((float)SCROLL_PARTICLE_COUNT / (float)THREADS_PER_GROUP);

        spawner.scrollMoveInputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(uint));
        spawner.moveInputs = new uint[SCROLL_PARTICLE_COUNT];
        spawner.scrollMoveInputBuffer.SetData(spawner.moveInputs);
        spawner.scrollCompute.SetBuffer(spawner.scrollKernelUpdate, "_MoveInput", spawner.scrollMoveInputBuffer);

        scrollOutputBuffer = new ComputeBuffer(SCROLL_PARTICLE_COUNT, sizeof(float) * 4);
        spawner.scrollCompute.SetBuffer(spawner.scrollKernelInit, "_ScrollOutput", scrollOutputBuffer);
        spawner.scrollCompute.SetBuffer(spawner.scrollKernelUpdate, "_ScrollOutput", scrollOutputBuffer);

        spawner.scrollCompute.Dispatch(spawner.scrollKernelInit, spawner.scrollComputeGroupSize, 1, 1);
        
        spawner.scrollMaterial.SetBuffer("_ScrollOutput", scrollOutputBuffer);
    }
    private void InitZoneSpawners()
    {
        spawner.zoneCompute.SetInt("_Awake", 0);
        for (int i = 0; i < zoneSpawners.Length; i++)
        {
            ZoneSpawner zoneSpawner = zoneSpawners[i];
            zoneSpawner.gameObject.SetActive(true);
            zoneSpawner.InitializeZoneSpawnData();
        }
       spawner.zoneCompute.SetInt("_Awake", 1);
    }
    private void InitScrollers()
    {
        ResetScrollerSpawner();
        scrollRendererPool = new Queue<Scroller>();
        activeScrollers = new Scroller[MAX_ACTIVE_SCROLLERS];
        SetScrollers();
    }
    private void ResetScrollerSpawner()
    {
        nextSpawnIndex = 0;
        activeSpawnerCount = 0;
    }
    private void SetScrollers()
    {
        for (int i = 0; i < activeSpawnerCount; i++)
        {
            Scroller scroller = activeScrollers[i];

            if (scroller.state == ScrollState.Dead)
            {
                ReturnScroller(scroller);
            }
        }

        for (int i = nextSpawnIndex; i < trip.scrollSprites.Length; i++)
        {
            ScrollSprite nextScrollSprite = trip.scrollSprites[i];

            if (nextScrollSprite.ticketCheckStart == spyStats.ticketsCheckedTotal)
            {
                GetScroller(ref nextScrollSprite);
            }
            else
            {
                nextSpawnIndex = i;
                break;
            }
        }
    }
    private void GetScroller(ref ScrollSprite scrollSprite)
    {
        if (scrollRendererPool.Count > 0)
        {
            Scroller scroller = scrollRendererPool.Dequeue();

            scroller.InitScroll(scrollSprite, activeSpawnerCount);

            activeScrollers[activeSpawnerCount] = scroller;
        }
        else
        {
            Scroller scroller = Instantiate(spawner.scroller_prefab);
            scroller.gameObject.SetActive(false);
            scroller.InitScroll(scrollSprite, activeSpawnerCount);

            activeScrollers[activeSpawnerCount] = scroller;
        }
        activeSpawnerCount++;
    }
    public void ReturnScroller(Scroller scroller)
    {
        Scroller lastScroller = activeScrollers[activeSpawnerCount];
        activeScrollers[scroller.activeScrollerIndex] = lastScroller;
        scroller.gameObject.SetActive(false);
        scrollRendererPool.Enqueue(scroller);
        activeSpawnerCount--;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(spawner.bounds.center, spawner.bounds.size);
    }
}
