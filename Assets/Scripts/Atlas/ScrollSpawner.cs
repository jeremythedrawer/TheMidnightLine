using System.Collections.Generic;
using UnityEngine;
using static AtlasSpawn;
public class ScrollSpawner : MonoBehaviour
{
    const int MAX_ACTIVE_SCROLLERS = 64;

    public SpawnSO spawner;
    public TripSO demoTrip;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;


    public Scroller scroller_prefab;

    public Queue<Scroller> scrollRendererPool;
    public Scroller[] activeScrollers;
    public ScrollSprite[] scrollSprites;
    public int nextSpawnIndex;
    public int activeSpawnerCount;
    public Bounds spawnerBounds;
    public Scroller scroller_staticPrefab;
    private void OnEnable()
    {
        gameEventData.OnTicketInspect.RegisterListener(SetScrollers);
    }
    private void OnDisable()
    {
        gameEventData.OnTicketInspect.UnregisterListener(SetScrollers);
    }
    private void Start()
    {
        scrollRendererPool = new Queue<Scroller>();
        activeScrollers = new Scroller[MAX_ACTIVE_SCROLLERS];
        scrollSprites = demoTrip.scrollSprites;
        spawnerBounds = spawner.bounds;
        scroller_staticPrefab = scroller_prefab;
    }
    public void SetScrollers()
    {
        for (int i = 0; i < activeSpawnerCount; i++)
        {
            Scroller scroller = activeScrollers[i];

            if (scroller.state == ScrollState.Dead)
            {
                ReturnScroller(scroller);
            }
        }

        for (int i = nextSpawnIndex; i < scrollSprites.Length; i++)
        {
            ScrollSprite nextScrollSprite = scrollSprites[i];

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
            scroller.gameObject.SetActive(true);
            scroller.InitScroll(scrollSprite, activeSpawnerCount);

            activeScrollers[activeSpawnerCount] = scroller;
        }
        else
        {
            Scroller scroller = Instantiate(scroller_staticPrefab);
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
