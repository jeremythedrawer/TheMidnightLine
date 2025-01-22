using UnityEngine;

public class CalmPath : PathFinder
{
    public override void SetPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        base.SetPath(currentPos, targetPos, colliderCentre);

    }
    public override void FindPath(Vector2 currentPos, Vector2 targetPos, float colliderCentre)
    {
        base.FindPath(currentPos, targetPos, colliderCentre);

        //TODO: if chairs in nearby radius is more than 0, trigger Chair Path, else trigger Stand Path
    }

    private void ChairPath()
    {
        //TODO: find closest chair in nearby radius
    }

    private void StandPath()
    {
        //TODO: find largest space in nearby radius
    }
}
