namespace DigitalBoardGameList.App;

public class GithubActionLog
{
    private static readonly bool IsGitHubAction;

    static GithubActionLog()
    {
        IsGitHubAction = string.Equals(
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    public static void Warning(string msg)
    {
        if (!IsGitHubAction)
            return;

        Console.WriteLine($"::warning::{Escape(msg)}");
    }

    public static void Error(string msg)
    {
        if (!IsGitHubAction)
            return;

        Console.WriteLine($"::error::{Escape(msg)}");
    }

    private static string Escape(string value)
    {
        return value
            .Replace("%", "%25")
            .Replace("\r", "%0D")
            .Replace("\n", "%0A");
    }
}