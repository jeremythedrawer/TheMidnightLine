using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public PlayerBrain playerBrain;
    public TrainController trainController;
    public CanvasBounds canvasBounds;

    [Header("Parameters")]
    [SerializeField] private AnimationCurve startingDecelCurve;

    private List<ParallaxController> parallaxObjects = new List<ParallaxController>();
    private List<Spawner> spawners = new List<Spawner>();
    private int trainGroundLayer => LayerMask.NameToLayer("Train Ground");

    private float startingTrainSpeed => trainController.kmPerHour;
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

        startingTrainPos = trainController.transform.position;
        trainController.TrainInputs();
        await MoveTrainToDecelThreshold();
        await MoveTrainToStart();

        canvasBounds.SetCanvasData();
        await SetAllSeatData();
        EnableParallaxAndSpawners(true);
        //Player Enters onto train
    }


    private async Task MoveTrainToDecelThreshold()
    {
        while (trainController.currentStation == null) { await Task.Yield(); }
        while (trainController.trainBounds.boundsMaxX < trainController.currentStation.decelThreshold)
        {
            float newTrainPosX = startingTrainPos.x + trainController.metersTravelled;
            trainController.transform.position = new Vector2(newTrainPosX, startingTrainPos.y);
            await Task.Yield();
        }
    }
    private async Task MoveTrainToStart()
    {
        float initialPosX = trainController.transform.position.x;
        while (trainController.transform.position.x < 0)
        {
            float currentPosX = Mathf.Lerp(initialPosX, 0, trainController.normalizedTime);
            trainController.transform.position = new Vector2(currentPosX, startingTrainPos.y);
            await Task.Yield();
        }
        trainController.transform.position = new Vector2(0, startingTrainPos.y);
    }

    private async Task SetAllSeatData()
    {
        while (trainController.kmPerHour != 0) await Task.Yield();
        foreach (SeatBounds setsOfSeats in trainController.seatBoundsList)
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
            spawner.enabled = false;
        }
    }
}
