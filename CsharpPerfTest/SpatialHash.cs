using System.Numerics;
using System.Runtime.InteropServices;

namespace CsharpPerfTest;

public enum ShapeKind : int {
    Aabb,
    Circle,
}

[StructLayout(LayoutKind.Explicit)]
public struct Shape {
    [FieldOffset(0)] public ShapeKind kind;

    [FieldOffset(4)] public AabbShape aabb;

    [FieldOffset(4)] public CircleShape circle;

    public static Shape Circle(CircleShape s) {
        var shape = new Shape {
            kind = ShapeKind.Circle,
            aabb = default,
            circle = default,
        };
        shape.circle = s;
        return shape;
    }

    public static Shape Aabb(AabbShape s) {
        var shape = new Shape {
            kind = ShapeKind.Aabb,
            aabb = default,
            circle = default,
        };
        shape.aabb = s;
        return shape;
    }

    public AabbShape BoundingRect() {
        return this.kind switch {
            ShapeKind.Circle => this.circle.BoundingRect(),
            ShapeKind.Aabb => this.aabb.BoundingRect(),
            // _ => throw new NotImplementedException(),
        };
    }

    public readonly bool Intersects(Shape other) {
        // return this.circle.intersects(other);

        return this.kind switch {
            ShapeKind.Circle => this.circle.Intersects(other),
            ShapeKind.Aabb => this.aabb.Intersects(other),
        };
    }
}

public struct AabbShape {
    public Vector2 Min;
    public Vector2 Max;

    public readonly bool IntersectsCircle(CircleShape circle) {
        // NOTE @Perf: Use DistanceSquared to avoid a square root. Yes, it
        // matters.
        var closest = Vector2.Max(this.Min, Vector2.Min(Max, circle.Center));
        var distanceSquared = Vector2.DistanceSquared(circle.Center, closest);
        return distanceSquared <= circle.Radius * circle.Radius;
    }

    public readonly bool IntersectsAabb(AabbShape other) {
        return this.Min.X <= other.Max.X
               && this.Max.X >= other.Min.X
               && this.Min.Y <= other.Max.Y
               && this.Max.Y >= other.Min.Y;
    }

    public readonly AabbShape BoundingRect() {
        return this;
    }

    public readonly bool Intersects(Shape other) {
        return other.kind switch {
            ShapeKind.Circle => this.IntersectsCircle(other.circle),
            ShapeKind.Aabb => this.IntersectsAabb(other.aabb),
            _ => throw new NotImplementedException(),
        };
    }

    public static Shape FromCenterSize(Vector2 position, Vector2 size) {
        return Shape.Aabb(new AabbShape { Min = position - size / 2, Max = position + size / 2, });
    }
}

public struct CircleShape {
    public Vector2 Center;
    public float Radius;

    public AabbShape BoundingRect() {
        return new AabbShape {
            Min = this.Center - new Vector2(this.Radius),
            Max = this.Center + new Vector2(this.Radius),
        };
    }

    public readonly bool IntersectsCircle(CircleShape other) {
        var distance = Vector2.Distance(Center, other.Center);
        return distance <= this.Radius + other.Radius;
    }

    public readonly bool IntersectsAabb(AabbShape aabb) {
        return aabb.IntersectsCircle(this);
    }

    public readonly bool Intersects(Shape other) {
        return other.kind switch {
            ShapeKind.Circle => this.IntersectsCircle(other.circle),
            ShapeKind.Aabb => this.IntersectsAabb(other.aabb),
            _ => throw new NotImplementedException(),
        };
    }
}

public record struct SpatialHashData(
    Shape Shape,
    EntityType Type,
    //  This is the entity id, to be interpreted differently for different
    //  types of entities. For enemies, it can be converted into an index of
    //  the enemy gen arena. For players, it's the playerIndex.
    int EntityId
);

public enum EntityType {
    Player,
    Enemy
}

public struct SpatialHashQueryResult {
    public required EntityType Type;
    public required int EntityId;

    public readonly Player AssumePlayer() {
        if (this.Type != EntityType.Player) {
            throw new Exception("Not a player");
        }

        return Player.AtIndex(this.EntityId);
    }

    public readonly Id<Enemy> AssumeEnemyId() {
        if (this.Type != EntityType.Enemy) {
            throw new Exception("Not an enemy");
        }

        return Id<Enemy>.From(this.EntityId);
    }

    public readonly ref Enemy AssumeEnemy() {
        throw new NotImplementedException();
        // return ref Enemy.All[this.AssumeEnemyId()];
    }
}

public class Id<T> {
    public static Id<Enemy> From(int entityId) {
        throw new NotImplementedException();
    }
}

public class Enemy {
    public static object? All { get; set; }
}

public class Player {
    public static Player AtIndex(int entityId) {
        throw new NotImplementedException();
    }
}

public struct SpatialHash {
    private static readonly SpatialHash Instance = new(2f);

    public static SpatialHash Get() {
        return Instance;
    }

    public float GridSize;
    public Dictionary<(int, int), List<SpatialHashData>> Cells;

    public SpatialHash(float gridSize) {
        this.GridSize = gridSize;
        this.Cells = [];
    }

    public readonly void Clear() {
        this.Cells.Clear();
    }

    public void AddShape(Shape shape, EntityType type, int entityId) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / this.GridSize).Floored();
        var max = (rect.Max / this.GridSize).Ceiled();

        for (var y = min.Y; y <= max.Y; y += 1) {
            for (var x = min.X; x <= max.X; x += 1) {
                var key = ((int)x, (int)y);

                if (!this.Cells.ContainsKey(key)) {
                    this.Cells[key] = [];
                }

                this.Cells[key]
                    .Add(new SpatialHashData(Shape: shape, Type: type, EntityId: entityId));
            }
        }
    }

    public readonly HashSet<SpatialHashQueryResult> Query(Shape shape, EntityType? filter) {
        var rect = shape.BoundingRect();
        var min = (rect.Min / this.GridSize).Floored();
        var max = (rect.Max / this.GridSize).Ceiled();
        var results = new HashSet<SpatialHashQueryResult>(1);

        for (var y = min.Y; y <= max.Y; y += 1) {
            for (var x = min.X; x <= max.X; x += 1) {
                var key = ((int)x, (int)y);

                if (this.Cells.TryGetValue(key, out var value)) {
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

        return results;
    }
}