using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using static Atlas;
using static NPC;

public class PresidentBrain : MonoBehaviour
{
    const float SHAKING_HANDS_TIME = 3f;

    public static event Action OnShakeHands;

    public AtlasRenderer atlasRenderer;

    public TripData curTrip;
    public SpyData spyStats;
    public MeridiaTowerData meridiaTowerData;

    public float handShakeDist;

    [Header("Generated")]
    public PresidentState curState;
    public AtlasClip curClip;
    private void OnEnable()
    {
        Scenes.OnLoadStart += SetToSittingState;
        Scenes.OnLoadScore += SetToSittingState;

        StartUI.OnPlayAgain += SetToSittingState;
        StartUI.OnFinishedOutcomeSequence += CheckAndSetStartHandShakeState;
    }
    private void OnDisable()
    {
        Scenes.OnLoadStart -= SetToSittingState;
        Scenes.OnLoadScore -= SetToSittingState;
        
        StartUI.OnPlayAgain -= SetToSittingState;
        StartUI.OnFinishedOutcomeSequence -= CheckAndSetStartHandShakeState;
    }
    private void Update()
    {
        UpdateState();
    }
    public void EnterState()
    {
        switch (curState)
        {
            case PresidentState.Sitting:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)PresidentMotion.SittingBreathing];
            }
            break;
            case PresidentState.StartHandshake:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)PresidentMotion.StartHandshake];
                atlasRenderer.PlayClipOneShot(curClip);
            }
            break;
            case PresidentState.Handshaking:
            {
                curClip = atlasRenderer.atlas.clipDict[(int)PresidentMotion.Handshake];
                OnShakeHands?.Invoke();
            }
            break;
        }
    }

    public void UpdateState()
    {
        switch (curState)
        {
            case PresidentState.Sitting:
            {
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
            case PresidentState.StartHandshake:
            {
                if (!spyStats.spriteFlip && !atlasRenderer.isAnimating && Mathf.Abs(spyStats.bounds.center.x - (transform.position.x - handShakeDist)) < 0.02f)
                {
                    SetState(PresidentState.Handshaking);
                }
            }
            break;
            case PresidentState.Handshaking:
            {
                atlasRenderer.PlayClip(ref curClip);
            }
            break;
        }
    }
    public void ExitState()
    {
        switch (curState)
        {
            case PresidentState.Sitting:
            {

            }
            break;
            case PresidentState.Handshaking:
            {

            }
            break;
        }
    }
    public void SetState(PresidentState newstate)
    {
        if (newstate == curState) return;
        ExitState();
        curState = newstate;
        EnterState();
    }
    private void CheckAndSetStartHandShakeState()
    {
        if (!curTrip.failed)
        {
            SetState(PresidentState.StartHandshake);
        }
    }
    private void SetToSittingState()
    {
        SetState(PresidentState.Sitting);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 shootPos = new Vector3(transform.position.x - handShakeDist, transform.position.y, transform.position.z);
        Gizmos.DrawLine(transform.position, shootPos);
    }

#endif
}

