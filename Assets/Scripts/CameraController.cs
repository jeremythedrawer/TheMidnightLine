using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

using static Atlas;
using static Spy;
public class CameraController : MonoBehaviour
{
    const float GAUSSIAN_VARIANCE = 90;
    const float CARRIAGE_BOUNDS_TEXTURE_SCALE = 32f;
    const float NORMAL_DAMPING = 3;
    const float SLOW_DAMPING = 1;
    public static event Action OnArrivedAtElevator;

    public CameraData camData;
    public SpyData spyData;
    public TrainData trainStats;
    public InputData spyInputs;
    public LayerData layerSettings;
    public SceneData sceneData;
    public NotepadData notepadData;

    public RenderTexture carriageBoundsRT;
    public ComputeShader carriageBoundsCompute;

    [Header("Generated")]
    public Camera cam;
    
    public LocationState curState;
    
    public Vector3 targetWorldPos;
    public Vector3 rawCurWorldPos;

    public float curXOffset;
    public float curDamping;

    public int carriageBoundsKernel;
    public int threadGroupX;
    public int threadGroupY;

    public bool isShaking;
    private void OnEnable()
    {
        Init();
        Scenes.OnLoadTrip0 += TripInit;
        Scenes.OnLoadStart += TowerInit;
        Scenes.OnLoadScore += TowerInit;

        StartUI.OnClickOptions += MoveToOptionsMenu;
        StartUI.OnClickBackFromOptions += MoveToMainMenu;
        StartUI.OnStartButtonClicked += MoveToPlayer;

        HenchmanBrain.OnShoot += ShakeFromGunShot;

        StartUI.OnPlayAgain += SetToNormalDamping;

        SpyBrain.OnAfterOutcomeSequence += SetToSlowDamping;
    }
    private void OnDisable()
    {
        Scenes.OnLoadTrip0 -= TripInit;
        Scenes.OnLoadStart -= TowerInit;
        Scenes.OnLoadScore -= TowerInit;

        StartUI.OnClickOptions -= MoveToOptionsMenu;
        StartUI.OnClickBackFromOptions -= MoveToMainMenu;
        StartUI.OnStartButtonClicked -= MoveToPlayer;
            
        HenchmanBrain.OnShoot -= ShakeFromGunShot;

        SpyBrain.OnAfterOutcomeSequence -= SetToSlowDamping;

        StartUI.OnPlayAgain -= SetToNormalDamping;

#if UNITY_EDITOR
        camData.curVelocity = Vector3.zero;
        Graphics.Blit(Texture2D.whiteTexture, carriageBoundsRT);
#endif
    }
    private void Update()
    {
        ChooseStates();
        UpdateStates();

        curXOffset = spyData.spriteFlip ? -camData.horizontalOffset : camData.horizontalOffset;
        camData.camBounds.center = transform.position;

        camData.worldToCam = cam.worldToCameraMatrix;
        camData.camToWorld = cam.cameraToWorldMatrix;
        
        camData.prevWorldPos = camData.curWorldPos;
        rawCurWorldPos = Vector3.Lerp(rawCurWorldPos, targetWorldPos, Time.deltaTime * curDamping);
        camData.curWorldPos = GetSnappedPosition(rawCurWorldPos, camData.worldUnitsPerPixel);
        transform.position = camData.curWorldPos;
        camData.curVelocity = -(camData.curWorldPos - camData.prevWorldPos) / Time.unscaledDeltaTime;
    }
    private void LateUpdate()
    {
        SendDataToPixelPerfectShader();
    }
    private void Init()
    {
        cam = Camera.main;
        cam.orthographicSize = GetSnappedOrthoSize();
        
        curDamping = NORMAL_DAMPING;

        targetWorldPos.z = transform.position.z;
        targetWorldPos.y = transform.position.y;
        
        camData.curWorldPos = transform.position;
        rawCurWorldPos = transform.position;

        camData.camBounds = new Bounds();
        camData.camBounds.size = new Vector3(cam.orthographicSize * 2 * cam.aspect, cam.orthographicSize * 2, cam.farClipPlane + cam.nearClipPlane);
        camData.worldUnitsPerPixel = (cam.orthographicSize * 2) / Screen.height;

        Shader.SetGlobalVector("_CameraSizeAndPos", new Vector4(camData.camBounds.size.x, camData.camBounds.size.y, camData.camBounds.center.x, camData.camBounds.center.y));
    }
    private void TripInit()
    {        
        camData.curLocationState = LocationState.Station;        
        SetCarriageSDFCompute();
    }
    private void TowerInit()
    {
        camData.curLocationState = LocationState.Elevator;
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
        SetState(camData.curLocationState);
    }
    private void UpdateStates()
    {
        switch (curState)
        {
            case LocationState.Station:
            {
                targetWorldPos.x = spyData.bounds.center.x + curXOffset;
            }
            break;

            case LocationState.Carriage:
            {
                float distFromCenter = spyData.bounds.center.x - camData.curLocationBounds.center.x;

                float carriageT = (1.0f - Mathf.Exp(-(distFromCenter * distFromCenter / GAUSSIAN_VARIANCE)));

                targetWorldPos.x = Mathf.Lerp(camData.curLocationBounds.center.x, spyData.bounds.center.x + curXOffset, carriageT);
                carriageBoundsCompute.SetFloat("_DeltaTime", Time.deltaTime);
                
                carriageBoundsCompute.Dispatch(carriageBoundsKernel, threadGroupX, threadGroupY, 1);
            }
            break;
            case LocationState.MeetingRoom:
            {
                if (!isShaking)
                {
                    if (notepadData.collected && sceneData.activeSceneType == Scenes.SceneType.Start)
                    {
                        float distFromCenter = (spyData.bounds.center.x + 3) - camData.curLocationBounds.center.x;
                        float t = (1.0f - Mathf.Exp(-(distFromCenter * distFromCenter / GAUSSIAN_VARIANCE)));
                        targetWorldPos.x = Mathf.Lerp(camData.curLocationBounds.center.x, (spyData.bounds.center.x + 3) + curXOffset, t);
                        targetWorldPos.x = Mathf.Clamp(targetWorldPos.x, camData.curLocationBounds.min.x + camData.camBounds.extents.x, camData.curLocationBounds.max.x - camData.camBounds.extents.x);
                    }
                    else
                    {
                        float distFromCenter = spyData.bounds.center.x - camData.curLocationBounds.center.x;
                        float t = (1.0f - Mathf.Exp(-(distFromCenter * distFromCenter / GAUSSIAN_VARIANCE)));
                        targetWorldPos.x = Mathf.Lerp(camData.curLocationBounds.center.x, spyData.bounds.center.x + curXOffset, t);
                        targetWorldPos.x = Mathf.Clamp(targetWorldPos.x, camData.curLocationBounds.min.x + camData.camBounds.extents.x, camData.curLocationBounds.max.x - camData.camBounds.extents.x);
                    }                    
                }
            }
            break;
            case LocationState.Bunker:
            {
                if (!isShaking)
                {
                    float distFromCenter = spyData.bounds.center.x - camData.curLocationBounds.center.x;
                    float t = (1.0f - Mathf.Exp(-(distFromCenter * distFromCenter / GAUSSIAN_VARIANCE)));
                    targetWorldPos.x = Mathf.Lerp(camData.curLocationBounds.center.x, spyData.bounds.center.x + curXOffset, t);
                    targetWorldPos.x = Mathf.Clamp(targetWorldPos.x, camData.curLocationBounds.min.x + camData.camBounds.extents.x, camData.curLocationBounds.max.x - camData.camBounds.extents.x);
                }
            }
            break;
            case LocationState.Gangway:
            {
                targetWorldPos.x = spyData.bounds.center.x + curXOffset;

                carriageBoundsCompute.SetFloat("_DeltaTime", Time.deltaTime);

                carriageBoundsCompute.Dispatch(carriageBoundsKernel, threadGroupX, threadGroupY, 1);
            }
            break;

            case LocationState.Elevator:
            {
                if ((spyData.curState == SpyState.ShotAt || spyData.curState == SpyState.HandShake) && (rawCurWorldPos - targetWorldPos).sqrMagnitude < 0.05f)
                {
                    OnArrivedAtElevator?.Invoke();
                }
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
                carriageBoundsCompute.SetVector("_BoundsCenter", (camData.curLocationBounds.center - trainStats.totalBounds.min));
                carriageBoundsCompute.SetVector("_BoundsSize", camData.curLocationBounds.size);
            }
            break;
            case LocationState.Gangway:
            {
                carriageBoundsCompute.SetVector("_BoundsCenter", (camData.curLocationBounds.center - trainStats.totalBounds.min));
                carriageBoundsCompute.SetVector("_BoundsSize", camData.curLocationBounds.size);
            }
            break;
            case LocationState.Elevator:
            {
                if (sceneData.activeSceneType == Scenes.SceneType.Start)
                {
                    Bounds curLocBounds = camData.curLocationBounds;
                    targetWorldPos.x = curLocBounds.center.x + (curLocBounds.size.x * 0.25f);
                }
                else if (sceneData.activeSceneType == Scenes.SceneType.Score)
                {
                    MoveToPlayer();
                }
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
    private void MoveToOptionsMenu()
    {
        Bounds curLocBounds = camData.curLocationBounds;
        targetWorldPos.x = curLocBounds.min.x + (curLocBounds.size.x * 0.25f);
    }
    private void MoveToMainMenu()
    {
        Bounds curLocBounds = camData.curLocationBounds;
        targetWorldPos.x = curLocBounds.center.x + (curLocBounds.size.x * 0.25f);
    }
    private void MoveToPlayer()
    {
        targetWorldPos.x = spyData.bounds.center.x;
    }
    private void SetToSlowDamping()
    {
        curDamping = SLOW_DAMPING;
    }
    private void SetToNormalDamping()
    {
        curDamping = NORMAL_DAMPING;
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
        Shader.SetGlobalVector("_SnapDiff", rawCurWorldPos - camData.curWorldPos);
    }
    private float GetSnappedOrthoSize()
    {
        return (Screen.height * 0.5f / PIXELS_PER_UNIT);
    }
    private void ShakeFromGunShot()
    {
        Shake(time: 0.5f, intensity: 5f);
    }
    private void Shake(float time, float intensity)
    {
        Shaking(time, intensity).Forget();
    }
    private async UniTask Shaking(float time, float intensity)
    {
        float clock = time;
        Vector3 startWorldPos = targetWorldPos;
        isShaking = true;
        while (clock >= 0)
        {
            clock -= Time.unscaledDeltaTime;
            float t = clock / time;
            Vector2 randPoint = UnityEngine.Random.insideUnitCircle * (intensity * t);
            targetWorldPos = new Vector3(startWorldPos.x + randPoint.x, startWorldPos.y + randPoint.y, startWorldPos.z);
            await UniTask.Yield();
        }
        isShaking = false;
        targetWorldPos = startWorldPos;
    }
}
