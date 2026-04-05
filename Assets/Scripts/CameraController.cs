using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;
using static Atlas;
[ExecuteAlways]
public class CameraController : MonoBehaviour
{
    public CameraSettingsSO settings;
    public CameraStatsSO stats;
    public SpyStatsSO spyStats;
    public TrainStatsSO trainStats;
    public SpySettingsSO spySettings;
    public PlayerInputsSO spyInputs;
    public LayerSettingsSO layerSettings;
    public GameEventDataSO gameEventData;

    [Header("Generated")]
    public Camera cam;
    private void Awake()
    {
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
        float orthoSize = (640 / (float)PIXELS_PER_UNIT);

        stats.targetSize = orthoSize;
        stats.initialSize = orthoSize;
        cam.orthographicSize = orthoSize;

        stats.targetWorldPos.z = transform.position.z;
        stats.curWorldPos = transform.position;
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
        stats.camHeight = cam.orthographicSize * 2;
        stats.camWidth = stats.camHeight * cam.aspect;
        stats.camWorldLeft = stats.curWorldPos.x - stats.camWidth * 0.5f;
        stats.camWorldRight = stats.curWorldPos.x + stats.camWidth * 0.5f;
        stats.camWorldBottom = stats.curWorldPos.y - stats.camHeight * 0.5f;
        stats.camWorldTop = stats.curWorldPos.y + stats.camHeight * 0.5f;
        stats.worldUnitsPerPixel = stats.camHeight / Screen.height;
        if (Application.isPlaying)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, stats.targetSize, Time.deltaTime * settings.damping);
            cam.orthographicSize = GetSnappedOrthoSize(stats.targetSize);

            stats.prevWorldPos = stats.curWorldPos;
            stats.curWorldPos = Vector3.Lerp(stats.curWorldPos, stats.targetWorldPos, Time.deltaTime * settings.damping);
            stats.curWorldPos = GetSnappedPosition(stats.curWorldPos);
            transform.position = stats.curWorldPos;
        }
        else
        {
            stats.curWorldPos = transform.position;
        }
    }
    private void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            stats.curVelocity = -((stats.curWorldPos - stats.prevWorldPos) / Time.fixedDeltaTime);
        }
    }
    private void SelectStates()
    {
        if (!spyStats.onTrain)
        {
            SetState(CameraStatsSO.State.Station);
        }
        else if (spyStats.curLocationLayer == layerSettings.trainLayers.insideCarriageBounds)
        {
            SetState(CameraStatsSO.State.Carriage);
        }
        else if (spyStats.curLocationLayer == layerSettings.trainLayers.gangwayBounds)
        {
            SetState(CameraStatsSO.State.Gangway);
        }
        //else if (spyStats.curLocationLayer == layerSettings.trainLayers.roofBounds)
        //{
        //    SetState(CameraStatsSO.State.Roof);
        //}
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
    private Vector3 GetSnappedPosition(Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x / UNITS_PER_PIXEL) * UNITS_PER_PIXEL;
        pos.y = Mathf.Round(pos.y / UNITS_PER_PIXEL) * UNITS_PER_PIXEL;

        return pos;
    }
    private float GetSnappedOrthoSize(float targetSize)
    {
        return Mathf.Round(targetSize / UNITS_PER_PIXEL) * UNITS_PER_PIXEL;
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
