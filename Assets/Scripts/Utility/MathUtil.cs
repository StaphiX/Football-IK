using UnityEngine;

public static class MathUtil
{
    public static float InterpolateClamp(float x, float xMin, float xMax, float yMin, float yMax)
    {
        return (Mathf.InverseLerp(Mathf.Clamp(x, xMin, xMax), xMin, xMax) * (yMax - yMin)) + yMin;
    }

    public static float CubicBezier(float p0, float p1, float m0, float m1, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        float u = 1.0f - t;
        float u2 = u * u;
        float u3 = u2 * u;

        float result = u3 * p0 +
                       3 * u2 * t * (p0 + m0) +
                       3 * u * t2 * (p1 + m1) +
                       t3 * p1;

        return result;
    }
}

