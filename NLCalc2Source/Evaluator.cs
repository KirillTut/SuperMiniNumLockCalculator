// Evaluator.cs — expression parser + UDF loader
using System.Text;
using System.Text.RegularExpressions;

// ── User-defined function ─────────────────────────────────────────────────────
internal sealed class UserFunc
{
    public string   Name;
    public string[] Params;
    public string   Body;

    public UserFunc(string name, string[] parms, string body)
    { Name = name; Params = parms; Body = body; }
}

// ── UDF file loader ────────────────────────────────────────────────────────────
internal static class UdfLoader
{
    public static List<UserFunc> LoadFolder(string dir)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc  = Encoding.GetEncoding(1251);
        var list = new List<UserFunc>();

        if (!Directory.Exists(dir)) return list;

        foreach (var file in Directory.GetFiles(dir, "*.udf"))
        {
            var lines = File.ReadAllLines(file, enc);
            UserFunc? cur = null;
            var body = new StringBuilder();

            void Flush()
            {
                if (cur == null) return;
                var b = body.ToString().Trim();
                if (!b.Equals("=internal", StringComparison.OrdinalIgnoreCase))
                    list.Add(new UserFunc(cur.Name, cur.Params, b));
                cur = null; body.Clear();
            }

            foreach (var raw in lines)
            {
                var ln = raw.Trim();
                if (ln.StartsWith("//") || ln == "") continue;
                if (ln.StartsWith("[") && ln.EndsWith("]"))
                {
                    Flush();
                    var sig = ln[1..^1];
                    var pi  = sig.IndexOf('(');
                    if (pi < 0) { cur = new UserFunc(sig, [], ""); body.Clear(); }
                    else
                    {
                        var nm  = sig[..pi].Trim();
                        var par = sig[(pi + 1)..sig.LastIndexOf(')')].Trim();
                        var ps  = par == "" ? [] : par.Split(',').Select(p => p.Trim()).ToArray();
                        cur = new UserFunc(nm, ps, "");
                        body.Clear();
                    }
                }
                else if (cur != null)
                {
                    if (body.Length > 0) body.Append('\n');
                    body.Append(ln);
                }
            }
            Flush();
        }
        return list;
    }
}

// ── Expression evaluator ───────────────────────────────────────────────────────
internal sealed class Evaluator
{
    private readonly List<UserFunc>      _udfs;
    private readonly Dictionary<string,double> _vars = new(StringComparer.OrdinalIgnoreCase);
    private AngleModeEnum                _angleMode;
    private NumberFmtEnum                _numFmt;
    private int                          _decPlaces;

    public Evaluator(List<UserFunc> udfs, AppSettings s)
    {
        _udfs       = udfs;
        _angleMode  = s.AngleMode;
        _numFmt     = s.NumberFormat;
        _decPlaces  = s.DecimalPlaces;
        _vars["pi"] = Math.PI;
        _vars["e"]  = Math.E;
    }

    public void ApplySettings(AppSettings s)
    {
        _angleMode = s.AngleMode;
        _numFmt    = s.NumberFormat;
        _decPlaces = s.DecimalPlaces;
    }

    // ── Public entry point ────────────────────────────────────────────────────
    public string Evaluate(string expr)
    {
        // Allow "x = expr" for variable assignment
        var m = Regex.Match(expr.Trim(), @"^([A-Za-z_]\w*)\s*=\s*(.+)$");
        if (m.Success)
        {
            var varName = m.Groups[1].Value.ToLower();
            var val     = ParseExpr(m.Groups[2].Value.Trim());
            _vars[varName] = val;
            return Format(val);
        }
        return Format(ParseExpr(expr));
    }

    // ── Formatting ────────────────────────────────────────────────────────────
    private string Format(double v)
    {
        if (double.IsNaN(v))      return Loc.Err_NaN;
        if (double.IsInfinity(v)) return v > 0 ? "∞" : "-∞";
        return _numFmt switch
        {
            NumberFmtEnum.Fixed      => v.ToString("F" + _decPlaces),
            NumberFmtEnum.Scientific => v.ToString("E" + _decPlaces),
            _ => v == Math.Floor(v) && Math.Abs(v) < 1e15
                    ? ((long)v).ToString()
                    : v.ToString("G" + Math.Max(6, _decPlaces)),
        };
    }

