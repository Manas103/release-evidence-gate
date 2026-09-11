using System;
using System.IO;
using GateCore;
using Xunit;

namespace GateCore.Tests;

public class TrxParserTests
{
    private static string FixturePath() =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "sample.trx");

    [Fact]
    public void ParsesTestNameAndOutcomeForEveryResult()
    {
        var outcomes = TrxParser.ParseOutcomes(FixturePath());

        Assert.Equal(3, outcomes.Count);
        Assert.Equal("Passed", outcomes["Fixture.Namespace.SomeTests.PassingTest"]);
        Assert.Equal("Failed", outcomes["Fixture.Namespace.SomeTests.FailingTest"]);
        Assert.Equal("NotExecuted", outcomes["Fixture.Namespace.SomeTests.NotExecutedTest"]);
    }

    [Fact]
    public void UnknownTestNameIsAbsentNotDefaulted()
    {
        var outcomes = TrxParser.ParseOutcomes(FixturePath());
        Assert.False(outcomes.ContainsKey("Fixture.Namespace.SomeTests.NeverTraced"));
    }
}
