using UnityEngine;

public class ExitBounds : Bounds
{
    public BoxCollider2D boxCollider2D {  get; private set; }
    public DisableBounds disableBounds { get; private set; }


    private void OnValidate()
    {
        SetComponents();
    }

    private void Awake()
    {
        SetComponents();
    }
    private void SetComponents()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        disableBounds = transform.parent.GetComponentInChildren<DisableBounds>();
    }
}
