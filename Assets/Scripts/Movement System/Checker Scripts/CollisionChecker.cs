using UnityEngine;

public class CollisionChecker : MonoBehaviour
{
    public LayerMask groundLayer;

    public BoxCollider2D characterBoxCollider;

    public bool collisionGizmos;

    public Vector2 TotalOffset = new Vector2(0, 0);
    public float YOffset = 0f;
    [Range(0f, 2.0f)] 
    public float YSensorScale = 0.0f;

    [Range(-0.5f, 0.5f)] 
    public float groundedThreshold = -0.02f;

    [Range(-1.0f, -0.0f)]
    public float groundXSensorScale = -0.5f;

    [Range(-1.0f, 0.0f)] 
    public float wallXSensorScale = -0.47f;

    [Range(0.0f,1.0f)]
    public float CMJThreshold = 0.5f;

    [Range(0.0f,1.0f)]
    public float BHCThreshold = 0.25f;

    public bool grounded { get; private set; }


    //Catch Missed Jump(cmj) Bools
    private bool cmjShinEnabled;
    private bool cmjHeadEnabled;
    public bool cmjEnabled;

    //Bumped Head Correction (bhc) Bools
    private bool bhcMiddleEnabled;
    public bool bhcleftEnabled { get; private set; }
    public bool bhcRightEnabled { get; private set; }
    public bool bhcEnabled { get; private set; }

    //Stairs Bools
    private bool stairsShinEnabled;
    private bool stairsHeadEnabled;
    public bool stairsEnabled { get; private set; }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;

