using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
public class LevelManager : MonoBehaviour
{
    private TrainData trainData;
    private TrainController trainController;

    private Vector2 startingTrainPos;

    void Start()
    {
        SetCompenents();
        StartSequence();
    }

    private async void StartSequence()
    {
        startingTrainPos = trainData.transform.position;
        trainController.TrainInputs();
        await MoveTrainToDecelThreshold();
        await MoveTrainToStart();
        await SetTrainObjectsData();
    }
    private async Task MoveTrainToDecelThreshold()
    {
        while (trainData.nextStation == null) { await Task.Yield(); }
        while (trainData.boundsMaxX < trainData.nextStation.decelThreshold)
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
            float currentPosX = Mathf.Lerp(initialPosX, 0, trainController.easeOutTime);
            trainData.transform.position = new Vector2(currentPosX, startingTrainPos.y);
            await Task.Yield();
        }
        trainData.transform.position = new Vector2(0, startingTrainPos.y);
        trainData.arrivedToStartPosition = true;
    }
    private async Task SetTrainObjectsData()
    {
        while (trainData.kmPerHour != 0) await Task.Yield();
        foreach (SeatBounds setsOfSeats in trainData.seatBoundsList)
        {
            setsOfSeats.SetSeatData();
        }
    }

    private void SetCompenents()
    {
        trainData = GlobalReferenceManager.Instance.trainData;
        trainController = GlobalReferenceManager.Instance.trainController;
    }
}
