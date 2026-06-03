using UnityEngine;
using static Atlas;
using static AtlasRendering;

[ExecuteAlways]
public class StationNameTag : MonoBehaviour
{
    const float PADDING = 0.05f; 
    public AtlasTextRenderer text_renderer;
    public AtlasRenderer background_renderer;

    [Header("Generated")]
    public Vector3 backgroundLocalPos;

    private void Update()
    {
#if UNITY_EDITOR
        SetTextEditor();
#endif
    }
    public void SetTextEditor()
    {
        if (text_renderer == null) return;
        if (Application.isPlaying) return;

        backgroundLocalPos.z = 1;
        SetText(text_renderer.text);

    }
    public void SetText(string text)
    {
        text_renderer.SetText(text);


        switch(text_renderer.alignmentType)
        {
            case AtlasTextAlignmentType.Left:
            {
                backgroundLocalPos.x = -PADDING;

            }
            break;
            case AtlasTextAlignmentType.Center:
            {
                backgroundLocalPos.x = -text_renderer.bounds.extents.x - PADDING;

            }
            break;
            case AtlasTextAlignmentType.Right:
            {

                backgroundLocalPos.x = -text_renderer.bounds.size.x - PADDING;
            }
            break;
        }

        backgroundLocalPos.y = -text_renderer.bounds.size.y - PADDING;

        if (!text_renderer.hasText)
        {
            background_renderer.enabled = false;
        }
        else
        {
            background_renderer.enabled = true;

            Vector2 worldSize = new Vector2();
            worldSize.x = text_renderer.bounds.size.x + PADDING * 2;
            worldSize.y = text_renderer.bounds.size.y + PADDING * 2;

            background_renderer.transform.localPosition = backgroundLocalPos;


            background_renderer.SetNineSliceSizeFromWorldSpace(worldSize, background_renderer.atlas.slicedSprites[background_renderer.spriteIndex]);
        }
    }
}