    // ── Angle helpers ─────────────────────────────────────────────────────────
    private double ToRad(double x) => _angleMode switch
    {
        AngleModeEnum.Degrees  => x * Math.PI / 180.0,
        AngleModeEnum.Gradians => x * Math.PI / 200.0,
        _                      => x,
    };
    private double FromRad(double x) => _angleMode switch
    {
        AngleModeEnum.Degrees  => x * 180.0 / Math.PI,
        AngleModeEnum.Gradians => x * 200.0 / Math.PI,
        _                      => x,
    };

    // ══════════════════════════════════════════════════════════════════════════
    // Recursive-descent parser
    // ══════════════════════════════════════════════════════════════════════════
    private string   _src = "";
    private int      _pos;

    private double ParseExpr(string src)
    {
        _src = src.Replace(',', '.').Trim(); // accept comma as decimal sep
        _pos = 0;
        var v = ParseAddSub();
        SkipWs();
        if (_pos < _src.Length)
            throw new Exception(Loc.Err_UnexpectedChar(_src[_pos]));
        return v;
    }

    private void SkipWs() { while (_pos < _src.Length && _src[_pos] == ' ') _pos++; }

    private double ParseAddSub()
    {
        var v = ParseMulDiv();
        SkipWs();
        while (_pos < _src.Length && (_src[_pos] == '+' || _src[_pos] == '-'))
        {
            var op = _src[_pos++];
            var r  = ParseMulDiv();
            v = op == '+' ? v + r : v - r;
            SkipWs();
        }
        return v;
    }

    private double ParseMulDiv()
    {
        var v = ParsePow();
        SkipWs();
        while (_pos < _src.Length && (_src[_pos] == '*' || _src[_pos] == '/' || _src[_pos] == '%'))
        {
            var op = _src[_pos++];
            var r  = ParsePow();
            v = op switch { '*' => v * r, '/' => v / r, _ => v % r };
            SkipWs();
        }
        return v;
    }

    private double ParsePow()
    {
        var v = ParseUnary();
        SkipWs();
        if (_pos < _src.Length && _src[_pos] == '^')
        {
            _pos++;
            var r = ParseUnary();
            v = Math.Pow(v, r);
        }
        return v;
    }

    private double ParseUnary()
    {
        SkipWs();
        if (_pos < _src.Length && _src[_pos] == '-') { _pos++; return -ParsePrimary(); }
        if (_pos < _src.Length && _src[_pos] == '+') { _pos++; return  ParsePrimary(); }
        return ParsePrimary();
    }

    private double ParsePrimary()
    {
        SkipWs();
        if (_pos >= _src.Length) throw new Exception(Loc.Err_ExpectedExpr);

        // parentheses
        if (_src[_pos] == '(')
        {
            _pos++;
            var v = ParseAddSub();
            SkipWs();
            if (_pos < _src.Length && _src[_pos] == ')') _pos++;
            return v;
        }

        // number literal
        if (char.IsDigit(_src[_pos]) || (_src[_pos] == '.' && _pos + 1 < _src.Length && char.IsDigit(_src[_pos + 1])))
            return ParseNumber();

        // identifier (function call or variable)
        if (char.IsLetter(_src[_pos]) || _src[_pos] == '_')
            return ParseIdentifier();

        throw new Exception(Loc.Err_UnexpectedChar(_src[_pos]));
    }

    private double ParseNumber()
    {
        int start = _pos;
        while (_pos < _src.Length && (char.IsDigit(_src[_pos]) || _src[_pos] == '.')) _pos++;
        if (_pos < _src.Length && (_src[_pos] == 'e' || _src[_pos] == 'E'))
        {
            _pos++;
            if (_pos < _src.Length && (_src[_pos] == '+' || _src[_pos] == '-')) _pos++;
            while (_pos < _src.Length && char.IsDigit(_src[_pos])) _pos++;
        }
        return double.Parse(_src[start.._pos], System.Globalization.CultureInfo.InvariantCulture);
    }

