using UnityEngine;
using UnityEngine.U2D;
using static Atlas;
using static AtlasRendering;

[ExecuteAlways]
public class AtlasMotionRenderer : AtlasRenderer
{
    public SingularRenderInput renderInput;
    public int spriteIndex;

    [Header("Motion (Generated)")]
    public float keyframeClock;
    public int curFrameIndex;
    public int prevFrameIndex;
    public SimpleSprite sprite;
    private void Awake()
    {
        if (renderInput == null) return;
        renderInput.InitRenderer(gameObject);
    }
    private void OnValidate()
    {
        if (renderInput == null || renderInput.atlas == null) return;
        spriteIndex = Mathf.Clamp(spriteIndex, 0, renderInput.atlas.motionSprites.Length - 1);
        sprite = renderInput.atlas.motionSprites[spriteIndex].sprite;
        renderInput.UpdateRenderInputsWorld(1, 1, sprite);
    }
    private void OnEnable()
    {
        if (renderInput == null) return;
        renderInput.InitRenderer(gameObject);
        RegisterSingleRenderInput(renderInput);
    }
    private void OnDisable()
    {
        UnregisterSingleRenderInput(renderInput);
    }
    private void OnDestroy()
    {
        UnregisterSingleRenderInput(renderInput);
    }
    private void Start()
    {

        renderInput.UpdateDepth(renderInput.batchKey.depthOrder);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (renderInput == null || renderInput.gameObject == null) return;
            renderInput.UpdateDepth((int)transform.position.z);
        }
#endif
        renderInput.UpdateBounds();
    }
    public void PlayClip(AtlasClip clip)
    {
        keyframeClock += Time.deltaTime;

        float frameTime = keyframeClock * FRAMES_PER_SEC;
        if (curFrameIndex >= clip.keyFrames.Length || curFrameIndex < 0) curFrameIndex = 0;
        AtlasKeyframe curKeyFrame = clip.keyFrames[curFrameIndex];

        switch (clip.clipType)
        {
            case ClipType.Loop:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    curFrameIndex++;

                    if (curFrameIndex >= clip.keyFrames.Length)
                    {
                        curFrameIndex = 0;
                    }

                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.PingPong:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    if (curFrameIndex < clip.keyFrames.Length - 1 && (curFrameIndex > prevFrameIndex || curFrameIndex == 0))
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex++;
                    }
                    else
                    {
                        prevFrameIndex = curFrameIndex;
                        curFrameIndex--;
                    }
                    keyframeClock = 0;
                }
            }
            break;
            case ClipType.OneShot:
            {
                if (frameTime >= curKeyFrame.holdTime)
                {
                    prevFrameIndex = curFrameIndex;
                    if (curFrameIndex < clip.keyFrames.Length - 1)
                    {
                        curFrameIndex++;
                    }
                    keyframeClock = 0;
                }
            }
            break;
        }

        sprite = clip.keyFrames[curFrameIndex].motionSprite.sprite;
        renderInput.UpdateRenderInputsWorld(1, 1, sprite);

    }
    public void PlayManualClip(AtlasClip clip, float currentTime)
    {
        int maxIndex = clip.keyFrames.Length - 1;
        int curFrameIndex = Mathf.Clamp(Mathf.FloorToInt((clip.keyFrames.Length - 1) * currentTime), 0, maxIndex);
        sprite = clip.keyFrames[curFrameIndex].motionSprite.sprite;
        renderInput.UpdateRenderInputsWorld(1, 1, sprite);
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
