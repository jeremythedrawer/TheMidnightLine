using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static InsideBounds;

public class CalmPath : PathFinder
{
    public List<SeatBounds.SeatData> seats => pathData.chosenSeatBounds.seats;
    public SeatBounds.SeatData chosenSeat {  get; private set; }
    public Vector2 chosenStandPos { get; private set; }
    public Vector2 chosenSlidingDoorsPos { get; private set; }
    public int chosenSeatIndex { get; private set; }

    private float standingPosThreshold = 8f;

    //Setting paths
    public override void SetPath(Vector2 currentPos, PathData.NamedPosition lastPos, float colliderCentre)
    {
        base.SetPath(currentPos, lastPos, colliderCentre);

        if (pathData.chosenSeatBounds != null)
        {
            if (seats[chosenSeatIndex].filled) // find new path
            {
                pathData.pathIsSet = false;
            }
        }
        else if (chosenStandPos == Vector2.zero)
        {
            pathData.pathIsSet = false;
        }
    }
    public override void FindPath(Vector2 currentPos, Vector2 lastPos, float colliderCentre)
    {

        base.FindPath(currentPos, lastPos, colliderCentre);

        //In station
        if (pathData.trainController.kmPerHour == 0 && currentInsideBounds == null)
        {
            FindChosenSlidingDoors(currentPos);
            SlideDoorsPath();
            return;
        }

        if (currentInsideBounds == null) return;

        //In train
        FindChosenChairBounds(currentPos);

        if (pathData.chosenSeatBounds != null)
        {
            SeatPath(currentPos);
        }
        else if (chosenStandPos == Vector2.zero)
        {
            FindChosenStandingArea(currentPos);
            StandPath();
        }
    }

    //Adding to Paths
    private void SeatPath(Vector2 currentPos)
    {

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
        chosenSeat = seats[chosenSeatIndex];

        AddToPath(chosenSeat.pos, PathData.PosType.Seat);
    }

    private void StandPath()
    {
        AddToPath(chosenStandPos, PathData.PosType.Stand);
    }

    private void SlideDoorsPath()
    {
        AddToPath(chosenSlidingDoorsPos, PathData.PosType.SlidingDoors);
    }

    //Find target
    private void FindChosenChairBounds(Vector2 currentPos)
    {
        List<SeatBounds> setsOfSeats = currentInsideBounds.setsOfSeats;
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

        if (!currentInsideBounds.setsOfSeats[closestPosIndex].seats.All(seat => seat.filled)) // check if all seats in the found set of seats are full
        {
            pathData.chosenSeatBounds = setsOfSeats[closestPosIndex];
        }
        else if (!setsOfSeats[secondClosestPosIndex].seats.All(seat => seat.filled)) //check the second closest set of seats
        {
            pathData.chosenSeatBounds = setsOfSeats[secondClosestPosIndex];
        }
        else // all seats are already filled
        {
            pathData.chosenSeatBounds = null;
        }
    }

    private void FindChosenStandingArea(Vector2 currentPos)
    {
        float insideBoundsMin = currentInsideBounds.objectBounds.min.x;
        float insideBoundsMax = currentInsideBounds.objectBounds.max.x;

        float standingPosThresholdMin = Math.Max(currentPos.x - standingPosThreshold, insideBoundsMin);
        float standingPosThresholdMax = Math.Min(currentPos.x + standingPosThreshold, insideBoundsMax);

        List<StandNpcPosData> standingNpcAndWallPosList = currentInsideBounds.standingNpcAndWallPosList;

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
        chosenStandPos = new Vector2(randomPosX, currentPos.y);
    }

    private void FindChosenSlidingDoors(Vector2 currentPos)
    {
        List<SlideDoorBounds> slideDoorsList = pathData.trainController.slideDoorsList;

        float shortestDistance = float.MaxValue;
        int chosenSlideDoorsIndex = -1;
        for (int i = 0; i < slideDoorsList.Count - 1; i++)
        {
            float distance = Mathf.Abs(currentPos.x - slideDoorsList[i].transform.position.x);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                chosenSlideDoorsIndex = i;
            }
        }
        pathData.chosenSlideDoorBounds = slideDoorsList[chosenSlideDoorsIndex];
        chosenSlidingDoorsPos = new Vector2(pathData.chosenSlideDoorBounds.transform.position.x, currentPos.y);
    }
}
