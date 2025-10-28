using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEditor;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    Camera cam;

    [Serializable] public struct SOData
    {
        public SpyStatsSO spyStats;
        public SpySettingsSO spySettings;
        public SpyInputsSO spyInputs;
        public CameraSettingsSO cameraSettings;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnReset;
    }
    [SerializeField] GameEventData gameEventData;
    public enum Target
    {
        Spy,
    }

    public enum StateVerb
    {
        InCarriage,
        OnRoof,
        AtStation
    }

    [Serializable] public struct StateData
    {
        internal Target curTarget;
        internal StateVerb curStateVerb;
    }
    public StateData stateData;

    [Serializable] public struct StatData
    {
        internal float initialSize;
        internal float curHorOffset;
        internal float curVertOffset;
        internal float targetSize;
        internal Vector2 targetWorldPos;
        internal Vector2 skullRunnerScreenPos;
    }
    public StatData statData;
    private void Awake()
    {
        cam = Camera.main;
    }

    private void OnEnable()
    {
        gameEventData.OnReset.RegisterListener(ResetCamera);
    }
    private void OnDisable()
    {
        gameEventData.OnReset.UnregisterListener(ResetCamera);

    }
    private void Start()
    {
        statData.targetSize = cam.orthographicSize;
        statData.initialSize = cam.orthographicSize;
    }

    private void Update()
    {
        switch (stateData.curTarget)
        {
            case Target.Spy:
            {
                if (soData.spyStats.moveVelocity.x > 0)
                {
                    statData.curHorOffset = soData.spyStats.spriteFlip ? -soData.cameraSettings.horizontalOffset : soData.cameraSettings.horizontalOffset; // camera offsets when player is running
                }
                statData.targetWorldPos.x = soData.spyStats.curWorldPos.x + statData.curHorOffset;

                SelectStateVerb();
            }
            break;
        }

        //Set size and position
        Vector2 camWorldPos = Vector2.Lerp(transform.position, statData.targetWorldPos, Time.unscaledDeltaTime * soData.cameraSettings.damping);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, statData.targetSize, Time.deltaTime * soData.cameraSettings.damping);
        transform.position = new Vector3(camWorldPos.x, camWorldPos.y, transform.position.z);

        statData.skullRunnerScreenPos = Camera.main.WorldToScreenPoint(soData.spyStats.curWorldPos);
    }

    private void SelectStateVerb()
    {

    }

    private void UpdatingStateVerb()
    {
        switch (stateData.curStateVerb)
        {
            case StateVerb.OnRoof:
            {

            }
            break;

            case StateVerb.InCarriage:
            {

            }
            break;

            case StateVerb.AtStation:
            {
            }
            break;
        }
    }
    private void SetStateVerb(StateVerb newStateVerb)
    {
        if (stateData.curStateVerb == newStateVerb) return;
        ExitStateVerb();
        stateData.curStateVerb = newStateVerb;
        EnterStateVerb();
    }
    private void EnterStateVerb()
    {
        //Exit Method
        switch (stateData.curStateVerb)
        {
            case StateVerb.OnRoof:
            {
            }
            break;
            case StateVerb.InCarriage:
            {

            }
            break;
            case StateVerb.AtStation:
            {

            }
            break;
        }
    }
    private void ExitStateVerb()
    {
        //Exit Method
        switch (stateData.curStateVerb)
        {
            case StateVerb.OnRoof:
            {
            }
            break;
            case StateVerb.InCarriage:
            {

            }
            break;
            case StateVerb.AtStation:
            {

            }
            break;
        }
    }
    private void ResetCamera()
    {
        stateData.curTarget = Target.Spy;
    }
    private async UniTaskVoid ShakingCamera()
    {
        float elaspedTime = 0;
        while (elaspedTime < soData.cameraSettings.shakeTime)
        {
            elaspedTime += Time.deltaTime;
            float t = elaspedTime / soData.cameraSettings.shakeTime;
            Vector2 randomOffset = Vector2.Lerp(UnityEngine.Random.insideUnitCircle * soData.cameraSettings.shakeIntensity, Vector2.zero, t);
            transform.position = transform.position + (Vector3)randomOffset;
            await UniTask.Yield();
        }
    }

