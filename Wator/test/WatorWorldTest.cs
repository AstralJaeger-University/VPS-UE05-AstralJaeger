namespace wator_test;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

public class PerformanceTests
{
    [Fact]
    public void OriginalWatorWorldPerformance()
    {
        var logger = new AccumulationLogger();

        var config = ManualConfig.Create(DefaultConfig.Instance)
            .AddLogger(logger)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkRunner.Run<WatorWorldBenchmarks>(config);
    }
}

[MaxIterationCount(20)]
public class WatorWorldBenchmarks {
    [Benchmark]
    public void OriginalPerformance() {
        var settings = new Wator.Settings {
            Height = 500,
            Width = 500,
            Iterations = 100,
            FishBreedTime = 10,
            InitialFishEnergy = 10,
            InitialFishPopulation = 20000,
            InitialSharkEnergy = 50,
            InitialSharkPopulation = 5000,
            SharkBreedEnergy = 100
        };
        var world = new Wator.Original.OriginalWatorWorld(settings);

        for (var i = 0; i < settings.Iterations; ++i) {
            world.ExecuteStep();
        }
    }
}