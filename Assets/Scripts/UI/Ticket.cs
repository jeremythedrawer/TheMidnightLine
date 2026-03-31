using UnityEditor;
using UnityEngine;

public class Ticket : MonoBehaviour
{
    public AtlasUISimpleRenderer ticket_renderer;
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
}
