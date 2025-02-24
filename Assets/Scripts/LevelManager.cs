using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public PlayerBrain playerBrain;
    public TrainData trainData;
    public TrainController trainController;
    public CanvasBounds canvasBounds;

    private List<ParallaxController> parallaxObjects = new List<ParallaxController>();
    private List<Spawner> spawners = new List<Spawner>();

    private Vector2 startingTrainPos;
    void Start()
    {
        StartSequence();
    }

    private async void StartSequence()
    {
        parallaxObjects.AddRange(FindObjectsByType<ParallaxController>(FindObjectsSortMode.None));
        spawners.AddRange(FindObjectsByType<Spawner>(FindObjectsSortMode.None));

        EnableParallaxAndSpawners(false);

        startingTrainPos = trainData.transform.position;
        trainController.TrainInputs();
        await MoveTrainToDecelThreshold();
        await MoveTrainToStart();

        canvasBounds.SetCanvasData();
        await SetTrainObjectsData();
        EnableParallaxAndSpawners(true);
        //Player Enters onto train
    }


    private async Task MoveTrainToDecelThreshold()
    {
        while (trainData.currentStation == null) { await Task.Yield(); }
        while (trainData.boundsMaxX < trainData.currentStation.decelThreshold)
        {
            float newTrainPosX = startingTrainPos.x + trainData.metersTravelled;
            trainData.transform.position = new Vector2(newTrainPosX, startingTrainPos.y);
            await Task.Yield();
        }
    }
    private async Task MoveTrainToStart()
    {
        float initialPosX = trainData.transform.position.x;
        while (trainData.transform.position.x < 0)
        {
            float currentPosX = Mathf.Lerp(initialPosX, 0, trainController.normalizedTime);
            trainData.transform.position = new Vector2(currentPosX, startingTrainPos.y);
            await Task.Yield();
        }
        trainData.transform.position = new Vector2(0, startingTrainPos.y);
    }

    private async Task SetTrainObjectsData()
    {
        while (trainData.kmPerHour != 0) await Task.Yield();
        foreach (SeatBounds setsOfSeats in trainData.seatBoundsList)
        {
            setsOfSeats.SetSeatData();
        }
    }

    private void EnableParallaxAndSpawners(bool isEnabled)
    {
        foreach (ParallaxController parallaxController in parallaxObjects)
        {
            parallaxController.enabled = isEnabled;
        }
        foreach (Spawner spawner in spawners)
        {
            if (spawner is LoopingTileSpawner)
            {
                if (spawner.startSpawnDistance != 0)
                {
                    spawner.gameObject.SetActive(isEnabled);
                }
            }
            spawner.enabled = isEnabled;
        }
    }
}
