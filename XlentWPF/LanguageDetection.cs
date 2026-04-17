namespace XlentWPF;

internal static class LanguageDetection
{
    public static string DetectSummaryLanguage(string text)
    {
        return ContainsSwedishCharacters(text) ? "sv-SE" : "en-US";
    }

    public static string DetectRedactionLanguage(IEnumerable<string> texts, string fallback = "sv")
    {
        var joined = string.Join(" ", texts.Where(t => !string.IsNullOrWhiteSpace(t)).Take(40));
        if (string.IsNullOrWhiteSpace(joined))
            return fallback;

        return ContainsSwedishCharacters(joined) ? "sv" : "en";
    }

    private static bool ContainsSwedishCharacters(string text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.Any(c => c is 'å' or 'ä' or 'ö' or 'Å' or 'Ä' or 'Ö');
    }
}
