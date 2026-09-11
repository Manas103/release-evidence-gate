using System;

namespace TraceContracts;

/// <summary>
/// Marks a test method as exercising one software requirement. Applied more than
/// once on the same method when a single test genuinely exercises more than one
/// requirement. This attribute is the only contract shared between the test
/// assembly and the TraceabilityGate console app; the gate never references
/// UnderTest.Tests directly except by loading its built assembly at run time and
/// reading this attribute back through reflection.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TraceAttribute : Attribute
{
    public string RequirementId { get; }

    public TraceAttribute(string requirementId)
    {
        if (string.IsNullOrWhiteSpace(requirementId))
        {
            throw new ArgumentException("Requirement id must not be empty.", nameof(requirementId));
        }

        RequirementId = requirementId;
    }
}
