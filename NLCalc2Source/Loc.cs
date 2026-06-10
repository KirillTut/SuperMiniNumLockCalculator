// Loc.cs — English / Ukrainian localisation (English is default)
internal static class Loc
{
    private static string _lang = "en";

    public static string Language => _lang;

    public static void Set(string? lang)
    {
        _lang = lang?.ToLower() == "uk" ? "uk" : "en";
    }

    private static string T(string en, string uk) => _lang == "uk" ? uk : en;

    // ── Languages list ────────────────────────────────────────────────────────
    public static readonly (string Code, string Name)[] Languages =
        [("en", "English"), ("uk", "Українська")];

    public static int LanguageIndex(string lang) =>
        Math.Max(0, Array.FindIndex(Languages, l => l.Code == lang));

    // ── Settings dialog ───────────────────────────────────────────────────────
    public static string Settings_Title          => T("Settings — NumLock Calculator",    "Налаштування — NumLock Calculator");
    public static string Group_AngleUnits        => T("Angle units",                      "Одиниці кутів");
    public static string RB_Degrees              => T("Degrees",                          "Градуси");
    public static string RB_Radians              => T("Radians",                          "Радіани");
    public static string RB_Gradians             => T("Gradians",                         "Гради");
    public static string Group_NumberFmt         => T("Number format",                    "Формат числа");
    public static string Lbl_Format              => T("Format:",                          "Формат:");
    public static string[] Fmt_Items             => [T("Auto","Авто"), T("Fixed","Фіксований"), T("Scientific","Науковий")];
    public static string Lbl_Digits              => T("Digits:",                          "Знаків:");
    public static string Group_Window            => T("Window",                           "Вікно");
    public static string Chk_AlwaysOnTop         => T("Always on top",                   "Поверх інших вікон");
    public static string Lbl_Opacity             => T("Opacity (%):",                    "Прозорість (%):");
    public static string Group_Language          => T("Language",                         "Мова");
    public static string Group_NumLock           => T("NumLock key",                      "Клавіша NumLock");
    public static string Chk_NumLockActivation   => T("Show/hide window on NumLock",      "Показати/сховати по NumLock");
    public static string Chk_KeepNumLockOn       => T("Keep NumLock always on",           "Тримати NumLock увімкненим");
    public static string Group_History           => T("History",                          "Історія");
    public static string Lbl_HistMax             => T("Max entries:",                     "Максимум записів:");
    public static string Group_Startup           => T("Startup",                          "Запуск");
    public static string Chk_StartWithWindows    => T("Start with Windows",               "Запускати разом з Windows");
    public static string Chk_StartMinimized      => T("Start minimized to tray",          "Запускати згорнутим у трей");
    public static string Btn_Cancel              => T("Cancel",                           "Скасувати");
    public static string Lbl_LangNote            => T("(takes effect next time Settings is opened)",
                                                       "(набуває чинності наступного разу)");

    // ── Evaluator errors ──────────────────────────────────────────────────────
    public static string Err_NaN                  => T("Error",               "Помилка");
    public static string Err_ExpectedExpr         => T("Expected expression", "Очікується вираз");
    public static string Err_UnexpectedChar(char c) =>
        string.Format(T("Unexpected character: '{0}'", "Неочікуваний символ: '{0}'"), c);
    public static string Err_NotEnoughArgs(string fn) =>
        string.Format(T("{0}: not enough arguments", "{0}: недостатньо аргументів"), fn);
    public static string Err_UnknownVar(string v) =>
        string.Format(T("Unknown variable: {0}",  "Невідома змінна: {0}"), v);
    public static string Err_UnknownFunc(string fn) =>
        string.Format(T("Unknown function: {0}",  "Невідома функція: {0}"), fn);

    // ── Main form ─────────────────────────────────────────────────────────────
    public static string App_Title     => "NumLock Calculator";
    public static string Tray_Show     => T("Show",         "Показати");
    public static string Tray_Settings => T("Settings...",  "Налаштування...");
    public static string Tray_Exit     => T("Exit",         "Вихід");
    public static string AngleMode_Deg  => T("DEG", "ГРД");
    public static string AngleMode_Rad  => T("RAD", "РАД");
    public static string AngleMode_Grad => T("GRD", "ГРА");
    public static string Tip_Input     => T("Enter expression and press Enter",
                                            "Введіть вираз і натисніть Enter");
}
