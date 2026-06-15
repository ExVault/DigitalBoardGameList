namespace DigitalBoardGameList.App.Enrichment;

public static class Util
{
    public static string CleanDeveloperName(string name)
    {
        name = name.Replace(" llc", string.Empty, StringComparison.OrdinalIgnoreCase);
        name = name.Replace(" inc", string.Empty, StringComparison.OrdinalIgnoreCase);
        name = name.Replace(" gmbh", string.Empty, StringComparison.OrdinalIgnoreCase);
        name = name.Replace(" s.r.o", string.Empty, StringComparison.OrdinalIgnoreCase);
        name = name.TrimEnd(' ', ',', '.');
        return name;
    }

    public static string? CleanDlcName(string dlcName, string gameName)
    {
        if (dlcName.Contains("soundtrack", StringComparison.OrdinalIgnoreCase))
            return null;

        if (dlcName.EndsWith(" module", StringComparison.OrdinalIgnoreCase))
        {
            dlcName = dlcName[..^" module".Length];
        }

        char[] trimChars = [' ', ':', '-', '–'];

        // Remove game name from dlc name
        var namesTokens = gameName.Split(' ');
        for (var i = 0; i < namesTokens.Length; i++)
        {
            var nameToken = namesTokens[i].Trim(trimChars);

            if (nameToken.Length == 0)
                continue;

            if (dlcName.StartsWith(nameToken, StringComparison.OrdinalIgnoreCase))
            {
                dlcName = dlcName[nameToken.Length..].TrimStart(trimChars);
                // they usually line up, no need to restart
                //i = 0;
            }
        }
        return dlcName;
    }
}