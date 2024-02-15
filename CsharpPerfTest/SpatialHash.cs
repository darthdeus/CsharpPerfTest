using System.Numerics;

namespace CsharpPerfTest;

public enum ShapeKind : int {
    Aabb,
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

    public static Shape Aabb(AabbShape s) {
        var shape = new Shape {
            Kind = ShapeKind.Aabb,
            First = s.Min,
            Second = s.Max,
        };
        return shape;
    }

    public AabbShape BoundingRect() {
        return this.Kind switch {
            ShapeKind.Circle => new AabbShape {
                Min = this.First - new Vector2(this.Second.X),
                Max = this.First + new Vector2(this.Second.X),
            }
        };
        // ShapeKind.Aabb => this.aabb.boundingRect(),
        // _ => throw new NotImplementedException(),
        // }
    }

    public readonly bool Intersects(Shape other) {
        // return this.circle.intersects(other);

        return (Kind, other.Kind) switch {
            (ShapeKind.Circle, ShapeKind.Circle) => Vector2.Distance(First, other.First) < Second.X + other.Second.X,
            // _ => throw new NotImplementedException()
        };

        // return this.kind switch {
        //     ShapeKind.Circle => this.circle.intersects(other),
        //     ShapeKind.Aabb => this.aabb.intersects(other),
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
    Player,
    Enemy
}

public struct SpatialHashQueryResult {
    public required EntityType Type;
    public required int EntityId;

}

public struct Enemy {
}

public struct Coord {
    public int X;
    public int Y;
}

public struct SpatialHash {
    private static readonly SpatialHash Instance = new(2f);

    public static SpatialHash Get() {
        return Instance;
    }

    public float gridSize;
    public Dictionary<Coord, List<SpatialHashData>> cells;

    public SpatialHash(float gridSize) {
        this.gridSize = gridSize;
        this.cells = [];
    }

    public readonly void clear() {
        this.cells.Clear();
    }

    public void addShape(Shape shape, EntityType type, int entityId) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / this.gridSize).Floored();
        var max = (rect.Max / this.gridSize).Ceiled();

        for (var y = min.Y; y <= max.Y; y += 1) {
            for (var x = min.X; x <= max.X; x += 1) {
                var key = new Coord() { X = (int)x, Y = (int)y };

                if (!this.cells.ContainsKey(key)) {
                    this.cells[key] = [];
                }

                this.cells[key]
                    .Add(new SpatialHashData(Shape: shape, Type: type, EntityId: entityId));
            }
        }
    }

    public readonly void query(HashSet<SpatialHashQueryResult> results, Shape shape, EntityType? filter) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / this.gridSize).Floored();
        var max = (rect.Max / this.gridSize).Ceiled();
        // var results = new HashSet<SpatialHashQueryResult>(1);

        results.Clear();

        for (var y = min.Y; y <= max.Y; y += 1) {
            for (var x = min.X; x <= max.X; x += 1) {
                var key = new Coord() { X = (int)x, Y = (int)y };

                if (this.cells.TryGetValue(key, out var value)) {
                    foreach (var data in value) {
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