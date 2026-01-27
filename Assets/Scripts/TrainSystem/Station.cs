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

    private void OnEnable()
    {
        if (station == stationsData.stations[0])
        {
            InitialiseFirstStation().Forget();
        }
        else
        {
            SpawnNPCs();
        }
        station.curWorldPos = transform.position;
        station.isFrontOfTrain = platformCollider.transform.position.z < trainSettings.maxMinWorldZPos.min;
        station.parallaxFactor = Parallax.GetParallaxFactor(camStats, trainStats, station.curWorldPos.z);
    }

    private void Update()
    {
        if (spyStats.onTrain)
        {
            station.curWorldPos.x -= Parallax.UpdateParallaxPosition(trainStats, station.parallaxFactor);
            transform.position = station.curWorldPos;
        }
    }
    private async UniTask InitialiseFirstStation()
    {
        while(npcData.agentPool.Count == 0) {  await UniTask.Yield(); }
        SpawnNPCs();
    }
    private void SpawnNPCs()
    {
        for (int i = 0; i < station.agentSpawnAmount; i++)
        {
            if (npcData.agentPool.Count == 0) { Debug.LogError($"Agent Pool is empty at {gameObject.name}"); return; }
            NPCsDataSO.AgentData agentData =  npcData.agentPool.Dequeue();
            agentData.agent.gameObject.SetActive(true);
            float randXPos = UnityEngine.Random.Range(platformCollider.bounds.min.x + 5f, platformCollider.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, platformCollider.transform.position.z);
            agentData.agent.transform.position = spawnPos;
            agentData.agent.startStation = station;
            agentData.agent.transform.SetParent(transform, true);
            //agentData.agent.SetStationDepth();
        }
        for (int i = 0; i < station.bystanderSpawnAmount; i++)
        {
            int randNPCIndex = UnityEngine.Random.Range(0, npcData.npcPrefabs.Length);
            float randXPos = UnityEngine.Random.Range(platformCollider.bounds.min.x + 5f, platformCollider.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, platformCollider.transform.position.z);
            NPCBrain bystanderNPC = Instantiate(npcData.npcPrefabs[randNPCIndex], spawnPos, Quaternion.identity, null); // spawn at random point on station
            bystanderNPC.stats.type = NPCBrain.Type.Bystander;
            bystanderNPC.startStation = station;
            bystanderNPC.transform.SetParent(transform, true);
            //bystanderNPC.SetStationDepth();
        }
    }
}
