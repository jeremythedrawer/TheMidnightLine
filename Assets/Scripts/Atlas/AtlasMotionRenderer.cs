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

        renderInput.UpdateDepthEditor(transform);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (renderInput == null || renderInput.gameObject == null) return;
            renderInput.UpdateDepthEditor(transform);
        }
#endif
        renderInput.UpdateBounds();
    }
    public void PlayClip(AtlasClip clip)
    {
        sprite = AtlasRendering.GetNextKeyframeSprite(clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        renderInput.UpdateRenderInputsWorld(1, 1, sprite);
    }

    public void PlayClipReverse(AtlasClip clip)
    {
        sprite = AtlasRendering.GetNextKeyframeSpriteReverse(clip, ref keyframeClock, ref curFrameIndex, ref prevFrameIndex);
        renderInput.UpdateRenderInputsWorld(1, 1, sprite);
    }
    public void PlayManualClip(AtlasClip clip, float currentTime)
    {
        sprite = AtlasRendering.GetNextKeyframeSpriteManual(clip, currentTime);
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
