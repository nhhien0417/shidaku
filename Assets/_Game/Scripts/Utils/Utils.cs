using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Utils
{
    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1) {
            n--;
            var k = Random.Range(0, n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}

public class MathUtils
{
    public static Vector2[] IntersectionPoint(Vector2 p1, Vector2 p2, Vector2 center, float radius)
    {
        Vector2 dp = new Vector2();
        Vector2[] sect;
        float a, b, c;
        float bb4ac;
        float mu1;
        float mu2;

        //  get the distance between X and Z on the segment
        dp.x = p2.x - p1.x;
        dp.y = p2.y - p1.y;
        //   I don't get the math here
        a = dp.x * dp.x + dp.y * dp.y;
        b = 2 * (dp.x * (p1.x - center.x) + dp.y * (p1.y - center.y));
        c = center.x * center.x + center.y * center.y;
        c += p1.x * p1.x + p1.y * p1.y;
        c -= 2 * (center.x * p1.x + center.y * p1.y);
        c -= radius * radius;
        bb4ac = b * b - 4 * a * c;
        if (Mathf.Abs(a) < float.Epsilon || bb4ac < 0)
        {
            //  line does not intersect
            return new Vector2[] { Vector2.zero, Vector2.zero };
        }
        mu1 = (-b + Mathf.Sqrt(bb4ac)) / (2 * a);
        mu2 = (-b - Mathf.Sqrt(bb4ac)) / (2 * a);
        sect = new Vector2[2];
        sect[0] = new Vector2(p1.x + mu1 * (p2.x - p1.x), p1.y + mu1 * (p2.y - p1.y));
        sect[1] = new Vector2(p1.x + mu2 * (p2.x - p1.x), p1.y + mu2 * (p2.y - p1.y));

        return sect;
    }

    public static Vector3 CalculateBezierCurvePoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    public static Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0 + 2 * u * t * p1 + tt * p2;
        return p;
    }
}

public static class MaterialPropertyBlockHelper
{
    private static MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

    public static void SetMaterialProperty<T>(this Renderer renderer, string name, T value)
    {
        bool modified = true;
        renderer.GetPropertyBlock(propBlock);
        switch (value)
        {
            case float floatValue:
                propBlock.SetFloat(name, floatValue);
                break;
            case Color colorValue:
                propBlock.SetColor(name, colorValue);
                break;
            case Texture textureValue:
                propBlock.SetTexture(name, textureValue);
                break;
            default:
                modified = false;
                Debug.LogError("Function is not implement yet!");
                break;
        }

        if (modified) renderer.SetPropertyBlock(propBlock);
    }

    public static void SetMaterialHDRColorProperty(this Renderer renderer, string name, Color color)
    {
        renderer.GetPropertyBlock(propBlock);
        propBlock.SetVector(name, (Vector4)color);
        renderer.SetPropertyBlock(propBlock);
    }
}

public static class ParseUtils
{
    public static bool TryParseToValue<T>(this string s, out T value) where T : IConvertible
    {
        value = default;
        if (string.IsNullOrEmpty(s))
            return false;

        try
        {
            value = (T)Convert.ChangeType(s, typeof(T), CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(new FormatException($"Failed to parse value '{s}' to type {typeof(T)}. ", e));
            return false;
        }
    }
}