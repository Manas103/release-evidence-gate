using System.Collections.Generic;
using System.Xml.Linq;

namespace GateCore;

public static class TrxParser
{
    /// <summary>
    /// Parses a VSTest .trx results file into a map of fully-qualified test
    /// name (Namespace.Class.Method, matching what reflection reports for
    /// the same test method) to its outcome string ("Passed", "Failed",
    /// "NotExecuted", etc). Uses the file's own root namespace instead of a
    /// hardcoded schema URL, since that is the only part of the TRX format
    /// this gate actually depends on.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ParseOutcomes(string trxPath)
    {
        var doc = XDocument.Load(trxPath);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var outcomes = new Dictionary<string, string>();

        var resultsNode = doc.Root?.Element(ns + "Results");
        if (resultsNode is null)
        {
            return outcomes;
        }

        foreach (var result in resultsNode.Elements(ns + "UnitTestResult"))
        {
            var testName = (string?)result.Attribute("testName");
            var outcome = (string?)result.Attribute("outcome");
            if (testName is null || outcome is null) continue;

            // Last result wins if a test name repeats (retries); this gate
            // does not enable retries, but this keeps behavior well-defined
            // if that ever changes.
            outcomes[testName] = outcome;
        }

        return outcomes;
    }
}
