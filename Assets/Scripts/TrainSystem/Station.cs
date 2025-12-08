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
        internal float moveThreshold;
        internal Parallax.ParallaxData parallaxData;
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
    }
    private void Start()
    {

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
            NPCBrain agentNPC =  soData.npcData.agentPool.Dequeue();
            agentNPC.gameObject.SetActive(true);
            float randXPos = UnityEngine.Random.Range(components.platformCollider.bounds.min.x, components.platformCollider.bounds.max.x);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y, components.platformCollider.transform.position.z);
            agentNPC.transform.position = spawnPos;
            agentNPC.transform.SetParent(transform, true);
        }
        for (int i = 0; i < soData.settings.bystanderSpawnAmount; i++)
        {
            int randNPCIndex = UnityEngine.Random.Range(0, soData.npcData.npcPrefabs.Length);
            float randXPos = UnityEngine.Random.Range(components.platformCollider.bounds.min.x, components.platformCollider.bounds.max.x);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y, components.platformCollider.transform.position.z);
            NPCBrain bystanderNPC = Instantiate(soData.npcData.npcPrefabs[randNPCIndex], spawnPos, Quaternion.identity, null); // spawn at random point on station
            bystanderNPC.stats.type = NPCBrain.Type.Bystander;
            bystanderNPC.transform.SetParent(transform, true);
        }
    }
}
