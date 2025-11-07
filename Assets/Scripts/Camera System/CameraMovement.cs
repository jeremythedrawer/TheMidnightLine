using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;
using static NPCBrain;

public class CameraMovement : MonoBehaviour
{
    Camera cam;

    [Serializable] public struct SOData
    {
        public CameraSettingsSO cameraSettings;
        public CameraStatsSO cameraStats;
        public SpyStatsSO spyStats;
        public SpySettingsSO spySettings;
        public SpyInputsSO spyInputs;
        public LayerSettingsSO layerSettings;
    }
    [SerializeField] SOData soData;

    [Serializable] public struct GameEventData
    {
        public GameEvent OnReset;
    }
    [SerializeField] GameEventData gameEventData;

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
        soData.cameraStats.targetSize = cam.orthographicSize;
        soData.cameraStats.initialSize = cam.orthographicSize;
    }
    private void Update()
    {
        SelectingStates();
        UpdateStates();
        if (soData.spyStats.moveVelocity.x > 0)
        {
            soData.cameraStats.curHorOffset = soData.spyStats.spriteFlip ? -soData.cameraSettings.horizontalOffset : soData.cameraSettings.horizontalOffset; // camera offsets when player is moving
        }

        //Set size and position
        Vector2 camWorldPos = Vector2.Lerp(transform.position, soData.cameraStats.targetWorldPos, Time.unscaledDeltaTime * soData.cameraSettings.damping);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, soData.cameraStats.targetSize, Time.deltaTime * soData.cameraSettings.damping);
        transform.position = new Vector3(camWorldPos.x, camWorldPos.y, transform.position.z);
    }
    private void SelectingStates()
    {
        if (!soData.spyStats.onTrain)
        {
            SetState(CameraStatsSO.State.Station);
        }
        else if (soData.spyStats.curLocationLayer == soData.layerSettings.insideCarriageBounds)
        {
            SetState(CameraStatsSO.State.Carriage);
        }
        else if (soData.spyStats.curLocationLayer == soData.layerSettings.roofBounds)
        {
            SetState(CameraStatsSO.State.Roof);
        }
        else if (soData.spyStats.curLocationLayer == soData.layerSettings.gangwayBounds)
        {
            SetState(CameraStatsSO.State.Gangway);
        }
    }
    private void UpdateStates()
    {
        switch (soData.cameraStats.curState)
        {
            case CameraStatsSO.State.Station:
            {
                soData.cameraStats.targetWorldPos.x = soData.spyStats.curWorldPos.x + soData.cameraStats.curHorOffset;
            }
            break;

            case CameraStatsSO.State.Carriage:
            {
                float halfSize = soData.spyStats.curLocationBounds.extents.x;
                float distFromCenter = soData.spyStats.curWorldPos.x - soData.spyStats.curLocationBounds.center.x;

                float t = (1.0f - Mathf.Pow(2.71828f, -(distFromCenter * distFromCenter / halfSize)));
                t *= t * 0.5f;

                soData.cameraStats.targetWorldPos.x = Mathf.Lerp(soData.spyStats.curLocationBounds.center.x, soData.spyStats.curWorldPos.x + soData.cameraStats.curHorOffset, t);
            }
            break;

            case CameraStatsSO.State.Roof:
            {

            }
            break;

            case CameraStatsSO.State.Gangway:
            {

            }
            break;
        }
    }
    private void SetState(CameraStatsSO.State newState)
    {
        if (soData.cameraStats.curState == newState) return;
        ExitState();
        soData.cameraStats.curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (soData.cameraStats.curState)
        {
            case CameraStatsSO.State.Station:
            {
                soData.cameraStats.targetWorldPos.y = soData.spyStats.curWorldPos.y;
            }
            break;

            case CameraStatsSO.State.Carriage:
            {
                soData.cameraStats.targetWorldPos.y = soData.spyStats.curLocationBounds.center.y;
            }
            break;

            case CameraStatsSO.State.Roof:
            {
                soData.cameraStats.targetWorldPos.y = soData.spyStats.curLocationBounds.center.y;
            }
            break;

            case CameraStatsSO.State.Gangway:
            {

            }
            break;
        }
    }
    private void ExitState()
    {
        switch (soData.cameraStats.curState)
        {
            case CameraStatsSO.State.Station:
            {
            }
            break;

            case CameraStatsSO.State.Carriage:
            {
            }
            break;

            case CameraStatsSO.State.Roof:
            {

            }
            break;

            case CameraStatsSO.State.Gangway:
            {

            }
            break;
        }
    }
    private void ResetCamera()
    {
        soData.cameraStats.curState = CameraStatsSO.State.Station;
        soData.cameraStats.initialSize = cam.orthographicSize;
        soData.cameraStats.curHorOffset = 0.0f;
        soData.cameraStats.targetSize = cam.orthographicSize;
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
}
