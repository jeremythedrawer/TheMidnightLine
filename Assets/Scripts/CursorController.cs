using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    const float VISIBLE_TIMER = 1f;
    const float MOVE_THRESHOLD = 0.01f;

    public PlayerInputsSO playerInputs;
    public AtlasRenderer cursorRenderer;
    [Header("Generated")]
    public float timer;
    public static Vector3 curWorldPos;
    public bool active;

    public static AtlasRenderer prevRenderer;
    private void Start()
    {
        Cursor.visible = false;
    }
    private void Update()
    {
        if (active)
        {
            cursorRenderer.enabled = true;
            curWorldPos = playerInputs.mouseWorldPos;
            transform.position = curWorldPos;
        }
        else
        {
            cursorRenderer.enabled = false;
        }
    }
    private void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        if (mouseDelta.sqrMagnitude < MOVE_THRESHOLD && !playerInputs.mouseLeftHold)
        {
            timer += Time.deltaTime;

            if (timer > VISIBLE_TIMER)
            {
                active = false;
            }
        }
        else
        {
            timer = 0;
            active = true;
        }
    }
    public static bool IsInsideBounds(Bounds bounds)
    {
        return curWorldPos.x >= bounds.min.x && curWorldPos.x <= bounds.max.x && curWorldPos.y >= bounds.min.y && curWorldPos.y <= bounds.max.y;
    }
    public static bool EnteredBounds(AtlasRenderer renderer)
    {
        if (IsInsideBounds(renderer.bounds))
        {
            if (prevRenderer != renderer)
            {
                prevRenderer = renderer;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    public static bool ExitBounds()
    {
        if (prevRenderer != null && !IsInsideBounds(prevRenderer.bounds))
        {
            prevRenderer = null;
            return true;
        }
        else
        {
            return false;
        }
    }
}
