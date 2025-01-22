using System.Collections.Generic;
using UnityEngine;

public class BystanderController : NPCController
{
    [Header("Paths")]
    public CalmPath calmPath;

    private List<PathData.NamedPosition> calmPathToTarget => calmPath.pathData.pathToTarget;

    public void CalmInputs()
    {
        if (npcCore.behaviourParams.isArmDistance) //TODO: and seat is not filled
        {
            //sit input is true
        }
        else
        {
            //sit input is false
            FollowCalmPath();
        }
    }
    private void FollowCalmPath()
    {
        colliderCenter = npcCore.boxCollider2D.size.y / 2f;
        currentPos = new Vector2(transform.position.x, transform.position.y) + new Vector2(0, colliderCenter);

        calmPath.SetPath(currentPos, targetPos, colliderCenter);

        //handle direction
        if (currentPos.x < calmPathToTarget[0].value.x)
        {
            movementInputs.walkInput = 1;
        }
        else
        {
            movementInputs.walkInput = -1;
        }
    }
}
