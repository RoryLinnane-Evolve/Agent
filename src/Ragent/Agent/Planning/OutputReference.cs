using System.Text.RegularExpressions;

namespace Ragent.Agent.Planning;

/// <summary>
/// Handles {{stepId}} output placeholders inside workflow step parameter values.
/// A placeholder is replaced with the referenced step's tool output at execution time,
/// which is how one tool's output is mapped onto another tool's input.
/// </summary>
public static partial class OutputReference {
    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_\-.]+)\s*\}\}")]
    private static partial Regex Pattern();

    /// <summary>
    /// Returns the distinct step IDs referenced by placeholders in the given value.
    /// </summary>
    public static IReadOnlyList<string> FindReferences(string value) =>
        Pattern().Matches(value).Select(m => m.Groups[1].Value).Distinct().ToList();

    /// <summary>
    /// Replaces every known placeholder in the value with the referenced step's output.
    /// Unknown references are left untouched.
    /// </summary>
    public static string Substitute(string value, IReadOnlyDictionary<string, string> outputs) =>
        Pattern().Replace(value, m => outputs.TryGetValue(m.Groups[1].Value, out var output) ? output : m.Value);
}
