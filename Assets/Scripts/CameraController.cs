using UnityEngine;
using System;
using static Atlas;
using static Spy;
public class CameraController : MonoBehaviour
{
    const float GAUSSIAN_VARIANCE = 90;

    public CameraSettingsSO settings;
    public CameraStatsSO stats;
    public SpyStatsSO spyStats;
    public TrainStatsSO trainStats;
    public SpySettingsSO spySettings;
    public PlayerInputsSO spyInputs;
    public LayerSettingsSO layerSettings;
    public GameEventDataSO gameEventData;


    public ComputeShader carriageBoundsCompute;
    public RenderTexture carriageBoundsRT;

    [Header("Generated")]
    public LocationState curState;
    public Camera cam;
    public Vector3 targetWorldPos;
    public float curXOffset;
    public float carriageT;

    public int carriageBoundsKernel;
    public int threadGroupX;
    public int threadGroupY;
    public float renderTextureScale;
    private void Start()
    {
        cam.orthographicSize = GetSnappedOrthoSize();
        targetWorldPos.z = transform.position.z;
        targetWorldPos.y = settings.verticalOffset;
        stats.curWorldPos = transform.position;
        stats.camBounds = new Bounds();
        stats.camBounds.size = new Vector3(cam.orthographicSize * 2 * cam.aspect, cam.orthographicSize * 2, cam.farClipPlane + cam.nearClipPlane);
        stats.worldUnitsPerPixel = (cam.orthographicSize * 2) / Screen.height;


        renderTextureScale = 16;
        carriageBoundsRT.Release();
        carriageBoundsRT.width = (int)(trainStats.totalBounds.size.x * renderTextureScale);
        carriageBoundsRT.height = (int)(trainStats.totalBounds.size.y * renderTextureScale);
        Graphics.Blit(Texture2D.whiteTexture, carriageBoundsRT);
        carriageBoundsRT.Create();

        threadGroupX = Mathf.CeilToInt(carriageBoundsRT.width / 8);
        threadGroupY = Mathf.CeilToInt(carriageBoundsRT.height / 8);

        carriageBoundsKernel = carriageBoundsCompute.FindKernel("CSCarriageBounds");
        carriageBoundsCompute.SetTexture(carriageBoundsKernel, "_SDFTexture", carriageBoundsRT);
        carriageBoundsCompute.SetVector("_TextureSize", new Vector3(carriageBoundsRT.width, carriageBoundsRT.height));
        
        Shader.SetGlobalTexture("_CarriageBoundsTexture", carriageBoundsRT);

        Shader.SetGlobalVector("_TrainBoundsSize", trainStats.totalBounds.size);
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        Graphics.Blit(Texture2D.whiteTexture, carriageBoundsRT);
#endif

        stats.curVelocity = Vector3.zero;
    }
    private void Update()
    {
        ChooseStates();
        UpdateStates();
        curXOffset = spyStats.spriteFlip ? -settings.horizontalOffset : settings.horizontalOffset; // camera offsets when player is moving
        stats.camBounds.center = transform.position;

        stats.worldToCam = cam.worldToCameraMatrix;
        stats.camToWorld = cam.cameraToWorldMatrix;
        
        stats.prevWorldPos = stats.curWorldPos;
        stats.curWorldPos = Vector3.Lerp(stats.curWorldPos, targetWorldPos, Time.deltaTime * settings.damping);

        //Vector3 snappedPos = GetSnappedPosition(stats.curWorldPos, stats.worldUnitsPerPixel);
        //stats.curWorldPos = snappedPos;
        transform.position = stats.curWorldPos;

        stats.curVelocity = -(stats.curWorldPos - stats.prevWorldPos) / Time.deltaTime;
    }
    private void LateUpdate()
    {
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

                targetWorldPos.x = Mathf.Lerp(spyStats.curLocationBounds.center.x, spyStats.curWorldPos.x + curXOffset, carriageT);

                Vector2 centerTrainSpace = (spyStats.curLocationBounds.center - trainStats.totalBounds.min) * renderTextureScale;
                Vector2 sizeTrainSpace = (spyStats.curLocationBounds.size) * renderTextureScale;
                carriageBoundsCompute.SetVector("_BoundCenter", centerTrainSpace);
                carriageBoundsCompute.SetVector("_BoundSize", sizeTrainSpace);
                carriageBoundsCompute.SetFloat("_DeltaTime", Time.deltaTime);
                carriageBoundsCompute.Dispatch(carriageBoundsKernel, threadGroupX, threadGroupY, 1);
            }
            break;
            case LocationState.Gangway:
            {
                targetWorldPos.x = spyStats.curWorldPos.x + curXOffset;
                Vector2 centerTrainSpace = (spyStats.curLocationBounds.center - trainStats.totalBounds.min) * renderTextureScale;
                Vector2 sizeTrainSpace = (spyStats.curLocationBounds.size) * renderTextureScale;
                carriageBoundsCompute.SetVector("_BoundCenter", centerTrainSpace);
                carriageBoundsCompute.SetVector("_BoundSize", sizeTrainSpace);
                carriageBoundsCompute.SetFloat("_DeltaTime", Time.deltaTime);
                carriageBoundsCompute.Dispatch(carriageBoundsKernel, threadGroupX, threadGroupY, 1);

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
        }
    }
    public static Vector3 GetSnappedPosition(Vector3 pos, float unitsPerPixel)
    {

        pos.x = Mathf.Round(pos.x / unitsPerPixel) * unitsPerPixel;
        pos.y = Mathf.Round(pos.y / unitsPerPixel) * unitsPerPixel;

        return pos;
    }
    private float GetSnappedOrthoSize()
    {
        return (Screen.height * 0.5f / PIXELS_PER_UNIT);
    }
}
