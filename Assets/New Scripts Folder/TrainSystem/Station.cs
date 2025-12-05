using Proselyte.Sigils;
using System;
using UnityEngine;

public class Station : MonoBehaviour
{
    [Serializable] public struct SOData
    {
        public StationSO station;
        public TrainStatsSO trainStats;
        public TrainSettingsSO trainSettings;
        public NPCDataSO npcData;
        public CameraStatsSO camStats;
    }
    [SerializeField] public SOData soData;
    [Serializable] public struct GameEventData
    {
        public GameEvent OnStationLeave;
    }
    [SerializeField] GameEventData gameEvents;

    [Serializable] public struct ComponentData
    {
        public BoxCollider2D platformCollider;
    }
    [SerializeField] ComponentData components;

    Parallax.ParallaxData parallaxData = new Parallax.ParallaxData();
    private void OnEnable()
    {
        gameEvents.OnStationLeave.RegisterListener(SpawnNPCs);
        parallaxData = Parallax.GetParallaxData(soData.camStats, soData.trainStats, transform);
    }
    private void OnDisable()
    {
        gameEvents.OnStationLeave.UnregisterListener(SpawnNPCs);
    }
    private void Start()
    {
        if (soData.station == soData.trainSettings.stations[0])
        {
            SpawnNPCs();
        }
    }
    private void Update()
    {
        float parallaxXPos = Parallax.UpdateParallax(soData.trainStats, parallaxData);
        transform.position = new Vector3(parallaxXPos, transform.position.y, transform.position.z);
    }
    private void SpawnNPCs()
    {
        for(int i = 0; i < soData.station.agentSpawnAmount; i++)
        {
            if (NPCManager.agentNPCPool.Count == 0) { Debug.LogWarning("Agent Pool is empty"); return; }
            NPCBrain agentNPC =  NPCManager.agentNPCPool.Dequeue();
            agentNPC.gameObject.SetActive(true);
            float randXPos = UnityEngine.Random.Range(components.platformCollider.bounds.min.x, components.platformCollider.bounds.max.x);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y, components.platformCollider.transform.position.z);
            agentNPC.transform.position = spawnPos;
        }
        for (int i = 0; i < soData.station.bystanderSpawnAmount; i++)
        {
            int randNPCIndex = UnityEngine.Random.Range(0, soData.npcData.npcPrefabs.Length - 1);
            float randXPos = UnityEngine.Random.Range(components.platformCollider.bounds.min.x, components.platformCollider.bounds.max.x);
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y, components.platformCollider.transform.position.z);
            NPCBrain npc = Instantiate(soData.npcData.npcPrefabs[randNPCIndex], spawnPos, Quaternion.identity, null); // spawn at random point on station
            npc.stats.type = NPCBrain.Type.Bystander;
        }
    }
}
