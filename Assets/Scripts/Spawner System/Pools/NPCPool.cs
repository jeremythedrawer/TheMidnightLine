using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
public class NPCPool : MonoBehaviour
{
    protected StationData station;
    public ObjectPool<NPCCore> npcPool { get; private set; }

    private void Start()
    {
        station = transform.parent.transform.parent.GetComponent<StationData>();
    }

    protected void CreatePool(NPCCore npc, Transform spawnerTransform, int npcMaxSpawns, int npcDefaultSpawns)
    {
        npcPool = new ObjectPool<NPCCore>(
            () => CreatedNPC(npc, spawnerTransform),
            ActivateNPC,
            DeactiveNPC,
            DestroyNPC,
            false,
            npcDefaultSpawns,
            npcMaxSpawns);
    }

    private NPCCore CreatedNPC(NPCCore npc, Transform spawnerTransform)
    {
        return Instantiate(npc, spawnerTransform);
    }

    public virtual void ActivateNPC(NPCCore npc)
    {
        npc.gameObject.SetActive(true);
    }

    private void DeactiveNPC(NPCCore npc)
    {
        npc.gameObject.SetActive(false);
    }

    private void DestroyNPC(NPCCore npc)
    {
        Destroy(npc.gameObject);
    }

    protected void SpawnNPCs(int npcsToSpawn, List<NPCCore> npcList)
    {
        for (int i = 0; i < npcsToSpawn; i++)
        {
            SpawnNPC(npcList);
        }
    }

    protected void SpawnNPC(List<NPCCore> npcList)
    {
        if (npcPool == null) return;
        NPCCore npc = npcPool.Get();
        npc.gameObject.SetActive(false);
        npcList.Add(npc);
    }

    protected int NPCCountOfStation<T>(int stationIndex)where T : NPCCore
    {
        int defaultNPCCount = 0;
        foreach (StationData.SpawnArea area in GlobalReferenceManager.Instance.stations[stationIndex].spawnAreas)
        {
            if (typeof(T) == typeof(BystanderBrain))
            {
                defaultNPCCount += area.bystanderCount;
            }
            else
            {
                defaultNPCCount += area.agentCount;
            }
        }

        return defaultNPCCount;
    }
}
