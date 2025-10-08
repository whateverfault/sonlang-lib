namespace sonlanglib.shared.localization;

public enum Language {
    Russian,
    English,
}

public class Localization {
    private readonly Dictionary<Language, string> _localizations;


    public Localization(Dictionary<Language, string> localizations) {
        _localizations = localizations;
    }

    public string? GetString(Language language) {
        return _localizations.GetValueOrDefault(language);
    }
}