        if (collisionGizmos)
        {
            // Box collider size
            Vector2 colliderSize = characterBoxCollider.size;
            float colliderSizeY = colliderSize.y + YSensorScale;
        
            // x Sensor Scales
            float verticalColliderSizeX = colliderSize.x + groundXSensorScale;
            float horizontalColliderSizeX = colliderSize.x + wallXSensorScale;

            // Ground origin
            Vector2 groundLeftOrigin = (Vector2)transform.position + new Vector2(-verticalColliderSizeX, groundedThreshold) + TotalOffset;
            Vector2 groundRightOrigin = (Vector2)transform.position + new Vector2(verticalColliderSizeX, groundedThreshold) + TotalOffset;

            // Feet
            Vector2 feetLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, groundedThreshold) + TotalOffset;
            Vector2 feetRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, groundedThreshold) + TotalOffset;

            // Shin origin
            Vector2 shinLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, CMJThreshold) + TotalOffset;
            Vector2 shinRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, CMJThreshold) + TotalOffset;

            // Head origin wall 
            Vector2 headWallLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, colliderSizeY) + TotalOffset;
            Vector2 headWallRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, colliderSizeY) + TotalOffset;

            // Head origin ceiling
            Vector2 headCeilingLeftOrigin = (Vector2)transform.position + new Vector2(-verticalColliderSizeX, colliderSizeY) + TotalOffset;
            Vector2 headCeilingRightOrigin = (Vector2)transform.position + new Vector2(verticalColliderSizeX, colliderSizeY) + TotalOffset;

            Vector2 middleLeftOrigin = (Vector2)transform.position + new Vector2(-BHCThreshold, colliderSizeY) + TotalOffset;
            Vector2 middleRightOrigin = (Vector2)transform.position + new Vector2(BHCThreshold, colliderSizeY) + TotalOffset;

            Gizmos.color = grounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundLeftOrigin, groundRightOrigin);

            Gizmos.color = cmjShinEnabled ? Color.green : Color.red;
            Gizmos.DrawLine(feetLeftOrigin, shinLeftOrigin);
            Gizmos.DrawLine(shinLeftOrigin, headWallLeftOrigin);
            Gizmos.DrawLine(shinLeftOrigin, new Vector2(transform.position.x, shinLeftOrigin.y));

            Gizmos.color = cmjShinEnabled ? Color.green : Color.blue;
            Gizmos.DrawLine(feetRightOrigin, shinRightOrigin);
            Gizmos.DrawLine(shinRightOrigin, headWallRightOrigin);
            Gizmos.DrawLine(shinRightOrigin, new Vector2(transform.position.x, shinRightOrigin.y));

            Gizmos.color = bhcleftEnabled ? Color.green : Color.red;
            Gizmos.DrawLine(headCeilingLeftOrigin, middleLeftOrigin);
            Gizmos.DrawLine(middleLeftOrigin, middleLeftOrigin - new Vector2(0, colliderSizeY));

            Gizmos.color = bhcRightEnabled ? Color.green : Color.blue;
            Gizmos.DrawLine(headCeilingRightOrigin, middleRightOrigin);
            Gizmos.DrawLine(middleRightOrigin, middleRightOrigin - new Vector2(0, colliderSizeY));

            Gizmos.color = bhcMiddleEnabled ? Color.green : Color.yellow;
            Gizmos.DrawLine(middleLeftOrigin, middleRightOrigin);
        }
    }
    private void FixedUpdate()
    {
        //box collider size
        Vector2 colliderSize = characterBoxCollider.size;
        float colliderSizeY = colliderSize.y + YSensorScale;

        //x Sensor Scales
        float verticalColliderSizeX = colliderSize.x + groundXSensorScale;
        float horizontalColliderSizeX = colliderSize.x + wallXSensorScale;

        //ground origin
        Vector2 groundLeftOrigin = (Vector2)transform.position + new Vector2(-verticalColliderSizeX, groundedThreshold) + TotalOffset;
        Vector2 groundRightOrigin = (Vector2)transform.position + new Vector2(verticalColliderSizeX, groundedThreshold) + TotalOffset;

        //feet origin
        Vector2 feetLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, 0.0f) + TotalOffset;
        Vector2 feetRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, 0.0f) + TotalOffset;

        //shin origin
        Vector2 shinLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, CMJThreshold) + TotalOffset;
        Vector2 shinRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, CMJThreshold) + TotalOffset;

        //head origin wall 
        Vector2 headWallLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, colliderSizeY) + TotalOffset;
        Vector2 headWallRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, colliderSizeY) + TotalOffset;

        //head origin ceiling
        Vector2 headCeilingLeftOrigin = (Vector2)transform.position + new Vector2(-verticalColliderSizeX, colliderSizeY) + TotalOffset;
        Vector2 headCeilingRightOrigin = (Vector2)transform.position + new Vector2(verticalColliderSizeX, colliderSizeY) + TotalOffset;

        CheckGround(colliderSize, groundLeftOrigin, groundRightOrigin);
        CheckWall(colliderSize, feetLeftOrigin, feetRightOrigin, shinLeftOrigin, shinRightOrigin, headWallLeftOrigin, headWallRightOrigin);
        CheckCeiling(colliderSize, headCeilingLeftOrigin, headCeilingRightOrigin, colliderSizeY);
    }

    private void CheckGround(Vector2 colliderSize, Vector2 groundLeftOrigin, Vector2 groundRightOrigin)
    {

        RaycastHit2D groundHit = Physics2D.Linecast(groundLeftOrigin, groundRightOrigin, groundLayer);
        grounded = groundHit.collider != null;

        Debug.DrawLine(groundLeftOrigin, groundRightOrigin, grounded ? Color.green : Color.red);
    }

    private void CheckWall(Vector2 colliderSize,
                           Vector2 feetLeftOrigin, 
                           Vector2 feetRightOrigin, 
                           Vector2 shinLeftOrigin, 
                           Vector2 shinRightOrigin, 
                           Vector2 headWallLeftOrigin, 
                           Vector2 headWallRightOrigin)
    {

        //feet raycasts
        RaycastHit2D feetLeftHit = Physics2D.Linecast(feetLeftOrigin, shinLeftOrigin, groundLayer);
        RaycastHit2D feetRightHit = Physics2D.Linecast(feetRightOrigin, shinRightOrigin, groundLayer);

        //head raycasts
        RaycastHit2D headLeftHit = Physics2D.Linecast(shinLeftOrigin, headWallLeftOrigin, groundLayer);
        RaycastHit2D headRightHit = Physics2D.Linecast(shinRightOrigin, headWallRightOrigin, groundLayer);


        cmjShinEnabled = feetLeftHit.collider != null || feetRightHit.collider != null;
        cmjHeadEnabled = headLeftHit.collider != null || headRightHit.collider != null;

        if (characterBoxCollider.offset.y == 0)
        {
            cmjEnabled = cmjShinEnabled && !cmjHeadEnabled;
        }
        if (collisionGizmos)
        {
            Debug.DrawLine(feetLeftOrigin, shinLeftOrigin, cmjShinEnabled ? Color.green : Color.red);
            Debug.DrawLine(feetRightOrigin, shinRightOrigin, cmjShinEnabled ? Color.green : Color.blue);

            Debug.DrawLine(shinLeftOrigin, headWallLeftOrigin, cmjHeadEnabled ? Color.green : Color.red);
            Debug.DrawLine(shinRightOrigin, headWallRightOrigin, cmjHeadEnabled ? Color.green : Color.blue);

            Debug.DrawLine(shinLeftOrigin, new Vector2 (transform.position.x, shinLeftOrigin.y), cmjShinEnabled ? Color.green : Color.red);
            Debug.DrawLine(shinRightOrigin, new Vector2(transform.position.x, shinRightOrigin.y), cmjShinEnabled ? Color.green : Color.blue);
        }



    }

    private void CheckCeiling(Vector2 colliderSize, Vector2 headCeilingLeftOrigin, Vector2 headCeilingRightOrigin, float colliderSizeY)
    {
        
        Vector2 middleLeftOrigin = (Vector2)transform.position + new Vector2 (-BHCThreshold, colliderSizeY);
        Vector2 middleRightOrigin = (Vector2)transform.position + new Vector2 (BHCThreshold, colliderSizeY);

        RaycastHit2D leftHit = Physics2D.Linecast(headCeilingLeftOrigin, middleLeftOrigin, groundLayer);
        RaycastHit2D middleHit = Physics2D.Linecast(middleLeftOrigin, middleRightOrigin, groundLayer);
        RaycastHit2D rightHit = Physics2D.Linecast(headCeilingRightOrigin, middleRightOrigin, groundLayer);

        if(!grounded)
        {
            bhcleftEnabled = leftHit.collider != null;
            bhcMiddleEnabled = middleHit.collider != null;
            bhcRightEnabled = rightHit.collider != null;
        }
        else
        {
            bhcleftEnabled = false;
            bhcMiddleEnabled = false;
            bhcRightEnabled = false;
        }
        bhcEnabled = !bhcMiddleEnabled && (bhcleftEnabled || bhcRightEnabled);

        if (collisionGizmos)
        {
            Debug.DrawLine(headCeilingLeftOrigin, middleLeftOrigin, bhcleftEnabled ? Color.green : Color.red);
            Debug.DrawLine(middleLeftOrigin, middleRightOrigin, bhcMiddleEnabled ? Color.green : Color.yellow);
            Debug.DrawLine(headCeilingRightOrigin, middleRightOrigin, bhcRightEnabled ? Color.green : Color.blue);

            Debug.DrawLine(middleLeftOrigin, middleLeftOrigin - new Vector2 (0, colliderSizeY), bhcleftEnabled ? Color.green : Color.red);
            Debug.DrawLine(middleRightOrigin, middleRightOrigin - new Vector2 (0, colliderSizeY), bhcRightEnabled ? Color.green : Color.blue);
        }

    }
}
