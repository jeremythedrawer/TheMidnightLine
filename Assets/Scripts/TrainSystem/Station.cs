using UnityEngine;
using static NPC;
public class Station : MonoBehaviour
{
    const float SPAWN_BUFFER = 5f;
    public StationSO station;
    public TrainStatsSO trainStats;
    public TripSO trip;
    public AtlasRenderer frontPlatformRenderer;
    public AtlasRenderer backPlatformRenderer;
    public Transform exitTransform;
    public void Start()
    {
        station.exitLocalPosX = exitTransform.localPosition.x;
    }
    public void SpawnNPCs()
    {
        AtlasRenderer activePlatformRenderer = station.isFrontOfTrain ? frontPlatformRenderer : backPlatformRenderer;
        for (int i = 0; i < station.bystanderProfiles.Length; i++)
        {
            NPCProfile bystanderProfile = station.bystanderProfiles[i];
            activePlatformRenderer.UpdateBounds();
            float randXPos = Random.Range(activePlatformRenderer.bounds.min.x + SPAWN_BUFFER, activePlatformRenderer.bounds.max.x - SPAWN_BUFFER);
            
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, activePlatformRenderer.transform.position.z);

            NPCBrain bystander = Instantiate(trip.npc_prefabsArray[bystanderProfile.npcPrefabIndex], spawnPos, Quaternion.identity, activePlatformRenderer.transform);
            
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
            activePlatformRenderer.bounds.min.x + SPAWN_BUFFER,
            activePlatformRenderer.bounds.max.x - SPAWN_BUFFER);
            
            Vector3 spawnPos = new Vector3(randXPos, 
            transform.position.y + 0.1f,
            activePlatformRenderer.transform.position.z);

            NPCBrain traitor = Instantiate(trip.npc_prefabsArray[traitorProfile.npcPrefabIndex], spawnPos, Quaternion.identity, activePlatformRenderer.transform);
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
