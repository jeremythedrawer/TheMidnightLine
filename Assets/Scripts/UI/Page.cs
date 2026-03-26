using System;
using UnityEngine;
using static Atlas;

public class Page : MonoBehaviour
{
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

    [Header("Generated")]
    public AtlasClip paperClip;
    private void Start()
    {
        paperClip = paperRenderer.renderInput.atlas.clipDict[(int)NotepadMotion.Page];
    }
    public void PlayPaperClip()
    {
        paperRenderer.PlayClipOneShot(paperClip);
    }

    public void PlayPaperClipReverse()
    {
        paperRenderer.PlayClipOneShotReverse(paperClip);
    }
    public void TogglePageContentBottomHalf(bool toggle)
    {
        behavioursRenderer.enabled = toggle;
        behaviourClue1Renderer.enabled = toggle;
        behaviourClue2Renderer.enabled = toggle;
        appearenceRenderer.enabled = toggle;
        appearenceClueRenderer.enabled = toggle;
        departureStationRenderer.enabled = toggle;
        stationAnswerRenderer.enabled = toggle;
    }

    public void TogglePageContentTopHalf(bool toggle)
    {
        nameRenderer.enabled = toggle;
        nameAnswerRenderer.enabled = toggle;
        mugshotRenderer.enabled = toggle;
    }
}
