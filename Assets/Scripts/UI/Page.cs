using UnityEngine;
using static Atlas;
using static PageOld;

public class Page : MonoBehaviour
{
    public AtlasUIMotionRenderer leftHandRenderer;
    public AtlasUIMotionRenderer paperRenderer;
    public AtlasTextRenderer behavioursRenderer;
    public AtlasTextRenderer behaviourClue1Renderer;
    public AtlasTextRenderer behaviourClue2Renderer;
    public AtlasTextRenderer nameRenderer;
    public AtlasTextRenderer nameAnswerRenderer;
    public AtlasTextRenderer appearenceRenderer;
    public AtlasTextRenderer appearenceClueRenderer;
    public AtlasTextRenderer departureStationRenderer;
    public AtlasTextRenderer stationAnswerRenderer;
    public AtlasUISimpleRenderer mugshotRenderer;

    
    public AtlasClip leftHandClip;
    public AtlasClip paperClip;

    private void Start()
    {
        leftHandClip = leftHandRenderer.renderInput.atlas.clipDict[(int)NotepadMotion.LeftHand];
        paperClip = leftHandRenderer.renderInput.atlas.clipDict[(int)NotepadMotion.Page];
    }
    public void FlipPageUp()
    {
        leftHandRenderer.PlayClipOneShot(leftHandClip);
        paperRenderer.PlayClipOneShot(paperClip);
        SetPageContentRenderers(false);

    }
    public void FlipPageDown()
    {
        leftHandRenderer.PlayClipOneShotReverse(leftHandClip);
        paperRenderer.PlayClipOneShotReverse(paperClip);
        SetPageContentRenderers(true);
    }

    private void SetPageContentRenderers(bool on)
    {
        behavioursRenderer.enabled = on;
        behaviourClue1Renderer.enabled = on;
        behaviourClue2Renderer.enabled = on;
        nameRenderer.enabled = on;
        nameAnswerRenderer.enabled = on;
        appearenceRenderer.enabled = on;
        appearenceClueRenderer.enabled = on;
        departureStationRenderer.enabled = on;
        stationAnswerRenderer.enabled = on;
        mugshotRenderer.enabled = on;
    }
}
