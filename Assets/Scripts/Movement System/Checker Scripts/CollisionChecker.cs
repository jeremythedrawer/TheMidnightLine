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

    [Range(0.0f, 1.0f)]
    public float cbcThreshold = 0.5f;

    [Range(0.0f, 1.0f)]
    public float bhcThreshold = 0.25f;

    public bool grounded { get; private set; }


    //Catch Missed Jump(cmj) Bools
    private bool cbcShinEnabled;
    private bool cbcHeadEnabled;
    public bool cbcEnabled;

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
            OriginPoints originPoints = CalculateOriginPoints();

            Vector2 colliderSizeY = new Vector2(0, originPoints.ColliderSizeY);

            // Ground origin
            Vector2 groundLeftOrigin = originPoints.GroundLeftOrigin;
            Vector2 groundRightOrigin = originPoints.GroundRightOrigin;

            // Feet Origin
            Vector2 feetLeftOrigin = originPoints.FeetLeftOrigin;
            Vector2 feetRightOrigin = originPoints.FeetRightOrigin;

            // Shin origin
            Vector2 shinLeftOrigin = originPoints.ShinLeftOrigin;
            Vector2 shinRightOrigin = originPoints.ShinRightOrigin;

            // Head origin wall 
            Vector2 headWallLeftOrigin = originPoints.HeadWallLeftOrigin;
            Vector2 headWallRightOrigin = originPoints.HeadWallRightOrigin;

            // Head origin ceiling
            Vector2 headCeilingLeftOrigin = originPoints.HeadCeilingLeftOrigin;
            Vector2 headCeilingRightOrigin = originPoints.HeadCeilingRightOrigin;

            Vector2 middleLeftOrigin = originPoints.MiddleLeftOrigin;
            Vector2 middleRightOrigin = originPoints.MiddleRightOrigin;

            Gizmos.color = grounded ? Color.green : Color.red;
            Gizmos.DrawLine(groundLeftOrigin, groundRightOrigin);

            Gizmos.color = cbcShinEnabled ? Color.green : Color.red;
            Gizmos.DrawLine(feetLeftOrigin, shinLeftOrigin);
            Gizmos.DrawLine(shinLeftOrigin, headWallLeftOrigin);
            Gizmos.DrawLine(shinLeftOrigin, new Vector2(transform.position.x, shinLeftOrigin.y));

            Gizmos.color = cbcShinEnabled ? Color.green : Color.blue;
            Gizmos.DrawLine(feetRightOrigin, shinRightOrigin);
            Gizmos.DrawLine(shinRightOrigin, headWallRightOrigin);
            Gizmos.DrawLine(shinRightOrigin, new Vector2(transform.position.x, shinRightOrigin.y));

            Gizmos.color = bhcleftEnabled ? Color.green : Color.red;
            Gizmos.DrawLine(headCeilingLeftOrigin, middleLeftOrigin);
            Gizmos.DrawLine(middleLeftOrigin, middleLeftOrigin - colliderSizeY);

            Gizmos.color = bhcRightEnabled ? Color.green : Color.blue;
            Gizmos.DrawLine(headCeilingRightOrigin, middleRightOrigin);
            Gizmos.DrawLine(middleRightOrigin, middleRightOrigin - colliderSizeY);

            Gizmos.color = bhcMiddleEnabled ? Color.green : Color.yellow;
            Gizmos.DrawLine(middleLeftOrigin, middleRightOrigin);
        }
    }
    private void FixedUpdate()
    {
        //box collider size
        Vector2 colliderSize = characterBoxCollider.size;
        OriginPoints originPoints = CalculateOriginPoints();

        Vector2 colliderSizeY = new Vector2(0, originPoints.ColliderSizeY);

        // Ground origin
        Vector2 groundLeftOrigin = originPoints.GroundLeftOrigin;
        Vector2 groundRightOrigin = originPoints.GroundRightOrigin;

        // Feet Origin
        Vector2 feetLeftOrigin = originPoints.FeetLeftOrigin;
        Vector2 feetRightOrigin = originPoints.FeetRightOrigin;

        // Shin origin
        Vector2 shinLeftOrigin = originPoints.ShinLeftOrigin;
        Vector2 shinRightOrigin = originPoints.ShinRightOrigin;

        // Head origin wall 
        Vector2 headWallLeftOrigin = originPoints.HeadWallLeftOrigin;
        Vector2 headWallRightOrigin = originPoints.HeadWallRightOrigin;

        // Head origin ceiling
        Vector2 headCeilingLeftOrigin = originPoints.HeadCeilingLeftOrigin;
        Vector2 headCeilingRightOrigin = originPoints.HeadCeilingRightOrigin;

        Vector2 middleLeftOrigin = originPoints.MiddleLeftOrigin;
        Vector2 middleRightOrigin = originPoints.MiddleRightOrigin;

        CheckGround(colliderSize, groundLeftOrigin, groundRightOrigin);
        CheckWall(colliderSize, feetLeftOrigin, feetRightOrigin, shinLeftOrigin, shinRightOrigin, headWallLeftOrigin, headWallRightOrigin);
        CheckCeiling(colliderSize, headCeilingLeftOrigin, headCeilingRightOrigin, colliderSizeY.y);
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


        cbcShinEnabled = feetLeftHit.collider != null || feetRightHit.collider != null;
        cbcHeadEnabled = headLeftHit.collider != null || headRightHit.collider != null;

        if (characterBoxCollider.offset.y == 0)
        {
            cbcEnabled = cbcShinEnabled && !cbcHeadEnabled;
        }
        if (collisionGizmos)
        {
            Debug.DrawLine(feetLeftOrigin, shinLeftOrigin, cbcShinEnabled ? Color.green : Color.red);
            Debug.DrawLine(feetRightOrigin, shinRightOrigin, cbcShinEnabled ? Color.green : Color.blue);

            Debug.DrawLine(shinLeftOrigin, headWallLeftOrigin, cbcHeadEnabled ? Color.green : Color.red);
            Debug.DrawLine(shinRightOrigin, headWallRightOrigin, cbcHeadEnabled ? Color.green : Color.blue);

            Debug.DrawLine(shinLeftOrigin, new Vector2(transform.position.x, shinLeftOrigin.y), cbcShinEnabled ? Color.green : Color.red);
            Debug.DrawLine(shinRightOrigin, new Vector2(transform.position.x, shinRightOrigin.y), cbcShinEnabled ? Color.green : Color.blue);
        }



    }

    private void CheckCeiling(Vector2 colliderSize, Vector2 headCeilingLeftOrigin, Vector2 headCeilingRightOrigin, float colliderSizeY)
    {

        Vector2 middleLeftOrigin = (Vector2)transform.position + new Vector2(-bhcThreshold, colliderSizeY);
        Vector2 middleRightOrigin = (Vector2)transform.position + new Vector2(bhcThreshold, colliderSizeY);

        RaycastHit2D leftHit = Physics2D.Linecast(headCeilingLeftOrigin, middleLeftOrigin, groundLayer);
        RaycastHit2D middleHit = Physics2D.Linecast(middleLeftOrigin, middleRightOrigin, groundLayer);
        RaycastHit2D rightHit = Physics2D.Linecast(headCeilingRightOrigin, middleRightOrigin, groundLayer);

        if (!grounded)
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

            Debug.DrawLine(middleLeftOrigin, middleLeftOrigin - new Vector2(0, colliderSizeY), bhcleftEnabled ? Color.green : Color.red);
            Debug.DrawLine(middleRightOrigin, middleRightOrigin - new Vector2(0, colliderSizeY), bhcRightEnabled ? Color.green : Color.blue);
        }

    }


    public struct OriginPoints
    {
        internal float ColliderSizeY;

        public Vector2 GroundLeftOrigin;
        public Vector2 GroundRightOrigin;
        public Vector2 FeetLeftOrigin;
        public Vector2 FeetRightOrigin;
        public Vector2 ShinLeftOrigin;
        public Vector2 ShinRightOrigin;
        public Vector2 HeadWallLeftOrigin;
        public Vector2 HeadWallRightOrigin;
        public Vector2 HeadCeilingLeftOrigin;
        public Vector2 HeadCeilingRightOrigin;
        public Vector2 MiddleLeftOrigin;
        public Vector2 MiddleRightOrigin;
    }

    private OriginPoints CalculateOriginPoints()
    {
        // Box collider size
        Vector2 colliderSize = characterBoxCollider.size;
        float colliderSizeY = colliderSize.y + YSensorScale;

        // x Sensor Scales
        float verticalColliderSizeX = colliderSize.x + groundXSensorScale;
        float horizontalColliderSizeX = colliderSize.x + wallXSensorScale;
        OriginPoints originPoints = new OriginPoints
        {
            ColliderSizeY = colliderSizeY,

            GroundLeftOrigin = (Vector2)transform.position + new Vector2(-verticalColliderSizeX, groundedThreshold) + TotalOffset,
            GroundRightOrigin = (Vector2)transform.position + new Vector2(verticalColliderSizeX, groundedThreshold) + TotalOffset,


            FeetLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, 0.0f) + TotalOffset,
            FeetRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, 0.0f) + TotalOffset,


            ShinLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, cbcThreshold) + TotalOffset,
            ShinRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, cbcThreshold) + TotalOffset,

            HeadWallLeftOrigin = (Vector2)transform.position + new Vector2(-horizontalColliderSizeX, colliderSizeY) + TotalOffset,
            HeadWallRightOrigin = (Vector2)transform.position + new Vector2(horizontalColliderSizeX, colliderSizeY) + TotalOffset,

            HeadCeilingLeftOrigin = (Vector2)transform.position + new Vector2(-verticalColliderSizeX, colliderSizeY) + TotalOffset,
            HeadCeilingRightOrigin = (Vector2)transform.position + new Vector2(verticalColliderSizeX, colliderSizeY) + TotalOffset,

            MiddleLeftOrigin = (Vector2)transform.position + new Vector2(-bhcThreshold, colliderSizeY) + TotalOffset,
            MiddleRightOrigin = (Vector2)transform.position + new Vector2(bhcThreshold, colliderSizeY) + TotalOffset
        };

        return originPoints;
    }
}