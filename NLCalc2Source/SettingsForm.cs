// SettingsForm.cs — settings dialog, localised via Loc
internal sealed class SettingsForm : Form
{
    private readonly AppSettings _s;

    private RadioButton   _rbDeg = null!, _rbRad = null!, _rbGrad = null!;
    private ComboBox      _cbFmt  = null!, _cbLang = null!;
    private NumericUpDown _nudDec = null!, _nudOpacity = null!, _nudHistMax = null!;
    private CheckBox      _chkTop = null!, _chkNumLock = null!, _chkKeepOn = null!,
                          _chkStartWin = null!, _chkStartMin = null!;

    public SettingsForm(AppSettings s)
    {
        _s = s;
        InitUi();
        LoadValues();
    }

    // ── Build UI ──────────────────────────────────────────────────────────────
    private void InitUi()
    {
        Text            = Loc.Settings_Title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        Font            = new Font("Segoe UI", 9.5f);
        BackColor       = SystemColors.Window;
        ForeColor       = SystemColors.WindowText;

        int y = 10;

        // ── Angle units ─────────────────────────────────────────────────────
        var grpAng = Group(Loc.Group_AngleUnits, 8, y, 318, 58); y += 64;
        _rbDeg  = Radio(Loc.RB_Degrees,  grpAng,  8, 22);
        _rbRad  = Radio(Loc.RB_Radians,  grpAng, 112, 22);
        _rbGrad = Radio(Loc.RB_Gradians, grpAng, 218, 22);

        // ── Number format ───────────────────────────────────────────────────
        var grpFmt = Group(Loc.Group_NumberFmt, 8, y, 318, 58); y += 64;
        Lbl(Loc.Lbl_Format, grpFmt, 8, 24);
        _cbFmt = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
            Left = 72, Top = 20, Width = 130 };
        _cbFmt.Items.AddRange(Loc.Fmt_Items);
        grpFmt.Controls.Add(_cbFmt);
        Lbl(Loc.Lbl_Digits, grpFmt, 210, 24);
        _nudDec = Nud(grpFmt, 268, 20, 50, 0, 15);

        // ── Window ──────────────────────────────────────────────────────────
        var grpWin = Group(Loc.Group_Window, 8, y, 318, 80); y += 86;
        _chkTop = Check(Loc.Chk_AlwaysOnTop, grpWin, 8, 22);
        Lbl(Loc.Lbl_Opacity, grpWin, 8, 52);
        _nudOpacity = Nud(grpWin, 160, 48, 58, 20, 100);

