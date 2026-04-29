using UnityEngine;
using System;
using static Atlas;
using static Spy;
public class CameraController : MonoBehaviour
{
    const float RES_Y = 640;
    const float RES_X = 1920;
    const float RES_Y_HALF = 320;
    const float GAUSSIAN_VARIANCE = 60;
    public LocationState curState;

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
    public Vector3 targetWorldPos;
    public Vector3 centerBounds;
    public float curXOffset;
    public float carriageT;
    private void Start()
    {
        cam.orthographicSize = GetSnappedOrthoSize();
        targetWorldPos.z = transform.position.z;
        targetWorldPos.y = settings.verticalOffset;
        stats.curWorldPos = transform.position;
        stats.camBounds = new Bounds();
        stats.camBounds.size = new Vector3(cam.orthographicSize * 2 * cam.aspect, cam.orthographicSize * 2, cam.farClipPlane + cam.nearClipPlane);
        stats.worldUnitsPerPixel = stats.camBounds.size.y / Screen.height;
    }
    private void Update()
    {
        ChooseStates();
        UpdateStates();
        curXOffset = spyStats.spriteFlip ? -settings.horizontalOffset : settings.horizontalOffset; // camera offsets when player is moving
        stats.camBounds.center = centerBounds;

        stats.prevWorldPos = stats.curWorldPos;
        stats.curWorldPos = Vector3.Lerp(stats.curWorldPos, targetWorldPos, Time.deltaTime * settings.damping);

        transform.position = stats.curWorldPos;
        stats.curVelocity = -(stats.curWorldPos - stats.prevWorldPos);
    }
    private void ChooseStates()
    {
        SetState(spyStats.curLocationState);
    }
    private void UpdateStates()
    {
        switch (curState)
        {
            case LocationState.Station:
            {
                targetWorldPos.x = spyStats.curWorldPos.x + curXOffset;
            }
            break;

            case LocationState.Carriage:
            {
                float distFromCenter = spyStats.curWorldPos.x - spyStats.curLocationBounds.center.x;

                carriageT = (1.0f - Mathf.Exp(-(distFromCenter * distFromCenter / GAUSSIAN_VARIANCE)));
                carriageT *= carriageT * 0.5f;

                targetWorldPos.x = Mathf.Lerp(spyStats.curLocationBounds.center.x, spyStats.curWorldPos.x + curXOffset, carriageT);
            }
            break;
            case LocationState.Gangway:
            {
                targetWorldPos.x = spyStats.curWorldPos.x + curXOffset;
            }
            break;
        }
    }
    private void SetState(LocationState newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }
    private void EnterState()
    {
        switch (curState)
        {
            case LocationState.Station:
            {

            }
            break;

            case LocationState.Carriage:
            {
            }
            break;
        }
    }
    private void ExitState()
    {
        switch (curState)
        {
            case LocationState.Station:
            {
            }
            break;

            case LocationState.Carriage:
            {
            }
            break;

            case LocationState.Gangway:
            {

            }
            break;
        }
    }
    private Vector3 GetSnappedPosition(Vector3 pos)
    {
        pos.x = Mathf.Round(pos.x / stats.worldUnitsPerPixel) * stats.worldUnitsPerPixel;
        pos.y = Mathf.Round(pos.y / stats.worldUnitsPerPixel) * stats.worldUnitsPerPixel;

        return pos;
    }
    private float GetSnappedOrthoSize()
    {
        return (RES_Y_HALF / PIXELS_PER_UNIT) * 2;
    }
}
