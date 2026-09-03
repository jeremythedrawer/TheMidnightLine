using UnityEngine;
using static Passenger;
using static Train;
public class Station : MonoBehaviour
{
    public StationSO station;
    public TrainData trainStats;
    public Options options;

    public AtlasRenderer platformRenderer;
    public Transform exitTransform;

    public ParallaxController parallaxController;
    public void OnEnable()
    {
        station.exitLocalPosX = exitTransform.localPosition.x;
        parallaxController.SetParrallaxFactor();
        parallaxController.SetWorldPos(transform.position);
    }
    public void SpawnNPCs()
    {
        int totalNPCSSpawned = 0;

        for (int i = 0; i < station.bystanderProfiles.Length; i++)
        {
            totalNPCSSpawned++;
            NPCProfile bystanderProfile = station.bystanderProfiles[i];
            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainStats.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainStats.totalBounds.extents.x);

            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, 0);
            PassengerBrain bystander = PassengerManager.GetNPC(options.curTrip.passengers[bystanderProfile.npcPrefabIndex].prefab, spawnPos, platformRenderer.transform);
            
            bystander.profile = bystanderProfile;
            bystander.role = Role.Bystander;
            bystander.boardingStation = station;
            bystander.disembarkingStation = options.curTrip.stationsDataArray[bystanderProfile.disembarkingStationIndex];

            if (i % 2 == 0)
            {
                bystander.atlasRenderer.FlipHSimple(true);
            }
            bystander.Init();
        }

        int maxTraitorSpawnIndex = options.curTrip.traitorsSpawned + station.traitorSpawnCount;

        for (int i = options.curTrip.traitorsSpawned; i < maxTraitorSpawnIndex; i++)
        {
            totalNPCSSpawned++;
            TraitorProfile traitorProfile = options.curTrip.traitorProfiles[i];
            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainStats.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainStats.totalBounds.extents.x);

            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, 0);

            PassengerBrain traitor = PassengerManager.GetNPC(options.curTrip.passengers[traitorProfile.npcProfile.npcPrefabIndex].prefab, spawnPos, platformRenderer.transform);
            traitor.profile = traitorProfile.npcProfile;
            traitor.role = Role.Traitor;
            traitor.boardingStation = station;
            traitor.disembarkingStation = options.curTrip.stationsDataArray[traitorProfile.npcProfile.disembarkingStationIndex];
            if (i % 2 == 0)
            {
                traitor.atlasRenderer.FlipHSimple(true);
            }
            traitor.Init();
        }
        options.curTrip.traitorsSpawned += station.traitorSpawnCount;

        for (int i = 0; i < station.accompliceProfiles.Length; i++)
        {
            totalNPCSSpawned++;
            NPCProfile accompliceProfile = station.accompliceProfiles[i];

            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainStats.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainStats.totalBounds.extents.x);

            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, 0);

            PassengerBrain accomplice = PassengerManager.GetNPC(options.curTrip.passengers[accompliceProfile.npcPrefabIndex].prefab, spawnPos, platformRenderer.transform);

            accomplice.profile = accompliceProfile;
            accomplice.role = Role.Accomplice;
            accomplice.boardingStation = station;
            accomplice.disembarkingStation = options.curTrip.stationsDataArray[accompliceProfile.disembarkingStationIndex];

            if (i % 2 == 0)
            {
                accomplice.atlasRenderer.FlipHSimple(true);
            }
            accomplice.Init();
        }
    }
    //public void SetFrontParallaxPosition()
    //{
    //    frontParallaxController.SetParrallaxFactor();
    //    float posX = TRAIN_WORLD_POS_X + ((transform.position.x - TRAIN_WORLD_POS_X) * (frontParallaxController.parallaxFactor / parallaxController.parallaxFactor));
    //    Vector2 pos = new Vector2(posX, 0);
    //    frontParallaxController.SetWorldPos(pos);
    //}
}
