// AppSettings.cs — INI-file settings, no registry (except optional Start-with-Windows)
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal enum AngleModeEnum   { Degrees, Radians, Gradians }
internal enum NumberFmtEnum   { Auto, Fixed, Scientific }

internal class AppSettings
{
    // ── Defaults ──────────────────────────────────────────────────────────────
    public AngleModeEnum AngleMode       = AngleModeEnum.Degrees;
    public NumberFmtEnum NumberFormat    = NumberFmtEnum.Auto;
    public int           DecimalPlaces   = 10;
    public bool          AlwaysOnTop     = false;
    public int           Opacity         = 100;          // 20–100 %
    public bool          NumLockActivation = true;       // show/hide on NumLock key
    public bool          KeepNumLockOn   = true;         // prevent NumLock LED from toggling
    public bool          StartWithWindows = false;
    public bool          StartMinimized  = false;
    public int           HistoryMax      = 50;
    public int           WindowLeft      = -1;           // -1 = centre on first run
    public int           WindowTop       = -1;
    public string        LastExpression  = "";
    public string        Language        = "en";      // "en" | "uk"

    // ── INI file path = same folder as the EXE ────────────────────────────────
    private static string IniPath => Path.Combine(
        AppContext.BaseDirectory, "nlcalc.ini");

    // ── Load ──────────────────────────────────────────────────────────────────
    public static AppSettings Load()
    {
        var s = new AppSettings();
        if (!File.Exists(IniPath)) return s;

        var kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadAllLines(IniPath))
        {
            var line = raw.Trim();
            if (line.StartsWith(';') || line.StartsWith('#') || line.StartsWith('[')) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            kv[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }

        s.AngleMode        = Enum.TryParse<AngleModeEnum>(Get(kv,"AngleMode"),    true, out var am) ? am  : s.AngleMode;
        s.NumberFormat     = Enum.TryParse<NumberFmtEnum>(Get(kv,"NumberFormat"), true, out var nf) ? nf  : s.NumberFormat;
        s.DecimalPlaces    = Int(kv, "DecimalPlaces",    s.DecimalPlaces,    0,  15);
        s.AlwaysOnTop      = Bool(kv, "AlwaysOnTop",     s.AlwaysOnTop);
        s.Opacity          = Int(kv, "Opacity",          s.Opacity,          20, 100);
        s.NumLockActivation = Bool(kv, "NumLockActivation", s.NumLockActivation);
        s.KeepNumLockOn    = Bool(kv, "KeepNumLockOn",   s.KeepNumLockOn);
        s.StartWithWindows = Bool(kv, "StartWithWindows",s.StartWithWindows);
        s.StartMinimized   = Bool(kv, "StartMinimized",  s.StartMinimized);
        s.HistoryMax       = Int(kv, "HistoryMax",       s.HistoryMax,       1,  500);
        s.WindowLeft       = Int(kv, "WindowLeft",       s.WindowLeft,       -1, 9999);
        s.WindowTop        = Int(kv, "WindowTop",        s.WindowTop,        -1, 9999);
        s.LastExpression   = Get(kv, "LastExpression");
        var lang           = Get(kv, "Language");
        s.Language         = lang == "uk" ? "uk" : "en";
        return s;
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    public void Save()
    {
        var lines = new[]
        {
            "; NumLock Calculator settings",
            "[Settings]",
            $"AngleMode={AngleMode}",
            $"NumberFormat={NumberFormat}",
            $"DecimalPlaces={DecimalPlaces}",
            $"AlwaysOnTop={AlwaysOnTop}",
            $"Opacity={Opacity}",
            $"NumLockActivation={NumLockActivation}",
            $"KeepNumLockOn={KeepNumLockOn}",
            $"StartWithWindows={StartWithWindows}",
            $"StartMinimized={StartMinimized}",
            $"HistoryMax={HistoryMax}",
            $"WindowLeft={WindowLeft}",
            $"WindowTop={WindowTop}",
            $"LastExpression={LastExpression}",
            $"Language={Language}",
        };
        File.WriteAllLines(IniPath, lines);

        ApplyStartWithWindows();
    }

    // ── Start-with-Windows via Run key ────────────────────────────────────────
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunVal = "NumLockCalculator";

    private void ApplyStartWithWindows()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        if (StartWithWindows)
            key.SetValue(RunVal, $"\"{AppContext.BaseDirectory}NLCalc2.exe\"");
        else
            key.DeleteValue(RunVal, throwOnMissingValue: false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string Get(Dictionary<string,string> d, string k) =>
        d.TryGetValue(k, out var v) ? v : "";

    private static int Int(Dictionary<string,string> d, string k, int def, int min, int max)
    {
        if (d.TryGetValue(k, out var v) && int.TryParse(v, out var n))
            return Math.Clamp(n, min, max);
        return def;
    }

    private static bool Bool(Dictionary<string,string> d, string k, bool def)
    {
        if (!d.TryGetValue(k, out var v)) return def;
        return v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }
}
