using System.Collections.Generic;
using UnityEngine;
using static AtlasSpawn;
[ExecuteAlways]
public class ScrollSpawner : MonoBehaviour
{


    public SpawnSO spawner;
    public TripSO trip;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;

    public Queue<Scroller> scrollRendererPool;
    public Scroller[] activeScrollers;
    public int nextSpawnIndex;
    public int activeSpawnerCount;
    private void OnEnable()
    {
        InitScrollers();
        gameEventData.OnTicketInspect.RegisterListener(SetScrollers);
    }
    private void OnDisable()
    {
        ResetScrollerSpawner();
        gameEventData.OnTicketInspect.UnregisterListener(SetScrollers);

    }
    private void Start()
    {
        InitScrollers();
    }



    private void InitScrollers()
    {
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
}
