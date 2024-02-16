using System.Numerics;
using System.Runtime.CompilerServices;

namespace CsharpPerfTest;

public enum ShapeKind : byte {
    Circle,
}

public struct Shape {
    public ShapeKind Kind;
    public Vector2 First;
    public Vector2 Second;

    public static Shape Circle(Vector2 center, float radius) {
        var shape = new Shape {
            Kind = ShapeKind.Circle,
            First = center,
            Second = new Vector2(radius),
        };
        return shape;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AabbShape BoundingRect() {
        return Kind switch {
            ShapeKind.Circle => new AabbShape {
                Min = First - new Vector2(Second.X),
                Max = First + new Vector2(Second.X),
            },
            _ => new AabbShape()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Intersects(Shape other) {
        // return this.circle.intersects(other);

        return (Kind, other.Kind) switch {
            (ShapeKind.Circle, ShapeKind.Circle) => Vector2.Distance(First, other.First) < Second.X + other.Second.X,
            _ => false
        };
    }
}

public struct AabbShape {
    public Vector2 Min;
    public Vector2 Max;
}

public record struct SpatialHashData(
    Shape Shape,
    EntityType Type,
    int EntityId
);

public enum EntityType {
    Enemy
}

public struct SpatialHashQueryResult {
    public required EntityType Type;
    public required int EntityId;
}

public struct Coord {
    public int X;
    public int Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MakeKey(float x, float y) {
        return 1000 * (int)x + (int)y;
    }
}

public class ObjectPool<T> where T : new() {
    private readonly Stack<T> _availableObjects = new Stack<T>();

    public T Get() {
        if (_availableObjects.Count == 0) {
            return new T();
        }

        return _availableObjects.Pop();
    }

    public void Release(T obj) {
        _availableObjects.Push(obj);
    }
}

public class ArrayPool {
    private readonly Stack<FixedSizeList<SpatialHashData>> _availableObjects = new();

    public FixedSizeList<SpatialHashData> Get() {
        if (_availableObjects.Count == 0) {
            return new FixedSizeList<SpatialHashData>(50);
        }

        return _availableObjects.Pop();
    }

    public void Release(FixedSizeList<SpatialHashData> obj) {
        obj.Clear();
        _availableObjects.Push(obj);
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

    public int Capacity => Items.Length;

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

public struct SpatialHash {
    // public ObjectPool<List<SpatialHashData>> Pool = new();
    public ArrayPool Pool = new();
    private static readonly SpatialHash Instance = new(2f);

    public static SpatialHash Get() {
        return Instance;
    }

    public float GridSize;
    public readonly Dictionary<int, FixedSizeList<SpatialHashData>> Cells;

    public SpatialHash(float gridSize) {
        GridSize = gridSize;
        Cells = [];
    }

    public readonly void Clear() {
        // return to pool
        foreach (var (key, value) in Cells) {
            Pool.Release(value);
        }

        Cells.Clear();
    }

    public void AddShape(Shape shape, EntityType type, int entityId) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / GridSize).Floored();
        var max = (rect.Max / GridSize).Ceiled();

        for (var y = min.Y; y < max.Y; y += 1) {
            for (var x = min.X; x < max.X; x += 1) {
                var key = Coord.MakeKey(x, y);

                var item = new SpatialHashData(Shape: shape, Type: type, EntityId: entityId);

                if (Cells.TryGetValue(key, out var entry)) {
                    entry.Add(item);
                } else {
                    var list = Pool.Get();
                    list.Add(item);
                    Cells[key] = list;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Query(List<SpatialHashQueryResult> results, Shape shape, EntityType? filter) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / GridSize).Floored();
        var max = (rect.Max / GridSize).Ceiled();
        // var results = new HashSet<SpatialHashQueryResult>(1);

        results.Clear();

        for (var y = min.Y; y <= max.Y; y += 1) {
            for (var x = min.X; x <= max.X; x += 1) {
                var key = Coord.MakeKey(x, y);

                if (this.Cells.TryGetValue(key, out var value)) {
                    for (int i = 0; i < value.Count; i++) {
                        ref var data = ref value[i];

                        if (filter is not null && data.Type != filter) {
                            continue;
                        }

                        if (data.Shape.Intersects(shape)) {
                            results.Add(
                                new SpatialHashQueryResult {
                                    Type = data.Type,
                                    EntityId = data.EntityId,
                                }
                            );
                        }
                    }
                }
            }
        }

        // return results;
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