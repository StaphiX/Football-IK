using UnityEngine;

public static class ColorUtil
{
    public static Color RGB(int r, int g, int b)
    {
        return new Color(r / 255.0f, g / 255.0f, b / 255.0f);
    }
}