using System.Numerics;
using System.Runtime.InteropServices;

namespace CsharpPerfTest;

public enum ShapeKind : int {
    Aabb,
    Circle,
}

// [StructLayout(LayoutKind.Explicit)]
public struct Shape {
    public ShapeKind Kind;
    public Vector2 First;
    public Vector2 Second;

    public static Shape Circle(Vector2 center, float radius) {
        return new Shape {
            Kind = ShapeKind.Circle,
            First = center,
            Second = new Vector2(radius),
        };
    }

    public Shape BoundingRect() {
        // return Circle.BoundingRect(First, Second.X);

        return this.Kind switch {
            ShapeKind.Circle => CircleShape.BoundingRect(this),
            ShapeKind.Aabb => this,
        };
    }

    public readonly bool Intersects(Shape other) {
        // return Circle.Intersects(other);

        return this.Kind switch {
            ShapeKind.Circle => CircleShape.Intersects(this, other),
            ShapeKind.Aabb => AabbShape.Intersects(this, other),
        };
    }
}

public struct AabbShape {
    public static bool IntersectsCircle(Shape aabb, Shape circle) {
        // NOTE @Perf: Use DistanceSquared to avoid a square root. Yes, it
        // matters.
        var closest = Vector2.Max(aabb.First, Vector2.Min(aabb.Second, circle.First));
        var distanceSquared = Vector2.DistanceSquared(circle.First, closest);
        return distanceSquared <= circle.Second.X * circle.Second.X;
    }

    public static bool IntersectsAabb(Shape self, Shape other) {
        return self.First.X <= other.Second.X
               && self.Second.X >= other.First.X
               && self.First.Y <= other.Second.Y
               && self.Second.Y >= other.First.Y;
    }

    public static bool Intersects(Shape self, Shape other) {
        // return IntersectsCircle(other.circle);

        return other.Kind switch {
            ShapeKind.Circle => AabbShape.IntersectsCircle(self, other),
            ShapeKind.Aabb => AabbShape.IntersectsAabb(self, other),
        };
    }

    public static Shape FromCenterSize(Vector2 position, Vector2 size) {
        return new Shape {
            Kind = ShapeKind.Aabb,
            First = position - size / 2, Second = position + size / 2,
        };
    }
}

public struct CircleShape {
    public static Shape BoundingRect(Shape circle) {
        return new Shape {
            First = circle.First - new Vector2(circle.Second.X),
            Second = circle.First + new Vector2(circle.Second.X),
        };
    }

    public static bool IntersectsCircle(Shape self, Shape other) {
        float r = self.Second.X + other.Second.X;
        float distance = Vector2.DistanceSquared(self.First, other.First);
        return distance <= r * r;
    }

    public static bool IntersectsAabb(Shape self, Shape aabb) {
        return AabbShape.IntersectsCircle(self, aabb);
    }

    public static bool Intersects(Shape self, Shape other) {
        // return this.IntersectsCircle(other.circle);

        return other.Kind switch {
            ShapeKind.Circle => CircleShape.IntersectsCircle(self, other),
            ShapeKind.Aabb => CircleShape.IntersectsAabb(self, other),
            // _ => throw new NotImplementedException(),
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

public record struct SpatialHashQueryResult {
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
        var min = (rect.First / this.GridSize).Floored();
        var max = (rect.Second / this.GridSize).Ceiled();

        for (var y = min.Y; y < max.Y; y += 1) {
            for (var x = min.X; x < max.X; x += 1) {
                var key = Coord.MakeKey(x, y);

                if (!this.Cells.ContainsKey(key)) {
                    this.Cells[key] = [];
                }

                this.Cells[key]
                    .Add(new SpatialHashData(Shape: shape, Type: type, EntityId: entityId));
            }
        }
    }

    public readonly void Query(HashSet<SpatialHashQueryResult> results, Shape shape, EntityType? filter) {
        var rect = shape.BoundingRect();
        var min = (rect.First / this.GridSize).Floored();
        var max = (rect.Second / this.GridSize).Ceiled();

        for (var y = min.Y; y < max.Y; y += 1) {
            for (var x = min.X; x < max.X; x += 1) {
                var key = Coord.MakeKey(x, y);

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
    }
}