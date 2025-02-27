using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    public float easeOutTime { get; private set; }
    public float easeInTime { get; private set; }
    private TrainData trainData => GlobalReferenceManager.Instance.trainData;
    private StationData currentStation => trainData.currentStation;
    private float boundsMaxX => trainData.boundsMaxX;
    private List<SlideDoorBounds> slideDoorsList => trainData.slideDoorsList;

    public float setSpeed { get; private set; }
    private void Start()
    {
        setSpeed = trainData.kmPerHour;
    }
    public async void TrainInputs()
    {   
        while (trainData == null) { await Task.Yield(); }
        while (currentStation == null) { await Task.Yield(); }
        while (boundsMaxX < currentStation.decelThreshold) { await Task.Yield(); } // wait until train cross decel threshold
        await UpdateSpeed(0);
        await UnlockDoors();
        await LockDoors();
        await ParentCharacters();
        await UpdateSpeed(1000);
    }
    private async Task UpdateSpeed(float newSpeed)
    {
        setSpeed = newSpeed;

        float startSpeed = trainData.kmPerHour;

        float stoppingDistance = currentStation.accelerationThresholds + trainData.boundsHalfXDistance;

        float startMetersTravelled = trainData.metersTravelled;
        float newMetersTravelled = startMetersTravelled + stoppingDistance;

        WheelData sampledWheel = trainData.wheels[0];
        float startWheelRotation = sampledWheel.transform.localEulerAngles.z;

        float accelationTime = (2 * stoppingDistance) / (Mathf.Max(startSpeed, newSpeed) * trainData.kmConversion); // using the equation of motion v=u+at where t is equal to 2s/u
        float elapsedTime = 0f;


        while (trainData.kmPerHour != newSpeed)
        {
            elapsedTime += Time.deltaTime;

            easeOutTime = Mathf.Clamp01(elapsedTime / accelationTime);
            easeInTime = Mathf.Clamp01(elapsedTime / accelationTime);

            easeOutTime = 1 - Mathf.Pow(1 - easeOutTime, 2f);
            easeInTime = Mathf.Pow(easeInTime, 2f);

            trainData.kmPerHour = Mathf.Lerp(startSpeed, newSpeed, easeOutTime); // kmPerHours only uses easeOut
            float meterTravelledT = startSpeed > newSpeed ? easeOutTime : easeInTime; //metersTravelled uses both depending if the train is accerlating or decelerating
            trainData.metersTravelled = Mathf.Lerp(startMetersTravelled, newMetersTravelled, meterTravelledT);

            //wheels
            float distanceTravelled = trainData.metersTravelled - startMetersTravelled;
            float wheelRotation = (distanceTravelled / sampledWheel.circumference) * 360f;
            float currentRotation = startWheelRotation - wheelRotation;
            foreach (WheelData wheel in trainData.wheels)
            {
                wheel.transform.localEulerAngles = new Vector3(0, 0, currentRotation);
            }

            await Task.Yield();
        }
    }
    private async Task UnlockDoors()
    {
        foreach (SlideDoorBounds slideDoors in slideDoorsList)
        {
            slideDoors.UnlockDoors();
        }
        await Task.Yield();
    }

    private async Task LockDoors()
    {
        while (currentStation.charactersList.Count != 0) { await Task.Yield(); }
        await Task.Delay(1000);
        List<SlideDoorBounds> openDoors = slideDoorsList.Where(slideDoors => slideDoors.openDoor).ToList();
        foreach (SlideDoorBounds slideDoors in openDoors)
        {
            slideDoors.CloseDoors();
        }
        while (openDoors.Any(openDoor => openDoor.normMoveDoorTime < 1.0f)) { await Task.Yield(); }
        foreach (SlideDoorBounds slideDoors in slideDoorsList)
        {
            slideDoors.LockDoors();
        }
    }

    private async Task ParentCharacters()
    {
        foreach (StateCore character in trainData.charactersList)
        {
            if (character is BystanderBrain) character.transform.SetParent(trainData.bystandersParent);
            else if (character is AgentBrain) character.transform.SetParent(trainData.agentsParent);
            else if (character is PlayerBrain) character.transform.SetParent(trainData.playerParent);
        }
        await Task.Yield();
    }
}
