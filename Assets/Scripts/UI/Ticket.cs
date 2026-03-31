using UnityEditor;
using UnityEngine;

public class Ticket : MonoBehaviour
{
    public AtlasUISimpleRenderer ticket_renderer;
    public AtlasTextRenderer name_renderer;
    public AtlasTextRenderer departureStation_renderer;
    public AtlasTextRenderer arrivalStation_renderer;


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
        totalBounds = ticket_renderer.renderInput.bounds;
    }

    public void SetText(string name, string depStation, string arrStation)
    {
        name_renderer.SetText(name);
        departureStation_renderer.SetText(depStation);
        arrivalStation_renderer.SetText(arrStation);
    }
}
