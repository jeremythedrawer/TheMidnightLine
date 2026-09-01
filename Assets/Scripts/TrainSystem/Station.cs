using UnityEngine;
using static Passenger;
using static Train;
public class Station : MonoBehaviour
{
    public StationSO station;
    public TrainData trainStats;
    public TripData trip;
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

            PassengerBrain bystander = PassengerManager.GetNPC(trip.npcDataArray[bystanderProfile.npcPrefabIndex].prefab, spawnPos, platformRenderer.transform);
            
            bystander.profile = bystanderProfile;
            bystander.role = Role.Bystander;
            bystander.boardingStation = station;
            bystander.disembarkingStation = trip.stationsDataArray[bystanderProfile.disembarkingStationIndex];

            if (i % 2 == 0)
            {
                bystander.atlasRenderer.FlipHSimple(true);
            }
            bystander.Init();
        }

        int maxTraitorSpawnIndex = trip.traitorsSpawned + station.traitorSpawnCount;

        for (int i = trip.traitorsSpawned; i < maxTraitorSpawnIndex; i++)
        {
            totalNPCSSpawned++;
            TraitorProfile traitorProfile = trip.traitorProfiles[i];
            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainStats.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainStats.totalBounds.extents.x);

            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, 0);

            PassengerBrain traitor = PassengerManager.GetNPC(trip.npcDataArray[traitorProfile.npcProfile.npcPrefabIndex].prefab, spawnPos, platformRenderer.transform);
            traitor.profile = traitorProfile.npcProfile;
            traitor.role = Role.Traitor;
            traitor.boardingStation = station;
            traitor.disembarkingStation = trip.stationsDataArray[traitorProfile.npcProfile.disembarkingStationIndex];
            if (i % 2 == 0)
            {
                traitor.atlasRenderer.FlipHSimple(true);
            }
            traitor.Init();
        }
        trip.traitorsSpawned += station.traitorSpawnCount;

        for (int i = 0; i < station.accompliceProfiles.Length; i++)
        {
            totalNPCSSpawned++;
            NPCProfile accompliceProfile = station.accompliceProfiles[i];

            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainStats.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainStats.totalBounds.extents.x);

            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, 0);

            PassengerBrain accomplice = PassengerManager.GetNPC(trip.npcDataArray[accompliceProfile.npcPrefabIndex].prefab, spawnPos, platformRenderer.transform);

            accomplice.profile = accompliceProfile;
            accomplice.role = Role.Accomplice;
            accomplice.boardingStation = station;
            accomplice.disembarkingStation = trip.stationsDataArray[accompliceProfile.disembarkingStationIndex];

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
