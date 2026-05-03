using System.Collections.Generic;
using UnityEngine;
using static AtlasSpawn;
public class ScrollSpawner : MonoBehaviour
{
    const int MAX_ACTIVE_SCROLLERS = 64;

    public ZoneSpawnerSO spawner;
    public TripSO demoTrip;
    public Scroller scroller_prefab;

    public static Queue<Scroller> scrollRendererPool;
    public static Scroller[] activeScrollers;
    public static ScrollSprite[] scrollSprites;
    public static int nextSpawnIndex;
    public static int activeSpawnerCount;
    public static Bounds spawnerBounds;
    public static Scroller scroller_staticPrefab;
    private void Start()
    {
        scrollRendererPool = new Queue<Scroller>();
        activeScrollers = new Scroller[MAX_ACTIVE_SCROLLERS];
        scrollSprites = demoTrip.scrollSprites;
        spawnerBounds = spawner.bounds;
        scroller_staticPrefab = scroller_prefab;
    }
    public static void UpdateScrollers()
    {
        for(int i = 0; i < activeSpawnerCount; i++)
        {
            Scroller scroller = activeScrollers[i];

            if (scroller.scrollSprite.ticketCheckEnd == SpyBrain.ticketsCheckedTotal)
            {
                scroller.ScrollAway();
            }
            else if (scroller.scrollSprite.ticketCheckEnd < SpyBrain.ticketsCheckedTotal)
            {
                scroller.CheckToDeactivate();
            }
        }

        for(int i = nextSpawnIndex; i < scrollSprites.Length; i++)
        {
            ScrollSprite nextScrollSprite = scrollSprites[i];

            if (nextScrollSprite.ticketCheckStart == SpyBrain.ticketsCheckedTotal)
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
    private static void GetScroller(ref ScrollSprite scrollSprite)
    {
        if (scrollRendererPool.Count > 0)
        {
            Scroller scroller = scrollRendererPool.Dequeue();
            scroller.gameObject.SetActive(true);
            scroller.InitScroll(ref scrollSprite, activeSpawnerCount);

            activeScrollers[activeSpawnerCount] = scroller;
        }
        else
        {
            Scroller scroller = Instantiate(scroller_staticPrefab);
            scroller.InitScroll(ref scrollSprite, activeSpawnerCount);

            activeScrollers[activeSpawnerCount] = scroller;
        }
        activeSpawnerCount++;
    }
    public static void ReturnScroller(Scroller scroller)
    {
        Scroller lastScroller = activeScrollers[activeSpawnerCount];
        activeScrollers[scroller.activeScrollerIndex] = lastScroller;
        scroller.gameObject.SetActive(false);
        scrollRendererPool.Enqueue(scroller);
        activeSpawnerCount--;
    }
}
