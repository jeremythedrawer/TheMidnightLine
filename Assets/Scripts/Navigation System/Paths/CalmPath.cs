using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class CalmPath : PathFinder
{
    public List<SeatBounds.SeatData> seats => pathData.chosenSeatBounds.seats;
    public SeatBounds.SeatData chosenSeat {  get; private set; }
    public int chosenSeatIndex { get; private set; }
    public override void SetPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        base.SetPath(currentPos, targetPos, colliderCentre);
            if (seats[chosenSeatIndex].filled)
            {
                pathIsSet = false;
            }

    }
    public override void FindPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        base.FindPath(currentPos, targetPos, colliderCentre);
        //TODO: if chairs in nearby radius is more than 0, trigger Chair Path, else trigger Stand Path
        SeatPath(currentPos);
    }

    private void SeatPath(Vector2 currentPos)
    {
        FindChosenChairBounds(currentPos);

        if (pathData.chosenSeatBounds != null)
        {
            float[] seatDistances = new float[seats.ToArray().Length];
            for (int i = 0; i < seats.ToArray().Length; i++)
            {
                if (!seats[i].filled) // check if seat is empty
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
            //Debug.Log("filled: " + chosenSeat.filled);

            AddToPath(chosenSeat.pos, PathData.PosType.Chair);

        }
    }

    private void StandPath()
    {
        //TODO: find largest space in nearby radius
    }
}
