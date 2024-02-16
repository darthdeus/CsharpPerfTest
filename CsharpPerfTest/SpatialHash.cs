using System.Numerics;
using System.Runtime.CompilerServices;

namespace CsharpPerfTest;

public struct Shape {
    public Vector2 First;
    public Vector2 Second;

    public static Shape Circle(Vector2 center, float radius) {
        var shape = new Shape {
            First = center,
            Second = new Vector2(radius),
        };
        return shape;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AabbShape BoundingRect() {
        return new AabbShape {
            Min = First - new Vector2(Second.X),
            Max = First + new Vector2(Second.X),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Intersects(Shape other) {
        // return this.circle.intersects(other);

        float d = Second.X + other.Second.X;
        return Vector2.DistanceSquared(First, other.First) < d * d;

        // return (Kind, other.Kind) switch {
        //     (ShapeKind.Circle, ShapeKind.Circle) =>,
        //     _ => false
        // };
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


public struct SpatialHash {
    public const int MapSize = 50;
    private static readonly SpatialHash Instance = new(2f);

    public static SpatialHash Get() {
        return Instance;
    }

    public readonly float GridSize;
    public readonly FixedSizeList<SpatialHashData>[,] Cells;

    public SpatialHash(float gridSize) {
        GridSize = gridSize;
        Cells = new FixedSizeList<SpatialHashData>[50, 50];

        for (int i = 0; i < Cells.GetLength(0); i++) {
            for (int j = 0; j < Cells.GetLength(1); j++) {
                Cells[i, j] = new FixedSizeList<SpatialHashData>(50);
            }
        }
    }

    public readonly void Clear() {
        for (int i = 0; i < Cells.GetLength(0); i++) {
            for (int j = 0; j < Cells.GetLength(1); j++) {
                Cells[i, j].Clear();
            }
        }
    }

    public void AddShape(Shape shape, EntityType type, int entityId) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / GridSize).Floored();
        var max = (rect.Max / GridSize).Ceiled();

        for (var y = min.Y; y < max.Y; y += 1) {
            for (var x = min.X; x < max.X; x += 1) {
                var item = new SpatialHashData(shape, type, entityId);
                var coord = Coord.FromPos(x, y);
                Cells[coord.X, coord.Y].Add(item);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Query(List<SpatialHashQueryResult> results, Shape shape, EntityType? _filter) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / GridSize).Floored();
        var max = (rect.Max / GridSize).Ceiled();

        results.Clear();

        for (var y = min.Y; y < max.Y; y += 1) {
            for (var x = min.X; x < max.X; x += 1) {
                var coord = Coord.FromPos(x, y);

                ref var value = ref Cells[coord.X, coord.Y];

                for (int i = 0; i < value.Count; i++) {
                    ref var data = ref value[i];

                    // if (filter is not null && data.Type != filter) {
                    //     continue;
                    // }

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
}
