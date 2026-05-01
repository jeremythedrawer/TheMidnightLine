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
    public Vector3 centerBounds;
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
        stats.worldUnitsPerPixel = stats.camBounds.size.y / Screen.height;

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
                carriageBoundsCompute.SetVector("_BoundSize", Vector2.zero);
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
            case LocationState.Gangway:
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