    private double ParseIdentifier()
    {
        int start = _pos;
        while (_pos < _src.Length && (char.IsLetterOrDigit(_src[_pos]) || _src[_pos] == '_')) _pos++;
        var name = _src[start.._pos];
        SkipWs();

        // function call
        if (_pos < _src.Length && _src[_pos] == '(')
        {
            _pos++;
            var args = new List<double>();
            SkipWs();
            if (_pos < _src.Length && _src[_pos] != ')')
            {
                args.Add(ParseAddSub());
                SkipWs();
                while (_pos < _src.Length && _src[_pos] == ',')
                {
                    _pos++; args.Add(ParseAddSub()); SkipWs();
                }
            }
            if (_pos < _src.Length && _src[_pos] == ')') _pos++;
            return CallFunc(name, args);
        }

        // variable
        if (_vars.TryGetValue(name, out var vv)) return vv;
        throw new Exception(Loc.Err_UnknownVar(name));
    }

    // ── Built-ins + UDF dispatcher ────────────────────────────────────────────
    private double CallFunc(string name, List<double> a)
    {
        double Arg(int i) => i < a.Count ? a[i] : throw new Exception(Loc.Err_NotEnoughArgs(name));

        var result = name.ToLower() switch
        {
            "abs"   => Math.Abs(Arg(0)),
            "sqrt"  => Math.Sqrt(Arg(0)),
            "sqr"   => Arg(0) * Arg(0),
            "exp"   => Math.Exp(Arg(0)),
            "ln"    => Math.Log(Arg(0)),
            "log"   => a.Count >= 2 ? Math.Log(Arg(0), Arg(1)) : Math.Log10(Arg(0)),
            "log2"  => Math.Log2(Arg(0)),
            "log10" => Math.Log10(Arg(0)),
            "sign"  => (double)Math.Sign(Arg(0)),
            "floor" => Math.Floor(Arg(0)),
            "ceil"  => Math.Ceiling(Arg(0)),
            "round" => a.Count >= 2 ? Math.Round(Arg(0), (int)Arg(1)) : Math.Round(Arg(0)),
            "trunc" => Math.Truncate(Arg(0)),
            "min"   => a.Count >= 2 ? Math.Min(Arg(0), Arg(1)) : Arg(0),
            "max"   => a.Count >= 2 ? Math.Max(Arg(0), Arg(1)) : Arg(0),
            "mod"   => Arg(0) % Arg(1),
            "pow"   => Math.Pow(Arg(0), Arg(1)),
            "fact"  => Factorial((int)Arg(0)),
            "frac"  => Arg(0) - Math.Truncate(Arg(0)),
            "int"   => Math.Truncate(Arg(0)),
            "pi"    => Math.PI,
            "e"     => Math.E,
            // trig (angle-mode aware)
            "sin"   => Math.Sin(ToRad(Arg(0))),
            "cos"   => Math.Cos(ToRad(Arg(0))),
            "tan"   => Math.Tan(ToRad(Arg(0))),
            "asin"  => FromRad(Math.Asin(Arg(0))),
            "acos"  => FromRad(Math.Acos(Arg(0))),
            "atan"  => FromRad(Math.Atan(Arg(0))),
            "atan2" => FromRad(Math.Atan2(Arg(0), Arg(1))),
            "sinh"  => Math.Sinh(Arg(0)),
            "cosh"  => Math.Cosh(Arg(0)),
            "tanh"  => Math.Tanh(Arg(0)),
            "deg"   => Arg(0) * 180.0 / Math.PI,
            "rad"   => Arg(0) * Math.PI / 180.0,
            _       => double.NaN,
        };

        if (!double.IsNaN(result)) return result;

        // Try UDF
        var udf = _udfs.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (udf == null) throw new Exception(Loc.Err_UnknownFunc(name));

        // Bind params
        var savedVars = new Dictionary<string, double>(_vars);
        for (int i = 0; i < udf.Params.Length; i++)
            _vars[udf.Params[i].ToLower()] = i < a.Count ? a[i] : 0;

        var res = ParseExpr(udf.Body);
        // Restore outer vars
        _vars.Clear();
        foreach (var kv in savedVars) _vars[kv.Key] = kv.Value;
        return res;
    }

    private static double Factorial(int n)
    {
        if (n < 0)  return double.NaN;
        if (n > 20) return double.PositiveInfinity;
        double r = 1;
        for (int i = 2; i <= n; i++) r *= i;
        return r;
    }
}
