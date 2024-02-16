// See https://aka.ms/new-console-template for more information

using System.Numerics;
using CsharpPerfTest;

Console.WriteLine("Hello, World!");

var spatial = SpatialHash.Get();

var stopwatch = new System.Diagnostics.Stopwatch();

var result = new HashSet<SpatialHashQueryResult>(2000);

while (true) {
    stopwatch.Start();

    // insert 1000 entities
    for (var i = 0; i < 1000; i++) {
        // random vec
        var vec = new Vector2(Random.Shared.NextSingle() * 20.0f, Random.Shared.NextSingle() * 20.0f);

        // var circle = Shape.Circle(vec, Random.Shared.NextSingle() * 2.0f);
        var circle = Shape.Circle(new CircleShape() { Center = vec, Radius = Random.Shared.NextSingle() * 2.0f });

        spatial.AddShape(circle,
            EntityType.Enemy, i);
    }

    // average cell size
    // var total = 0;
    // foreach ((int key, var cell) in spatial.Cells) {
    //     total += cell.Count;
    // }

    int count = 0;

    // query 1000 times 
    for (var i = 0; i < 1000; i++) {
        var vec = new Vector2(Random.Shared.NextSingle() * 20.0f, Random.Shared.NextSingle() * 20.0f);
        var circle = Shape.Circle(new CircleShape() { Center = vec, Radius = 2.0f });
        result = spatial.Query( circle, EntityType.Enemy);

        count += result.Count;
    }

    Console.WriteLine($"Count: {count} ... {stopwatch.Elapsed.TotalMicroseconds}us");
    // Console.WriteLine($"Count: {count} ... {stopwatch.ElapsedMilliseconds}ms  ... cells: {spatial.Cells.Count} ... Average cell size: {total / int.Max(spatial.Cells.Count, 1)}");

    stopwatch.Reset();
    spatial.Clear();
}