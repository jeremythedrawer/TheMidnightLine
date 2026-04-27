using UnityEngine;
using static Atlas;

[ExecuteAlways]
public class StationNameTag : MonoBehaviour
{
    const float PADDING = 0.05f; 
    public AtlasRenderer text_renderer;
    public AtlasRenderer background_renderer;

    [Header("Generated")]
    public Vector3 backgroundLocalPos;

    private void Start()
    {
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            backgroundLocalPos.y = background_renderer.transform.localPosition.y;
            backgroundLocalPos.z = background_renderer.transform.localPosition.z;
            SetText(text_renderer.text);
        }
#endif
    }
    public void SetText(string text)
    {
        text_renderer.SetText(text);

        backgroundLocalPos.x = -text_renderer.bounds.extents.x - PADDING;

        if (!text_renderer.hasText)
        {
            background_renderer.enabled = false;
        }
        else
        {
            background_renderer.enabled = true;
            background_renderer.transform.localPosition = backgroundLocalPos;
            background_renderer.SetNineSliceWidthFromWorldSpace(text_renderer.bounds.size.x + PADDING * 2);
        }
    }
}
