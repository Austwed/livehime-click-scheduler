using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LivehimeClickScheduler;

public sealed class MainForm : Form
{
    private const string ApplicationVersion = "v55";
    private const string ShutdownTaskName = "B站激励抢码工具_自动关机";
    private static readonly Color Void = Color.FromArgb(255, 253, 248);
    private static readonly Color Nebula = Color.FromArgb(224, 247, 253);
    private static readonly Color Card = Color.White;
    private static readonly Color CardHover = Color.FromArgb(225, 246, 253);
    private static readonly Color StarJade = Color.FromArgb(20, 139, 210);
    private static readonly Color StarJadeDark = Color.FromArgb(4, 82, 166);
    private static readonly Color Starlight = Color.FromArgb(18, 57, 112);
    private static readonly Color MutedStarlight = Color.FromArgb(71, 112, 151);
    private const string LivehimeShortcut = @"C:\Users\Public\Desktop\哔哩哔哩直播姬.lnk";
    private const string GameShortcut = @"C:\Users\22320\Desktop\崩坏：星穹铁道.lnk";
    private const int HotkeyStartPoint = 101, HotkeyRecord = 103;
    private const string SharedCssSelector = "#app > div > div.home-wrap.select-disable > section.tool-wrap > div";
    private readonly TextBox _delaySeconds = new() { Width = 80, Text = "0" };
    private readonly CheckBox _useSystemTime = new() { Text = "按指定系统时间开播", AutoSize = true };
    private readonly TextBox _startTime = new() { Width = 90, Text = "23:50:00", Enabled = false };
    private readonly TextBox _liveSeconds = new() { Width = 80, Text = "4380" };
    private readonly TextBox _livehimePath = new() { Width = 520, Text = LivehimeShortcut };
    private readonly TextBox _gamePath = new() { Width = 520, Text = GameShortcut };
    private readonly Label _startPoint = new() { AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, MaximumSize = new Size(650, 0) };
    private readonly Button _runButton = new() { Text = "开始执行", AutoSize = true };
    private readonly Button _recordButton = new() { Text = "开始录制（F10）", AutoSize = true };
    private readonly ComboBox _applicationSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly TextBox _applicationName = new() { Width = 110 };
    private readonly TextBox _applicationPath = new() { Width = 360 };
    private readonly Button _launchApplicationButton = new() { Text = "打开当前应用", AutoSize = true };
    private readonly Button _clearMacroButton = new() { Text = "清空当前录制", AutoSize = true };
    private readonly Button _cancelButton = new() { Text = "取消全部预约", AutoSize = true, Enabled = false };
    private readonly CheckBox _testGameButton = new() { Text = "测试游戏录制", AutoSize = true };
    private readonly CheckBox _testBrowserButton = new() { Text = "测试浏览器录制", AutoSize = true };
    private readonly Button _webConsoleRunButton = new() { Text = "预约抢码", AutoSize = true };
    private readonly Button _webConsoleCancelButton = new() { Text = "取消全部预约", AutoSize = true, Enabled = false };
    private readonly Button _webConsoleTestButton = new() { Text = "抢码测试", AutoSize = true };
    private readonly Button _addWebPageButton = new() { Text = "添加网页", AutoSize = true };
    private readonly Button _removeWebPageButton = new() { Text = "删除选中网页", AutoSize = true };
    private readonly CheckBox _autoShutdownEnabled = new() { Text = "自动关机", AutoSize = true };
    private readonly TextBox _shutdownHour = new() { Width = 42, Text = "01" };
    private readonly TextBox _shutdownMinute = new() { Width = 42, Text = "05" };
    private readonly TextBox _shutdownSecond = new() { Width = 42, Text = "00" };
    private readonly DataGridView _webPageGrid = new() { AutoGenerateColumns = false, Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing };
    private readonly ListBox _reservationLog = new() { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, IntegralHeight = false };
    private readonly Button _instructionsButton = new() { Text = "说明", AutoSize = true };
    private readonly Button _browserTableToggle = new() { Text = "收起浏览器录制表格", AutoSize = true };
    private readonly Button _gameTableToggle = new() { Text = "收起游戏录制表格", AutoSize = true };
    private GroupBox? _browserMacroBox;
    private GroupBox? _gameMacroBox;
    private readonly DataGridView _macroGrid = new() { AutoGenerateColumns = false, Dock = DockStyle.Fill, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, RowHeadersVisible = false };
    private readonly DataGridView _applicationMacroGrid = new() { AutoGenerateColumns = false, Dock = DockStyle.Fill, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, RowHeadersVisible = false };
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 250 };
    private readonly SettingsStore _settingsStore = new();
    private readonly LowLevelMouseProc _mouseProc;
    private readonly LowLevelKeyboardProc _keyboardProc;
    private ClickSettings _settings;
    private readonly BindingList<MacroStep> _macroSteps;
    private readonly List<ApplicationSlot> _applicationSlots;
    private readonly List<BindingList<MacroStep>> _applicationMacroSteps;
    private readonly BindingList<WebControlPage> _webPages;
    private IntPtr _mouseHook;
    private IntPtr _keyboardHook;
    private bool _isRecording;
    private DateTimeOffset _lastRecordedAt;
    private (double X, double Y) _lastMouseRatio;
    private BindingList<MacroStep>? _recordTarget;
    private bool _recordWindowCoordinates;
    private readonly HashSet<uint> _heldKeys = [];
    private CancellationTokenSource? _runCancellation;
    private CancellationTokenSource? _webConsoleCancellation;
    private CancellationTokenSource? _shutdownCancellation;
    private DateTimeOffset? _liveReservationStart;
    private DateTime? _webReservationStart;
    private DateTime? _scheduledShutdownAt;
    private bool _reservationAutoShutdown;
    private DateTimeOffset? _nextActionAt;
    private string _nextActionName = "";
    private int _currentMacroRow = -1;
    private int _currentApplicationMacroRow = -1;
    private int _selectedApplicationIndex;
    private bool _isLoadingApplicationSlot;
    private bool _recordApplicationMode = true;

    public MainForm()
    {
        Text = $"B站激励抢码工具 {ApplicationVersion}";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(980, 640);
        MaximumSize = Size.Empty;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        _settings = _settingsStore.Load();
        if (_settings.Start is null && _settings.Stop is not null) _settings = _settings with { Start = _settings.Stop };
        _livehimePath.Text = string.IsNullOrWhiteSpace(_settings.LivehimeLaunchPath) ? LivehimeShortcut : _settings.LivehimeLaunchPath;
        _gamePath.Text = string.IsNullOrWhiteSpace(_settings.GameLaunchPath) ? GameShortcut : _settings.GameLaunchPath;
        _autoShutdownEnabled.Checked = _settings.WebConsole.AutoShutdownEnabled;
        SetShutdownTimeFields(_settings.WebConsole.AutoShutdownTime);
        NormalizeLegacySteps(_settings.MacroSteps);
        EnsureApplicationSlots(_settings);
        _macroSteps = new BindingList<MacroStep>(_settings.MacroSteps);
        _applicationSlots = _settings.ApplicationSlots;
        _applicationMacroSteps = _applicationSlots.Select(slot =>
        {
            NormalizeLegacySteps(slot.MacroSteps);
            var steps = new BindingList<MacroStep>(slot.MacroSteps);
            steps.ListChanged += (_, _) => SaveAllMacros();
            return steps;
        }).ToList();
        _webPages = new BindingList<WebControlPage>(GetWebPagesFromSettings(_settings.WebConsole));
        _macroSteps.ListChanged += (_, _) => SaveAllMacros();
        _mouseProc = MouseHookCallback;
        _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseProc, IntPtr.Zero, 0);
        _keyboardProc = KeyboardHookCallback;
        _keyboardHook = SetWindowsHookExKeyboard(WhKeyboardLl, _keyboardProc, IntPtr.Zero, 0);
        BuildUi();
        LoadWebConsoleSettings();
        RefreshPointLabels();
        SetStatus("F8 记录开关播共用位置；F10 开始或停止录制鼠标和键盘操作。", false);
        _runButton.Click += async (_, _) => await StartPlanAsync();
        _recordButton.Click += (_, _) => ToggleRecording();
        _clearMacroButton.Click += (_, _) => ClearMacro();
        _cancelButton.Click += (_, _) => CancelAllReservations();
        _webConsoleRunButton.Click += async (_, _) => await StartWebConsoleScheduleAsync();
        _webConsoleCancelButton.Click += (_, _) => CancelAllReservations();
        _webConsoleTestButton.Click += async (_, _) => await StartWebConsoleTestAsync();
        _addWebPageButton.Click += (_, _) => _webPages.Add(WebControlPage.CreateDefault(_webPages.Count + 1));
        _removeWebPageButton.Click += (_, _) => { if (_webPageGrid.CurrentRow?.DataBoundItem is WebControlPage page) _webPages.Remove(page); };
        _webPages.ListChanged += (_, _) => SaveWebConsoleSettings();
        _webPageGrid.CurrentCellDirtyStateChanged += (_, _) => { if (_webPageGrid.IsCurrentCellDirty) _webPageGrid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        _webPageGrid.CellValueChanged += (_, _) => SaveWebConsoleSettings();
        _autoShutdownEnabled.CheckedChanged += (_, _) => ToggleAutoShutdown();
        _shutdownHour.TextChanged += (_, _) => SaveWebConsoleSettings();
        _shutdownMinute.TextChanged += (_, _) => SaveWebConsoleSettings();
        _shutdownSecond.TextChanged += (_, _) => SaveWebConsoleSettings();
        _instructionsButton.Click += (_, _) => ShowInstructions();
        _browserTableToggle.Click += (_, _) => ToggleMacroTable(true);
        _gameTableToggle.Click += (_, _) => ToggleMacroTable(false);
        _applicationSelector.SelectedIndexChanged += (_, _) => SelectApplicationSlot(_applicationSelector.SelectedIndex);
        _applicationName.TextChanged += (_, _) => UpdateSelectedApplicationDetails();
        _applicationPath.TextChanged += (_, _) => UpdateSelectedApplicationDetails();
        _livehimePath.TextChanged += (_, _) => SaveLaunchPaths();
        _gamePath.TextChanged += (_, _) => SaveLaunchPaths();
        _launchApplicationButton.Click += (_, _) => LaunchSelectedApplication();
        _applicationMacroGrid.Enter += (_, _) => _recordApplicationMode = true;
        _macroGrid.Enter += (_, _) => _recordApplicationMode = false;
        SelectApplicationSlot(0);
        _useSystemTime.CheckedChanged += (_, _) => { _delaySeconds.Enabled = !_useSystemTime.Checked; _startTime.Enabled = _useSystemTime.Checked; };
        _statusTimer.Tick += (_, _) => RefreshCountdown();
        _statusTimer.Start();
        RegisterHotKey(Handle, HotkeyStartPoint, 0, (uint)Keys.F8);
        RegisterHotKey(Handle, HotkeyRecord, 0, (uint)Keys.F10);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0312)
        {
            switch (m.WParam.ToInt32())
            {
                case HotkeyStartPoint: CapturePoint(); break;
                case HotkeyRecord: ToggleRecording(); break;
            }
        }
        base.WndProc(ref m);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        CancelAllReservations();
        if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
        if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
        UnregisterHotKey(Handle, HotkeyStartPoint); UnregisterHotKey(Handle, HotkeyRecord);
        base.OnFormClosed(e);
    }

    private void BuildUi()
    {
        var root = new Panel { Dock = DockStyle.Fill, BackColor = Void, Padding = new Padding(14) };
        var header = new Panel { Height = 50, Dock = DockStyle.Top, BackColor = Nebula };
        header.Controls.Add(new Label { Text = "B站激励抢码工具", ForeColor = Starlight, Font = new Font(Font.FontFamily, 16, FontStyle.Bold), AutoSize = true, Location = new Point(18, 13) });
        var crystalAccent = new Panel { Dock = DockStyle.Bottom, Height = 4 };
        crystalAccent.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(crystalAccent.ClientRectangle, Color.FromArgb(38, 199, 234), StarJade, LinearGradientMode.Horizontal);
            brush.InterpolationColors = new ColorBlend { Colors = [Color.FromArgb(38, 199, 234), Color.FromArgb(0, 100, 194), Color.FromArgb(255, 184, 100)], Positions = [0f, 0.60f, 1f] };
            e.Graphics.FillRectangle(brush, crystalAccent.ClientRectangle);
        };
        header.Controls.Add(crystalAccent);
        _instructionsButton.AutoSize = false; _instructionsButton.Width = 72; _instructionsButton.Height = 30; _instructionsButton.Dock = DockStyle.Right; _instructionsButton.Margin = new Padding(10); header.Controls.Add(_instructionsButton);
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font(Font.FontFamily, 10) };
        var mainPage = new TabPage("网页自动抢码") { Padding = new Padding(14) };
        var livePage = new TabPage("自动直播") { Padding = new Padding(14) };
        var recordingPage = new TabPage("录制操作") { Padding = new Padding(10) };
        BuildWebControlPage(mainPage); BuildLivePage(livePage); BuildRecordingPage(recordingPage);
        tabs.TabPages.AddRange([mainPage, livePage, recordingPage]);
        var statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(4, 6, 4, 0) };
        _status.Dock = DockStyle.Fill; _status.MaximumSize = Size.Empty; statusPanel.Controls.Add(_status);
        root.Controls.Add(tabs); root.Controls.Add(statusPanel); root.Controls.Add(header); Controls.Add(root);
        ApplyStarJadeTheme(root, tabs);
    }

    private void ApplyStarJadeTheme(Control root, TabControl tabs)
    {
        BackColor = Void; ForeColor = Starlight;
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed; tabs.SizeMode = TabSizeMode.Fixed; tabs.ItemSize = new Size(116, 34);
        tabs.DrawItem += (_, e) =>
        {
            var selected = (e.State & DrawItemState.Selected) != 0;
            using var fill = new SolidBrush(selected ? StarJadeDark : Nebula);
            e.Graphics.FillRectangle(fill, e.Bounds);
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, selected ? Void : MutedStarlight, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        StyleTree(root);
        StyleButton(_runButton, StarJade, Void); StyleButton(_webConsoleRunButton, StarJade, Void);
        StyleButton(_cancelButton, CardHover, Starlight); StyleButton(_webConsoleCancelButton, CardHover, Starlight);
        StyleButton(_instructionsButton, Color.FromArgb(255, 241, 213), Color.FromArgb(166, 92, 20)); _instructionsButton.FlatAppearance.BorderColor = Color.FromArgb(238, 173, 83);
    }

    private void StyleTree(Control control)
    {
        foreach (Control child in control.Controls)
        {
            if (child is TabPage tabPage) { tabPage.BackColor = Void; tabPage.ForeColor = Starlight; }
            else if (child is GroupBox group) { group.BackColor = Card; group.ForeColor = StarJade; group.Font = new Font(Font.FontFamily, 10, FontStyle.Bold); }
            else if (child is Panel or FlowLayoutPanel or TableLayoutPanel) { child.BackColor = child.BackColor == Color.Empty || child.BackColor == SystemColors.Control ? (child.Parent is GroupBox ? Card : Void) : child.BackColor; }
            else if (child is Label label) { label.ForeColor = MutedStarlight; }
            else if (child is TextBox text) { text.BackColor = Nebula; text.ForeColor = Starlight; text.BorderStyle = BorderStyle.FixedSingle; }
            else if (child is ComboBox combo) { combo.BackColor = Nebula; combo.ForeColor = Starlight; FlatComboBox(combo); }
            else if (child is ListBox list) { list.BackColor = Card; list.ForeColor = Starlight; }
            else if (child is CheckBox check) { check.ForeColor = Starlight; }
            else if (child is Button button) { StyleButton(button, CardHover, Starlight); }
            else if (child is DataGridView grid) { StyleGrid(grid); }
            StyleTree(child);
        }
    }

    private static void StyleButton(Button button, Color background, Color foreground)
    {
        button.AutoSize = false; button.Height = 32; button.Padding = new Padding(12, 0, 12, 0); button.MinimumSize = new Size(96, 32);
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = 1; button.FlatAppearance.BorderColor = Color.FromArgb(152, 205, 232);
        button.BackColor = background; button.ForeColor = foreground; button.Font = new Font(button.Font, FontStyle.Bold);
    }
    private static void FlatComboBox(ComboBox combo) => combo.FlatStyle = FlatStyle.Flat;
    private static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Card; grid.BorderStyle = BorderStyle.None; grid.GridColor = Color.FromArgb(186, 220, 239);
        grid.EnableHeadersVisualStyles = false; grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Nebula, ForeColor = StarJade, SelectionBackColor = Nebula, SelectionForeColor = StarJade, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
        grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Card, ForeColor = Starlight, SelectionBackColor = StarJadeDark, SelectionForeColor = Void, Padding = new Padding(4, 2, 4, 2) };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(238, 249, 253), ForeColor = Starlight };
        grid.ColumnHeadersHeight = 34; grid.RowTemplate.Height = 30;
    }

    private void BuildMainPage(TabPage page)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "直播预约", Font = new Font(Font.FontFamily, 15, FontStyle.Bold), AutoSize = true, Padding = new Padding(0, 0, 0, 10) }, 0, 0);
        var liveActions = new GroupBox { Text = "直播计划", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12) };
        var liveButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = true }; liveButtons.Controls.AddRange([_runButton, _cancelButton]);
        var testOptions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 8, 0, 0) }; testOptions.Controls.AddRange([_testGameButton, _testBrowserButton]);
        var liveContent = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false }; liveContent.Controls.AddRange([liveButtons, testOptions]); liveActions.Controls.Add(liveContent);
        layout.Controls.Add(liveActions, 0, 1);
        page.Controls.Add(layout);
    }

    private void BuildLivePage(TabPage page)
    {
        BuildMainPage(page);
        BuildSettingsPage(page);
    }

    private void BuildSettingsPage(TabPage page)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, RowCount = 5, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < 5; row++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var startMode = new FlowLayoutPanel { AutoSize = true };
        startMode.Controls.AddRange([_useSystemTime, _startTime, new Label { Text = "当天已过则明天执行", AutoSize = true, Padding = new Padding(6, 5, 0, 0) }]);
        AddRow(layout, 0, "开关播位置（F8）", _startPoint); AddRow(layout, 1, "开播计划", startMode);
        AddRow(layout, 2, "延迟 / 时长", new FlowLayoutPanel { AutoSize = true, Controls = { new Label { Text = "延迟秒", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, _delaySeconds, new Label { Text = "直播时长秒", AutoSize = true, Padding = new Padding(12, 5, 0, 0) }, _liveSeconds } });
        AddRow(layout, 3, "直播姬启动路径", _livehimePath);
        AddRow(layout, 4, "游戏启动路径", _gamePath);
        page.Controls.Add(layout);
    }

    private void BuildRecordingPage(TabPage page)
    {
        ConfigureMacroGrid(_applicationMacroGrid, null); ConfigureMacroGrid(_macroGrid, _macroSteps);
        _macroGrid.CellPainting += PaintCurrentStep; _applicationMacroGrid.CellPainting += PaintCurrentStep;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var browserPage = new TabPage("浏览器 / 应用录制") { Padding = new Padding(10) };
        var gamePage = new TabPage("游戏录制") { Padding = new Padding(10) };

        var browserControls = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, WrapContents = true, Padding = new Padding(0, 0, 0, 8) };
        browserControls.Controls.AddRange([new Label { Text = "应用槽位", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, _applicationSelector, new Label { Text = "名称", AutoSize = true, Padding = new Padding(8, 6, 0, 0) }, _applicationName, new Label { Text = "启动路径", AutoSize = true, Padding = new Padding(8, 6, 0, 0) }, _applicationPath, _launchApplicationButton, _browserTableToggle]);
        _browserMacroBox = new GroupBox { Text = "当前浏览器 / 应用录制", Dock = DockStyle.Fill, Padding = new Padding(8) }; _browserMacroBox.Controls.Add(_applicationMacroGrid);
        browserPage.Controls.Add(_browserMacroBox); browserPage.Controls.Add(browserControls);

        var gameControls = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top, WrapContents = true, Padding = new Padding(0, 0, 0, 8) };
        gameControls.Controls.AddRange([new Label { Text = "游戏录制会保留鼠标、键盘按下/松开和长按时长。", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, _gameTableToggle]);
        _gameMacroBox = new GroupBox { Text = "游戏录制步骤", Dock = DockStyle.Fill, Padding = new Padding(8) }; _gameMacroBox.Controls.Add(_macroGrid);
        gamePage.Controls.Add(_gameMacroBox); gamePage.Controls.Add(gameControls);
        tabs.TabPages.AddRange([browserPage, gamePage]);
        tabs.SelectedIndexChanged += (_, _) => _recordApplicationMode = tabs.SelectedIndex == 0;
        var commonActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(0, 6, 0, 0) };
        commonActions.Controls.AddRange([_recordButton, _clearMacroButton]);
        page.Controls.Add(tabs); page.Controls.Add(commonActions);
        for (var index = 0; index < _applicationSlots.Count; index++) _applicationSelector.Items.Add(FormatApplicationSlot(index));
        _applicationSelector.SelectedIndex = 0;
    }

    private void BuildWebControlPage(TabPage page)
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 82, Padding = new Padding(0, 4, 0, 4) };
        var actionBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, WrapContents = false };
        var powerBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, WrapContents = false };
        var shutdownLabel = new Label { Text = "关机时间", AutoSize = true, Padding = new Padding(8, 6, 0, 0) };
        var hourLabel = new Label { Text = "时", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var minuteLabel = new Label { Text = "分", AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var secondLabel = new Label { Text = "秒", AutoSize = true, Padding = new Padding(0, 6, 8, 0) };
        actionBar.Controls.AddRange([_webConsoleRunButton, _webConsoleCancelButton, _webConsoleTestButton, _addWebPageButton, _removeWebPageButton]);
        powerBar.Controls.AddRange([_autoShutdownEnabled, shutdownLabel, _shutdownHour, hourLabel, _shutdownMinute, minuteLabel, _shutdownSecond, secondLabel]);
        bar.Controls.Add(powerBar);
        bar.Controls.Add(actionBar);
        SetAutoShutdownFieldsEnabled();
        ConfigureWebPageGrid(); _webPageGrid.DataSource = _webPages;
        var logBox = new GroupBox { Text = "预约状态日志", Dock = DockStyle.Bottom, Height = 160, Padding = new Padding(10) };
        logBox.Controls.Add(_reservationLog);
        page.Controls.Add(_webPageGrid); page.Controls.Add(logBox); page.Controls.Add(bar);
    }

    private void ShowInstructions()
    {
        var message = """
网页自动抢码
• 添加网页预约，填写浏览器路径、网址、时间和随机连点参数；点击“预约抢码”后可在状态日志核对内容。
• “抢码测试”会立即运行全部启用的预约；“取消全部预约”会同时停止网页预约、自动关机和直播计划。
• 请只在你有权操作的页面使用网页控制功能。

自动直播
• 按 F8 校准开关播位置，设置开播时间和直播时长后点击“执行计划”。
• 可选择测试游戏录制或测试浏览器录制；后者会遍历全部已配置的浏览器/应用槽位。测试同样由“执行计划”启动，可用“取消计划”停止。

录制操作
• 浏览器/应用与游戏录制已分为两个标签页；应用鼠标只记录单击。
• 游戏表格保留鼠标和键盘按下/松开及长按时长。
• F10 开始或停止当前标签的录制。每个表格都可用“收起/展开”按钮隐藏或显示。
""";
        MessageBox.Show(message, "操作说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ConfigureMacroGrid(DataGridView grid, BindingList<MacroStep>? steps)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CurrentStep", HeaderText = "序号", Width = 76, MinimumWidth = 76, Frozen = true, ReadOnly = true });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "间隔ms", DataPropertyName = nameof(MacroStep.DelayMs), Width = 94, MinimumWidth = 94 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "类型", DataPropertyName = nameof(MacroStep.ActionType), Width = 70, MinimumWidth = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "键鼠操作", DataPropertyName = nameof(MacroStep.Button), Width = 130, MinimumWidth = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "起始X", DataPropertyName = nameof(MacroStep.StartXRatio), Width = 90, MinimumWidth = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "起始Y", DataPropertyName = nameof(MacroStep.StartYRatio), Width = 90, MinimumWidth = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "目标X", DataPropertyName = nameof(MacroStep.XRatio), Width = 90, MinimumWidth = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "目标Y", DataPropertyName = nameof(MacroStep.YRatio), Width = 90, MinimumWidth = 90 });
        foreach (DataGridViewColumn column in grid.Columns) { column.SortMode = DataGridViewColumnSortMode.NotSortable; column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter; column.HeaderCell.Style.WrapMode = DataGridViewTriState.False; }
        grid.DataSource = steps;
    }
    private void ConfigureWebPageGrid()
    {
        if (_webPageGrid.Columns.Count > 0) return;
        _webPageGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "启用", DataPropertyName = nameof(WebControlPage.Enabled), Width = 70, MinimumWidth = 70, FlatStyle = FlatStyle.Standard, ThreeState = false, TrueValue = true, FalseValue = false, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, NullValue = false } });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "名称", DataPropertyName = nameof(WebControlPage.Name), Width = 88, MinimumWidth = 88 });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时", DataPropertyName = nameof(WebControlPage.ScheduleHour), Width = 48, MinimumWidth = 48 });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "分", DataPropertyName = nameof(WebControlPage.ScheduleMinute), Width = 48, MinimumWidth = 48 });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "秒", DataPropertyName = nameof(WebControlPage.ScheduleSecond), Width = 48, MinimumWidth = 48 });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "浏览器路径", DataPropertyName = nameof(WebControlPage.BrowserPath), Width = 250, MinimumWidth = 250, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "网址", DataPropertyName = nameof(WebControlPage.Url), Width = 280, MinimumWidth = 280, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "最大ms", DataPropertyName = nameof(WebControlPage.MaxIntervalMs), Width = 84, MinimumWidth = 84 });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "最小ms", DataPropertyName = nameof(WebControlPage.MinIntervalMs), Width = 84, MinimumWidth = 84 });
        _webPageGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时长s", DataPropertyName = nameof(WebControlPage.DurationSeconds), Width = 78, MinimumWidth = 78 });
        foreach (DataGridViewColumn column in _webPageGrid.Columns) { column.SortMode = DataGridViewColumnSortMode.NotSortable; column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter; column.HeaderCell.Style.WrapMode = DataGridViewTriState.False; }
        _webPageGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _webPageGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
    }

    private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
    {
        table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row); table.Controls.Add(control, 1, row);
    }

    private void CapturePoint()
    {
        var window = FindLivehimeWindow();
        if (window == IntPtr.Zero || !GetWindowRect(window, out var rect)) { SetStatus("未找到可操作的直播姬窗口。", true); return; }
        GetCursorPos(out var cursor); var width = rect.Right - rect.Left; var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0 || cursor.X < rect.Left || cursor.X >= rect.Right || cursor.Y < rect.Top || cursor.Y >= rect.Bottom) { SetStatus("鼠标不在直播姬窗口内。", true); return; }
        var point = new NormalizedPoint((cursor.X - rect.Left) / (double)width, (cursor.Y - rect.Top) / (double)height);
        _settings = _settings with { Start = point, Stop = point }; _settingsStore.Save(_settings); RefreshPointLabels(); SetStatus("已记录开关播共用位置。", false);
    }

    private async Task StartPlanAsync()
    {
        if (_runCancellation is not null || _isRecording) { SetStatus(_isRecording ? "请先按 F10 停止录制。" : "计划正在执行。", true); return; }
        var sharedPoint = _settings.Start ?? _settings.Stop;
        var isTestRun = _testGameButton.Checked || _testBrowserButton.Checked;
        var livehimePath = _livehimePath.Text.Trim(); var gamePath = _gamePath.Text.Trim();
        if (!isTestRun && sharedPoint is null) { SetStatus("请先用 F8 记录开关播共用位置。", true); return; }
        var duration = TimeSpan.Zero; var startAt = default(DateTimeOffset);
        if (!isTestRun)
        {
            if (!TryReadSeconds(_liveSeconds, 1, 86_400, out var liveSeconds, "直播时长") || !TryGetScheduledStart(out startAt)) return;
            duration = TimeSpan.FromSeconds(liveSeconds);
            _liveReservationStart = startAt;
            AppendReservationLog($"已预约直播 | {startAt.LocalDateTime:yyyy-MM-dd HH:mm:ss} | 时长 {liveSeconds} 秒");
            SetFlowStatus("等待直播");
        }
        _runCancellation = new CancellationTokenSource(); _runButton.Enabled = false; _cancelButton.Enabled = true;
        _testGameButton.Enabled = _testBrowserButton.Enabled = false;
        try
        {
            if (isTestRun) { await RunSelectedTestsAsync(gamePath, _runCancellation.Token); return; }
            var startPoint = sharedPoint ?? throw new InvalidOperationException("未记录开关播位置。");
            await WaitForAsync(startAt, "直播预约开始", _runCancellation.Token);
            SetFlowStatus("开启直播");
            AppendReservationLog("直播预约到点，准备启动直播姬。");
            await WaitForAsync(DateTimeOffset.Now.AddSeconds(5), "启动直播姬", _runCancellation.Token); StartShortcut(livehimePath, "直播姬");
            AppendReservationLog("已启动直播姬，等待开播。");
            await WaitForAsync(DateTimeOffset.Now.AddSeconds(5), "开播", _runCancellation.Token); await WaitForLivehimeAsync(TimeSpan.FromSeconds(12), _runCancellation.Token);
            var stopAt = DateTimeOffset.Now.Add(duration); _nextActionAt = stopAt; _nextActionName = "关播";
            ClickSavedPoint(startPoint, "开播");
            AppendReservationLog("已执行开播操作。");
            SetFlowStatus("运行游戏");
            await WaitForAsync(DateTimeOffset.Now.AddSeconds(5), "启动游戏", _runCancellation.Token);
            StartShortcut(gamePath, "崩坏：星穹铁道");
            AppendReservationLog("已启动游戏，准备播放游戏录制。");
            await WaitForAsync(DateTimeOffset.Now.AddSeconds(5), "执行游戏录制", _runCancellation.Token);
            await PlayMacroOnceAsync(_macroSteps, _macroGrid, "游戏录制操作", _runCancellation.Token);
            AppendReservationLog("游戏录制操作已完成。");
            await WaitForAsync(DateTimeOffset.Now.AddSeconds(5), "开始遍历浏览器操作", _runCancellation.Token);
            SetFlowStatus("运行浏览器");
            AppendReservationLog("开始遍历浏览器 / 应用录制。");
            for (var index = 0; index < _applicationSlots.Count; index++)
            {
                var slot = _applicationSlots[index];
                if (string.IsNullOrWhiteSpace(slot.LaunchPath)) continue;
                SelectApplicationSlot(index);
                var existingWindows = GetVisibleTopLevelWindows();
                StartShortcut(slot.LaunchPath, slot.Name);
                await WaitForAsync(DateTimeOffset.Now.AddSeconds(5), $"{slot.Name}加载", _runCancellation.Token);
                BringNewWindowToFront(existingWindows);
                await Task.Delay(200, _runCancellation.Token);
                await PlayMacroOnceAsync(_applicationMacroSteps[index], _applicationMacroGrid, $"{slot.Name}操作", _runCancellation.Token);
                AppendReservationLog($"应用录制已完成 | {slot.Name}");
                if (_applicationSlots.Skip(index + 1).Any(next => !string.IsNullOrWhiteSpace(next.LaunchPath)))
                    await WaitForAsync(DateTimeOffset.Now.AddSeconds(2), "打开下一个应用页面", _runCancellation.Token);
            }
            await WaitForAsync(stopAt, "关播", _runCancellation.Token);
            SetFlowStatus("停止直播");
            SetStatus("正在恢复直播姬窗口并执行关播…", false);
            ClickSavedPoint(startPoint, "关播");
            AppendReservationLog("已执行关播操作。");
            await Task.Delay(TimeSpan.FromSeconds(5), _runCancellation.Token); await CloseLivehimeAsync(_runCancellation.Token);
            SetStatus("关播完成，已关闭直播姬。", false);
            AppendReservationLog("直播已结束，直播姬已关闭。");
        }
        catch (OperationCanceledException) { SetStatus("计划已取消；未再执行后续操作。", false); AppendReservationLog("直播预约已取消。"); SetFlowStatus("已取消"); }
        catch (Exception ex) { SetStatus($"未执行：{ex.Message}", true); AppendReservationLog($"直播预约失败 | {ex.Message}"); SetFlowStatus("异常"); }
        finally { _liveReservationStart = null; _nextActionAt = null; _nextActionName = ""; _runCancellation?.Dispose(); _runCancellation = null; _runButton.Enabled = true; _cancelButton.Enabled = false; _testGameButton.Enabled = _testBrowserButton.Enabled = true; }
    }

    private async Task RunGameTestAsync(TimeSpan duration, string gamePath, CancellationToken token)
    {
        StartShortcut(gamePath, "崩坏：星穹铁道");
        var stopAt = DateTimeOffset.Now.Add(duration);
        _nextActionAt = stopAt; _nextActionName = "结束游戏测试";
        await WaitForAsync(DateTimeOffset.Now.AddSeconds(5), "执行游戏录制", token);
        await PlayMacroOnceAsync(_macroSteps, _macroGrid, "游戏录制操作", token);
        await WaitForAsync(stopAt, "结束游戏测试", token);
        SetStatus("游戏启动测试完成。", false);
    }

    private async Task PlayMacroOnceAsync(BindingList<MacroStep> macroSteps, DataGridView grid, string name, CancellationToken token)
    {
        if (macroSteps.Count == 0) { SetStatus($"{name}为空，跳过回放。", false); return; }
        SetStatus($"正在回放{name}…", false);
        var steps = macroSteps.Select(item => item.Clone()).ToArray();
        var applicationClickOnly = ReferenceEquals(grid, _applicationMacroGrid);
        try
        {
            for (var stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                var step = steps[stepIndex];
                if (applicationClickOnly && step.ActionType == "鼠标" && step.MouseEventRecorded && !step.MouseIsDown) continue;
                SetCurrentMacroRow(grid, stepIndex);
                SetStatus($"正在回放{name}：第 {stepIndex + 1}/{steps.Length} 步（{step.Button}）。", false);
                SetCurrentReservationStep($"{name} 第 {stepIndex + 1}/{steps.Length} 步");
                var interval = TimeSpan.FromMilliseconds(Math.Max(step.DelayMs, 0)); if (interval > TimeSpan.Zero) await Task.Delay(interval, token);
                if (step.ActionType == "键盘")
                {
                    SendKeyEvent((ushort)step.KeyCode, step.KeyIsDown);
                }
                else
                {
                    TryMoveAlongCurve(step);
                    SendMouseEvent(step.MouseButton, applicationClickOnly || !step.MouseEventRecorded ? true : step.MouseIsDown, applicationClickOnly || !step.MouseEventRecorded);
                }
            }
        }
        finally { SetCurrentMacroRow(grid, -1); }
    }

    private void SetCurrentMacroRow(DataGridView grid, int row)
    {
        if (ReferenceEquals(grid, _applicationMacroGrid)) _currentApplicationMacroRow = row; else _currentMacroRow = row;
        grid.InvalidateColumn(0);
    }

    private void PaintCurrentStep(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.ColumnIndex != 0 || e.RowIndex < 0) return;
        e.PaintBackground(e.CellBounds, true);
        var currentRow = ReferenceEquals(sender, _applicationMacroGrid) ? _currentApplicationMacroRow : _currentMacroRow;
        var numberBounds = new Rectangle(e.CellBounds.Left + 16, e.CellBounds.Top, e.CellBounds.Width - 16, e.CellBounds.Height);
        if (e.Graphics is { } graphics && sender is DataGridView grid) TextRenderer.DrawText(graphics, (e.RowIndex + 1).ToString(), grid.Font, numberBounds, e.CellStyle?.ForeColor ?? grid.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        if (e.RowIndex == currentRow)
        {
            var size = 8; var x = e.CellBounds.Left + 4; var y = e.CellBounds.Top + (e.CellBounds.Height - size) / 2;
            e.Graphics?.FillEllipse(Brushes.Red, x, y, size, size);
        }
        e.Handled = true;
    }

    private async Task WaitForAsync(DateTimeOffset target, string action, CancellationToken token)
    {
        _nextActionAt = target; _nextActionName = action; SetCurrentReservationStep(action); var wait = target - DateTimeOffset.Now; if (wait > TimeSpan.Zero) await Task.Delay(wait, token);
    }
    private static async Task WaitForLivehimeAsync(TimeSpan timeout, CancellationToken token)
    {
        var until = DateTimeOffset.Now.Add(timeout);
        while (DateTimeOffset.Now < until) { if (FindLivehimeWindow() != IntPtr.Zero) return; await Task.Delay(200, token); }
        throw new InvalidOperationException("直播姬未在预留时间内打开。");
    }
    private void CancelAllReservations()
    {
        var hadReservation = _runCancellation is not null || _webConsoleCancellation is not null || _shutdownCancellation is not null || _scheduledShutdownAt is not null;
        _runCancellation?.Cancel();
        _webConsoleCancellation?.Cancel();
        _shutdownCancellation?.Cancel();
        _liveReservationStart = null;
        _webReservationStart = null;
        _scheduledShutdownAt = null;
        _reservationAutoShutdown = false;
        CancelWindowsShutdown("已取消全部预约和自动关机。");
        if (hadReservation) SetStatus("已取消全部预约、自动关机和直播计划。", false);
    }
    private bool TryGetScheduledStart(out DateTimeOffset start)
    {
        start = default;
        if (!_useSystemTime.Checked)
        {
            if (!TryReadSeconds(_delaySeconds, 0, 86_400, out var delay, "延迟秒数")) return false;
            start = DateTimeOffset.Now.AddSeconds(delay); return true;
        }
        if (!TimeSpan.TryParse(_startTime.Text.Trim(), out var time)) { SetStatus("开播时间格式应为 HH:mm:ss。", true); return false; }
        var candidate = DateTime.Today.Add(time); if (candidate <= DateTime.Now) candidate = candidate.AddDays(1); start = new DateTimeOffset(candidate); return true;
    }

    private static bool TryReadSeconds(TextBox input, int minimum, int maximum, out int value, string label)
    {
        value = 0;
        if (int.TryParse(input.Text.Trim(), out value) && value >= minimum && value <= maximum) return true;
        MessageBox.Show($"{label}请输入 {minimum} 到 {maximum} 的整数。", "输入无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private async Task RunSelectedTestsAsync(string gamePath, CancellationToken token)
    {
        if (_testBrowserButton.Checked)
        {
            var slots = _applicationSlots.Select((slot, index) => (slot, index)).Where(item => !string.IsNullOrWhiteSpace(item.slot.LaunchPath)).ToArray();
            if (slots.Length == 0) throw new InvalidOperationException("请先在浏览器/应用录制页填写至少一个启动路径。");
            foreach (var (slot, index) in slots)
            {
                SelectApplicationSlot(index);
                var windows = GetVisibleTopLevelWindows(); StartShortcut(slot.LaunchPath, slot.Name);
                await Task.Delay(TimeSpan.FromSeconds(5), token); BringNewWindowToFront(windows);
                await PlayMacroOnceAsync(_applicationMacroSteps[index], _applicationMacroGrid, $"{slot.Name}操作", token);
                if (index != slots[^1].index) await Task.Delay(TimeSpan.FromSeconds(2), token);
            }
        }
        if (_testGameButton.Checked)
        {
            StartShortcut(gamePath, "崩坏：星穹铁道"); await Task.Delay(TimeSpan.FromSeconds(5), token); await PlayMacroOnceAsync(_macroSteps, _macroGrid, "游戏录制操作", token);
        }
        SetStatus("所选测试已完成。", false);
    }
    private async Task StartWebConsoleScheduleAsync()
    {
        if (_webConsoleCancellation is not null) { SetStatus("网页控制任务正在等待或执行。", true); return; }
        var pages = _webPages.Where(page => page.Enabled).Select(page => page.Clone()).ToArray();
        if (pages.Length == 0) { SetStatus("请至少启用一条网页预约。", true); return; }
        if (pages.Any(page => !ValidateWebPage(page))) return;
        if (!TryGetShutdownTime(out var shutdownTime)) return;
        _webReservationStart = pages.Select(GetReservationTime).Min();
        _webConsoleCancellation = new CancellationTokenSource(); _webConsoleRunButton.Enabled = false; _webConsoleCancelButton.Enabled = true;
        _reservationAutoShutdown = _autoShutdownEnabled.Checked;
        _scheduledShutdownAt = _reservationAutoShutdown ? GetShutdownTime(shutdownTime) : null;
        if (_reservationAutoShutdown) _shutdownCancellation = new CancellationTokenSource();
        SetReservationOptionsLocked(true);
        try
        {
            if (_reservationAutoShutdown && _scheduledShutdownAt is { } shutdownAt) ScheduleWindowsShutdown(shutdownAt);
            SetStatus($"已预约 {pages.Length} 个网页任务。", false);
            foreach (var page in pages) AppendReservationLog($"已预约 | {GetReservationTime(page):yyyy-MM-dd HH:mm:ss} | {page.Name} | {page.Url} | {page.MinIntervalMs}-{page.MaxIntervalMs}ms，{page.DurationSeconds}秒");
            SetFlowStatus("等待抢码网页");
            var reservations = Task.WhenAll(pages.Select(page => RunWebReservationAsync(page, _webConsoleCancellation.Token)));
            await reservations;
            AppendReservationLog("全部预约任务已完整执行。");
            SaveCompletionArtifacts("网页预约完成");
            if (_reservationAutoShutdown && _scheduledShutdownAt is { } scheduledShutdown)
            {
                try
                {
                    await Task.Delay(scheduledShutdown - DateTime.Now, _shutdownCancellation?.Token ?? CancellationToken.None);
                    SetFlowStatus("关机进程");
                    SetStatus("已到达关机时间，Windows 将在 1 分钟后关机。", false);
                    AppendReservationLog("关机进程已启动，1 分钟后关机。");
                }
                catch (OperationCanceledException) { AppendReservationLog("自动关机已强制取消。"); SetFlowStatus("完成"); }
            }
            else { SetFlowStatus("完成"); SetStatus("全部网页预约已执行，日志和截图已保存到桌面。", false); }
        }
        catch (OperationCanceledException) { SetStatus("网页控制已取消。", false); AppendReservationLog("网页预约已取消。"); }
        catch (Exception ex) { SetStatus($"网页控制未执行：{ex.Message}", true); AppendReservationLog($"预约失败 | {ex.Message}"); }
        finally { _webReservationStart = null; _scheduledShutdownAt = null; _reservationAutoShutdown = false; SetReservationOptionsLocked(false); _shutdownCancellation?.Dispose(); _shutdownCancellation = null; _webConsoleCancellation?.Dispose(); _webConsoleCancellation = null; _webConsoleRunButton.Enabled = true; _webConsoleCancelButton.Enabled = false; }
    }
    private async Task StartWebConsoleTestAsync()
    {
        if (_webConsoleCancellation is not null) { SetStatus("网页控制任务正在等待或执行。", true); return; }
        var pages = _webPages.Where(page => page.Enabled).Select(page => page.Clone()).ToArray();
        if (pages.Length == 0 || pages.Any(page => !ValidateWebPage(page))) { if (pages.Length == 0) SetStatus("请至少启用一条网页预约。", true); return; }
        _webConsoleCancellation = new CancellationTokenSource(); _webConsoleRunButton.Enabled = false; _webConsoleTestButton.Enabled = false; _webConsoleCancelButton.Enabled = true;
        try
        {
            AppendReservationLog($"抢码测试开始 | 立即执行 {pages.Length} 个网页任务。");
            await Task.WhenAll(pages.Select(async page =>
            {
                AppendReservationLog($"测试执行 | {page.Name} | {page.Url} | {page.MinIntervalMs}-{page.MaxIntervalMs}ms，{page.DurationSeconds}秒");
                await RunWebClickTaskAsync(page, _webConsoleCancellation.Token);
            }));
            AppendReservationLog("抢码测试已完整执行。");
            SaveCompletionArtifacts("抢码测试完成");
            SetStatus("抢码测试已执行，日志和截图已保存到桌面。", false);
        }
        catch (OperationCanceledException) { SetStatus("抢码测试已取消。", false); AppendReservationLog("抢码测试已取消。"); }
        catch (Exception ex) { SetStatus($"抢码测试失败：{ex.Message}", true); AppendReservationLog($"抢码测试失败 | {ex.Message}"); }
        finally { _webConsoleCancellation?.Dispose(); _webConsoleCancellation = null; _webConsoleRunButton.Enabled = true; _webConsoleTestButton.Enabled = true; _webConsoleCancelButton.Enabled = false; }
    }
    private bool ValidateWebPage(WebControlPage page)
    {
        if (!File.Exists(page.BrowserPath)) { SetStatus($"{page.Name}：未找到浏览器。", true); return false; }
        if (!Uri.TryCreate(page.Url, UriKind.Absolute, out _)) { SetStatus($"{page.Name}：请输入完整网页地址。", true); return false; }
        if (!TimeSpan.TryParse(page.Time, out _)) { SetStatus($"{page.Name}：时间格式应为 HH:mm:ss。", true); return false; }
        if (!int.TryParse(page.MinIntervalMs, out var min) || !int.TryParse(page.MaxIntervalMs, out var max) || !int.TryParse(page.DurationSeconds, out var duration) || min < 1 || max < min || duration < 1) { SetStatus($"{page.Name}：请检查连点间隔和持续时间。", true); return false; }
        return true;
    }
    private async Task RunWebReservationAsync(WebControlPage page, CancellationToken token)
    {
        var candidate = GetReservationTime(page);
        SetCurrentReservationStep($"等待 {page.Name} 抢码网页");
        await Task.Delay(candidate - DateTime.Now, token);
        SetFlowStatus("抢码网页");
        SetCurrentReservationStep($"打开 {page.Name}");
        AppendReservationLog($"开始执行 | {page.Name} | {page.Url} | {page.MinIntervalMs}-{page.MaxIntervalMs}ms，{page.DurationSeconds}秒");
        await RunWebClickTaskAsync(page, token);
    }

    private async Task RunWebClickTaskAsync(WebControlPage page, CancellationToken token)
    {
        var script = BuildRandomClickScript(SharedCssSelector, int.Parse(page.MinIntervalMs), int.Parse(page.MaxIntervalMs), int.Parse(page.DurationSeconds));
        await RunWebConsoleAsync(page.BrowserPath, page.Url, script, token);
        SetFlowStatus("抢码");
        SetCurrentReservationStep($"{page.Name} 连点中");
        AppendReservationLog($"已发送连点脚本 | {page.Name}");
        await Task.Delay(TimeSpan.FromSeconds(int.Parse(page.DurationSeconds)), token);
        AppendReservationLog($"连点时长结束 | {page.Name}");
        SetCurrentReservationStep($"{page.Name} 已完成");
    }
    private static DateTime GetReservationTime(WebControlPage page)
    {
        var candidate = DateTime.Today.Add(TimeSpan.Parse(page.Time)); return candidate <= DateTime.Now ? candidate.AddDays(1) : candidate;
    }
    private void AppendReservationLog(string message)
    {
        if (InvokeRequired) { BeginInvoke(new Action(() => AppendReservationLog(message))); return; }
        _reservationLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (_reservationLog.Items.Count > 100) _reservationLog.Items.RemoveAt(_reservationLog.Items.Count - 1);
    }

    private void SetFlowStatus(string state)
    {
        // 状态会写入日志和底部提示，主页不再单独保留状态区。
    }

    private void SetCurrentReservationStep(string step)
    {
        // 当前步骤会写入底部提示和日志，主页不再单独保留状态区。
    }

    private void SaveCompletionArtifacts(string prefix)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var logPath = Path.Combine(desktop, $"{prefix}_{stamp}.txt");
            var logLines = _reservationLog.Items.Cast<object>().Reverse().Select(item => item?.ToString() ?? string.Empty);
            File.WriteAllText(logPath, $"B站激励抢码工具 {prefix}{Environment.NewLine}生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, logLines)}");
            var screenshotPath = Path.Combine(desktop, $"{prefix}_{stamp}.png");
            SaveFullScreenScreenshot(screenshotPath);
            AppendReservationLog($"日志已保存到桌面 | {Path.GetFileName(logPath)}");
            AppendReservationLog($"全屏截图已保存到桌面 | {Path.GetFileName(screenshotPath)}");
        }
        catch (Exception ex)
        {
            AppendReservationLog($"保存日志或截图失败 | {ex.Message}");
        }
    }

    private static void SaveFullScreenScreenshot(string path)
    {
        var left = GetSystemMetrics(SmXVirtualScreen);
        var top = GetSystemMetrics(SmYVirtualScreen);
        var width = Math.Max(1, GetSystemMetrics(SmCxVirtualScreen));
        var height = Math.Max(1, GetSystemMetrics(SmCyVirtualScreen));
        using var image = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(image);
        graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    private void ToggleAutoShutdown()
    {
        SetAutoShutdownFieldsEnabled();
        SaveWebConsoleSettings();
    }

    private void SetAutoShutdownFieldsEnabled()
    {
        var enabled = _autoShutdownEnabled.Checked && _webConsoleCancellation is null;
        _shutdownHour.Enabled = enabled; _shutdownMinute.Enabled = enabled; _shutdownSecond.Enabled = enabled;
    }

    private void SetReservationOptionsLocked(bool locked)
    {
        _autoShutdownEnabled.Enabled = !locked;
        _shutdownHour.Enabled = _autoShutdownEnabled.Checked && !locked;
        _shutdownMinute.Enabled = _autoShutdownEnabled.Checked && !locked;
        _shutdownSecond.Enabled = _autoShutdownEnabled.Checked && !locked;
    }

    private void SetShutdownTimeFields(string value)
    {
        if (!TimeSpan.TryParse(value, out var time)) time = new TimeSpan(1, 5, 0);
        _shutdownHour.Text = time.Hours.ToString("00");
        _shutdownMinute.Text = time.Minutes.ToString("00");
        _shutdownSecond.Text = time.Seconds.ToString("00");
    }

    private bool TryGetShutdownTime(out TimeSpan time)
    {
        time = default;
        if (!_autoShutdownEnabled.Checked) return true;
        if (!int.TryParse(_shutdownHour.Text, out var hour) || !int.TryParse(_shutdownMinute.Text, out var minute) || !int.TryParse(_shutdownSecond.Text, out var second) || hour is < 0 or > 23 || minute is < 0 or > 59 || second is < 0 or > 59)
        {
            SetStatus("自动关机时间请分别填写 0-23 时、0-59 分、0-59 秒。", true);
            return false;
        }
        time = new TimeSpan(hour, minute, second);
        return true;
    }

    private static DateTime GetShutdownTime(TimeSpan time)
    {
        var scheduled = DateTime.Today.Add(time);
        if (scheduled <= DateTime.Now) scheduled = scheduled.AddDays(1);
        return scheduled;
    }

    private void ScheduleWindowsShutdown(DateTime scheduled)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LivehimeClickScheduler");
        Directory.CreateDirectory(folder);
        var scriptPath = Path.Combine(folder, "pending-shutdown.ps1");
        File.WriteAllText(scriptPath, $"Start-Sleep -Seconds {scheduled.Second}{Environment.NewLine}& \"$env:SystemRoot\\System32\\shutdown.exe\" /s /t 60");
        var action = $"powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \\\"{scriptPath}\\\"";
        var arguments = $"/Create /TN \"{ShutdownTaskName}\" /TR \"{action}\" /SC ONCE /SD {scheduled:yyyy/MM/dd} /ST {scheduled:HH:mm} /RU SYSTEM /RL HIGHEST /F";
        RunTaskScheduler(arguments, true);
        AppendReservationLog($"已预约自动关机 | {scheduled:yyyy-MM-dd HH:mm:ss}，届时延迟 1 分钟关机。");
    }

    private void CancelWindowsShutdown(string logMessage)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = "shutdown.exe", Arguments = "/a", UseShellExecute = false, CreateNoWindow = true });
            RunTaskScheduler($"/Delete /TN \"{ShutdownTaskName}\" /F", false);
            AppendReservationLog(logMessage);
        }
        catch { }
    }

    private static void RunTaskScheduler(string arguments, bool throwOnFailure)
    {
        using var process = Process.Start(new ProcessStartInfo { FileName = "schtasks.exe", Arguments = arguments, UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true }) ?? throw new InvalidOperationException("无法启动 Windows 任务计划程序。");
        process.WaitForExit();
        if (throwOnFailure && process.ExitCode != 0) throw new InvalidOperationException($"无法创建自动关机任务：{process.StandardError.ReadToEnd().Trim()}");
    }

    private async Task RunWebConsoleAsync(string browserPath, string url, string script, CancellationToken token)
    {
        var existingWindows = GetVisibleTopLevelWindows();
        Process.Start(new ProcessStartInfo { FileName = browserPath, Arguments = $"\"{url}\"", UseShellExecute = browserPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) });
        await Task.Delay(TimeSpan.FromSeconds(5), token);
        BringNewWindowToFront(existingWindows);
        await Task.Delay(200, token);
        SendKeyEvent((ushort)Keys.F12, true); SendKeyEvent((ushort)Keys.F12, false);
        await Task.Delay(TimeSpan.FromSeconds(5), token);
        SendKeyEvent((ushort)Keys.ControlKey, true); SendKeyEvent((ushort)Keys.ShiftKey, true); SendKeyEvent((ushort)Keys.J, true); SendKeyEvent((ushort)Keys.J, false); SendKeyEvent((ushort)Keys.ShiftKey, false); SendKeyEvent((ushort)Keys.ControlKey, false);
        await Task.Delay(TimeSpan.FromSeconds(5), token);
        var oneLineExpression = $"eval({JsonSerializer.Serialize(script)})";
        SendUnicodeText(oneLineExpression); SendKeyEvent((ushort)Keys.Enter, true); SendKeyEvent((ushort)Keys.Enter, false);
    }

    private static string BuildRandomClickScript(string selector, int minIntervalMs, int maxIntervalMs, int durationSeconds)
    {
        var selectorLiteral = JsonSerializer.Serialize(selector);
        return string.Join("\n", [
            "(() => {",
            $"  const selector = {selectorLiteral};",
            $"  const minDelay = {minIntervalMs};",
            $"  const maxDelay = {maxIntervalMs};",
            $"  const endAfter = {durationSeconds * 1000};",
            "  const endAt = Date.now() + endAfter;",
            "  const clickNext = () => {",
            "    if (Date.now() >= endAt) return;",
            "    const target = document.querySelector(selector);",
            "    if (target) target.click();",
            "    const delay = Math.floor(Math.random() * (maxDelay - minDelay + 1)) + minDelay;",
            "    setTimeout(clickNext, delay);",
            "  };",
            "  clickNext();",
            "})();"
        ]);
    }

    private void LoadWebConsoleSettings()
    {
        _webPageGrid.DataSource = _webPages;
    }

    private void SaveWebConsoleSettings()
    {
        _settings = _settings with { WebConsole = new WebConsoleSettings { Pages = _webPages.Select(page => page.Clone()).ToList(), AutoShutdownEnabled = _autoShutdownEnabled.Checked, AutoShutdownTime = $"{_shutdownHour.Text}:{_shutdownMinute.Text}:{_shutdownSecond.Text}" } };
        _settingsStore.Save(_settings);
    }
    private void ToggleRecording()
    {
        if (!_isRecording)
        {
            _recordTarget = _recordApplicationMode ? _applicationMacroSteps[_selectedApplicationIndex] : _macroSteps;
            _recordWindowCoordinates = _recordApplicationMode;
        }
        _isRecording = !_isRecording; _recordButton.Text = _isRecording ? "停止录制（F10）" : "开始录制（F10）"; _lastRecordedAt = DateTimeOffset.Now;
        if (_isRecording) _heldKeys.Clear(); else _heldKeys.Clear();
        GetCursorPos(out var cursor); _lastMouseRatio = GetScreenRatio(cursor.X, cursor.Y);
        var targetName = _recordApplicationMode ? _applicationSlots[_selectedApplicationIndex].Name : "游戏";
        var mouseRule = _recordWindowCoordinates ? "鼠标仅记录单击" : "鼠标记录按下和松开";
        SetStatus(_isRecording ? $"正在录制{targetName}操作：{mouseRule}，并记录键盘按键。操作完成后按 F10。" : $"{targetName}录制已停止，共 {_recordTarget?.Count ?? 0} 步。", false);
        if (!_isRecording) { _recordTarget = null; _recordWindowCoordinates = false; }
    }
    private void ClearMacro()
    {
        if (_isRecording) { SetStatus("请先停止录制。", true); return; }
        var target = _recordApplicationMode ? _applicationMacroSteps[_selectedApplicationIndex] : _macroSteps;
        var targetName = _recordApplicationMode ? _applicationSlots[_selectedApplicationIndex].Name : "游戏";
        if (MessageBox.Show($"清空全部{targetName}录制操作吗？", "清空录制", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK) target.Clear();
    }
    private void ToggleMacroTable(bool browser)
    {
        var box = browser ? _browserMacroBox : _gameMacroBox;
        if (box is null) return;
        box.Visible = !box.Visible;
        var toggle = browser ? _browserTableToggle : _gameTableToggle;
        toggle.Text = box.Visible ? (browser ? "收起浏览器录制表格" : "收起游戏录制表格") : (browser ? "展开浏览器录制表格" : "展开游戏录制表格");
    }
    private IntPtr MouseHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && _isRecording && (wParam == (IntPtr)WmLButtonDown || wParam == (IntPtr)WmRButtonDown || wParam == (IntPtr)WmLButtonUp || wParam == (IntPtr)WmRButtonUp))
        {
            var target = _recordTarget;
            if (target is null) return CallNextHookEx(_mouseHook, code, wParam, lParam);
            var down = wParam == (IntPtr)WmLButtonDown || wParam == (IntPtr)WmRButtonDown;
            if (_recordWindowCoordinates && !down) return CallNextHookEx(_mouseHook, code, wParam, lParam);
            var info = Marshal.PtrToStructure<MsllHookStruct>(lParam); var now = DateTimeOffset.Now; var delay = (int)Math.Clamp((now - _lastRecordedAt).TotalMilliseconds, 0, int.MaxValue); _lastRecordedAt = now;
            var button = wParam == (IntPtr)WmLButtonDown || wParam == (IntPtr)WmLButtonUp ? "左键" : "右键";
            var (xRatio, yRatio) = GetScreenRatio(info.pt.X, info.pt.Y);
            var start = _lastMouseRatio; _lastMouseRatio = (xRatio, yRatio);
            var windowRect = default(RECT);
            var usesWindowCoordinates = _recordWindowCoordinates && TryGetForegroundWindowRect(out windowRect);
            var startPoint = GetScreenPoint(start.X, start.Y);
            var startX = start.X; var startY = start.Y; var endX = xRatio; var endY = yRatio;
            if (usesWindowCoordinates)
            {
                var recordedStart = GetWindowRatio(startPoint.X, startPoint.Y, windowRect);
                var recordedEnd = GetWindowRatio(info.pt.X, info.pt.Y, windowRect);
                startX = recordedStart.X; startY = recordedStart.Y; endX = recordedEnd.X; endY = recordedEnd.Y;
            }
            BeginInvoke(new Action(() => target.Add(new MacroStep { DelayMs = delay, ActionType = "鼠标", Button = _recordWindowCoordinates ? button + " 单击" : button + (down ? " ↓" : " ↑"), MouseButton = button, MouseIsDown = true, MouseEventRecorded = !_recordWindowCoordinates, UseWindowCoordinates = usesWindowCoordinates, StartXRatio = startX, StartYRatio = startY, XRatio = endX, YRatio = endY })));
        }
        return CallNextHookEx(_mouseHook, code, wParam, lParam);
    }
    private IntPtr KeyboardHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && _isRecording && (wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown || wParam == (IntPtr)WmKeyUp || wParam == (IntPtr)WmSysKeyUp))
        {
            var target = _recordTarget;
            if (target is null) return CallNextHookEx(_keyboardHook, code, wParam, lParam);
            var info = Marshal.PtrToStructure<KbdllHookStruct>(lParam);
            if (info.vkCode != (uint)Keys.F10)
            {
                var key = ((Keys)info.vkCode).ToString();
                var down = wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown;
                if (down ? !_heldKeys.Add(info.vkCode) : !_heldKeys.Remove(info.vkCode)) return CallNextHookEx(_keyboardHook, code, wParam, lParam);
                var now = DateTimeOffset.Now; var delay = (int)Math.Clamp((now - _lastRecordedAt).TotalMilliseconds, 0, int.MaxValue); _lastRecordedAt = now;
                BeginInvoke(new Action(() => target.Add(new MacroStep { DelayMs = delay, ActionType = "键盘", Button = key + (down ? " ↓" : " ↑"), KeyCode = (int)info.vkCode, KeyIsDown = down })));
            }
        }
        return CallNextHookEx(_keyboardHook, code, wParam, lParam);
    }
    private void SelectApplicationSlot(int index)
    {
        if (index < 0 || index >= _applicationSlots.Count || _isRecording) return;
        _selectedApplicationIndex = index;
        _isLoadingApplicationSlot = true;
        try
        {
            var slot = _applicationSlots[index];
            if (_applicationSelector.SelectedIndex != index) _applicationSelector.SelectedIndex = index;
            _applicationName.Text = slot.Name;
            _applicationPath.Text = slot.LaunchPath;
            _applicationMacroGrid.DataSource = _applicationMacroSteps[index];
        }
        finally { _isLoadingApplicationSlot = false; }
        _recordApplicationMode = true;
    }

    private string FormatApplicationSlot(int index) => $"页面 {index + 1}：{_applicationSlots[index].Name}";

    private void UpdateSelectedApplicationDetails()
    {
        if (_isLoadingApplicationSlot || _applicationSlots.Count == 0) return;
        var slot = _applicationSlots[_selectedApplicationIndex];
        slot.Name = string.IsNullOrWhiteSpace(_applicationName.Text) ? $"浏览器页面 {_selectedApplicationIndex + 1}" : _applicationName.Text.Trim();
        slot.LaunchPath = _applicationPath.Text.Trim();
        _applicationSelector.Items[_selectedApplicationIndex] = FormatApplicationSlot(_selectedApplicationIndex);
        SaveAllMacros();
    }

    private void LaunchSelectedApplication()
    {
        var slot = _applicationSlots[_selectedApplicationIndex];
        try { StartShortcut(slot.LaunchPath, slot.Name); SetStatus($"已打开 {slot.Name}，现在可按 F10 录制。", false); }
        catch (Exception ex) { SetStatus($"未打开应用：{ex.Message}", true); }
    }

    private void SaveAllMacros()
    {
        for (var index = 0; index < _applicationSlots.Count; index++) _applicationSlots[index].MacroSteps = _applicationMacroSteps[index].Select(item => item.Clone()).ToList();
        _settings = _settings with { MacroSteps = _macroSteps.Select(item => item.Clone()).ToList(), ApplicationSlots = _applicationSlots.Select(item => item.Clone()).ToList() };
        _settingsStore.Save(_settings);
    }
    private void SaveLaunchPaths()
    {
        _settings = _settings with { LivehimeLaunchPath = _livehimePath.Text.Trim(), GameLaunchPath = _gamePath.Text.Trim() };
        _settingsStore.Save(_settings);
    }

    private void ClickSavedPoint(NormalizedPoint point, string action)
    {
        var (x, y) = RestoreLivehimeAndGetPoint(point); MoveCursorAndVerify(x, y); SendMouseClick("左键"); SetStatus($"已点击{action}位置 ({x}, {y})。", false);
    }
    private void MoveToSavedPoint(NormalizedPoint point, string action)
    {
        var (x, y) = RestoreLivehimeAndGetPoint(point); MoveCursorAndVerify(x, y); SetStatus($"定位测试：已移动至{action}位置 ({x}, {y})，未点击。", false);
    }
    private static (int X, int Y) RestoreLivehimeAndGetPoint(NormalizedPoint point)
    {
        var window = FindLivehimeWindow(); if (window == IntPtr.Zero || !GetWindowRect(window, out var rect)) throw new InvalidOperationException("找不到直播姬窗口。");
        ShowWindow(window, Restore); SetForegroundWindow(window); Thread.Sleep(400); if (!GetWindowRect(window, out rect)) throw new InvalidOperationException("无法读取直播姬窗口位置。");
        return (rect.Left + (int)Math.Round((rect.Right - rect.Left) * point.X), rect.Top + (int)Math.Round((rect.Bottom - rect.Top) * point.Y));
    }
    private static void StartShortcut(string path, string name)
    {
        if (!File.Exists(path)) throw new InvalidOperationException($"未找到{name}快捷方式：{path}"); Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }
    private static string GetDefaultEdgePath()
    {
        var candidates = new[] { @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", @"C:\Program Files\Microsoft\Edge\Application\msedge.exe" };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
    private static async Task CloseLivehimeAsync(CancellationToken token)
    {
        var processes = Process.GetProcessesByName("livehime"); foreach (var process in processes) { try { if (process.MainWindowHandle != IntPtr.Zero) process.CloseMainWindow(); } catch { } }
        await Task.Delay(TimeSpan.FromSeconds(5), token); foreach (var process in processes) { try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { } finally { process.Dispose(); } }
    }
    private static IntPtr FindLivehimeWindow() => Process.GetProcessesByName("livehime").Select(process => process.MainWindowHandle).FirstOrDefault(handle => handle != IntPtr.Zero);
    private void RefreshPointLabels() { _startPoint.Text = FormatPoint(_settings.Start ?? _settings.Stop); }
    private static string FormatPoint(NormalizedPoint? point) => point is { } known ? $"已记录（窗口内 {known.X:P0}, {known.Y:P0}）" : "未记录";
    private void RefreshCountdown() { if (_nextActionAt is { } at && at > DateTimeOffset.Now) _status.Text = $"等待{_nextActionName}：剩余 {(at - DateTimeOffset.Now).TotalSeconds:0} 秒。"; }
    private void SetStatus(string message, bool error) { _status.ForeColor = error ? Color.Firebrick : SystemColors.ControlText; _status.Text = message; }
    private static bool TryMoveAlongCurve(MacroStep step)
    {
        var (sx, sy) = GetScreenPoint(step.StartXRatio, step.StartYRatio); var (ex, ey) = GetScreenPoint(step.XRatio, step.YRatio);
        if (step.UseWindowCoordinates && TryGetForegroundWindowRect(out var rect))
        {
            (sx, sy) = GetWindowPoint(step.StartXRatio, step.StartYRatio, rect);
            (ex, ey) = GetWindowPoint(step.XRatio, step.YRatio, rect);
        }
        var dx = ex - sx; var dy = ey - sy; var distance = Math.Sqrt(dx * dx + dy * dy);
        var offset = Math.Min(80, Math.Max(8, distance * 0.16));
        var controlX = (sx + ex) / 2.0 - dy / Math.Max(1, distance) * Random.Shared.NextDouble() * offset * (Random.Shared.Next(2) == 0 ? -1 : 1);
        var controlY = (sy + ey) / 2.0 + dx / Math.Max(1, distance) * Random.Shared.NextDouble() * offset * (Random.Shared.Next(2) == 0 ? -1 : 1);
        var steps = Math.Clamp((int)(distance / 22), 3, 28);
        for (var i = 1; i <= steps; i++)
        {
            var t = i / (double)steps; var u = 1 - t;
            if (!SetCursorPos((int)Math.Round(u * u * sx + 2 * u * t * controlX + t * t * ex), (int)Math.Round(u * u * sy + 2 * u * t * controlY + t * t * ey))) return false;
            Thread.Sleep(Random.Shared.Next(5, 14));
        }
        GetCursorPos(out var actual); return actual.X == ex && actual.Y == ey;
    }
    private static void MoveCursorAndVerify(int x, int y)
    {
        if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows 拒绝移动鼠标指针。"); Thread.Sleep(50); GetCursorPos(out var actual);
        if (actual.X != x || actual.Y != y) throw new InvalidOperationException($"鼠标未到达目标位置（目标 {x}, {y}；实际 {actual.X}, {actual.Y}）。");
    }
    private static (double X, double Y) GetScreenRatio(int x, int y)
    {
        var left = GetSystemMetrics(SmXVirtualScreen); var top = GetSystemMetrics(SmYVirtualScreen);
        var width = Math.Max(1, GetSystemMetrics(SmCxVirtualScreen) - 1); var height = Math.Max(1, GetSystemMetrics(SmCyVirtualScreen) - 1);
        return ((x - left) / (double)width, (y - top) / (double)height);
    }
    private static (int X, int Y) GetScreenPoint(double xRatio, double yRatio)
    {
        var left = GetSystemMetrics(SmXVirtualScreen); var top = GetSystemMetrics(SmYVirtualScreen);
        var width = Math.Max(1, GetSystemMetrics(SmCxVirtualScreen) - 1); var height = Math.Max(1, GetSystemMetrics(SmCyVirtualScreen) - 1);
        return (left + (int)Math.Round(Math.Clamp(xRatio, 0, 1) * width), top + (int)Math.Round(Math.Clamp(yRatio, 0, 1) * height));
    }
    private static bool TryGetForegroundWindowRect(out RECT rect)
    {
        rect = default;
        var window = GetForegroundWindow();
        return window != IntPtr.Zero && GetWindowRect(window, out rect) && rect.Right > rect.Left && rect.Bottom > rect.Top;
    }
    private static HashSet<IntPtr> GetVisibleTopLevelWindows()
    {
        var windows = new HashSet<IntPtr>();
        EnumWindows((window, _) => { if (IsWindowVisible(window)) windows.Add(window); return true; }, IntPtr.Zero);
        return windows;
    }
    private static void BringNewWindowToFront(HashSet<IntPtr> existingWindows)
    {
        IntPtr candidate = IntPtr.Zero;
        EnumWindows((window, _) =>
        {
            if (!existingWindows.Contains(window) && IsWindowVisible(window)) { candidate = window; return false; }
            return true;
        }, IntPtr.Zero);
        if (candidate != IntPtr.Zero) SetForegroundWindow(candidate);
    }
    private static (double X, double Y) GetWindowRatio(int x, int y, RECT rect)
    {
        var width = Math.Max(1, rect.Right - rect.Left); var height = Math.Max(1, rect.Bottom - rect.Top);
        return ((x - rect.Left) / (double)width, (y - rect.Top) / (double)height);
    }
    private static (int X, int Y) GetWindowPoint(double xRatio, double yRatio, RECT rect)
    {
        var width = Math.Max(1, rect.Right - rect.Left); var height = Math.Max(1, rect.Bottom - rect.Top);
        return (rect.Left + (int)Math.Round(xRatio * width), rect.Top + (int)Math.Round(yRatio * height));
    }
    private static void SendMouseClick(string button) => SendMouseEvent(button, true, true);

    private static void SendMouseEvent(string button, bool isDown, bool isClick)
    {
        var flags = button.Contains("右键") ? (MouseRightDown, MouseRightUp) : (MouseLeftDown, MouseLeftUp);
        var inputs = isClick ? new[] { new INPUT { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags.Item1 } } }, new INPUT { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flags.Item2 } } } } : new[] { new INPUT { type = InputMouse, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = isDown ? flags.Item1 : flags.Item2 } } } };
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) != inputs.Length) throw new InvalidOperationException("Windows 未能发送鼠标点击。请确认已同意管理员权限提示。");
    }
    private static void SendKeyEvent(ushort keyCode, bool isDown)
    {
        if (keyCode == 0) return;
        var inputs = new[] { new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wVk = keyCode, dwFlags = isDown ? 0u : KeyEventFKeyUp } } } };
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) != inputs.Length) throw new InvalidOperationException("Windows 未能发送键盘按键。请确认已同意管理员权限提示。");
    }
    private static void SendUnicodeText(string text)
    {
        var inputs = text.SelectMany(character => new[]
        {
            new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KeyEventFUnicode } } },
            new INPUT { type = InputKeyboard, U = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KeyEventFUnicode | KeyEventFKeyUp } } }
        }).ToArray();
        if (inputs.Length > 0 && SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) != inputs.Length) throw new InvalidOperationException("Windows 未能输入控制台内容。请确认已同意管理员权限提示。");
    }

    private readonly record struct NormalizedPoint(double X, double Y);
    private sealed record ClickSettings(NormalizedPoint? Start, NormalizedPoint? Stop, List<MacroStep>? SavedSteps = null)
    {
        public List<MacroStep> MacroSteps { get; init; } = SavedSteps ?? [];
        public List<MacroStep> WebMacroSteps { get; init; } = [];
        public List<ApplicationSlot> ApplicationSlots { get; init; } = [];
        public WebConsoleSettings WebConsole { get; init; } = new();
        public string LivehimeLaunchPath { get; init; } = LivehimeShortcut;
        public string GameLaunchPath { get; init; } = GameShortcut;
    }
    public sealed class WebConsoleSettings
    {
        public List<WebControlPage> Pages { get; set; } = [];
        public bool AutoShutdownEnabled { get; set; }
        public string AutoShutdownTime { get; set; } = "01:05:00";
        // 以下字段用于读取旧版设置；新版本统一使用 Pages。
        public bool Enabled { get; set; }
        public string Time { get; set; } = "00:00:00";
        public string BrowserPath { get; set; } = "";
        public string Url { get; set; } = "about:blank";
        public string Selector { get; set; } = "";
        public int MinIntervalMs { get; set; } = 200;
        public int MaxIntervalMs { get; set; } = 400;
        public int DurationSeconds { get; set; } = 30;
    }
    public sealed class WebControlPage
    {
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = "网页";
        public string Time { get; set; } = "00:00:00";
        public string BrowserPath { get; set; } = GetDefaultEdgePath();
        public string Url { get; set; } = "about:blank";
        public string MinIntervalMs { get; set; } = "200";
        public string MaxIntervalMs { get; set; } = "400";
        public string DurationSeconds { get; set; } = "30";
        [JsonIgnore]
        public string ScheduleHour { get => GetTimePart(0); set => SetTimePart(0, value, 23); }
        [JsonIgnore]
        public string ScheduleMinute { get => GetTimePart(1); set => SetTimePart(1, value, 59); }
        [JsonIgnore]
        public string ScheduleSecond { get => GetTimePart(2); set => SetTimePart(2, value, 59); }
        private string GetTimePart(int index)
        {
            var parts = Time.Split(':');
            return parts.Length == 3 && int.TryParse(parts[index], out var number) ? number.ToString("00") : "00";
        }
        private void SetTimePart(int index, string value, int maximum)
        {
            var parts = Time.Split(':');
            var values = new[] { 0, 0, 0 };
            for (var i = 0; i < values.Length; i++) if (i < parts.Length) int.TryParse(parts[i], out values[i]);
            values[index] = int.TryParse(value, out var number) ? Math.Clamp(number, 0, maximum) : 0;
            Time = $"{values[0]:00}:{values[1]:00}:{values[2]:00}";
        }
        public static WebControlPage CreateDefault(int number) => new() { Name = $"网页 {number}" };
        public WebControlPage Clone() => new() { Enabled = Enabled, Name = Name, Time = Time, BrowserPath = BrowserPath, Url = Url, MinIntervalMs = MinIntervalMs, MaxIntervalMs = MaxIntervalMs, DurationSeconds = DurationSeconds };
    }
    private static List<WebControlPage> GetWebPagesFromSettings(WebConsoleSettings? settings)
    {
        if (settings?.Pages is { Count: > 0 }) return settings.Pages.Select(page => page.Clone()).ToList();
        var legacy = settings ?? new WebConsoleSettings();
        return [new WebControlPage { Enabled = legacy.Enabled, Name = "网页 1", Time = legacy.Time, BrowserPath = string.IsNullOrWhiteSpace(legacy.BrowserPath) ? GetDefaultEdgePath() : legacy.BrowserPath, Url = string.IsNullOrWhiteSpace(legacy.Url) ? "about:blank" : legacy.Url, MinIntervalMs = legacy.MinIntervalMs.ToString(), MaxIntervalMs = legacy.MaxIntervalMs.ToString(), DurationSeconds = legacy.DurationSeconds.ToString() }];
    }
    public sealed class ApplicationSlot
    {
        public string Name { get; set; } = "浏览器页面";
        public string LaunchPath { get; set; } = "";
        public List<MacroStep> MacroSteps { get; set; } = [];
        public ApplicationSlot Clone() => new() { Name = Name, LaunchPath = LaunchPath, MacroSteps = MacroSteps.Select(item => item.Clone()).ToList() };
    }
    public sealed class MacroStep
    {
        public int DelayMs { get; set; }
        public string ActionType { get; set; } = "鼠标";
        public string Button { get; set; } = "左键";
        public string MouseButton { get; set; } = "左键";
        public bool MouseIsDown { get; set; } = true;
        public bool MouseEventRecorded { get; set; }
        public bool UseWindowCoordinates { get; set; }
        public double StartXRatio { get; set; }
        public double StartYRatio { get; set; }
        public double XRatio { get; set; }
        public double YRatio { get; set; }
        public int KeyCode { get; set; }
        public bool KeyIsDown { get; set; } = true;
        public int X { get; set; }
        public int Y { get; set; }
        public MacroStep Clone() => new() { DelayMs = DelayMs, ActionType = ActionType, Button = Button, MouseButton = MouseButton, MouseIsDown = MouseIsDown, MouseEventRecorded = MouseEventRecorded, UseWindowCoordinates = UseWindowCoordinates, StartXRatio = StartXRatio, StartYRatio = StartYRatio, XRatio = XRatio, YRatio = YRatio, KeyCode = KeyCode, KeyIsDown = KeyIsDown, X = X, Y = Y };
    }
    private static void NormalizeLegacySteps(IEnumerable<MacroStep> steps)
    {
        foreach (var step in steps)
        {
            if (step.ActionType == "鼠标" && step.XRatio == 0 && step.YRatio == 0 && (step.X != 0 || step.Y != 0)) (step.XRatio, step.YRatio) = GetScreenRatio(step.X, step.Y);
            if (step.ActionType == "鼠标" && step.StartXRatio == 0 && step.StartYRatio == 0) (step.StartXRatio, step.StartYRatio) = (step.XRatio, step.YRatio);
        }
    }
    private static void EnsureApplicationSlots(ClickSettings settings)
    {
        if (settings.ApplicationSlots.Count == 0)
        {
            settings.ApplicationSlots.AddRange([
                new ApplicationSlot { Name = "夸克", LaunchPath = @"C:\Users\22320\Desktop\夸克.lnk", MacroSteps = settings.WebMacroSteps.Select(item => item.Clone()).ToList() },
                new ApplicationSlot { Name = "Google Chrome", LaunchPath = @"C:\Users\Public\Desktop\Google Chrome.lnk" },
                new ApplicationSlot { Name = "浏览器页面 3" },
                new ApplicationSlot { Name = "浏览器页面 4" }
            ]);
        }
        while (settings.ApplicationSlots.Count < 4) settings.ApplicationSlots.Add(new ApplicationSlot { Name = $"浏览器页面 {settings.ApplicationSlots.Count + 1}" });
        if (settings.ApplicationSlots.Count > 4) settings.ApplicationSlots.RemoveRange(4, settings.ApplicationSlots.Count - 4);
    }
    private sealed class SettingsStore
    {
        private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LivehimeClickScheduler", "settings.json");
        public ClickSettings Load() { try { return JsonSerializer.Deserialize<ClickSettings>(File.ReadAllText(_path)) ?? new(null, null); } catch { return new(null, null); } }
        public void Save(ClickSettings settings) { Directory.CreateDirectory(Path.GetDirectoryName(_path)!); File.WriteAllText(_path, JsonSerializer.Serialize(settings)); }
    }

    private const int Restore = 9, WhMouseLl = 14, WhKeyboardLl = 13, WmLButtonDown = 0x0201, WmLButtonUp = 0x0202, WmRButtonDown = 0x0204, WmRButtonUp = 0x0205, WmKeyDown = 0x0100, WmKeyUp = 0x0101, WmSysKeyDown = 0x0104, WmSysKeyUp = 0x0105;
    private const int SmXVirtualScreen = 76, SmYVirtualScreen = 77, SmCxVirtualScreen = 78, SmCyVirtualScreen = 79;
    private const uint InputMouse = 0, InputKeyboard = 1, MouseLeftDown = 0x0002, MouseLeftUp = 0x0004, MouseRightDown = 0x0008, MouseRightUp = 0x0010, KeyEventFKeyUp = 0x0002, KeyEventFUnicode = 0x0004;
    private delegate IntPtr LowLevelMouseProc(int code, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct MsllHookStruct { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct KbdllHookStruct { public uint vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll", EntryPoint = "SetWindowsHookEx")] private static extern IntPtr SetWindowsHookExKeyboard(int idHook, LowLevelKeyboardProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
