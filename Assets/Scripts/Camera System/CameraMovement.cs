using Cysharp.Threading.Tasks;
using Proselyte.Sigils;
using System;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    Camera cam;

    [Serializable] public struct SOData
    {
        public CameraSettingsSO settings;
        public CameraStatsSO stats;
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
        soData.stats.targetSize = cam.orthographicSize;
        soData.stats.initialSize = cam.orthographicSize;
        soData.stats.farClipPlane = cam.farClipPlane;
    }
    private void Update()
    {
        SelectStates();
        UpdateStates();
        if (soData.spyStats.moveVelocity.x > 0)
        {
            soData.stats.curHorOffset = soData.spyStats.spriteFlip ? -soData.settings.horizontalOffset : soData.settings.horizontalOffset; // camera offsets when player is moving
        }

        //Set size and position
        Vector2 camWorldPos = Vector2.Lerp(transform.position, soData.stats.targetWorldPos, Time.unscaledDeltaTime * soData.settings.damping);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, soData.stats.targetSize, Time.deltaTime * soData.settings.damping);
        transform.position = new Vector3(camWorldPos.x, camWorldPos.y, transform.position.z);
        soData.stats.curWorldPos = transform.position;
    }
    private void OnApplicationQuit()
    {
        ResetCamera();
    }
    private void SelectStates()
    {
        if (!soData.spyStats.onTrain)
        {
            SetState(CameraStatsSO.State.Station);
        }
        else if (soData.spyStats.curLocationLayer == soData.layerSettings.trainLayers.insideCarriageBounds)
        {
            SetState(CameraStatsSO.State.Carriage);
        }
        else if (soData.spyStats.curLocationLayer == soData.layerSettings.trainLayers.roofBounds)
        {
            SetState(CameraStatsSO.State.Roof);
        }
        else if (soData.spyStats.curLocationLayer == soData.layerSettings.trainLayers.gangwayBounds)
        {
            SetState(CameraStatsSO.State.Gangway);
        }
    }
    private void UpdateStates()
    {
        switch (soData.stats.curState)
        {
            case CameraStatsSO.State.Station:
            {
                soData.stats.targetWorldPos.x = soData.spyStats.curWorldPos.x + soData.stats.curHorOffset;
            }
            break;

            case CameraStatsSO.State.Carriage:
            {
                float halfSize = soData.spyStats.curLocationBounds.extents.x;
                float distFromCenter = soData.spyStats.curWorldPos.x - soData.spyStats.curLocationBounds.center.x;

                float t = (1.0f - Mathf.Pow(2.71828f, -(distFromCenter * distFromCenter / halfSize)));
                t *= t * 0.5f;

                soData.stats.targetWorldPos.x = Mathf.Lerp(soData.spyStats.curLocationBounds.center.x, soData.spyStats.curWorldPos.x + soData.stats.curHorOffset, t);
            }
            break;

            case CameraStatsSO.State.Roof:
            {
                soData.stats.targetWorldPos.x = soData.spyStats.curWorldPos.x + soData.stats.curHorOffset;
            }
            break;

            case CameraStatsSO.State.Gangway:
            {
                soData.stats.targetWorldPos.x = soData.spyStats.curWorldPos.x + soData.stats.curHorOffset;
            }
            break;
        }
    }
    private void SetState(CameraStatsSO.State newState)
    {
        if (soData.stats.curState == newState) return;
        ExitState();
        soData.stats.curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (soData.stats.curState)
        {
            case CameraStatsSO.State.Station:
            {
                soData.stats.targetWorldPos.y = soData.spyStats.curWorldPos.y;
                soData.stats.targetSize = soData.stats.initialSize;

            }
            break;

            case CameraStatsSO.State.Carriage:
            {
                soData.stats.targetWorldPos.y = soData.spyStats.curLocationBounds.center.y;
                soData.stats.targetSize = soData.stats.initialSize;
            }
            break;

            case CameraStatsSO.State.Roof:
            {
                soData.stats.targetWorldPos.y = soData.spyStats.curLocationBounds.center.y;
                soData.stats.targetSize = soData.settings.roofProjectionSize;
            }
            break;

            case CameraStatsSO.State.Gangway:
            {
                soData.stats.targetWorldPos.y = soData.spyStats.curLocationBounds.center.y;
                soData.stats.targetSize = soData.stats.initialSize;
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (soData.stats.curState)
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
        soData.stats.curState = CameraStatsSO.State.None;
        soData.stats.initialSize = cam.orthographicSize;
        soData.stats.curHorOffset = 0.0f;
        soData.stats.targetSize = cam.orthographicSize;
    }
    private async UniTaskVoid ShakingCamera()
    {
        float elaspedTime = 0;
        while (elaspedTime < soData.settings.shakeTime)
        {
            elaspedTime += Time.deltaTime;
            float t = elaspedTime / soData.settings.shakeTime;
            Vector2 randomOffset = Vector2.Lerp(UnityEngine.Random.insideUnitCircle * soData.settings.shakeIntensity, Vector2.zero, t);
            transform.position = transform.position + (Vector3)randomOffset;
            await UniTask.Yield();
        }
    }
}
