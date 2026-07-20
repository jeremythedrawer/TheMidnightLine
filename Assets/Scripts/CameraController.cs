using UnityEngine;

using static Atlas;
using static Spy;
public class CameraController : MonoBehaviour
{
    const float GAUSSIAN_VARIANCE = 90;
    const float CARRIAGE_BOUNDS_TEXTURE_SCALE = 32f;

    public CameraSettingsSO settings;
    public CameraStatsSO stats;
    public SpyStatsSO spyStats;
    public TrainStatsSO trainStats;
    public SpySettingsSO spySettings;
    public PlayerInputsSO spyInputs;
    public LayerSettingsSO layerSettings;
    public GameEventDataSO gameEventData;

    public RenderTexture carriageBoundsRT;
    public ComputeShader carriageBoundsCompute;

    [Header("Generated")]
    public LocationState curState;
    public Camera cam;
    public Vector3 targetWorldPos;
    public Vector3 rawCurWorldPos;
    public float curXOffset;

    public int carriageBoundsKernel;
    public int threadGroupX;
    public int threadGroupY;
    private void Start()
    {
    }
    private void OnEnable()
    {
        Init();
        Scenes.OnLoadTrip0 += TripInit;
    }
    private void OnDisable()
    {
        Scenes.OnLoadTrip0 -= TripInit;

        stats.curVelocity = Vector3.zero;

#if UNITY_EDITOR
        Graphics.Blit(Texture2D.whiteTexture, carriageBoundsRT);
#endif
    }
    
    private void Update()
    {
        ChooseStates();
        UpdateStates();

        curXOffset = spyStats.spriteFlip ? -settings.horizontalOffset : settings.horizontalOffset;
        stats.camBounds.center = transform.position;

        stats.worldToCam = cam.worldToCameraMatrix;
        stats.camToWorld = cam.cameraToWorldMatrix;
        
        stats.prevWorldPos = stats.curWorldPos;
        rawCurWorldPos = Vector3.Lerp(rawCurWorldPos, targetWorldPos, Time.deltaTime * settings.damping);
        stats.curWorldPos = GetSnappedPosition(rawCurWorldPos, stats.worldUnitsPerPixel);
        transform.position = stats.curWorldPos;
        stats.curVelocity = -(stats.curWorldPos - stats.prevWorldPos) / Time.deltaTime;
    }
    private void LateUpdate()
    {
        SendDataToPixelPerfectShader();
    }
    private void Init()
    {
        cam = Camera.main;
        cam.orthographicSize = GetSnappedOrthoSize();
        targetWorldPos.z = transform.position.z;
        targetWorldPos.y = settings.verticalOffset;
        
        stats.curWorldPos = transform.position;
        stats.camBounds = new Bounds();
        stats.camBounds.size = new Vector3(cam.orthographicSize * 2 * cam.aspect, cam.orthographicSize * 2, cam.farClipPlane + cam.nearClipPlane);
        stats.worldUnitsPerPixel = (cam.orthographicSize * 2) / Screen.height;

        Graphics.Blit(Texture2D.whiteTexture, carriageBoundsRT);
    }
    private void TripInit()
    {
        cam = Camera.main;
        cam.orthographicSize = GetSnappedOrthoSize();
        targetWorldPos.z = transform.position.z;
        targetWorldPos.y = settings.verticalOffset;
        SetCarriageSDFCompute();
    }
    private void SetCarriageSDFCompute()
    {
        carriageBoundsRT.Release();
        carriageBoundsRT.width = (int)(trainStats.totalBounds.size.x * CARRIAGE_BOUNDS_TEXTURE_SCALE);
        carriageBoundsRT.height = (int)(trainStats.totalBounds.size.y * CARRIAGE_BOUNDS_TEXTURE_SCALE);
        carriageBoundsRT.enableRandomWrite = true;
        carriageBoundsRT.Create();

        Graphics.Blit(Texture2D.whiteTexture, carriageBoundsRT);

        threadGroupX = Mathf.CeilToInt(carriageBoundsRT.width / 8.0f);
        threadGroupY = Mathf.CeilToInt(carriageBoundsRT.height / 8.0f);

        carriageBoundsKernel = carriageBoundsCompute.FindKernel("CSCarriageBounds");
        carriageBoundsCompute.SetTexture(carriageBoundsKernel, "_SDFTexture", carriageBoundsRT);
        carriageBoundsCompute.SetVector("_TextureSize", new Vector4(carriageBoundsRT.width, carriageBoundsRT.height, 0, 0));

        Shader.SetGlobalTexture("_CarriageBoundsTexture", carriageBoundsRT);
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

                float carriageT = (1.0f - Mathf.Exp(-(distFromCenter * distFromCenter / GAUSSIAN_VARIANCE)));

                targetWorldPos.x = Mathf.Lerp(spyStats.curLocationBounds.center.x, spyStats.curWorldPos.x + curXOffset, carriageT);
                carriageBoundsCompute.SetFloat("_DeltaTime", Time.deltaTime);
                
                carriageBoundsCompute.Dispatch(carriageBoundsKernel, threadGroupX, threadGroupY, 1);
            }
            break;
            case LocationState.MeetingRoom:
            case LocationState.Bunker:
            {
                float distFromCenter = spyStats.curWorldPos.x - spyStats.curLocationBounds.center.x;
                float t = (1.0f - Mathf.Exp(-(distFromCenter * distFromCenter / GAUSSIAN_VARIANCE)));
                targetWorldPos.x = Mathf.Lerp(spyStats.curLocationBounds.center.x, spyStats.curWorldPos.x + curXOffset, t);
                targetWorldPos.x = Mathf.Clamp(targetWorldPos.x, spyStats.curLocationBounds.min.x + stats.camBounds.extents.x, spyStats.curLocationBounds.max.x - stats.camBounds.extents.x);
            }
            break;
            case LocationState.Gangway:
            {
                targetWorldPos.x = spyStats.curWorldPos.x + curXOffset;

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
                carriageBoundsCompute.SetVector("_BoundsCenter", (spyStats.curLocationBounds.center - trainStats.totalBounds.min));
                carriageBoundsCompute.SetVector("_BoundsSize", spyStats.curLocationBounds.size);
            }
            break;
            case LocationState.Gangway:
            {
                carriageBoundsCompute.SetVector("_BoundsCenter", (spyStats.curLocationBounds.center - trainStats.totalBounds.min));
                carriageBoundsCompute.SetVector("_BoundsSize", spyStats.curLocationBounds.size);
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
        Matrix4x4 W2C = Camera.main.worldToCameraMatrix;
        Matrix4x4 C2W = Camera.main.cameraToWorldMatrix;

        Vector3 camSpace = W2C.MultiplyPoint3x4(pos);
        camSpace.x = Mathf.Round(camSpace.x / unitsPerPixel) * unitsPerPixel;
        camSpace.y = Mathf.Round(camSpace.y / unitsPerPixel) * unitsPerPixel;

        Vector3 snappedPos = C2W.MultiplyPoint3x4(camSpace);
        return snappedPos;
    }

    private void SendDataToPixelPerfectShader()
    {
        Shader.SetGlobalVector("_SnapDiff", rawCurWorldPos - stats.curWorldPos);
    }
    private float GetSnappedOrthoSize()
    {
        return (Screen.height * 0.5f / PIXELS_PER_UNIT);
    }
}
