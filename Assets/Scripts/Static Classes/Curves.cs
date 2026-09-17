using UnityEngine;

public static class Curves
{

    public static float EaseInOutCubic(float t)
    {
        return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
    }
    public static float EaseOutT(float t, float p)
    {
        return Mathf.Max(1 - Mathf.Pow(1 - t, p), 0);
    }

    public static float NormalGaussianValue(float t)
    {
        return Mathf.Exp(-(Mathf.Pow(t - 0.5f, 2) / 0.045f)) * 0.5f;
    }
}
