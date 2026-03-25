using UnityEngine;

public class Station : MonoBehaviour
{
    public StationSO station;
    public TrainStatsSO trainStats;
    public NPCsDataSO npcData;
    public AtlasSimpleRenderer platformRenderer;
    private void OnEnable()
    {
        SpawnNPCs();
        station.isFrontOfTrain = platformRenderer.renderInput.batchKey.depthOrder < trainStats.minDepth;
    }

    private void SpawnNPCs()
    {
        for (int i = 0; i < station.traitorSpawnAmount; i++)
        {
            if (npcData.agentPool.Count == 0) { Debug.LogError($"Traitor Pool is empty at {gameObject.name}"); return; }
            NPC.TraitorData traitorData =  npcData.agentPool.Dequeue();
            traitorData.traitor_prefab.gameObject.SetActive(true);
            float randXPos = Random.Range(platformRenderer.renderInput.bounds.min.x + 5f, platformRenderer.renderInput.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, platformRenderer.transform.position.z);
            traitorData.traitor_prefab.transform.position = spawnPos;
            traitorData.traitor_prefab.startStation = station;
            traitorData.traitor_prefab.transform.SetParent(platformRenderer.transform, true);
        }
        for (int i = 0; i < station.bystanderSpawnAmount; i++)
        {
            int randNPCIndex = Random.Range(0, npcData.npc_prefab.Length);
            float randXPos = Random.Range(platformRenderer.renderInput.bounds.min.x + 5, platformRenderer.renderInput.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, platformRenderer.transform.position.z);
            NPCBrain bystanderNPC = Instantiate(npcData.npc_prefab[randNPCIndex], spawnPos, Quaternion.identity); // spawn at random point on station
            bystanderNPC.stats.role = NPC.Role.Bystander;
            bystanderNPC.startStation = station;
            bystanderNPC.transform.SetParent(platformRenderer.transform, true);
        }
    }
}
