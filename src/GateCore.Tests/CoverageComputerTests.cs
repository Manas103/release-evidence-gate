using System;
using System.Collections.Generic;
using System.Linq;
using GateCore;
using Xunit;

namespace GateCore.Tests;

/// <summary>
/// Tests CoverageComputer against a small, hand-built fixture where the
/// correct answer was worked out by hand before the code was run, then
/// diffed exactly against what the computer produces, rather than eyeballing
/// the output and deciding it looked plausible.
/// </summary>
public class CoverageComputerTests
{
    // Fixture: 6 requirements, deliberately covering every combination that
    // matters: a requirement with one passing test, a requirement with one
    // failing test and no passing test, a requirement with two tests where
    // only one passes, a requirement with two tests where both fail, a
    // requirement with zero traced tests at all, and a requirement whose
    // only traced test never ran (present in the trace map but absent from
    // the outcomes map, simulating a test that was deleted or renamed after
    // being traced).
    private static readonly List<string> Requirements = new()
    {
        "REQ-A", "REQ-B", "REQ-C", "REQ-D", "REQ-E", "REQ-F"
    };

    private static readonly Dictionary<string, List<string>> ReqToTests = new()
    {
        ["REQ-A"] = new List<string> { "T.A1" },
        ["REQ-B"] = new List<string> { "T.B1" },
        ["REQ-C"] = new List<string> { "T.C1", "T.C2" },
        ["REQ-D"] = new List<string> { "T.D1", "T.D2" },
        ["REQ-E"] = new List<string>(),
        ["REQ-F"] = new List<string> { "T.F1" },
        // REQ-F's only test, T.F1, is intentionally absent from Outcomes below.
    };

    private static readonly Dictionary<string, string> Outcomes = new()
    {
        ["T.A1"] = "Passed",
        ["T.B1"] = "Failed",
        ["T.C1"] = "Failed",
        ["T.C2"] = "Passed",
        ["T.D1"] = "Failed",
        ["T.D2"] = "Failed",
        // T.F1 deliberately missing.
    };

    // Hand-computed expected result, worked out on paper before running
    // anything: A covered (passing test), B orphaned (only test failed),
    // C covered (one of two tests passed), D orphaned (both tests failed),
    // E orphaned (zero traced tests), F orphaned (traced test never ran).
    private static readonly HashSet<string> ExpectedCovered = new() { "REQ-A", "REQ-C" };
    private static readonly HashSet<string> ExpectedOrphaned = new() { "REQ-B", "REQ-D", "REQ-E", "REQ-F" };

    [Fact]
    public void MatchesHandComputedReferenceOracleExactly()
    {
        var report = CoverageComputer.Compute(Requirements, ReqToTests, Outcomes);

        Assert.Equal(ExpectedCovered.OrderBy(x => x), report.CoveredRequirementIds.OrderBy(x => x));
        Assert.Equal(ExpectedOrphaned.OrderBy(x => x), report.OrphanedRequirementIds.OrderBy(x => x));
    }

    [Fact]
    public void InvariantHoldsOnTheReferenceFixture()
    {
        var report = CoverageComputer.Compute(Requirements, ReqToTests, Outcomes);
        Assert.Empty(report.CheckInvariant());
    }

    [Fact]
    public void RequirementWithZeroTracedTestsIsOrphaned()
    {
        var report = CoverageComputer.Compute(Requirements, ReqToTests, Outcomes);
        var reqE = report.Requirements.Single(r => r.RequirementId == "REQ-E");
        Assert.False(reqE.Covered);
        Assert.Empty(reqE.PassingTests);
        Assert.Empty(reqE.NonPassingTests);
    }

    [Fact]
    public void RequirementWithTracedTestThatNeverRanIsOrphanedNotCovered()
    {
        var report = CoverageComputer.Compute(Requirements, ReqToTests, Outcomes);
        var reqF = report.Requirements.Single(r => r.RequirementId == "REQ-F");
        Assert.False(reqF.Covered);
        Assert.Contains("T.F1", reqF.NonPassingTests);
    }

    /// <summary>
    /// Property-style check over 500 randomly generated fixtures with a
    /// fixed seed (deterministic, not flaky): for every random requirement
    /// list, random test tracing, and random outcome assignment, the
    /// invariant (every requirement classified exactly once, covered or
    /// orphaned, never both, never neither) holds. This is the same
    /// invariant the real TraceabilityGate run relies on when it reports
    /// "48 requirements, X covered, Y orphaned" and expects X + Y == 48.
    /// </summary>
    [Fact]
    public void InvariantHoldsAcrossFiveHundredRandomFixtures()
    {
        var rng = new Random(20260911);
        var outcomeChoices = new[] { "Passed", "Failed", "NotExecuted" };

        for (var trial = 0; trial < 500; trial++)
        {
            var reqCount = rng.Next(1, 20);
            var reqIds = Enumerable.Range(1, reqCount).Select(i => $"REQ-{trial}-{i}").ToList();

            var reqToTests = new Dictionary<string, List<string>>();
            var outcomes = new Dictionary<string, string>();
            var testCounter = 0;

            foreach (var reqId in reqIds)
            {
                var testCount = rng.Next(0, 4);
                var tests = new List<string>();
                for (var t = 0; t < testCount; t++)
                {
                    var testKey = $"T-{trial}-{testCounter++}";
                    tests.Add(testKey);
                    // 30% chance the test key never made it into outcomes
                    // at all (simulates a stale trace, same as REQ-F above).
                    if (rng.NextDouble() > 0.3)
                    {
                        outcomes[testKey] = outcomeChoices[rng.Next(outcomeChoices.Length)];
                    }
                }

                reqToTests[reqId] = tests;
            }

            var report = CoverageComputer.Compute(reqIds, reqToTests, outcomes);
            var violations = report.CheckInvariant();

            Assert.True(violations.Count == 0,
                $"trial {trial} violated the invariant: {string.Join("; ", violations)}");
            Assert.Equal(reqCount, report.CoveredRequirementIds.Count + report.OrphanedRequirementIds.Count);
        }
    }
}
