using UnityEngine;
using static Passenger;
using static Train;
public class Station : MonoBehaviour
{
    public StationSO station;
    public TrainData trainData;
    public Options options;

    public AtlasRenderer platformRenderer;
    public Transform exitTransform;

    public ParallaxController parallaxController;

    public PassengerBrain[] passengers;
    public int passengerCount;
    public void OnEnable()
    {
        station.exitLocalPosX = exitTransform.localPosition.x;
        parallaxController.SetParrallaxFactor();
        parallaxController.SetWorldPos(transform.position);

        passengers = new PassengerBrain[64];
    }
    public void SpawnNPCs()
    {
        int totalNPCSSpawned = 0;
        for (int i = 0; i < station.bystanderProfiles.Length; i++)
        {
            totalNPCSSpawned++;
            PassengerProfile bystanderProfile = station.bystanderProfiles[i];
            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainData.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainData.totalBounds.extents.x);

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

            passengers[passengerCount] = bystander;
            passengerCount++;
        }

        int maxTraitorSpawnIndex = options.curTrip.traitorsSpawned + station.traitorSpawnCount;

        for (int i = options.curTrip.traitorsSpawned; i < maxTraitorSpawnIndex; i++)
        {
            totalNPCSSpawned++;
            TraitorProfile traitorProfile = options.curTrip.traitorProfiles[i];
            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainData.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainData.totalBounds.extents.x);

            Vector3 spawnPos = new Vector3(randXPos, transform.position.y + 0.1f, 0);

            PassengerBrain traitor = PassengerManager.GetNPC(options.curTrip.passengers[traitorProfile.passengerProfile.npcPrefabIndex].prefab, spawnPos, platformRenderer.transform);
            traitor.profile = traitorProfile.passengerProfile;
            traitor.role = Role.Traitor;
            traitor.boardingStation = station;
            traitor.disembarkingStation = options.curTrip.stationsDataArray[traitorProfile.passengerProfile.disembarkingStationIndex];
            if (i % 2 == 0)
            {
                traitor.atlasRenderer.FlipHSimple(true);
            }
            traitor.Init();

            passengers[passengerCount] = traitor;
            passengerCount++;
        }
        options.curTrip.traitorsSpawned += station.traitorSpawnCount;

        for (int i = 0; i < station.accompliceProfiles.Length; i++)
        {
            totalNPCSSpawned++;
            PassengerProfile accompliceProfile = station.accompliceProfiles[i];

            float randXPos = Random.Range(platformRenderer.bounds.extents.x - trainData.totalBounds.extents.x, platformRenderer.bounds.extents.x + trainData.totalBounds.extents.x);

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

            passengers[passengerCount] = accomplice;
            passengerCount++;
        }

        HenchmanBrain henchman = Instantiate(options.henchmanPrefab);
        Vector3 henchmanStartPos = new Vector3();
        henchmanStartPos.x = -trainData.totalBounds.extents.x;
        henchmanStartPos.y = 0;
        henchmanStartPos.z = 0;

        henchman.transform.position = henchmanStartPos;

        henchman.transform.SetParent(platformRenderer.transform, worldPositionStays: true);
    }
}
