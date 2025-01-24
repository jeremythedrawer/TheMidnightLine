using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class CalmPath : PathFinder
{
    public List<SeatBounds.SeatData> seats => pathData.chosenSeatBounds.seats;
    public SeatBounds.SeatData chosenSeat {  get; private set; }
    public Vector2 chosenStandPos { get; private set; }
    public int chosenSeatIndex { get; private set; }
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
        float insideBoundsMin = currentInsideBounds.Bounds.min.x;
        float insideBoundsMax = currentInsideBounds.Bounds.max.x;


        //TODO find larget distances between bystanders
        if (currentInsideBounds.npcStandingPosList.Count == 0)
        {
            float randomPosX = UnityEngine.Random.Range(insideBoundsMax, insideBoundsMin);
            chosenStandPos = new Vector2(randomPosX, currentPos.y);
        }
    }
}
