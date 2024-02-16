using System.Numerics;
using System.Runtime.CompilerServices;

namespace CsharpPerfTest;

public struct Coord {
    public int X;
    public int Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeKey(float x, float y) {
        return 1000 * (int)x + (int)y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coord FromPos(float x, float y) {
        int h = SpatialHash.MapSize / 2;

        return new Coord { X = (int)x + h, Y = (int)y + h };
    }
}

public class FixedSizeList<T> where T : struct {
    public T[] Items;
    public int Count;

    public FixedSizeList(int capacity) {
        Items = new T[capacity];
        Count = 0;
    }

    public ref T this[int index] {
        get {
            if (index < 0 || index >= Count) // Now checks _count instead of _items.Length
                throw new IndexOutOfRangeException();
            return ref Items[index];
        }
    }

    public void Add(T item) {
        if (Count >= Items.Length) {
            Grow();
        }

        Items[Count] = item;
        Count++;
    }

    private void Grow() {
        int newCapacity = Items.Length == 0 ? 4 : Items.Length * 2;
        T[] newArray = new T[newCapacity];
        Array.Copy(Items, newArray, Items.Length);
        Items = newArray;
    }

    public void Clear() {
        Count = 0;
    }
}

public static class Vec2Extensions {
    public static Vector2 Floored(this Vector2 v) {
        return new Vector2((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
    }

    public static Vector2 Ceiled(this Vector2 v) {
        return new Vector2((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y));
    }
}