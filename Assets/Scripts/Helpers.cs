using UnityEngine;

public static class Helpers
{
    //Math
    public static float Remap(float value, float inMin, float inMax, float outMin, float outMax, bool clamp)
    {
        float newValue = (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
        if (clamp)
        {
            newValue = Mathf.Clamp(newValue, outMin, outMax);
        }
        return newValue;
    }

    public static float Step(float edge, float value)
    {
        return value < edge ? 0f : 1f;
    }

    public static float Posterize(float value, float steps)
    {
        return Mathf.Floor(value / (1 / steps) * (1 /  steps));
    }

    //Debug
    public static void DrawBoxCastDebug(Vector2 start, Vector2 end, Color color)
    {
        // box corners
        Vector2 topLeft = new Vector2(start.x, end.y);
        Vector2 topRight = end;
        Vector2 bottomLeft = start;
        Vector2 bottomRight = new Vector2(end.x, start.y);

        // edges
        Debug.DrawLine(topLeft, topRight, color);
        Debug.DrawLine(topRight, bottomRight, color);
        Debug.DrawLine(bottomRight, bottomLeft, color);
        Debug.DrawLine(bottomLeft, topLeft, color);
    }

    public static void DrawCircle(Vector2 center, float radius, Color color, int segments, bool useGizmos)
    {
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.Deg2Rad * (angleStep * i);
            float angle2 = Mathf.Deg2Rad * (angleStep * (i + 1));

            Vector2 point1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            Vector2 point2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

            if (useGizmos)
            {
                Gizmos.color = color;
                Gizmos.DrawLine(point1, point2);
            }
            else
            {

                Debug.DrawLine(point1, point2, color);
            }
        }
    }

}
