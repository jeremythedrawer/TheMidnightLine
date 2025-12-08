using UnityEngine;

public class DisableBounds : OldBounds
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //put bystander in object pool
    }
}
