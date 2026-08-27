using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;
using static NPC;
public class HenchmanBrain : MonoBehaviour
{
    const int SHOOTING_FRAME_INDEX = 22;
    public const float SHOOT_HOLD_TIME = 0.125f;
    public static event Action OnShoot;

    public SpyData spyStats;
    public TripData curTrip;

    public AtlasRenderer atlasRenderer;
    
    public Material postProcessingMaterial;

    public HenchmanMotion sittingMotion;

    public float shootDist;

    public bool isShooter;

    [Header("Generated")]
    public HenchmanState curState;
    
    public AtlasClip curClip;

    public int curFrameIndex;

    public bool hasShot;
    public bool willShoot;
    private void OnEnable()
    {
        Scenes.OnLoadStart += SetToSittingState;
        Scenes.OnLoadScore += SetToSittingState;
    }
    private void OnDisable()
    {
        Scenes.OnLoadStart -= SetToSittingState;
        Scenes.OnLoadScore -= SetToSittingState;
    }
    private void Update()
    {
        UpdateState();
    }
    public void SetState(HenchmanState newstate)
    {
        if (newstate == curState) return;
        ExitState();
        curState = newstate;
        EnterState();
    }
    public void EnterState()
    {
        switch (curState)
        {
            case HenchmanState.Sitting:
            {
                hasShot = false;
                willShoot = false;
                curClip = atlasRenderer.atlas.clipDict[(int)sittingMotion];
            }
            break;
            case HenchmanState.Shooting:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)HenchmanMotion.ShootGun];
                atlasRenderer.PlayClipOneShot(curClip);
            }
            break;
        }
    }
    public void UpdateState()
    {
        switch (curState)
        {
            case HenchmanState.Sitting:
            {
                atlasRenderer.PlayClip(ref curClip);

                if (isShooter && willShoot && spyStats.spriteFlip && spyStats.bounds.center.x < transform.position.x + shootDist)
                {
                    SetState(HenchmanState.Shooting);
                }
            }
            break;
            case HenchmanState.Shooting:
            {
                if (!hasShot && atlasRenderer.curFrameIndex == SHOOTING_FRAME_INDEX)
                {
                    OnShoot?.Invoke();
                    postProcessingMaterial.SetInt("_Invert", 1);
                    hasShot = true;
                    Time.timeScale = 0;
                    PauseFrame();
                }
            }
            break;
        }
    }
    public void ExitState()
    {
        switch (curState)
        {
            case HenchmanState.Sitting:
            {

            }
            break;
            case HenchmanState.Shooting:
            {

            }
            break;
        }
    }
    private void SetToSittingState()
    {
        SetState(HenchmanState.Sitting);
    }
    private void SetWillShoot()
    {
        if (curTrip.failed) willShoot = true;
    }
    private void PauseFrame()
    {
        PausingFrame().Forget();
    }
    private async UniTask PausingFrame()
    {
        float clock = 0;
        while(clock < SHOOT_HOLD_TIME)
        {
            clock += Time.unscaledDeltaTime;
            await UniTask.Yield();
        }
        Time.timeScale = 1;
        postProcessingMaterial.SetInt("_Invert", 0);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (isShooter)
        {
            Gizmos.color = Color.red;
            Vector3 shootPos = new Vector3(transform.position.x + shootDist, transform.position.y, transform.position.z);
            Gizmos.DrawLine(transform.position, shootPos);
        }
    }
#endif
}