//    private void SetCameraTarget()
//    {

//        target = player.transform.TransformPoint(widthOffset, height, 0);
//        horizontalDamping = Mathf.Abs(widthOffset) > 0 ? initialDamping : initialDamping * 2;
//        FallingCamera();

//        float camOffsetY = 1f;
//        if (InsideBounds.Instance == null || !playerBrain.onTrain) // outside carriage
//        {
//            target.y = Mathf.Max(target.y + camOffsetY, trainCamBounds.wheelLevel + cam.orthographicSize);
//        }
//        else
//        {
//            CalculateCarriageBoundOffset();
//            target.x = Mathf.Clamp(target.x, InsideBounds.Instance.leftEdge + updatingBoundOffset, InsideBounds.Instance.rightEdge - updatingBoundOffset);
//            target.y = Mathf.Max(target.y + camOffsetY, trainCamBounds.wheelLevel + cam.orthographicSize);
//        }
//    }

//    private void SetCameraPos()
//    {
//        float camWorldPosX = Mathf.Lerp(transform.position.x, target.x, Time.deltaTime * horizontalDamping);
//        float camWorldPosY = Mathf.Lerp(transform.position.y, target.y, Time.deltaTime * verticalDamping);

//        transform.position = new Vector3 (camWorldPosX, camWorldPosY, transform.position.z);
//    }

//    private void FallingCamera()
//    {
//        if (playerRb.linearVelocityY <= -airborneState.clampedFallSpeed) // when player is falling
//        {
//            currentfallStateTime += Time.deltaTime;
//            verticalDamping = initialDamping * 2f;
//            float t = Mathf.Clamp(currentfallStateTime / fallstateEndTime, 0f, 1f);
//            height = initialHeight - Mathf.Lerp(initialHeight, 0.5f * cam.orthographicSize, t);
//        }
//        else
//        {
//            currentfallStateTime = 0;
//            verticalDamping = initialDamping;
//            height = initialHeight;
//        }
//    }

//    private void CalculateCarriageBoundOffset()
//    {
//        float carriageOriginX = InsideBounds.Instance.transform.position.x;
//        float playerOriginX = player.transform.position.x;
//        float carriageSize = InsideBounds.Instance.rightEdge - InsideBounds.Instance.leftEdge;
//        float normalizedCarriageX = 1.0f - ((Mathf.Abs(carriageOriginX - playerOriginX) / carriageSize) * 2.0f);

//        offsetT = Mathf.Pow(normalizedCarriageX, 0.5f);
//        camHalfWidthWorld = cam.orthographicSize * cam.aspect;

//        updatingBoundOffset = Mathf.Lerp(0, camHalfWidthWorld, offsetT);
//    }

//    private void SetCameraSize()
//    {
//        float endTime = 1.0f;
//        float t = currentCamSizeTransTime / endTime;

//        if (player.transform.position.y > trainCamBounds.roofLevel)
//        {
//            currentCamSizeTransTime = Mathf.Min(currentCamSizeTransTime + Time.deltaTime, endTime);
//        }
//        else
//        {
//            currentCamSizeTransTime = Mathf.Max(currentCamSizeTransTime - Time.deltaTime, 0);
//        }
//        Camera.main.orthographicSize = Mathf.Lerp(carriageProjectionSize, roofProjectionSize, t);
//    }

//    private void DrawDebugLines()
//    {
//#if UNITY_EDITOR
//        if (seeGizmos)
//        {
//            if (cam == null) return;

//            Vector3 top = cam.ScreenToWorldPoint(new Vector3(camWidth / 2.0f, camHeight, 0.0f));
//            Vector3 bottom = cam.ScreenToWorldPoint(new Vector3(camWidth / 2.0f, 0.0f, 0.0f));
//            Vector3 left = cam.ScreenToWorldPoint(new Vector3(0.0f, camHeight / 2.0f, 0.0f));
//            Vector3 right = cam.ScreenToWorldPoint(new Vector3(camWidth, camHeight / 2.0f, 0.0f));

//            // Draw the lines
//            Debug.DrawLine(top, bottom, Color.red); // Vertical line
//            Debug.DrawLine(left, right, Color.blue); // Horizontal line
//        }
//#endif
//    }

}
