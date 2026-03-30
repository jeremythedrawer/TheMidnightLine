using System.Xml.Linq;
using UnityEngine;
using static NPC;
public class Station : MonoBehaviour
{
    const float SPAWN_BUFFER = 5f;
    public StationSO station;
    public TrainStatsSO trainStats;
    public TripSO trip;
    public AtlasSimpleRenderer platformRenderer;

    private void Awake()
    {
    }
    private void OnEnable()
    {
        SpawnNPCs();
        station.isFrontOfTrain = platformRenderer.renderInput.batchKey.depthOrder < trainStats.minDepth;
    }

    private void SpawnNPCs()
    {
        for (int i = 0; i < station.bystanderProfiles.Count; i++)
        {
            NPCProfile bystanderProfile = station.bystanderProfiles[i];
            
            float randXPos = Random.Range(
            platformRenderer.renderInput.bounds.min.x + SPAWN_BUFFER, 
            platformRenderer.renderInput.bounds.max.x - SPAWN_BUFFER);
            
            Vector3 spawnPos = new Vector3(
            randXPos, 
            transform.position.y + 0.1f, 
            platformRenderer.transform.position.z);

            NPCBrain bystander = Instantiate(
            trip.npc_prefabsArray[bystanderProfile.npcPrefabIndex], 
            spawnPos, 
            Quaternion.identity, 
            platformRenderer.transform);
            
            bystander.profile = bystanderProfile;
            bystander.role = Role.Bystander;
        }

        for (int i = 0; i < station.traitorProfiles.Count; i++)
        {
            NPCProfile traitorProfile = station.traitorProfiles[i];
            float randXPos = Random.Range(
            platformRenderer.renderInput.bounds.min.x + SPAWN_BUFFER,
            platformRenderer.renderInput.bounds.max.x - SPAWN_BUFFER);
            
            Vector3 spawnPos = new Vector3(randXPos, 
            transform.position.y + 0.1f, 
            platformRenderer.transform.position.z);

            NPCBrain traitor = Instantiate(
            trip.npc_prefabsArray[traitorProfile.npcPrefabIndex], 
            spawnPos, 
            Quaternion.identity, 
            platformRenderer.transform);
            traitor.profile = traitorProfile;
            traitor.role = Role.Traitor;
        }
    }
}
