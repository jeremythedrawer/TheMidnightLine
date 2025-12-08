using UnityEngine;

public class StalkPath : ToPlayerPaths
{
    public override void InsideBoundsPath(Vector2 currentPos, Vector2 targetPos)
    {
        base.InsideBoundsPath(currentPos, targetPos);
    }
}
