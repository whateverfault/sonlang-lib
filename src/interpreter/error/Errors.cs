using sonlanglib.shared.localization;

namespace sonlanglib.interpreter.error;

public enum Error {
    SmthWentWrong,
    NotInitialized,
    InterpreterNotInitialized,
    UnknownIdentifier,
    IllegalOperation,
    InvalidSyntax,
}

internal static class Errors {
    private static readonly Localization[] _errors = [
                                                         new Localization(
                                                                          new Dictionary<Language, string> {
                                                                                  { Language.English, "Something Went Wrong."},
                                                                                  { Language.Russian, "Что-то пошло не так."},
                                                                              }
                                                                         ),
                                                         new Localization(
                                                                          new Dictionary<Language, string> {
                                                                                  { Language.English, "Not Initialized."},
                                                                                  { Language.Russian, "Не инициализировано."},
                                                                              }
                                                                         ),
                                                         new Localization(
                                                                          new Dictionary<Language, string> {
                                                                              { Language.English, "Interpreter is not Initialized."},
                                                                              { Language.Russian, "Интерпретатор не инициализирован."},
                                                                          }
                                                                         ),
                                                         new Localization(
                                                                          new Dictionary<Language, string> {
                                                                                  { Language.English, "Unknown Identifier."},
                                                                                  { Language.Russian, "Идентификатор не определен."},
                                                                              }
                                                                         ),
                                                         new Localization(
                                                                          new Dictionary<Language, string> {
                                                                                  { Language.English, "Illegal Operation"},
                                                                                  { Language.Russian, "Неопределенная операция."},
                                                                              }
                                                                         ),
                                                         new Localization(
                                                                          new Dictionary<Language, string> {
                                                                                  { Language.English, "Invalid Synstax."},
                                                                                  { Language.Russian, "Неверный синтаксис."},
                                                                              }
                                                                         ),
                                                     ];
    
    
    public static string? GetErrorString(Error? error, Language lang = Language.English) {
        if (error == null) return null;
        
        var localization = _errors[(int)error];
        return localization.GetString(lang);
    } 
}