using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public CameraSettingsSO settings;
    public CameraStatsSO stats;
    public SpyStatsSO spyStats;
    public SpySettingsSO spySettings;
    public PlayerInputsSO spyInputs;
    public LayerSettingsSO layerSettings;
    public GameEventDataSO gameEventData;

    [Header("Generated")]
    public Camera cam;
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
        ResetCamera();

    }
    private void Start()
    {
        stats.targetSize = cam.orthographicSize;
        stats.initialSize = cam.orthographicSize;
        stats.localFarClipPlane = cam.farClipPlane;
        stats.curWorldPos = transform.position;
        stats.worldFarClipPlane = stats.curWorldPos.z + stats.localFarClipPlane;
        stats.aspect = cam.aspect;
    }
    private void Update()
    {
        SelectStates();
        UpdateStates();
        if (spyStats.moveVelocity.x > 0)
        {
            stats.curHorOffset = spyStats.spriteFlip ? -settings.horizontalOffset : settings.horizontalOffset; // camera offsets when player is moving
        }

        //Set size and position
        stats.curWorldPos = Vector2.Lerp(stats.curWorldPos, stats.targetWorldPos, Time.deltaTime * settings.damping);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, stats.targetSize, Time.deltaTime * settings.damping);
        transform.position = stats.curWorldPos;
    }
    private void SelectStates()
    {
        if (!spyStats.onTrain)
        {
            SetState(CameraStatsSO.State.Station);
        }
        else if (spyStats.curLocationLayer == layerSettings.trainLayerStruct.insideCarriageBounds)
        {
            SetState(CameraStatsSO.State.Carriage);
        }
        else if (spyStats.curLocationLayer == layerSettings.trainLayerStruct.roofBounds)
        {
            SetState(CameraStatsSO.State.Roof);
        }
        else if (spyStats.curLocationLayer == layerSettings.trainLayerStruct.gangwayBounds)
        {
            SetState(CameraStatsSO.State.Gangway);
        }
    }
    private void UpdateStates()
    {
        switch (stats.curState)
        {
            case CameraStatsSO.State.Station:
            {
                stats.targetWorldPos.x = spyStats.curWorldPos.x + stats.curHorOffset;
            }
            break;

            case CameraStatsSO.State.Carriage:
            {
                float halfSize = spyStats.curLocationBounds.extents.x;
                float distFromCenter = spyStats.curWorldPos.x - spyStats.curLocationBounds.center.x;

                float t = (1.0f - Mathf.Pow(2.71828f, -(distFromCenter * distFromCenter / halfSize)));
                t *= t * 0.5f;

                stats.targetWorldPos.x = Mathf.Lerp(spyStats.curLocationBounds.center.x, spyStats.curWorldPos.x + stats.curHorOffset, t);
            }
            break;

            case CameraStatsSO.State.Roof:
            {
                stats.targetWorldPos.x = spyStats.curWorldPos.x + stats.curHorOffset;
            }
            break;

            case CameraStatsSO.State.Gangway:
            {
                stats.targetWorldPos.x = spyStats.curWorldPos.x + stats.curHorOffset;
            }
            break;
        }
    }
    private void SetState(CameraStatsSO.State newState)
    {
        if (stats.curState == newState) return;
        ExitState();
        stats.curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (stats.curState)
        {
            case CameraStatsSO.State.Station:
            {
                stats.targetWorldPos.y = spyStats.curWorldPos.y + settings.verticalOffset;
                stats.targetSize = stats.initialSize;

            }
            break;

            case CameraStatsSO.State.Carriage:
            {
                stats.targetWorldPos.y = spyStats.curLocationBounds.center.y + settings.verticalOffset;
                stats.targetSize = stats.initialSize;
            }
            break;

            case CameraStatsSO.State.Roof:
            {
                stats.targetWorldPos.y = spyStats.curLocationBounds.center.y + settings.verticalOffset;
                stats.targetSize = settings.maxProjectionSize;
            }
            break;

            case CameraStatsSO.State.Gangway:
            {
                stats.targetWorldPos.y = spyStats.curLocationBounds.center.y + settings.verticalOffset;
                stats.targetSize = stats.initialSize;
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (stats.curState)
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
        stats.curState = CameraStatsSO.State.None;
        stats.initialSize = cam.orthographicSize;
        stats.curHorOffset = 0.0f;
        stats.targetSize = cam.orthographicSize;
    }
    private async UniTaskVoid ShakingCamera()
    {
        float elaspedTime = 0;
        while (elaspedTime < settings.shakeTime)
        {
            elaspedTime += Time.deltaTime;
            float t = elaspedTime / settings.shakeTime;
            Vector2 randomOffset = Vector2.Lerp(UnityEngine.Random.insideUnitCircle * settings.shakeIntensity, Vector2.zero, t);
            transform.position = transform.position + (Vector3)randomOffset;
            await UniTask.Yield();
        }
    }
}
