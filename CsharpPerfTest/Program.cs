// See https://aka.ms/new-console-template for more information

using System.Numerics;
using CsharpPerfTest;

Console.WriteLine("Hello, World!");

var spatial = SpatialHash.Get();

var stopwatch = new System.Diagnostics.Stopwatch();

var result = new HashSet<SpatialHashQueryResult>();

while (true) {
    stopwatch.Start();

    // insert 1000 entities
    for (var i = 0; i < 1000; i++) {
        // random vec
        var vec = new Vector2(Random.Shared.NextSingle() * 20.0f, Random.Shared.NextSingle() * 20.0f);

        spatial.addShape(Shape.Circle(vec, Random.Shared.NextSingle() * 2.0f),
            EntityType.Enemy, i);
    }

    int count = 0;

    // query 1000 times 
    for (var i = 0; i < 1000; i++) {
        var vec = new Vector2(Random.Shared.NextSingle() * 20.0f, Random.Shared.NextSingle() * 20.0f);
        spatial.query(result, Shape.Circle(vec, 2.0f), EntityType.Enemy);

        count += result.Count;
    }

    spatial.clear();

    Console.WriteLine($"Count: {count} ... {stopwatch.ElapsedMilliseconds}ms");

    stopwatch.Reset();
}