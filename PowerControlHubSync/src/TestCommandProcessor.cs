namespace SmartFuseSync;

/// <summary>
/// Represents a test case with command and expected response(s)
/// </summary>
internal class TestCommand
{
    public string Command { get; set; } = string.Empty;
    public string[] ExpectedResponses { get; set; } = Array.Empty<string>();

    public bool IsMatch(string actualResponse)
    {
        foreach (string expected in ExpectedResponses)
        {
            if (MatchesPattern(actualResponse, expected))
                return true;
        }
        return false;
    }

    private static bool MatchesPattern(string actual, string pattern)
    {
        // Handle wildcard matching
        if (pattern.Contains('*'))
        {
            // Convert pattern to regex
            string regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(actual, regexPattern);
        }

        // Exact match
        return actual.Trim().Equals(pattern.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Parser for test command files
/// </summary>
internal static class TestCommandParser
{
    /// <summary>
    /// Parses a test command file into TestCommand objects
    /// Format: COMMAND -> EXPECTED_RESPONSE | ALTERNATE_RESPONSE
    /// </summary>
    public static List<TestCommand> ParseTestFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Test file not found: {filePath}");

        List<TestCommand> tests = [];
        string[] lines = File.ReadAllLines(filePath);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            // Skip blank lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            // Parse format: COMMAND -> EXPECTED_RESPONSE
            string[] parts = line.Split(new[] { "->" }, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                Console.WriteLine($"Warning: Skipping invalid test line: {line}");
                continue;
            }

            string command = parts[0].Trim();
            string[] responses = [.. parts[1].Split('|', StringSplitOptions.TrimEntries).Select(r => r.Trim())];

            tests.Add(new TestCommand
            {
                Command = command,
                ExpectedResponses = responses
            });
        }

        return tests;
    }
}

/// <summary>
/// Result of a test execution
/// </summary>
internal class TestResult
{
    public string Command { get; set; } = string.Empty;
    public string ActualResponse { get; set; } = string.Empty;
    public string[] ExpectedResponses { get; set; } = Array.Empty<string>();
    public bool Passed { get; set; }
    public string? ErrorMessage { get; set; }
}
