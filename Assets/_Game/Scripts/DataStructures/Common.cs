using System;
using Newtonsoft.Json;
using StorageUtils;
using UnityEngine;

[Serializable]
public class RandomRange
{
    public int Min;
    public int Max;

    public void AssignValues(Vector2Int vector2Int)
    {
        Min = vector2Int.x;
        Max = vector2Int.y;
    }

    public int GetRandomValue()
    {
        return UnityEngine.Random.Range(Min, Max + 1);
    }
}

[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3()
    {
    }

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializableVector3(Vector3 vector3)
    {
        x = vector3.x;
        y = vector3.y;
        z = vector3.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class GridPosition
{
    public int X;
    public int Y;

    public GridPosition()
    {
    }

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public GridPosition(Vector2Int vector2Int)
    {
        X = vector2Int.x;
        Y = vector2Int.y;
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(X, Y);
    }

    public int CompareTo(GridPosition other)
    {
        if (other == null) return 1; // Null is considered greater
        if (X != other.X) return X.CompareTo(other.X);
        return Y.CompareTo(other.Y);
    }

    public bool HasSameValues(GridPosition other)
    {
        if (other == null) return false;
        return X == other.X && Y == other.Y;
    }
}

[Serializable]
public class KeyValue<T1, T2> : IHasWeight
{
    [JsonProperty("k")]
    public T1 Key;
    [JsonProperty("v")]
    public T2 Value;

    public float GetWeight()
    {
        switch (Value)
        {
            case int i:
                return i;
            case float f:
                return f;

            default:
                return 0;
        }
    }
}
