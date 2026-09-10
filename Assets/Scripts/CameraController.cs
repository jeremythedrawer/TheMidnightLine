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
    const float SOUND_DAMP = 0.5f;

    public CamUIController camUIController;
    public AudioSource audioSource;

    public Options options;

    public AudioData audioData;
    public CameraData camData;
    public SpyData spyData;
    public TrainData trainData;
    public InputData spyInputs;
    public LayerData layerData;
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
    public float startTripTitlePosY;

    public int carriageBoundsKernel;
    public int threadGroupX;
    public int threadGroupY;

    public bool isShaking;
    public bool showingTitle;
    private void OnEnable()
    {
        Init();
    }
    private void OnDisable()
    {

    }
    private void Update()
    {
        ChooseStates();
        UpdateStates();
        SmoothMove();
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

        camData.bounds = new Bounds();
        camData.bounds.size = new Vector3(cam.orthographicSize * 2 * cam.aspect, cam.orthographicSize * 2, cam.farClipPlane + cam.nearClipPlane);
        camData.worldUnitsPerPixel = (cam.orthographicSize * 2) / Screen.height;

        Shader.SetGlobalVector("_CameraSizeAndPos", new Vector4(camData.bounds.size.x, camData.bounds.size.y, camData.bounds.center.x, camData.bounds.center.y));

        camData.curLocationState = LocationState.Menu;
    }
    private void SetCarriageSDFCompute()
    {
        carriageBoundsRT.Release();
        carriageBoundsRT.width = (int)(trainData.totalBounds.size.x * CARRIAGE_BOUNDS_TEXTURE_SCALE);
        carriageBoundsRT.height = (int)(trainData.totalBounds.size.y * CARRIAGE_BOUNDS_TEXTURE_SCALE);
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
                curXOffset = spyData.spriteFlip ? -camData.horizontalOffset : camData.horizontalOffset;
                targetWorldPos.x = Mathf.Lerp(camData.curLocationBounds.center.x, spyData.bounds.center.x + curXOffset, carriageT);
                carriageBoundsCompute.SetFloat("_DeltaTime", Time.deltaTime);
                
                carriageBoundsCompute.Dispatch(carriageBoundsKernel, threadGroupX, threadGroupY, 1);
            }
            break;

            case LocationState.Gangway:
            {
                curXOffset = spyData.spriteFlip ? -camData.horizontalOffset : camData.horizontalOffset;
                targetWorldPos.x = spyData.bounds.center.x + curXOffset;

                carriageBoundsCompute.SetFloat("_DeltaTime", Time.deltaTime);

                carriageBoundsCompute.Dispatch(carriageBoundsKernel, threadGroupX, threadGroupY, 1);
            }
            break;

            case LocationState.Menu:
            {
                targetWorldPos.x = camData.curLocationBounds.center.x;
                targetWorldPos.y = camData.curLocationBounds.center.y;
            }
            break;

            case LocationState.Title:
            {
                camData.tripTitleMenuClock += Time.deltaTime;
                float t = camData.tripTitleMenuClock / camData.tripTitleTime;
                t = Curves.EaseInOutCubic(t);
                targetWorldPos.y = Mathf.Lerp(startTripTitlePosY, spyData.bounds.center.y, t);

                float windVol = -Mathf.Cos(t * 2 * Mathf.PI) * 0.5f + 0.5f;

                audioSource.volume = windVol * audioData.soundEffectsVolume;

                if (t > 0.5f)
                {
                    if (!showingTitle)
                    {
                        camUIController.SetTitleText();
                        camUIController.SetTitleAlpha(1);
                        showingTitle = true;
                    }
                }

                if (Mathf.Abs(camData.curWorldPos.y - spyData.bounds.center.y) < 0.01f)
                {
                    camUIController.DissappearTitleAlpha();
                    camData.curLocationState = LocationState.Station;
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
                audioSource.Stop();
                audioSource.clip = audioData.stationAmbience;
                audioSource.Play();
                InterpolateVolume(targetVol: 1, time: 5);
            }
            break;

            case LocationState.Carriage:
            {
                carriageBoundsCompute.SetVector("_BoundsCenter", (camData.curLocationBounds.center - trainData.totalBounds.min));
                carriageBoundsCompute.SetVector("_BoundsSize", camData.curLocationBounds.size);
            }
            break;
            case LocationState.Gangway:
            {
                carriageBoundsCompute.SetVector("_BoundsCenter", (camData.curLocationBounds.center - trainData.totalBounds.min));
                carriageBoundsCompute.SetVector("_BoundsSize", camData.curLocationBounds.size);
            }
            break;
            case LocationState.Title:
            {
                startTripTitlePosY = transform.position.y;
                camData.tripTitleMenuClock = 0;
                curXOffset = 0;
                showingTitle = false;

                audioSource.volume = audioData.soundEffectsVolume;
                audioSource.clip = audioData.wind;
                audioSource.Play();
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
    private void SmoothMove()
    {
        camData.prevWorldPos = camData.curWorldPos;
        rawCurWorldPos = Vector3.Lerp(rawCurWorldPos, targetWorldPos, Time.deltaTime * curDamping);
        camData.curWorldPos = GetSnappedPosition(rawCurWorldPos, camData.worldUnitsPerPixel);
        transform.position = camData.curWorldPos;
        camData.bounds.center = transform.position;

        camData.curVelocity = -(camData.curWorldPos - camData.prevWorldPos) / Time.unscaledDeltaTime;
        Shader.SetGlobalVector("_CamVelocity", camData.curVelocity);
        Shader.SetGlobalVector("_CamPos", camData.curWorldPos);
    }
    private void SetToSlowDamping()
    {
        curDamping = SLOW_DAMPING;
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
    private void InterpolateVolume(float targetVol, float time)
    {
        InterpolatingVolume(targetVol, time).Forget();
    }
    private async UniTask InterpolatingVolume(float targetVol, float time)
    {
        float clock = 0;

        while (clock < time)
        {
            float t = clock / time;
            
            t = Mathf.Pow(t, 2);

            audioSource.volume = t * targetVol;

            clock += Time.deltaTime;

            await UniTask.Yield();
        }
        audioSource.volume = targetVol;
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
