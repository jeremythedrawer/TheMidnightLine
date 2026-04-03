using UnityEditor;
using UnityEngine;

public class Ticket : MonoBehaviour
{
    public AtlasRenderer ticket_renderer;
    public AtlasRenderer departureStation_renderer;
    public AtlasRenderer arrivalStation_renderer;


    public Bounds totalBounds;
    private void OnValidate()
    {
        SetTotalBounds();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    private void SetTotalBounds()
    {
        if (ticket_renderer == null) return;
        totalBounds = ticket_renderer.bounds;
    }

    public void SetText(string depStation, string arrStation)
    {
        departureStation_renderer.SetText(depStation);
        arrivalStation_renderer.SetText(arrStation);
    }
}
