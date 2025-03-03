using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static InsideBounds;

public class CalmPath : ToEnvironmentPaths
{
    protected SeatBounds chosenChairBounds => pathData.chosenSeatBounds;
    public List<SeatBounds.SeatData> seats => pathData.chosenSeatBounds.seats;
    public SeatBounds.SeatData chosenSeat {  get; private set; }
    public Vector2 chosenStandPos { get; private set; }
    public int chosenSeatIndex { get; private set; }

    private BystanderBrain bystanderBrain;

    private float standingPosThreshold = 8f;

    public override void Awake()
    {
        base.Awake();
        bystanderBrain = npcCore as BystanderBrain;
    }

    public override async void SetPath(Vector2 currentPos, float colliderCentre)
    {
        base.SetPath(currentPos, colliderCentre);

        //slide doors
        bool onStationGround = pathData.collisionChecker.activeGroundLayer == 1 << stationGroundLayer;
        bool arrivedAtDepartureStation = bystanderBrain?.departureStation == trainData.currentStation;
        bool enteringTrain = onStationGround && !arrivedAtDepartureStation;
        bool exitingTrain = pathData.trainData.kmPerHour == 0 && arrivedAtDepartureStation;

        if (enteringTrain || exitingTrain)
        {
            //when train position is updating at start of level
            if (npcCore.startingStation == GlobalReferenceManager.Instance.stations[0] && enteringTrain)
            {
                await EnterTrain(currentPos);
            }
            else
            {
                AddToPath(SlideDoorsPath(currentPos), PathData.PosType.SlidingDoors);
            }
        }
        else
        {
            await GetSeatPath(currentPos);

            if (pathData.chosenSeatBounds == null || chosenSeat.filled)
            {
                await GetStandPath(currentPos);
            }
        }
    }
    private async Task EnterTrain(Vector2 currentPos)
    {
        while (trainData.kmPerHour > 0) { await Task.Yield(); }
        chosenSlidingDoorsPos = SlideDoorsPath(currentPos);
        AddToPath(chosenSlidingDoorsPos, PathData.PosType.SlidingDoors);
    }
    private async Task GetSeatPath(Vector2 currentPos)
    {
        while (pathData.currentInsideBounds == null) { await Task.Yield(); }

        while (!npcCore.isSitting)
        {
            pathData.chosenSeatBounds = FindChosenChairBounds(currentPos);
            if (pathData.chosenSeatBounds == null) return;

            float[] seatDistances = new float[seats.Count];
            for (int i = 0; i < seatDistances.Length; i++)
            {
                if (!seats[i].filled) // check if seat is empty and add to array
                {
                    float distanceToSeat = Vector2.Distance(currentPos, seats[i].pos);
                    seatDistances[i] = distanceToSeat;
                }
                else
                {
                    seatDistances[i] = float.MaxValue;
                }
            }

            float smallestDistance = seatDistances.Min();
            chosenSeatIndex = Array.IndexOf(seatDistances, smallestDistance);

            if (!seats[chosenSeatIndex].filled)
            {
                chosenSeat = seats[chosenSeatIndex];
                if (pathToTarget.Count == 0)
                {
                    AddToPath(chosenSeat.pos, PathData.PosType.Seat);
                }
                else if (pathToTarget[pathToTarget.Count - 1].value != chosenSeat.pos)
                {
                    npcCore.movementInputs.canMove = false;
                    pathToTarget[pathToTarget.Count - 1] = new PathData.NamedPosition(chosenSeat.pos, PathData.PosType.Seat);
                    await Task.Delay(500);
                    npcCore.movementInputs.canMove = true;
                }
            }
            
            if (chosenSeat.filled) continue;
            if (chosenChairBounds.isFull) break;
            await Task.Yield();
        }
    }
    private async Task GetStandPath(Vector2 currentPos)
    {
        while (pathData.currentInsideBounds == null) { await Task.Yield(); }
        chosenStandPos = FindChosenStandingArea(currentPos);
        AddToPath(chosenStandPos, PathData.PosType.Stand);
    }

    //Find target
    private SeatBounds FindChosenChairBounds(Vector2 currentPos)
    {
        List<SeatBounds> setsOfSeats = pathData.currentInsideBounds.setsOfSeats;
        int setsOfSeatsCount = setsOfSeats.Count;

        float[] setOfSeatsDistances = new float[setsOfSeatsCount];

        for (int i = 0; i < setsOfSeatsCount; i++)
        {
            setOfSeatsDistances[i] = Vector2.Distance(currentPos, setsOfSeats[i].transform.position); //measuring the distance between the npc pos and each set of seats in the carriage
        }

        float smallestDistance = float.MaxValue;
        float secondSmallestDistance = float.MaxValue;

        foreach (float distance in setOfSeatsDistances)
        {
            if (distance < smallestDistance) //iterating and comparing each index until the smallest value is found
            {
                secondSmallestDistance = smallestDistance;
                smallestDistance = distance;
            }
            else if (distance < secondSmallestDistance && distance != smallestDistance) //whatever is left is check one more time to find the second smallest distance
            {
                secondSmallestDistance = distance;
            }
        }

        int closestPosIndex = Array.IndexOf(setOfSeatsDistances, smallestDistance);
        int secondClosestPosIndex = Array.IndexOf(setOfSeatsDistances, secondSmallestDistance);

        if (!pathData.currentInsideBounds.setsOfSeats[closestPosIndex].seats.All(seat => seat.filled)) // check if all seats in the found set of seats are full
        {
            return setsOfSeats[closestPosIndex];
        }
        else if (!setsOfSeats[secondClosestPosIndex].seats.All(seat => seat.filled)) //check the second closest set of seats
        {
            return setsOfSeats[secondClosestPosIndex];
        }
        else // all seats are already filled
        {
            return null;
        }
    }

    private Vector2 FindChosenStandingArea(Vector2 currentPos)
    {
        float insideBoundsMin = pathData.currentInsideBounds.objectBounds.min.x;
        float insideBoundsMax = pathData.currentInsideBounds.objectBounds.max.x;

        float standingPosThresholdMin = Math.Max(currentPos.x - standingPosThreshold, insideBoundsMin);
        float standingPosThresholdMax = Math.Min(currentPos.x + standingPosThreshold, insideBoundsMax);

        List<StandNpcPosData> standingNpcAndWallPosList = pathData.currentInsideBounds.standingNpcAndWallPosList;

        StandNpcPosData largestDistanceSelected = new StandNpcPosData(standingPosThresholdMin, standingPosThresholdMax);
        float largestDistance = float.MaxValue;


        for (int i = 0; i < standingNpcAndWallPosList.Count - 1; i++)
        {
            if (standingNpcAndWallPosList[i].startPos > standingPosThresholdMin || standingNpcAndWallPosList[i].endPos < standingPosThresholdMax)
            {
                float distance = standingNpcAndWallPosList[i].endPos - standingNpcAndWallPosList[i].startPos;
                if (distance > largestDistance)
                {
                    largestDistance = distance;
                    largestDistanceSelected = standingNpcAndWallPosList[i];
                }
            }
        }

        float randomPosX = UnityEngine.Random.Range(largestDistanceSelected.startPos, largestDistanceSelected.endPos);
        return new Vector2(randomPosX, currentPos.y);  
    }
}
