using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CarrotSurvivors;

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

    public AabbShape boundingRect() {
        return this.kind switch {
            ShapeKind.Circle => this.circle.boundingRect(),
            ShapeKind.Aabb => this.aabb.boundingRect(),
            // _ => throw new NotImplementedException(),
        };
    }

    public readonly bool intersects(Shape other) {
        // return this.circle.intersects(other);
        
        return this.kind switch {
            ShapeKind.Circle => this.circle.intersects(other),
            ShapeKind.Aabb => this.aabb.intersects(other),
        };
    }
}

public struct AabbShape {
    public Vector2 min;
    public Vector2 max;

    public readonly bool intersectsCircle(CircleShape circle) {
        // NOTE @Perf: Use DistanceSquared to avoid a square root. Yes, it
        // matters.
        var closest = Vector2.Max(this.min, Vector2.Min(max, circle.center));
        var distanceSquared = Vector2.DistanceSquared(circle.center, closest);
        return distanceSquared <= circle.radius * circle.radius;
    }

    public readonly bool intersectsAabb(AabbShape other) {
        return this.min.X <= other.max.X
               && this.max.X >= other.min.X
               && this.min.Y <= other.max.Y
               && this.max.Y >= other.min.Y;
    }

    public readonly AabbShape boundingRect() {
        return this;
    }

    public readonly bool intersects(Shape other) {
        return other.kind switch {
            ShapeKind.Circle => this.intersectsCircle(other.circle),
            ShapeKind.Aabb => this.intersectsAabb(other.aabb),
            _ => throw new NotImplementedException(),
        };
    }

    public static Shape fromCenterSize(Vector2 position, Vector2 size) {
        return Shape.Aabb(new AabbShape { min = position - size / 2, max = position + size / 2, });
    }
}

public struct CircleShape {
    public Vector2 center;
    public float radius;

    public AabbShape boundingRect() {
        return new AabbShape {
            min = this.center - new Vector2(this.radius),
            max = this.center + new Vector2(this.radius),
        };
    }

    public readonly bool intersectsCircle(CircleShape other) {
        var distance = Vector2.Distance(center, other.center);
        return distance <= this.radius + other.radius;
    }

    public readonly bool intersectsAabb(AabbShape aabb) {
        return aabb.intersectsCircle(this);
    }

    public readonly bool intersects(Shape other) {
        return other.kind switch {
            ShapeKind.Circle => this.intersectsCircle(other.circle),
            ShapeKind.Aabb => this.intersectsAabb(other.aabb),
            _ => throw new NotImplementedException(),
        };
    }
}

public record struct SpatialHashData(
    Shape shape,
    EntityType type,
    /// <summary>
    ///  This is the entity id, to be interpreted differently for different
    ///  types of entities. For enemies, it can be converted into an index of
    ///  the enemy gen arena. For players, it's the playerIndex.
    /// </summary>
    int entityId
);

public enum EntityType {
    Player,
    Enemy
}

public struct SpatialHashQueryResult {
    public required EntityType type;
    public required int entityId;

    public readonly Player AssumePlayer() {
        if (this.type != EntityType.Player) {
            throw new Exception("Not a player");
        }

        return Player.atIndex(this.entityId);
    }

    public readonly Id<Enemy> AssumeEnemyId() {
        if (this.type != EntityType.Enemy) {
            throw new Exception("Not an enemy");
        }

        return Id<Enemy>.From(this.entityId);
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
    public static object All { get; set; }
}

public class Player {
    public static Player atIndex(int entityId) {
        throw new NotImplementedException();
    }
}

public struct SpatialHash {
    private static readonly SpatialHash instance = new(2f);

    public static SpatialHash get() {
        return instance;
    }

    public float gridSize;
    public Dictionary<(int, int), List<SpatialHashData>> cells;

    public SpatialHash(float gridSize) {
        this.gridSize = gridSize;
        this.cells = [];
    }

    public readonly void clear() {
        this.cells.Clear();
    }

    public void addShape(Shape shape, EntityType type, int entityId) {
        var rect = shape.boundingRect();
        var min = (rect.min / this.gridSize).Floored();
        var max = (rect.max / this.gridSize).Ceiled();

        for (var y = min.Y; y <= max.Y; y += 1) {
            for (var x = min.X; x <= max.X; x += 1) {
                var key = ((int)x, (int)y);

                if (!this.cells.ContainsKey(key)) {
                    this.cells[key] = [];
                }

                this.cells[key]
                    .Add(new SpatialHashData(shape: shape, type: type, entityId: entityId));
            }
        }
    }

    public readonly HashSet<SpatialHashQueryResult> query(Shape shape, EntityType? filter) {
        var rect = shape.boundingRect();
        var min = (rect.min / this.gridSize).Floored();
        var max = (rect.max / this.gridSize).Ceiled();
        var results = new HashSet<SpatialHashQueryResult>(1);

        for (var y = min.Y; y <= max.Y; y += 1) {
            for (var x = min.X; x <= max.X; x += 1) {
                var key = ((int)x, (int)y);

                if (this.cells.TryGetValue(key, out var value)) {
                    foreach (var data in value) {
                        if (filter is not null && data.type != filter) {
                            continue;
                        }

                        if (data.shape.intersects(shape)) {
                            results.Add(
                                new SpatialHashQueryResult {
                                    type = data.type,
                                    entityId = data.entityId,
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

public static class Vec2Extensions {
    public static Vector2 Floored(this Vector2 v) {
        return new Vector2((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
    }

    public static Vector2 Ceiled(this Vector2 v) {
        return new Vector2((float)Math.Ceiling(v.X), (float)Math.Ceiling(v.Y));
    }
}

