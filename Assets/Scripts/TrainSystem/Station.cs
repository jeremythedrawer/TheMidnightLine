using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class Station : MonoBehaviour
{
    public StationSO station;
    public TrainStatsSO trainStats;
    public TrainSettingsSO trainSettings;
    public StationsDataSO stationsData;
    public NPCsDataSO npcData;
    public CameraStatsSO camStats;
    public SpyStatsSO spyStats;
    public GameEventDataSO gameEventData;
    public BoxCollider2D platformCollider;
    public AtlasRenderer platformRenderer;
    private void OnEnable()
    {
        SpawnNPCs();
        station.isFrontOfTrain = platformRenderer.depthOrder < trainStats.minDepth;
    }

    private void SpawnNPCs()
    {
        for (int i = 0; i < station.agentSpawnAmount; i++)
        {
            if (npcData.agentPool.Count == 0) { Debug.LogError($"Traitor Pool is empty at {gameObject.name}"); return; }
            NPC.TraitorData traitorData =  npcData.agentPool.Dequeue();
            traitorData.traitor_prefab.gameObject.SetActive(true);
            float randXPos = UnityEngine.Random.Range(platformCollider.bounds.min.x + 5f, platformCollider.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, platformCollider.transform.position.z);
            traitorData.traitor_prefab.transform.position = spawnPos;
            traitorData.traitor_prefab.startStation = station;
            traitorData.traitor_prefab.transform.SetParent(platformCollider.transform, true);
        }
        for (int i = 0; i < station.bystanderSpawnAmount; i++)
        {
            int randNPCIndex = UnityEngine.Random.Range(0, npcData.npc_prefab.Length);
            float randXPos = UnityEngine.Random.Range(platformCollider.bounds.min.x + 5, platformCollider.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, platformCollider.transform.position.z);
            NPCBrain bystanderNPC = Instantiate(npcData.npc_prefab[randNPCIndex], spawnPos, Quaternion.identity); // spawn at random point on station
            bystanderNPC.atlasRenderer.depthOrder = (int)platformCollider.transform.position.z;
            bystanderNPC.stats.role = NPC.Role.Bystander;
            bystanderNPC.startStation = station;
            bystanderNPC.transform.SetParent(platformCollider.transform, true);
        }
    }
}
