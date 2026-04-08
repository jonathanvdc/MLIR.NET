namespace MLIR.Tests;

using System;
using System.Linq;
using Xunit;

public sealed class TableGenBenchmarksTests
{
    [Fact]
    public void RunForDurationCollectsSamplesUntilTargetDurationIsMet()
    {
        var durations = new Queue<double>(new[] { 250.0, 250.0, 500.0, 125.0 });

        var samples = BenchmarkSampling.RunForDuration(() => durations.Dequeue(), TimeSpan.FromMilliseconds(1000));

        Assert.Equal(3, samples.Count);
        Assert.Equal(1000.0, samples.Sum(), precision: 6);
    }

    [Fact]
    public void RunForCountCollectsTheRequestedNumberOfSamples()
    {
        var calls = 0;

        var samples = BenchmarkSampling.RunForCount(() =>
        {
            calls++;
            return calls * 10.0;
        }, 3);

        Assert.Equal(3, samples.Count);
        Assert.Equal(new[] { 10.0, 20.0, 30.0 }, samples);
    }

    [Fact]
    public void CompareMarkdownShowsACommitOnceWhenBaselineAndCandidateMatch()
    {
        var report = CreateReport("e1e2e369a5a1bde255fe9ef6f969ffd4d02603cf");

        var markdown = BenchmarkComparison.CreateMarkdown(report, report);

        Assert.Contains("Commit: `e1e2e369a5a1bde255fe9ef6f969ffd4d02603cf`", markdown);
        Assert.DoesNotContain("Baseline commit:", markdown);
        Assert.DoesNotContain("Candidate commit:", markdown);
    }

    private static BenchmarkReport CreateReport(string commit)
    {
        var result = BenchmarkResult.FromSamples(
            "mini",
            "A tiny benchmark",
            new[] { 1.0 },
            new[] { 1.0, 1.0, 1.0 });

        return new BenchmarkReport(
            commit,
            "test-machine",
            DateTimeOffset.UnixEpoch,
            1,
            3,
            null,
            1000,
            [result]);
    }
}
