using System.Collections.Generic;
using System.Linq;

namespace GateCore;

/// <summary>Coverage decision for one requirement.</summary>
public sealed record RequirementCoverage(
    string RequirementId,
    bool Covered,
    IReadOnlyList<string> PassingTests,
    IReadOnlyList<string> NonPassingTests);

/// <summary>
/// The full coverage decision for every requirement in the manifest. The
/// invariant this type exists to make checkable: every requirement id that
/// went in comes back out in <see cref="Requirements"/> exactly once, and
/// each one lands in exactly one of Covered or Orphaned, never both, never
/// neither. <see cref="CoverageComputer"/> is what is expected to keep that
/// true; <see cref="CheckInvariant"/> verifies it independently rather than
/// trusting the computer that produced the report.
/// </summary>
public sealed record CoverageReport(IReadOnlyList<string> AllRequirementIds, IReadOnlyList<RequirementCoverage> Requirements)
{
    public IReadOnlyList<string> CoveredRequirementIds =>
        Requirements.Where(r => r.Covered).Select(r => r.RequirementId).OrderBy(x => x).ToList();

    public IReadOnlyList<string> OrphanedRequirementIds =>
        Requirements.Where(r => !r.Covered).Select(r => r.RequirementId).OrderBy(x => x).ToList();

    /// <summary>
    /// Independent check of the "exactly once, never both, never neither"
    /// invariant. Returns a human-readable list of violations; empty means
    /// the invariant holds.
    /// </summary>
    public IReadOnlyList<string> CheckInvariant()
    {
        var problems = new List<string>();
        var seen = new Dictionary<string, int>();
        foreach (var r in Requirements)
        {
            seen[r.RequirementId] = seen.GetValueOrDefault(r.RequirementId) + 1;
        }

        foreach (var id in AllRequirementIds)
        {
            var count = seen.GetValueOrDefault(id);
            if (count == 0)
            {
                problems.Add($"{id}: missing from coverage report entirely");
            }
            else if (count > 1)
            {
                problems.Add($"{id}: appears {count} times in coverage report");
            }
        }

        foreach (var id in seen.Keys)
        {
            if (!AllRequirementIds.Contains(id))
            {
                problems.Add($"{id}: appears in coverage report but not in the requirements manifest");
            }
        }

        var coveredSet = CoveredRequirementIds.ToHashSet();
        var orphanedSet = OrphanedRequirementIds.ToHashSet();
        var both = coveredSet.Intersect(orphanedSet).ToList();
        if (both.Count > 0)
        {
            problems.Add($"classified as both covered and orphaned: {string.Join(", ", both)}");
        }

        return problems;
    }
}
