// MainForm.cs — single-row borderless calculator window
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;

internal sealed class MainForm : Form
{
    // ── State ─────────────────────────────────────────────────────────────────
    private AppSettings  _s;
    private Evaluator    _ev;
    private readonly List<string> _history = new();
    private bool   _hasResult   = false;
    private string _lastResult  = "";
    private bool   _preferResultContinuation = false;
    private int    _savedSelectionStart = 0;
    private int    _savedSelectionLength = 0;
    private bool   _hasSavedSelection = false;
    private int    _inputPointerVersion = 0;
    private bool   _suppress    = false;   // suppress SelectedIndexChanged side-effects
    private bool   _initialHide;

    // ── Controls ──────────────────────────────────────────────────────────────
    private ComboBox          _cbInput  = null!;
    private Button            _btnClose = null!;
    private NotifyIcon        _tray     = null!;
    private ContextMenuStrip  _ctxMenu  = null!;
    private ToolStripMenuItem _miShow = null!, _miSettings = null!, _miExit = null!;
    private ToolTip           _tip    = null!;
    private System.Windows.Forms.Timer _numLockTimer = null!;

    // ── App icon ──────────────────────────────────────────────────────────────
    private static readonly Icon _appIcon = BuildIcon();

    // ── Keyboard hook (static = GC-safe) ─────────────────────────────────────
    private static NativeMethods.LowLevelKeyboardProc? _hookProc;
    private static IntPtr    _hookHandle = IntPtr.Zero;
    private static MainForm? _inst;

    // ═════════════════════════════════════════════════════════════════════════
    public MainForm()
    {
        _inst        = this;
        _s           = AppSettings.Load();
        Loc.Set(_s.Language);
        _initialHide = _s.StartMinimized;

        var udfs = UdfLoader.LoadFolder(AppContext.BaseDirectory);
        _ev = new Evaluator(udfs, _s);

        BuildContextMenu();
        InitUi();
        InitNumLockTimer();
        ApplySettings(firstRun: true);

        ApplyNumLockHookSettings();
        ApplyNumLockStateSettings();

        // Listen for second-instance activation signals
        StartShowEventListener();
    }

    private void StartShowEventListener()
    {
        var showEvent = new System.Threading.EventWaitHandle(
            false, System.Threading.EventResetMode.AutoReset,
            "NLCalc2-ShowWindow-8F3A1B2C");
        var thread = new System.Threading.Thread(() =>
        {
            while (true)
            {
                showEvent.WaitOne();
                try { BeginInvoke(ShowWindow); } catch { break; }
            }
        })
        { IsBackground = true };
        thread.Start();
    }

    private void InitNumLockTimer()
    {
        _numLockTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _numLockTimer.Tick += (_, _) =>
        {
            if (_s.KeepNumLockOn) RestoreNumLockIfNeeded();
        };
    }