        // ── Language ────────────────────────────────────────────────────────
        var grpLang = Group(Loc.Group_Language, 8, y, 318, 54); y += 60;
        _cbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
            Left = 8, Top = 20, Width = 180 };
        foreach (var (_, name) in Loc.Languages) _cbLang.Items.Add(name);
        grpLang.Controls.Add(_cbLang);
        Lbl(Loc.Lbl_LangNote, grpLang, 196, 26, italic: true);

        // ── NumLock key ─────────────────────────────────────────────────────
        var grpNL = Group(Loc.Group_NumLock, 8, y, 318, 68); y += 74;
        _chkNumLock = Check(Loc.Chk_NumLockActivation, grpNL, 8, 20);
        _chkKeepOn  = Check(Loc.Chk_KeepNumLockOn,     grpNL, 8, 44);

        // ── History ─────────────────────────────────────────────────────────
        var grpHist = Group(Loc.Group_History, 8, y, 318, 46); y += 52;
        Lbl(Loc.Lbl_HistMax, grpHist, 8, 18);
        _nudHistMax = Nud(grpHist, 154, 14, 65, 1, 500);

        // ── Startup ─────────────────────────────────────────────────────────
        var grpStart = Group(Loc.Group_Startup, 8, y, 318, 66); y += 72;
        _chkStartWin = Check(Loc.Chk_StartWithWindows, grpStart, 8, 20);
        _chkStartMin = Check(Loc.Chk_StartMinimized,   grpStart, 8, 44);

        // ── OK / Cancel ──────────────────────────────────────────────────────
        var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK,
            Left = 148, Top = y, Width = 80, Height = 28 };
        var btnCancel = new Button { Text = Loc.Btn_Cancel, DialogResult = DialogResult.Cancel,
            Left = 240, Top = y, Width = 80, Height = 28 };
        AcceptButton = btnOk;
        CancelButton = btnCancel;
        Controls.AddRange([btnOk, btnCancel]);
        ClientSize = new Size(336, y + 40);
    }

    // ── Load / save values ────────────────────────────────────────────────────
    private void LoadValues()
    {
        _rbDeg.Checked   = _s.AngleMode == AngleModeEnum.Degrees;
        _rbRad.Checked   = _s.AngleMode == AngleModeEnum.Radians;
        _rbGrad.Checked  = _s.AngleMode == AngleModeEnum.Gradians;
        _cbFmt.SelectedIndex  = (int)_s.NumberFormat;
        _nudDec.Value         = _s.DecimalPlaces;
        _chkTop.Checked       = _s.AlwaysOnTop;
        _nudOpacity.Value     = _s.Opacity;
        _cbLang.SelectedIndex = Loc.LanguageIndex(_s.Language);
        _chkNumLock.Checked   = _s.NumLockActivation;
        _chkKeepOn.Checked    = _s.KeepNumLockOn;
        _nudHistMax.Value     = _s.HistoryMax;
        _chkStartWin.Checked  = _s.StartWithWindows;
        _chkStartMin.Checked  = _s.StartMinimized;
    }

    public void SaveValues()
    {
        _s.AngleMode       = _rbRad.Checked  ? AngleModeEnum.Radians
                           : _rbGrad.Checked ? AngleModeEnum.Gradians
                                             : AngleModeEnum.Degrees;
        _s.NumberFormat    = (NumberFmtEnum)_cbFmt.SelectedIndex;
        _s.DecimalPlaces   = (int)_nudDec.Value;
        _s.AlwaysOnTop     = _chkTop.Checked;
        _s.Opacity         = (int)_nudOpacity.Value;

        var li = _cbLang.SelectedIndex;
        if (li >= 0 && li < Loc.Languages.Length)
            _s.Language = Loc.Languages[li].Code;

        _s.NumLockActivation = _chkNumLock.Checked;
        _s.KeepNumLockOn     = _chkKeepOn.Checked;
        _s.HistoryMax        = (int)_nudHistMax.Value;
        _s.StartWithWindows  = _chkStartWin.Checked;
        _s.StartMinimized    = _chkStartMin.Checked;
    }

    // ── DWM dark/rounded ──────────────────────────────────────────────────────
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int dark = MainForm.IsDark() ? 1 : 0;
        int corners = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(Handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4);
        NativeMethods.DwmSetWindowAttribute(Handle,
            NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, 4);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private GroupBox Group(string text, int x, int y, int w, int h)
    {
        var g = new GroupBox { Text = text, Left = x, Top = y, Width = w, Height = h };
        Controls.Add(g);
        return g;
    }

    private static RadioButton Radio(string text, Control parent, int x, int y)
    {
        var r = new RadioButton { Text = text, Left = x, Top = y, AutoSize = true };
        parent.Controls.Add(r);
        return r;
    }

    private static CheckBox Check(string text, Control parent, int x, int y)
    {
        var c = new CheckBox { Text = text, Left = x, Top = y, AutoSize = true };
        parent.Controls.Add(c);
        return c;
    }

    private static void Lbl(string text, Control parent, int x, int y, bool italic = false)
    {
        var lbl = new Label { Text = text, Left = x, Top = y, AutoSize = true };
        if (italic) lbl.Font = new Font(lbl.Font, FontStyle.Italic);
        parent.Controls.Add(lbl);
    }

    private static NumericUpDown Nud(Control parent, int x, int y, int w, int min, int max)
    {
        var n = new NumericUpDown { Left = x, Top = y, Width = w, Minimum = min, Maximum = max };
        parent.Controls.Add(n);
        return n;
    }
}

