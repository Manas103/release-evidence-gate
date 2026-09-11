using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GateCore;

public static class RequirementsCatalog
{
    /// <summary>
    /// Loads the requirements manifest CSV (id,description; header row).
    /// Returns ids in file order, deduplicated is not attempted here on
    /// purpose: a duplicate id in the manifest itself is a data error the
    /// gate should surface, not silently absorb, so callers should treat a
    /// non-distinct result as a bug in the manifest.
    /// </summary>
    public static IReadOnlyList<string> LoadIds(string path)
    {
        var lines = File.ReadAllLines(path);
        return lines
            .Skip(1)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split(',', 2)[0].Trim())
            .ToList();
    }

    public static IReadOnlyDictionary<string, string> LoadDescriptions(string path)
    {
        var lines = File.ReadAllLines(path);
        var result = new Dictionary<string, string>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',', 2);
            result[parts[0].Trim()] = parts.Length > 1 ? parts[1].Trim() : "";
        }

        return result;
    }
}
