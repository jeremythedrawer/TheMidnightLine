using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class Station : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public StationSO station;
        public TrainStatsSO trainStats;
        public StationsDataSO stationsData;
        public NPCsDataSO npcData;
        public CameraStatsSO camStats;
        public SpyStatsSO spyStats;
    }
    [SerializeField] public SOData soData;
    [Serializable] public struct GameEventData
    {
        public GameEvent OnStationLeave; // not in use atm
        public GameEvent OnBoardingSpy;
    }
    [SerializeField] GameEventData gameEvents;

    [Serializable] public struct ComponentData
    {
        public BoxCollider2D platformCollider;
    }
    public ComponentData components;

    Parallax.ParallaxData parallaxData = new Parallax.ParallaxData();
    private void OnEnable()
    {
        if (soData.station == soData.stationsData.stations[0])
        {
            InitialiseFirstStation().Forget();
        }
        else
        {
            SpawnNPCs();
            InitialiseParallax();
        }
        gameEvents.OnBoardingSpy.RegisterListener(InitialiseParallax);
    }
    private void OnDisable()
    {
        gameEvents.OnBoardingSpy.UnregisterListener(InitialiseParallax);
    }
    private void Start()
    {

    }
    private void Update()
    {
        if (soData.spyStats.onTrain)
        {
            float parallaxXPos = Parallax.UpdateParallax(soData.trainStats, parallaxData);
            transform.position = new Vector3(parallaxXPos, transform.position.y, transform.position.z);
        }
    }
    private void InitialiseParallax()
    {
        parallaxData = Parallax.GetParallaxData(soData.camStats, soData.trainStats, transform);
    }
    private async UniTask InitialiseFirstStation()
    {
        while(soData.npcData.agentPool.Count == 0) {  await UniTask.Yield(); }
        SpawnNPCs();
    }
    private void SpawnNPCs()
    {
        for (int i = 0; i < soData.station.agentSpawnAmount; i++)
        {
            if (soData.npcData.agentPool.Count == 0) { Debug.LogError($"Agent Pool is empty at {gameObject.name}"); return; }
            NPCBrain agentNPC =  soData.npcData.agentPool.Dequeue();
            agentNPC.gameObject.SetActive(true);
            float randXPos = UnityEngine.Random.Range(components.platformCollider.bounds.min.x, components.platformCollider.bounds.max.x);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y, components.platformCollider.transform.position.z);
            agentNPC.transform.position = spawnPos;
            agentNPC.transform.SetParent(transform, true);
        }
        for (int i = 0; i < soData.station.bystanderSpawnAmount; i++)
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
