using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using static Atlas;
using static AtlasRendering;

[ExecuteAlways]
public class AtlasUIMotionRenderer : AtlasRenderer
{
    public SingularRenderInput renderInput;
    public CameraStatsSO camStats;
    public int spriteIndex;

    [Header("Motion (Generated)")]
    public float keyframeClock;
    public int curFrameIndex;
    public int prevFrameIndex;
    public SimpleSprite sprite;
    public CancellationTokenSource ctsOneShot;
    private void Awake()
    {
        if (renderInput == null) return;
        renderInput.InitRenderer(gameObject);
    }
    private void OnValidate()
    {
        if (renderInput == null || renderInput.atlas == null) return;
        renderInput.InitRenderer(gameObject);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, renderInput.atlas.motionSprites.Length - 1);
        sprite = renderInput.atlas.motionSprites[spriteIndex].sprite;

        renderInput.UpdateRenderInputsScreen(1, 1, sprite, camStats);
    }
    private void OnEnable()
    {
        renderInput.InitRenderer(gameObject);
        RegisterSingleRenderInput(renderInput);
    }
    private void OnDisable()
    {
        ctsOneShot?.Cancel();
        ctsOneShot = null;
        UnregisterSingleRenderInput(renderInput);
    }
    private void OnDestroy()
    {
        UnregisterSingleRenderInput(renderInput);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            renderInput.UpdateDepthEditor(transform);
            renderInput.UpdateRenderInputsScreen(1, 1, sprite, camStats);
        }
#endif

        if (transform.hasChanged)
        {
            renderInput.bounds.center = new Vector3(transform.position.x + renderInput.boundsOffset.x, transform.position.y + renderInput.boundsOffset.y, transform.position.z);
        }
    }
    public void PlayClip(AtlasClip clip)
    {
        sprite = AtlasRendering.GetNextKeyframeSprite(clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        renderInput.UpdateRenderInputsScreen(1, 1, sprite, camStats);
    }
    public void PlayClipOneShot(AtlasClip clip)
    {
        ctsOneShot?.Cancel();
        ctsOneShot = null;
        ctsOneShot = new CancellationTokenSource();

        PlayingClipOneShot(clip).Forget();
    }
    public void PlayClipReverse(AtlasClip clip)
    {
        sprite = AtlasRendering.GetNextKeyframeSpriteReverse(clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        renderInput.UpdateRenderInputsScreen(1, 1, sprite, camStats);
    }

    public void PlayClipOneShotReverse(AtlasClip clip)
    {
        ctsOneShot?.Cancel();
        ctsOneShot = null;
        ctsOneShot = new CancellationTokenSource();
        PlayingClipOneShotReverse(clip).Forget();

    }
    public void PlayManualClip(AtlasClip clip, float currentTime)
    {
        sprite = AtlasRendering.GetNextKeyframeSpriteManual(clip, currentTime);
        renderInput.UpdateRenderInputsScreen(1, 1, sprite, camStats);
    }

    private async UniTask PlayingClipOneShot(AtlasClip clip)
    {
        keyframeClock = 0;
        try
        {
            while (curFrameIndex < clip.keyFrames.Length)
            {
                sprite = AtlasRendering.GetNextKeyframeSprite(clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
                renderInput.UpdateRenderInputsScreen(1, 1, sprite, camStats);
                await UniTask.Yield(ctsOneShot.Token);
            }
        }
        catch (OperationCanceledException)
        { }
    }

    private async UniTask PlayingClipOneShotReverse(AtlasClip clip)
    {
        keyframeClock = 0;

        try
        {
            while (curFrameIndex >= 0)
            {
                sprite = AtlasRendering.GetNextKeyframeSpriteReverse(clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);

                renderInput.UpdateRenderInputsScreen(1, 1, sprite, camStats);

                await UniTask.Yield(ctsOneShot.Token);
            }
        }
        catch (OperationCanceledException)
        { }
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.clear;
        Gizmos.DrawCube(renderInput.bounds.center, renderInput.bounds.size);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.indigo;
        Gizmos.DrawWireCube(renderInput.bounds.center, renderInput.bounds.size);
    }
#endif
}
