using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class Station : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public StationSO settings;
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
        public StationsDataSO stationsData;
        public NPCsDataSO npcData;
        public CameraStatsSO camStats;
        public SpyStatsSO spyStats;
        public GameEventDataSO gameEventData;
    }
    [SerializeField] public SOData soData;

    [Serializable] public struct ComponentData
    {
        public BoxCollider2D platformCollider;
    }
    public ComponentData components;

    [Serializable] public struct Stats
    { 
        internal Parallax.ParallaxData parallaxData;
        internal float moveThreshold;
        internal bool initialParallaxData;
    }
    [SerializeField] Stats stats;
    private void OnEnable()
    {
        if (soData.settings == soData.stationsData.stations[0])
        {
            InitialiseFirstStation().Forget();
        }
        else
        {
            SpawnNPCs();
        }

        stats.moveThreshold = soData.settings.metersPosition - components.platformCollider.bounds.extents.x;
        soData.settings.isFrontOfTrain = components.platformCollider.transform.position.z < soData.trainSettings.maxMinWorldZPos.min;
    }

    private void Update()
    {
        if (soData.spyStats.onTrain && soData.trainStats.metersTravelled > stats.moveThreshold)
        {
            if (!stats.initialParallaxData)
            {
                stats.parallaxData = Parallax.GetParallaxData(soData.camStats, soData.trainStats, transform);
                stats.initialParallaxData = true;
            }
            else
            {
                float parallaxXPos = Parallax.UpdateParallax(soData.trainStats, stats.parallaxData);
                transform.position = new Vector3(parallaxXPos, transform.position.y, transform.position.z);
            }
        }
    }
    private async UniTask InitialiseFirstStation()
    {
        while(soData.npcData.agentPool.Count == 0) {  await UniTask.Yield(); }
        SpawnNPCs();
    }
    private void SpawnNPCs()
    {
        for (int i = 0; i < soData.settings.agentSpawnAmount; i++)
        {
            if (soData.npcData.agentPool.Count == 0) { Debug.LogError($"Agent Pool is empty at {gameObject.name}"); return; }
            NPCsDataSO.AgentData agentData =  soData.npcData.agentPool.Dequeue();
            agentData.agent.gameObject.SetActive(true);
            float randXPos = UnityEngine.Random.Range(components.platformCollider.bounds.min.x + 5f, components.platformCollider.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, components.platformCollider.transform.position.z);
            agentData.agent.transform.position = spawnPos;
            agentData.agent.stats.targetXPos = spawnPos.x;
            agentData.agent.soData.startStation = soData.settings;
            agentData.agent.transform.SetParent(transform, true);
            //agentData.agent.SetStationDepth();
        }
        for (int i = 0; i < soData.settings.bystanderSpawnAmount; i++)
        {
            int randNPCIndex = UnityEngine.Random.Range(0, soData.npcData.npcPrefabs.Length);
            float randXPos = UnityEngine.Random.Range(components.platformCollider.bounds.min.x + 5f, components.platformCollider.bounds.max.x - 5f);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, components.platformCollider.transform.position.z);
            NPCBrain bystanderNPC = Instantiate(soData.npcData.npcPrefabs[randNPCIndex], spawnPos, Quaternion.identity, null); // spawn at random point on station
            bystanderNPC.stats.type = NPCBrain.Type.Bystander;
            bystanderNPC.soData.startStation = soData.settings;
            bystanderNPC.stats.targetXPos = spawnPos.x;
            bystanderNPC.transform.SetParent(transform, true);
            //bystanderNPC.SetStationDepth();
        }
    }
}
