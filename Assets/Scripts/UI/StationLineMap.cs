using UnityEngine;

public class StationLineMap : MonoBehaviour
{
    public AtlasTextRenderer textRenderer;
    public AtlasRenderer[] renderers;
    public void SetText(string text)
    {
        textRenderer.SetText(text);
    }
}
