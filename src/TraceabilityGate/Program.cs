using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using GateCore;

namespace TraceabilityGate;

/// <summary>
/// The requirements traceability gate. Genuinely separate from both the
/// under-test project and its tests: it never sees UnderTest's source, only
/// the built UnderTest.Tests.dll, which it loads and reflects over after
/// running the real test suite. Exits non-zero and names exactly the
/// orphaned requirements when any of the 48 requirements in
/// data/requirements.csv has no passing traced test.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var options = ParseArgs(args);

        var repoRoot = options.RepoRoot ?? Directory.GetCurrentDirectory();
        var requirementsPath = Path.GetFullPath(options.RequirementsPath ?? Path.Combine(repoRoot, "data", "requirements.csv"));
        var testProjectPath = Path.GetFullPath(options.TestProjectPath ?? Path.Combine(repoRoot, "src", "UnderTest.Tests", "UnderTest.Tests.csproj"));
        var testAssemblyPath = options.TestAssemblyPath is null
            ? Path.GetFullPath(Path.Combine(Path.GetDirectoryName(testProjectPath)!, "bin", "Release", "net8.0", "UnderTest.Tests.dll"))
            : Path.GetFullPath(options.TestAssemblyPath);

        Console.WriteLine("== release-evidence-gate: traceability gate ==");
        Console.WriteLine($"requirements manifest : {requirementsPath}");
        Console.WriteLine($"test project          : {testProjectPath}");
        Console.WriteLine($"test assembly         : {testAssemblyPath}");
        Console.WriteLine();

        var trxDir = Directory.CreateTempSubdirectory("reg-trx-").FullName;
        var trxFileName = "trace_results.trx";

        Console.WriteLine("-- running dotnet test --");
        var exitCode = RunDotnetTest(testProjectPath, trxDir, trxFileName);
        var trxPath = Path.Combine(trxDir, trxFileName);
        Console.WriteLine($"dotnet test exit code: {exitCode}");
        Console.WriteLine();

        if (!File.Exists(trxPath))
        {
            Console.Error.WriteLine($"FATAL: expected trx results file was not produced at {trxPath}");
            return 2;
        }

        var outcomes = TrxParser.ParseOutcomes(trxPath);
        Console.WriteLine($"parsed {outcomes.Count} test results from trx");

        var assembly = Assembly.LoadFrom(testAssemblyPath);
        var requirementToTests = BuildRequirementToTestMap(assembly);
        var tracedTestCount = requirementToTests.Values.SelectMany(v => v).Distinct().Count();
        Console.WriteLine($"found {tracedTestCount} distinct traced test methods across {requirementToTests.Count} requirements via reflection");
        Console.WriteLine();

        var requirementIds = RequirementsCatalog.LoadIds(requirementsPath);
        var descriptions = RequirementsCatalog.LoadDescriptions(requirementsPath);
        var report = CoverageComputer.Compute(requirementIds, requirementToTests, outcomes);

        var invariantProblems = report.CheckInvariant();
        if (invariantProblems.Count > 0)
        {
            Console.Error.WriteLine("FATAL: coverage report invariant violated:");
            foreach (var p in invariantProblems) Console.Error.WriteLine($"  - {p}");
            return 2;
        }

        Console.WriteLine("-- per-requirement coverage --");
        foreach (var r in report.Requirements.OrderBy(r => r.RequirementId))
        {
            var desc = descriptions.GetValueOrDefault(r.RequirementId, "");
            if (r.Covered)
            {
                Console.WriteLine($"[COVERED ] {r.RequirementId} ({desc}) via {string.Join(", ", r.PassingTests)}");
            }
            else
            {
                var reason = r.NonPassingTests.Count == 0
                    ? "no traced test at all"
                    : $"traced test(s) present but none passing: {string.Join(", ", r.NonPassingTests)}";
                Console.WriteLine($"[ORPHANED] {r.RequirementId} ({desc}) -- {reason}");
            }
        }

        Console.WriteLine();
        var covered = report.CoveredRequirementIds;
        var orphaned = report.OrphanedRequirementIds;
        Console.WriteLine($"SUMMARY: {requirementIds.Count} requirements, {covered.Count} covered, {orphaned.Count} orphaned");

        if (orphaned.Count > 0)
        {
            Console.WriteLine($"ORPHANED REQUIREMENTS ({orphaned.Count}): {string.Join(", ", orphaned)}");
            Console.WriteLine("GATE RESULT: FAIL");
            return 1;
        }

        Console.WriteLine("GATE RESULT: PASS");
        return 0;
    }

    private static Dictionary<string, List<string>> BuildRequirementToTestMap(Assembly assembly)
    {
        var map = new Dictionary<string, List<string>>();

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var attrData in method.GetCustomAttributesData())
                {
                    if (attrData.AttributeType.FullName != "TraceContracts.TraceAttribute") continue;
                    if (attrData.ConstructorArguments.Count == 0) continue;
                    var reqId = attrData.ConstructorArguments[0].Value as string;
                    if (string.IsNullOrWhiteSpace(reqId)) continue;

                    var testKey = $"{type.FullName}.{method.Name}";
                    if (!map.TryGetValue(reqId, out var list))
                    {
                        list = new List<string>();
                        map[reqId] = list;
                    }

                    list.Add(testKey);
                }
            }
        }

        return map;
    }

    private static int RunDotnetTest(string testProjectPath, string trxDir, string trxFileName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"test \"{testProjectPath}\" -c Release --logger \"trx;LogFileName={trxFileName}\" --results-directory \"{trxDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
    }

    private sealed class Options
    {
        public string? RepoRoot;
        public string? RequirementsPath;
        public string? TestProjectPath;
        public string? TestAssemblyPath;
    }

    private static Options ParseArgs(string[] args)
    {
        var options = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--repo-root" when i + 1 < args.Length:
                    options.RepoRoot = args[++i];
                    break;
                case "--requirements" when i + 1 < args.Length:
                    options.RequirementsPath = args[++i];
                    break;
                case "--test-project" when i + 1 < args.Length:
                    options.TestProjectPath = args[++i];
                    break;
                case "--test-assembly" when i + 1 < args.Length:
                    options.TestAssemblyPath = args[++i];
                    break;
            }
        }

        return options;
    }
}
