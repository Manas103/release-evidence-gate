using System.Collections.Generic;
using System.Linq;

namespace GateCore;

public static class CoverageComputer
{
    /// <summary>
    /// Pure function: given the full requirement id list, a map of
    /// requirement id to the test keys traced to it, and a map of test key
    /// to outcome, decides which requirements are covered. A requirement is
    /// covered only if at least one traced test both exists in the outcome
    /// map and passed; zero traced tests, a traced test that never ran, and
    /// a traced test that failed are all treated as not covered, on
    /// purpose, since "a test exists" and "a test currently proves the
    /// requirement" are different claims and only the second one is the one
    /// a release gate should trust.
    /// </summary>
    public static CoverageReport Compute(
        IReadOnlyList<string> requirementIds,
        IReadOnlyDictionary<string, List<string>> requirementToTestKeys,
        IReadOnlyDictionary<string, string> testOutcomes)
    {
        var requirements = new List<RequirementCoverage>(requirementIds.Count);

        foreach (var reqId in requirementIds)
        {
            var testKeys = requirementToTestKeys.TryGetValue(reqId, out var keys) ? keys : new List<string>();

            var passing = new List<string>();
            var nonPassing = new List<string>();

            foreach (var key in testKeys)
            {
                if (testOutcomes.TryGetValue(key, out var outcome) && outcome == "Passed")
                {
                    passing.Add(key);
                }
                else
                {
                    nonPassing.Add(key);
                }
            }

            requirements.Add(new RequirementCoverage(reqId, passing.Count > 0, passing, nonPassing));
        }

        return new CoverageReport(requirementIds, requirements);
    }
}