    // ── Programmatic icon ─────────────────────────────────────────────────────
    private static Icon BuildIcon()
    {
        const int sz = 32;
        var bmp = new Bitmap(sz, sz, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        using var bg   = new SolidBrush(Color.FromArgb(0, 99, 177));
        using var path = new GraphicsPath();
        const int r = 6;
        path.AddArc(0,      0,      r*2, r*2, 180, 90);
        path.AddArc(sz-r*2, 0,      r*2, r*2, 270, 90);
        path.AddArc(sz-r*2, sz-r*2, r*2, r*2,   0, 90);
        path.AddArc(0,      sz-r*2, r*2, r*2,  90, 90);
        path.CloseFigure();
        g.FillPath(bg, path);

        using var f = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("=", f, Brushes.White, new RectangleF(0, 1, sz, sz), sf);

        GC.KeepAlive(bmp);
        return Icon.FromHandle(bmp.GetHicon());
    }

    // ── UI layout ─────────────────────────────────────────────────────────────
    private void InitUi()
    {
        const int M  = 8;   // margin on all sides
        const int BW = 32;  // close button width
        const int G  = 4;   // gap between combo and button
        const int W  = 420; // total client width

        Text             = Loc.App_Title;
        Icon             = _appIcon;
        FormBorderStyle  = FormBorderStyle.None;
        ShowInTaskbar    = true;
        ContextMenuStrip = _ctxMenu;
        _tip             = new ToolTip { ShowAlways = true };

        _cbInput = new ComboBox
        {
            Font             = new Font("Segoe UI", 11f),
            Left             = M,
            Top              = M,
            Width            = W - M - G - BW - M,
            DropDownStyle    = ComboBoxStyle.DropDown,
            MaxDropDownItems = 12,
            FlatStyle        = FlatStyle.Flat,
        };
        _cbInput.KeyDown             += CbInput_KeyDown;
        _cbInput.KeyPress            += CbInput_KeyPress;
        _cbInput.MouseDown           += CbInput_MouseDown;
        _cbInput.SelectedIndexChanged += CbInput_SelectedIndexChanged;
        Controls.Add(_cbInput);
        Deactivate += MainForm_Deactivate;
        Activated  += MainForm_Activated;

        int cbH = _cbInput.Height;  // auto-sized from font

        _btnClose = new Button
        {
            Text      = "x",
            Left      = W - M - BW,
            Top       = M,
            Width     = BW,
            Height    = cbH,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            TabStop   = false,
            Cursor    = Cursors.Hand,
        };
        _btnClose.FlatAppearance.BorderSize        = 0;
        _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28);
        _btnClose.Click += (_, _) => HideWindow();
        Controls.Add(_btnClose);

        ClientSize = new Size(W, M + cbH + M);
        MouseDown += OnDragMouseDown;

        _tray = new NotifyIcon
        {
            Icon             = _appIcon,
            ContextMenuStrip = _ctxMenu,
            Visible          = true,
        };
        _tray.DoubleClick += (_, _) => ToggleVisible();
    }

    private void BuildContextMenu()
    {
        _miShow     = new ToolStripMenuItem(Loc.Tray_Show,     null, (_, _) => ShowWindow());
        _miSettings = new ToolStripMenuItem(Loc.Tray_Settings, null, (_, _) => OpenSettings());
        _miExit     = new ToolStripMenuItem(Loc.Tray_Exit,     null, (_, _) => ExitApp());
        _ctxMenu    = new ContextMenuStrip();
        _ctxMenu.Items.AddRange(new ToolStripItem[] {
            _miShow, new ToolStripSeparator(),
            _miSettings, new ToolStripSeparator(),
            _miExit,
        });
    }

    // ── Settings ──────────────────────────────────────────────────────────────
    private void ApplySettings(bool firstRun = false)
    {
        _ev.ApplySettings(_s);
        TopMost = _s.AlwaysOnTop;
        Opacity = _s.Opacity / 100.0;
        UpdateColors();
        UpdateTip();
        _miShow.Text     = Loc.Tray_Show;
        _miSettings.Text = Loc.Tray_Settings;
        _miExit.Text     = Loc.Tray_Exit;

        if (firstRun)
        {
            StartPosition = _s.WindowLeft >= 0 && _s.WindowTop >= 0
                ? FormStartPosition.Manual : FormStartPosition.CenterScreen;
            if (_s.WindowLeft >= 0)
                Location = new Point(_s.WindowLeft, _s.WindowTop);
        }
    }

    private void UpdateTip()
    {
        var mode = _s.AngleMode switch
        {
            AngleModeEnum.Radians  => Loc.AngleMode_Rad,
            AngleModeEnum.Gradians => Loc.AngleMode_Grad,
            _                      => Loc.AngleMode_Deg,
        };
        _tip.SetToolTip(_cbInput, $"{Loc.Tip_Input} [{mode}]");
        _tray.Text = $"{Loc.App_Title} [{mode}]";
    }

    private void OpenSettings()
    {
        using var dlg = new SettingsForm(_s);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        dlg.SaveValues();
        Loc.Set(_s.Language);
        _s.Save();
        ApplyNumLockHookSettings();
        ApplyNumLockStateSettings();
        ApplySettings();
    }

