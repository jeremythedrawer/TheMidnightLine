using UnityEngine;
using static NPC;
public class Station : MonoBehaviour
{
    const float SPAWN_BUFFER = 5f;
    public StationSO station;
    public TrainStatsSO trainStats;
    public TripSO trip;
    public AtlasRenderer platformRenderer;

    private void Awake()
    {
        station.isFrontOfTrain = platformRenderer.batchKey.depthOrder < trainStats.depthSections.min;
    }
    public void SpawnNPCs()
    {
        for (int i = 0; i < station.bystanderProfiles.Length; i++)
        {
            NPCProfile bystanderProfile = station.bystanderProfiles[i];
            platformRenderer.UpdateBounds();
            float randXPos = Random.Range(
            platformRenderer.bounds.min.x + SPAWN_BUFFER, 
            platformRenderer.bounds.max.x - SPAWN_BUFFER);
            
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, platformRenderer.transform.position.z);

            NPCBrain bystander = Instantiate(trip.npc_prefabsArray[bystanderProfile.npcPrefabIndex], spawnPos, Quaternion.identity, platformRenderer.transform);
            
            bystander.profile = bystanderProfile;
            bystander.role = Role.Bystander;
            bystander.gameObject.name = bystanderProfile.fullName;

            if (i % 2 == 0)
            {
                bystander.atlasRenderer.FlipH(true, bystander.atlasRenderer.sprite);
            }
        }

        for (int i = 0; i < station.traitorProfiles.Length; i++)
        {
            NPCProfile traitorProfile = station.traitorProfiles[i];
            float randXPos = Random.Range(
            platformRenderer.bounds.min.x + SPAWN_BUFFER,
            platformRenderer.bounds.max.x - SPAWN_BUFFER);
            
            Vector3 spawnPos = new Vector3(randXPos, 
            transform.position.y + 0.1f, 
            platformRenderer.transform.position.z);

            NPCBrain traitor = Instantiate(trip.npc_prefabsArray[traitorProfile.npcPrefabIndex], spawnPos, Quaternion.identity, platformRenderer.transform);
            traitor.profile = traitorProfile;
            traitor.role = Role.Traitor;
            traitor.gameObject.name = traitorProfile.fullName;
            if (i % 2 == 0)
            {
                traitor.atlasRenderer.FlipH(true, traitor.atlasRenderer.sprite);
            }
        }
    }
}
