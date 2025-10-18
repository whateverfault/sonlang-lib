using sonlanglib.shared.localization;

namespace sonlanglib.interpreter.error;

public enum Error {
    SmthWentWrong,
    NotInitialized,
    InterpreterNotInitialized,
    UnknownIdentifier,
    IllegalOperation,
    InvalidSyntax,
    OutOfBounds,
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
                                                                                  { Language.English, "Invalid Syntax."},
                                                                                  { Language.Russian, "Неверный синтаксис."},
                                                                              }
                                                                         ),
                                                         new Localization(
                                                                          new Dictionary<Language, string> {
                                                                              { Language.English, "Out of Bounds."},
                                                                              { Language.Russian, "За границами."},
                                                                          }
                                                                         ),
                                                     ];
    
    
    public static string GetErrorString(Error? error, Language lang = Language.English) {
        if (error == null) return string.Empty;
        
        var localization = _errors[(int)error];
        return localization.GetString(lang);
    } 
}