using UnityEngine;
using static NPC;
using static Train;
public class Station : MonoBehaviour
{
    const float SPAWN_BUFFER = 5f;
    public StationSO station;
    public TrainStatsSO trainStats;
    public TripSO trip;
    public AtlasRenderer frontPlatformRenderer;
    public AtlasRenderer backPlatformRenderer;
    public Transform exitTransform;

    public ParallaxController parallaxController;
    public ParallaxController frontParallaxController;
    public void Start()
    {
        station.exitLocalPosX = exitTransform.localPosition.x;
        parallaxController.Init(transform.position);
        SetFrontParallaxPosition();
    }
    public void SpawnNPCs()
    {
        AtlasRenderer activePlatformRenderer = station.isFrontOfTrain ? frontPlatformRenderer : backPlatformRenderer;
        activePlatformRenderer.UpdateBounds();
        
        for (int i = 0; i < station.bystanderProfiles.Length; i++)
        {
            NPCProfile bystanderProfile = station.bystanderProfiles[i];
            float randXPos = Random.Range(activePlatformRenderer.bounds.min.x + SPAWN_BUFFER, activePlatformRenderer.bounds.max.x - SPAWN_BUFFER);

            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, activePlatformRenderer.transform.position.z);

            NPCBrain bystander = NPCManager.GetNPC(trip.npcDataArray[bystanderProfile.npcPrefabIndex].prefab, spawnPos, activePlatformRenderer.transform);
            
            bystander.profile = bystanderProfile;
            bystander.role = Role.Bystander;
            bystander.boardingStation = station;
            bystander.disembarkingStation = trip.stationsDataArray[bystanderProfile.disembarkingStationIndex];

            if (i % 2 == 0)
            {
                bystander.atlasRenderer.FlipHSimple(true);
            }
        }

        int maxTraitorSpawnIndex = trip.traitorsSpawned + station.traitorSpawnAmount;

        for (int i = trip.traitorsSpawned; i < maxTraitorSpawnIndex; i++)
        {
            TraitorProfile traitorProfile = trip.traitorProfiles[i];
            float randXPos = Random.Range(activePlatformRenderer.bounds.min.x + SPAWN_BUFFER, activePlatformRenderer.bounds.max.x - SPAWN_BUFFER);
            
            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, activePlatformRenderer.transform.position.z);

            NPCBrain traitor = NPCManager.GetNPC(trip.npcDataArray[traitorProfile.npcProfile.npcPrefabIndex].prefab, spawnPos, activePlatformRenderer.transform);
            traitor.profile = traitorProfile.npcProfile;
            traitor.role = Role.Traitor;
            traitor.boardingStation = station;
            traitor.disembarkingStation = trip.stationsDataArray[traitorProfile.npcProfile.disembarkingStationIndex];
            if (i % 2 == 0)
            {
                traitor.atlasRenderer.FlipHSimple(true);
            }
        }
        trip.traitorsSpawned += station.traitorSpawnAmount;
    }
    public void SetFrontParallaxPosition()
    {
        float posX = TRAIN_WORLD_POS_X + ((transform.position.x - TRAIN_WORLD_POS_X) * (frontParallaxController.parallaxFactor / parallaxController.parallaxFactor));
        Vector2 pos = new Vector2(posX, 0);
        frontParallaxController.Init(pos);
    }
}
