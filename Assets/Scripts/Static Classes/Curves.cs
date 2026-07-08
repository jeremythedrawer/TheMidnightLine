using UnityEngine;

public static class Curves
{

    public static float EaseInOutCubic(float t)
    {
        return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) * 0.5f;
    }
    public static float EaseOutT(float t, float p)
    {
        return Mathf.Max(1 - Mathf.Pow(1 - t, p), 0);
    }
}
