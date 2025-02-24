using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TrainController : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private AnimationCurve accelerationCurve;

    public float normalizedTime { get; private set; }
    private TrainData trainData => GetComponent<TrainData>();
    private StationData currentStation => trainData.currentStation;
    private float boundsMaxX => trainData.boundsMaxX;
    private List<SlideDoorBounds> slideDoorsList => trainData.slideDoorsList;

    public async void TrainInputs()
    {   
        while (boundsMaxX < currentStation.decelThreshold) { await Task.Yield(); } // wait until train cross decel threshold
        await UpdateSpeed(0);
        await UnlockDoors();
        await LockDoors();
        await ParentCharacters();
        await UpdateSpeed(100);
    }
    private async Task UpdateSpeed(float newSpeed)
    {
        float startSpeed = trainData.kmPerHour;
        float stoppingDistance = currentStation.accelerationThresholds + (boundsMaxX - trainData.boundsHalfX);
        float accelationTime = (2 * stoppingDistance) / (Mathf.Max(startSpeed, newSpeed) / trainData.kmConversion); // using the equation of motion v=u+at where t is equal to 2s/u
        float elapsedTime = 0f;
        while (trainData.kmPerHour != newSpeed)
        {
            elapsedTime += Time.deltaTime;

            normalizedTime = Mathf.Clamp01(elapsedTime / accelationTime);
            normalizedTime = accelerationCurve.Evaluate(normalizedTime);
            trainData.kmPerHour = Mathf.Lerp(startSpeed, newSpeed, normalizedTime);

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
            if (character is AgentBrain) character.transform.SetParent(trainData.agentsParent);
        }
        await Task.Yield();
    }
}