    // ── Input handling ────────────────────────────────────────────────────────
    private void CbInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            EvaluateCurrent();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            ClearInput();
        }
        else if (_hasResult && IsExpressionEditKey(e))
        {
            // Backspace/Delete and clipboard edits do not necessarily produce a
            // printable KeyPress event, so enter expression-edit mode here.
            BeginEditingDisplayedExpression();
        }
        else if (_hasResult && IsExpressionNavigationKey(e))
        {
            // Moving the caret is an explicit request to edit the expression.
            _preferResultContinuation = false;
        }
    }

    // Operator continuation: typing +/-/*/etc after a result prepends the result.
    // Non-operator key after result: clear field and start fresh.
    private void CbInput_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!_hasResult || e.KeyChar < ' ') return;

        if (!_preferResultContinuation &&
            TryGetExpressionSelection(out var expr, out var selectionStart,
                                      out var selectionLength))
        {
            // The caret is in the expression, so this is an edit rather than a
            // request to continue from (or replace) the calculated result.
            var edited = expr.Remove(selectionStart, selectionLength)
                             .Insert(selectionStart, e.KeyChar.ToString());
            _hasResult = false;
            _lastResult = "";
            _preferResultContinuation = false;
            e.Handled = true;
            SetText(edited, selectionStart + 1);
            return;
        }

        _hasResult = false;  // consumed — do this first
        _preferResultContinuation = false;

        if (e.KeyChar is '+' or '-' or '*' or '/' or '%' or '^')
        {
            e.Handled = true;   // prevent char from being appended by the control
            SetText(_lastResult + e.KeyChar);
        }
        else
        {
            // Let the char through — but erase the result text first so the
            // user starts with just that char
            e.Handled = true;
            SetText(e.KeyChar.ToString());
        }
    }

    private static bool IsExpressionEditKey(KeyEventArgs e) =>
        e.KeyCode is Keys.Back or Keys.Delete ||
        (e.Control && (e.KeyCode == Keys.X || e.KeyCode == Keys.V)) ||
        (e.Shift && e.KeyCode == Keys.Insert);

    private static bool IsExpressionNavigationKey(KeyEventArgs e) =>
        e.KeyCode is Keys.Left or Keys.Right or Keys.Home or Keys.End;

    private bool BeginEditingDisplayedExpression()
    {
        if (!TryGetExpressionSelection(out var expr, out var selectionStart,
                                       out var selectionLength))
            return false;

        _hasResult = false;
        _lastResult = "";
        _preferResultContinuation = false;
        SetText(expr, selectionStart, selectionLength);
        return true;
    }

    private bool TryGetExpressionSelection(out string expr, out int selectionStart,
                                           out int selectionLength)
    {
        var text  = _cbInput.Text;
        var eqIdx = text.LastIndexOf(" = ");
        if (!_hasResult || eqIdx < 0 || _cbInput.SelectionStart > eqIdx)
        {
            expr = "";
            selectionStart = 0;
            selectionLength = 0;
            return false;
        }

        expr = text[..eqIdx].TrimEnd();
        selectionStart = Math.Min(_cbInput.SelectionStart, expr.Length);
        selectionLength = Math.Min(_cbInput.SelectionLength,
                                   expr.Length - selectionStart);
        return true;
    }

    // A click changes only the caret/selection. The result remains available for
    // copying and is removed later only if the expression is actually edited.
    private void CbInput_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _inputPointerVersion++;
    }

    private void MainForm_Deactivate(object? sender, EventArgs e)
    {
        _savedSelectionStart = _cbInput.SelectionStart;
        _savedSelectionLength = _cbInput.SelectionLength;
        _hasSavedSelection = true;
    }

    private void MainForm_Activated(object? sender, EventArgs e)
    {
        if (!_hasSavedSelection) return;

        var pointerVersion = _inputPointerVersion;
        BeginInvoke(() =>
        {
            if (IsDisposed || !_cbInput.Focused ||
                Control.MouseButtons != MouseButtons.None ||
                pointerVersion != _inputPointerVersion)
                return;

            var start = Math.Clamp(_savedSelectionStart, 0, _cbInput.Text.Length);
            _cbInput.SelectionStart = start;
            _cbInput.SelectionLength = Math.Clamp(
                _savedSelectionLength, 0, _cbInput.Text.Length - start);
        });
    }

    // Selecting a history result keeps result-continuation behavior.
    private void CbInput_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_suppress) return;
        if (_cbInput.SelectedIndex < 0) return;
        var item  = _cbInput.Items[_cbInput.SelectedIndex]?.ToString() ?? "";
        var eqIdx = item.LastIndexOf(" = ");
        if (eqIdx >= 0)
        {
            _lastResult = item[(eqIdx + 3)..].Trim();
            _hasResult = _lastResult.Length > 0;
            SetText(item);
            if (_hasResult) PreferResultContinuation();
        }
        else
        {
            _hasResult = false;
            _lastResult = "";
            _preferResultContinuation = false;
            SetText(item);
        }
    }

    // ── Evaluation ────────────────────────────────────────────────────────────
    private void EvaluateCurrent()
    {
        var text  = _cbInput.Text.Trim();
        var eqIdx = text.LastIndexOf(" = ");
        var expr  = eqIdx >= 0 ? text[..eqIdx].Trim() : text;
        if (expr == "") return;

        try
        {
            var result = _ev.Evaluate(expr);
            _lastResult = result;
            _hasResult  = true;
            var display = $"{expr} = {result}";
            AddToHistory(display);
            SetText(display);
            PreferResultContinuation();
        }
        catch (Exception ex)
        {
            _hasResult = false;
            _preferResultContinuation = false;
            SetText(ex.Message);
        }
    }

    // Set ComboBox text without triggering our SelectedIndexChanged logic.
    private void SetText(string text, int? selectionStart = null, int selectionLength = 0)
    {
        _suppress = true;
        try
        {
            var start = Math.Clamp(selectionStart ?? text.Length, 0, text.Length);
            _cbInput.Text            = text;
            _cbInput.SelectionStart  = start;
            _cbInput.SelectionLength = Math.Clamp(selectionLength, 0, text.Length - start);
        }
        finally { _suppress = false; }
    }

    private void PreferResultContinuation()
    {
        _preferResultContinuation = true;
        MoveCaretToEnd();

        // ComboBox activation/history selection can overwrite SelectionStart
        // after the current event. Normalize it once that native event finishes,
        // then let later deliberate caret placement select expression editing.
        BeginInvoke(() =>
        {
            if (IsDisposed || !_hasResult)
            {
                _preferResultContinuation = false;
                return;
            }

            MoveCaretToEnd();
            _preferResultContinuation = false;
        });
    }

    private void MoveCaretToEnd()
    {
        _cbInput.SelectionStart = _cbInput.Text.Length;
        _cbInput.SelectionLength = 0;
    }

    private void ClearInput()
    {
        _hasResult  = false;
        _lastResult = "";
        _preferResultContinuation = false;
        SetText("");
    }

    private void AddToHistory(string entry)
    {
        _history.Remove(entry);
        _history.Insert(0, entry);
        while (_history.Count > _s.HistoryMax)
            _history.RemoveAt(_history.Count - 1);

        _suppress = true;
        try
        {
            _cbInput.BeginUpdate();
            _cbInput.Items.Clear();
            foreach (var h in _history) _cbInput.Items.Add(h);
            _cbInput.EndUpdate();
        }
        finally { _suppress = false; }
    }

    // ── DWM: dark frame + rounded corners ─────────────────────────────────────
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int dark    = IsDark() ? 1 : 0;
        int corners = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(Handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, 4);
        NativeMethods.DwmSetWindowAttribute(Handle,
            NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref corners, 4);
    }

    private void UpdateColors()
    {
        bool dark = IsDark();
        if (dark)
        {
            BackColor           = Color.FromArgb(30, 30, 30);
            ForeColor           = Color.White;
            _cbInput.BackColor  = Color.FromArgb(48, 48, 48);
            _cbInput.ForeColor  = Color.White;
            _btnClose.BackColor = Color.FromArgb(30, 30, 30);
            _btnClose.ForeColor = Color.FromArgb(180, 180, 180);
        }
        else
        {
            BackColor           = SystemColors.Control;
            ForeColor           = SystemColors.ControlText;
            _cbInput.BackColor  = SystemColors.Window;
            _cbInput.ForeColor  = SystemColors.WindowText;
            _btnClose.BackColor = SystemColors.Control;
            _btnClose.ForeColor = SystemColors.ControlText;
        }
    }

    internal static bool IsDark()
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return k?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return false; }
    }

    // ── NumLock hook ──────────────────────────────────────────────────────────
    private void ApplyNumLockHookSettings()
    {
        if (_s.NumLockActivation || _s.KeepNumLockOn) InstallHook();
        else UninstallHook();
    }

    private void ApplyNumLockStateSettings()
    {
        _numLockTimer.Enabled = _s.KeepNumLockOn;
        if (_s.KeepNumLockOn) ScheduleNumLockRestore();
    }

    private void InstallHook()
    {
        if (_hookHandle != IntPtr.Zero) return;
        _hookProc   = HookCallback;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _hookProc,
            NativeMethods.GetModuleHandle(null), 0);
    }

    private void UninstallHook()
    {
        if (_hookHandle == IntPtr.Zero) return;
        NativeMethods.UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 &&
            (wParam.ToInt32() == NativeMethods.WM_KEYDOWN ||
             wParam.ToInt32() == NativeMethods.WM_KEYUP))
        {
            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            if (info.vkCode == NativeMethods.VK_NUMLOCK &&
                (info.flags & NativeMethods.LLKHF_INJECTED) == 0)
            {
                if (wParam.ToInt32() == NativeMethods.WM_KEYDOWN)
                    _inst?.BeginInvoke(_inst.OnNumLockPressed);
                else if (_inst?._s.KeepNumLockOn == true)
                    _inst.BeginInvoke(_inst.ScheduleNumLockRestore);
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void OnNumLockPressed()
    {
        if (_s.NumLockActivation) ToggleVisible();
        if (_s.KeepNumLockOn) ScheduleNumLockRestore();
    }

    private async void ScheduleNumLockRestore()
    {
        await Task.Delay(300);
        if (!IsDisposed && _s.KeepNumLockOn) RestoreNumLockIfNeeded();
    }

    private static void RestoreNumLockIfNeeded()
    {
        if ((GetKeyState((int)NativeMethods.VK_NUMLOCK) & 1) == 0)
        {
            NativeMethods.keybd_event((byte)NativeMethods.VK_NUMLOCK, 0x45, 0, 0);
            NativeMethods.keybd_event((byte)NativeMethods.VK_NUMLOCK, 0x45, NativeMethods.KEYEVENTF_KEYUP, 0);
        }
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    // ── Visibility ────────────────────────────────────────────────────────────
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_initialHide) { _initialHide = false; BeginInvoke(HideWindow); }
    }

    private void ToggleVisible()
    {
        if (Visible && WindowState != FormWindowState.Minimized) HideWindow();
        else ShowWindow();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        NativeMethods.SetForegroundWindow(Handle);
        Activate();
        ActiveControl = _cbInput;
        _cbInput.Select();
        _cbInput.Focus();
        if (_hasResult)
            PreferResultContinuation();
        if (_s.KeepNumLockOn) ScheduleNumLockRestore();
    }

    private void HideWindow() => Hide();

    private void ExitApp()
    {
        _s.WindowLeft = Left;
        _s.WindowTop  = Top;
        _s.Save();
        UninstallHook();
        _tray.Visible = false;
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideWindow();
            return;
        }
        base.OnFormClosing(e);
    }

    // ── Window drag (borderless) ──────────────────────────────────────────────
    private void OnDragMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN,
                new IntPtr(NativeMethods.HT_CAPTION), IntPtr.Zero);
        }
    }
}
