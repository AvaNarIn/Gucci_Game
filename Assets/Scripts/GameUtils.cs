using UnityEngine;

public static class CellConstants
{
    public static float[] AllowedMultipliers = new float[]
    {
        0.5f, 0.6f, 0.7f, 0.8f, 0.9f,
        1.0f, 1.1f, 1.2f, 1.3f, 1.4f,
        1.5f, 1.6f, 1.7f, 1.8f, 1.9f, 2.0f
    };
}

public static class GameUtils
{
    public static int RoundToPretty(float value)
    {
        if (value < 10)
            return Mathf.CeilToInt(value);

        // 10–99: округляем вверх до ближайшего числа, кратного 5
        if (value < 100)
        {
            int rounded = Mathf.CeilToInt(value / 5f) * 5;
            if (rounded <= value)
                rounded += 5;
            return rounded;
        }

        // 100 и больше: шаг зависит от порядка числа
        int digits = Mathf.FloorToInt(Mathf.Log10(value));
        int step = Mathf.RoundToInt(Mathf.Pow(10, digits - 1));
        if (step < 10) step = 10;  // для 100–999 шаг = 10

        int rounded2 = Mathf.CeilToInt(value / step) * step;
        if (rounded2 <= Mathf.FloorToInt(value))
            rounded2 += step;
        return rounded2;
    }

    public static string FormatNumber(int value)
    {
        if (value < 1000)
            return value.ToString();

        float v = value;
        string[] suffixes = { "K", "M", "B", "T" };
        int suffixIndex = -1;
        while (v >= 1000f && suffixIndex < suffixes.Length - 1)
        {
            v /= 1000f;
            suffixIndex++;
        }
        return v.ToString("F1") + suffixes[suffixIndex];
    }
}
