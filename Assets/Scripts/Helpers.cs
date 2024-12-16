using UnityEngine;

public static class Helpers
{
    public static float Remap(float value, float inMin, float inMax, float outMin, float outMax, bool clamp)
    {
        float newValue = (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
        if (clamp)
        {
            newValue = Mathf.Clamp(newValue, outMin, outMax);
        }
        return newValue;
    }

    public static void DrawBoxCastDebug(Vector2 origin, Vector2 size, Vector2 direction, Color color)
    {
        Vector2 halfSize = size / 2;

        // box corners
        Vector2 topLeft = origin + new Vector2(-halfSize.x, halfSize.y);
        Vector2 topRight = origin + new Vector2(halfSize.x, halfSize.y);
        Vector2 bottomLeft = origin + new Vector2(-halfSize.x, -halfSize.y);
        Vector2 bottomRight = origin + new Vector2(halfSize.x, -halfSize.y);

        // edges
        Debug.DrawLine(topLeft, topRight, color);
        Debug.DrawLine(topRight, bottomRight, color);
        Debug.DrawLine(bottomRight, bottomLeft, color);
        Debug.DrawLine(bottomLeft, topLeft, color);
    }

    public static void DrawCircle(Vector2 center, float radius, Color color, int segments = 36)
    {
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.Deg2Rad * (angleStep * i);
            float angle2 = Mathf.Deg2Rad * (angleStep * (i + 1));

            Vector2 point1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            Vector2 point2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

            Debug.DrawLine(point1, point2, color);
        }
    }
}
