using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class Notepad : MonoBehaviour
{
    public PlayerInputsSO playerInputs;
    public Page activePage;
    public AtlasUISimpleRenderer rightHand;
    public AtlasUISimpleRenderer frontFingers;
    public AtlasUISimpleRenderer bindingRings;
    public AtlasUIMotionRenderer leftHand;
    public Bounds totalBounds;
    private void OnValidate()
    {
        SetTotalBounds();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    private void Start()
    {
    }

    private void Update()
    {
        if (playerInputs.notepad.y == 1)
        {
            activePage.FlipPageUp();
        }
        if (playerInputs.notepad.y == -1)
        {
            activePage.FlipPageDown();
        }
    }
    private void SetTotalBounds()
    {
        if (rightHand == null || frontFingers == null || bindingRings == null) return;
        totalBounds = rightHand.renderInput.bounds;
        totalBounds.Encapsulate(frontFingers.renderInput.bounds);
        totalBounds.Encapsulate(bindingRings.renderInput.bounds);
        totalBounds.Encapsulate(leftHand.renderInput.bounds);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.indigo;
        Gizmos.DrawWireCube(totalBounds.center, totalBounds.size);
    }
}
