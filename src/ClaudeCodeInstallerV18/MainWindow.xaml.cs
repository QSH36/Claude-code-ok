using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ClaudeCodeInstallerV18;

public partial class MainWindow : Window
{
    // ── State ───────────────────────────────────────
    int _step;
    bool _isSimple;
    string _drive;
    List<string> _drives = new();
    string _selectedApi = "deepseek";
    bool _showCustomPaths;
    bool _showCustomLogic;
    Dictionary<string, string> _toolDrives = new();
    bool _maxThinking = true, _noUpdate;
    string _claudeMdContent = "";
    List<string> _selectedSkills = new();
    bool _installing;

    // ── Page storage ─────────────────────────────────
    Grid[] _pages = new Grid[8];
    // Page 0 fields
    ComboBox _cmbLang, _cmbDrive;
    TextBlock _lblNode, _lblGit, _lblPython, _lblClaude, _lblPath;
    CheckBox _chkTools, _chkLogic, _chkCustomLogic, _chkThinking, _chkNoUpdate;
    TextBox _txtLogic;
    RadioButton _rbSafe, _rbPro;
    ComboBox _cmbDriveSimple, _cmbDrive6;
    // Page 5 API fields
    TextBox _dsKey, _custKey, _custUrl, _custMain, _custHaiku, _custSonnet, _custOpus, _custSub;
    CheckBox _chkSubagent;
    // Page 6
    TextBlock _pathHint;
    CheckBox _chkCustomPaths;
    ComboBox _tpNode, _tpGit, _tpNpm, _tpCc, _tpPy, _tpTess;
    TextBlock _tpPathNode, _tpPathGit, _tpPathNpm, _tpPathCc, _tpPathPy, _tpPathTess;
    // Page 7
    ProgressBar _bar;
    TextBlock _termLog;
    Button _btnInstall;

    readonly (string n, string d, string i)[] _skills = {
        ("Superpowers","开发工作流全家桶：规划→编写→TDD→审查→调试→验证","obra/superpowers"),
        ("Find Skills","20万+Skills搜索引擎，按需匹配推荐","vercel-labs/skills@find-skills"),
        ("Document Skills","Word/Excel/PDF/PPT文档四件套","document-skills@anthropic-agent-skills"),
        ("Frontend Design","消除AI通用审美，高质量有风格UI","obra/frontend-design"),
        ("Skill Creator","创建/修改/优化自定义Skills","skill-creator@claude-plugins-official"),
        ("Caveman","Prompt压缩器，减少Token消耗","JuliusBrussee/caveman"),
        ("Web Access","操控本地浏览器，保持登录态","yize/web-access"),
        ("Claude-mem","长期跨会话记忆系统","claude-mem@thedotmack"),
        ("PUA Skill","AI摆烂时强制换思路","pua@claude-code-plugins"),
        ("Excalidraw Diagram","自然语言生成架构/流程图","excalidraw-diagram@claude-plugins-official"),
        ("Code Review","并行5Agent代码审查","genskills--code-review"),
        ("Security Audit","安全漏洞扫描，OWASP/依赖风险","genskills--security-audit"),
        ("Test Generator","自动生成测试套件","genskills--test-generator"),
    };

    Color BrandBlue => Color.FromRgb(0, 102, 255);
    Color BrandEnd => Color.FromRgb(77, 163, 255);
    Color Green => Color.FromRgb(0, 184, 77);
    Color Orange => Color.FromRgb(255, 107, 53);

    public MainWindow()
    {
        InitializeComponent();
        try { _drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed).Select(d => d.Name.TrimEnd('\\')).ToList(); } catch { }
        if (_drives.Count == 0) _drives = new List<string> { "C:", "D:", "F:" };
        _drive = _drives[0];
        BuildPages();
        Navigate(0);
    }

    // ═══ PAGE BUILDERS ══════════════════════════════
    void BuildPages()
    {
        _pages[0] = BuildPage0();
        _pages[1] = BuildPage1();
        _pages[2] = BuildPage2();
        _pages[3] = BuildPage3();
        _pages[4] = BuildPage4();
        _pages[5] = BuildPage5();
        _pages[6] = BuildPage6();
        _pages[7] = BuildPage7();
    }

    // ── Helpers ──────────────────────────────────────
    Grid GradientBanner(string stepNum, string title, string? subtitle = null)
    {
        var g = new Grid { Height = 52 };
        g.Background = new LinearGradientBrush(BrandBlue, BrandEnd, 135);
        if (stepNum != "")
        {
            var badge = new Border { Width = 32, Height = 32, CornerRadius = new CornerRadius(16), Background = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)), Margin = new Thickness(24, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            badge.Child = new TextBlock { Text = stepNum, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 13, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            g.Children.Add(badge);
        }
        var tb = new TextBlock { Text = title, Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14, VerticalAlignment = VerticalAlignment.Center };
        tb.Margin = stepNum != "" ? new Thickness(68, 0, 0, 0) : new Thickness(24, 0, 0, 0);
        g.Children.Add(tb);
        if (subtitle != null)
        {
            var sub = new TextBlock { Text = subtitle, Foreground = new SolidColorBrush(Color.FromArgb(153, 255, 255, 255)), FontSize = 11, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 24, 0) };
            g.Children.Add(sub);
        }
        return g;
    }

    Grid BigBanner()
    {
        var g = new Grid { Height = 180 };
        g.Background = new LinearGradientBrush(BrandBlue, BrandEnd, 135);
        var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new TextBlock { Text = "SHIMIZU", Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)), FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });
        sp.Children.Add(new TextBlock { Text = "Claude Code 安装器", Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });
        sp.Children.Add(new TextBlock { Text = "一键安装 · 永久免费", Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });
        sp.Children.Add(new TextBlock { Text = "v1.0.6", Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center });
        g.Children.Add(sp);
        return g;
    }

    Border OptionCard(string title, string desc, Color accent, bool isSelected, Action onClick, string? badge = null, string? badgeColor = null)
    {
        var b = new Border { CornerRadius = new CornerRadius(12), Background = isSelected ? new SolidColorBrush(Color.FromRgb(238, 242, 255)) : new SolidColorBrush(Color.FromRgb(249, 250, 251)), BorderBrush = isSelected ? new SolidColorBrush(BrandBlue) : new SolidColorBrush(Colors.Transparent), BorderThickness = new Thickness(2), Cursor = Cursors.Hand, Margin = new Thickness(0, 0, 0, 8) };
        var g = new Grid { Margin = new Thickness(16) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        // Radio dot
        var dot = new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(10), BorderBrush = isSelected ? new SolidColorBrush(BrandBlue) : new SolidColorBrush(Color.FromRgb(204, 204, 204)), BorderThickness = new Thickness(2), Background = isSelected ? new SolidColorBrush(BrandBlue) : Brushes.Transparent, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
        if (isSelected) dot.Child = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = Brushes.White };
        Grid.SetColumn(dot, 0); g.Children.Add(dot);
        var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(sp, 1); g.Children.Add(sp);
        var titleSp = new StackPanel { Orientation = Orientation.Horizontal };
        titleSp.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)) });
        if (badge != null)
        {
            var bg = new Border { CornerRadius = new CornerRadius(10), Background = badgeColor == "warn" ? new SolidColorBrush(Color.FromRgb(255, 243, 224)) : new SolidColorBrush(Color.FromRgb(232, 245, 233)), Padding = new Thickness(8, 2, 8, 2), Margin = new Thickness(6, 0, 0, 0) };
            bg.Child = new TextBlock { Text = badge, FontSize = 9, FontWeight = FontWeights.Bold, Foreground = badgeColor == "warn" ? new SolidColorBrush(Color.FromRgb(230, 81, 0)) : new SolidColorBrush(Color.FromRgb(46, 125, 50)) };
            titleSp.Children.Add(bg);
        }
        sp.Children.Add(titleSp);
        sp.Children.Add(new TextBlock { Text = desc, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), FontSize = 11, Margin = new Thickness(0, 2, 0, 0) });
        b.Child = g;
        b.MouseLeftButtonDown += (_, _) => onClick();
        return b;
    }

    Border StatusCard(string name, bool ok, string ver)
    {
        var b = new Border { CornerRadius = new CornerRadius(12), Background = ok ? new SolidColorBrush(Color.FromRgb(240, 253, 244)) : new SolidColorBrush(Color.FromRgb(255, 247, 237)), Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(14, 10, 18, 10) };
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var leftBar = new Border { Width = 3, Background = ok ? new SolidColorBrush(new Color { R = 0, G = 184, B = 77 }) : new SolidColorBrush(new Color { R = 255, G = 107, B = 53 }), CornerRadius = new CornerRadius(2), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-18, -10, 0, -10) };
        Grid.SetColumn(leftBar, 0); g.Children.Add(leftBar);
        var dot = new System.Windows.Shapes.Ellipse { Width = 10, Height = 10, Fill = ok ? new SolidColorBrush(Green) : new SolidColorBrush(Orange), Margin = new Thickness(12, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(dot, 0); g.Children.Add(dot);
        var nameTb = new TextBlock { Text = name, FontWeight = FontWeights.Bold, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(nameTb, 1); g.Children.Add(nameTb);
        var verTb = new TextBlock { Text = ver, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(verTb, 2); g.Children.Add(verTb);
        b.Child = g;
        return b;
    }

    // ── Page 0: Welcome ──────────────────────────────
    Grid BuildPage0()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(BigBanner(), 0); g.Children.Add(BigBanner());

        var content = new StackPanel { Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = "选择安装方式", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Margin = new Thickness(0, 8, 0, 12) });

        var cards = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        cards.ColumnDefinitions.Add(new ColumnDefinition());
        cards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        cards.ColumnDefinitions.Add(new ColumnDefinition());

        var proCard = new Border { CornerRadius = new CornerRadius(14), Background = new SolidColorBrush(Color.FromRgb(240, 244, 255)), Cursor = Cursors.Hand, Padding = new Thickness(20, 20, 20, 16) };
        proCard.Background = new LinearGradientBrush(Color.FromRgb(238, 242, 255), Color.FromRgb(245, 248, 255), 135);
        var proSp = new StackPanel();
        proSp.Children.Add(new TextBlock { Text = "⚙️", FontSize = 28, Margin = new Thickness(0, 0, 0, 8) });
        proSp.Children.Add(new TextBlock { Text = "我是专业用户", FontWeight = FontWeights.Bold, FontSize = 15, Foreground = new SolidColorBrush(BrandBlue) });
        proSp.Children.Add(new TextBlock { Text = "逐步选择 Skills、工具、安全模式、API 配置", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 4, 0, 0) });
        proCard.Child = proSp;
        proCard.MouseLeftButtonDown += (_, _) => { _isSimple = false; Navigate(1); };
        Grid.SetColumn(proCard, 0); cards.Children.Add(proCard);

        var simCard = new Border { CornerRadius = new CornerRadius(14), Background = new SolidColorBrush(Color.FromRgb(236, 253, 245)), Cursor = Cursors.Hand, Padding = new Thickness(20, 20, 20, 16) };
        simCard.Background = new LinearGradientBrush(Color.FromRgb(236, 253, 245), Color.FromRgb(245, 255, 250), 135);
        var simSp = new StackPanel();
        simSp.Children.Add(new TextBlock { Text = "🚀", FontSize = 28, Margin = new Thickness(0, 0, 0, 8) });
        simSp.Children.Add(new TextBlock { Text = "我是小白用户", FontWeight = FontWeights.Bold, FontSize = 15, Foreground = new SolidColorBrush(Green) });
        simSp.Children.Add(new TextBlock { Text = "简易安装 · 快速配置", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 4, 0, 0) });
        simCard.Child = simSp;
        simCard.MouseLeftButtonDown += (_, _) => { _isSimple = true; Navigate(5); };
        Grid.SetColumn(simCard, 2); cards.Children.Add(simCard);
        content.Children.Add(cards);

        content.Children.Add(new TextBlock { Text = "专业模式：完整控制安装过程  |  小白模式：简易安装 · 快速配置", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(150, 160, 175)) });
        Grid.SetRow(content, 1); g.Children.Add(content);
        return g;
    }

    // ── Page 1: Env ──────────────────────────────────
    Grid BuildPage1()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(GradientBanner("1", "环境检测"), 0); g.Children.Add(GradientBanner("1", "环境检测"));

        var c = new StackPanel { Margin = new Thickness(24, 12, 24, 12) };
        var row1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 12) };
        row1.Children.Add(new TextBlock { Text = "语言:", FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        _cmbLang = new ComboBox { Width = 80, Margin = new Thickness(0, 0, 16, 0) }; _cmbLang.Items.Add("中文"); _cmbLang.Items.Add("English"); _cmbLang.SelectedIndex = 0;
        row1.Children.Add(_cmbLang);
        row1.Children.Add(new TextBlock { Text = "安装盘符:", FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        _cmbDrive = new ComboBox { Width = 80, FontWeight = FontWeights.Bold, FontSize = 13, Foreground = new SolidColorBrush(BrandBlue), Background = new SolidColorBrush(Color.FromRgb(240, 244, 255)), Margin = new Thickness(0, 0, 12, 0) };
        foreach (var d in _drives) _cmbDrive.Items.Add(d); _cmbDrive.SelectedIndex = 0;
        _cmbDrive.SelectionChanged += (_, _) => _drive = _cmbDrive.SelectedItem.ToString()!;
        row1.Children.Add(_cmbDrive);
        var btnRe = new Button { Content = "⟳ 重新检测", Width = 90, Height = 28, Background = new SolidColorBrush(Color.FromRgb(80, 90, 110)), Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 11, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
        row1.Children.Add(btnRe);
        c.Children.Add(row1);

        c.Children.Add(StatusCard("Node.js", false, "检测中..."));
        c.Children.Add(StatusCard("Git", false, "检测中..."));
        c.Children.Add(StatusCard("Python", false, "检测中..."));
        c.Children.Add(StatusCard("Claude Code", false, "检测中..."));
        c.Children.Add(new TextBlock { Text = $"路径: Node → {_drive}\\Claude Code tool\\NodeJS  Git → {_drive}\\Claude Code tool\\Git", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 4, 0, 0) });

        Grid.SetRow(c, 1); g.Children.Add(c);
        return g;
    }

    // ── Page 2: Skills ───────────────────────────────
    Grid BuildPage2()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(GradientBanner("2", "Skills 选择"), 0); g.Children.Add(GradientBanner("2", "Skills 选择"));

        var c = new DockPanel { Margin = new Thickness(24, 12, 24, 12) };
        var selectAll = new Button { Content = "☐ 全选 / 取消全选", Padding = new Thickness(14, 6, 14, 6), Background = new SolidColorBrush(Color.FromRgb(238, 242, 255)), Foreground = new SolidColorBrush(BrandBlue), FontWeight = FontWeights.Bold, FontSize = 11, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Left };
        bool allOn = true;
        selectAll.Click += (_, _) => { allOn = !allOn; }; // simplified
        DockPanel.SetDock(selectAll, Dock.Top);
        c.Children.Add(selectAll);

        var sp = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        foreach (var sk in _skills)
        {
            var row = new Border { CornerRadius = new CornerRadius(10), Background = sk.i.StartsWith("genskills--") ? new SolidColorBrush(Color.FromRgb(248, 248, 249)) : new SolidColorBrush(Color.FromRgb(250, 251, 252)), Padding = new Thickness(10, 8, 14, 8), Margin = new Thickness(0, 0, 0, 4), Cursor = sk.i.StartsWith("genskills--") ? Cursors.Arrow : Cursors.Hand, Opacity = sk.i.StartsWith("genskills--") ? 0.55 : 1.0 };
            var rowGrid = new Grid { Margin = new Thickness(4, 0, 0, 0) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var chk = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(4), Background = sk.i.StartsWith("genskills--") ? new SolidColorBrush(Color.FromRgb(229, 231, 235)) : new SolidColorBrush(BrandBlue), BorderBrush = sk.i.StartsWith("genskills--") ? new SolidColorBrush(Color.FromRgb(204, 204, 204)) : new SolidColorBrush(BrandBlue), BorderThickness = new Thickness(2), VerticalAlignment = VerticalAlignment.Center };
            if (!sk.i.StartsWith("genskills--")) chk.Child = new TextBlock { Text = "✓", Foreground = Brushes.White, FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(chk, 0); rowGrid.Children.Add(chk);
            rowGrid.Children.Add(new TextBlock { Text = sk.n, FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
            rowGrid.Children.Add(new TextBlock { Text = sk.d, FontSize = 10, Foreground = sk.i.StartsWith("genskills--") ? new SolidColorBrush(Color.FromRgb(180, 185, 195)) : new SolidColorBrush(Color.FromRgb(107, 114, 128)), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, TextTrimming = TextTrimming.CharacterEllipsis });
            row.Child = rowGrid;
            sp.Children.Add(row);
        }
        var sv = new ScrollViewer { Content = sp, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 8, 0, 0) };
        c.Children.Add(sv);
        Grid.SetRow(c, 1); g.Children.Add(c);
        return g;
    }

    // ── Page 3: Tools ────────────────────────────────
    Grid BuildPage3()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(GradientBanner("3", "工具与逻辑"), 0); g.Children.Add(GradientBanner("3", "工具与逻辑"));

        var c = new StackPanel { Margin = new Thickness(24, 12, 24, 12) };
        _chkTools = new CheckBox { Content = "安装截图操作工具 (Python)", FontWeight = FontWeights.Bold, FontSize = 13, IsChecked = true, Margin = new Thickness(0, 0, 0, 4) };
        c.Children.Add(_chkTools);
        c.Children.Add(new TextBlock { Text = "scr / ocr / see / act / browser — 屏幕截图、OCR、鼠标键盘、浏览器自动化", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(24, 0, 0, 16) });
        _chkLogic = new CheckBox { Content = "添加截图辅助逻辑 (写入 CLAUDE.md)", FontWeight = FontWeights.Bold, FontSize = 13, IsChecked = true, Margin = new Thickness(0, 0, 0, 4) };
        c.Children.Add(_chkLogic);
        _txtLogic = new TextBox { Text = "当用户请求不清晰时，主动询问是否需要截图查看。", IsReadOnly = true, Height = 46, Margin = new Thickness(24, 0, 0, 16), Background = new SolidColorBrush(Color.FromRgb(248, 250, 253)), BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 238)), FontFamily = new FontFamily("Consolas"), FontSize = 10.5, TextWrapping = TextWrapping.Wrap };
        c.Children.Add(_txtLogic);
        _chkCustomLogic = new CheckBox { Content = "为 Claude Code 添加底层逻辑", FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 0, 0, 4) };
        c.Children.Add(_chkCustomLogic);
        c.Children.Add(new TextBlock { Text = "自定义 CLAUDE.md 内容，控制 Claude Code 行为", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(24, 0, 0, 16) });
        Grid.SetRow(c, 1); g.Children.Add(c);
        return g;
    }

    // ── Page 4: Security ─────────────────────────────
    Grid BuildPage4()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(GradientBanner("4", "安全模式"), 0); g.Children.Add(GradientBanner("4", "安全模式"));
        var c = new StackPanel { Margin = new Thickness(24, 16, 24, 12) };
        _rbSafe = new RadioButton();
        _rbPro = new RadioButton { IsChecked = true };
        c.Children.Add(OptionCard("安全模式", "高威胁操作需要用户确认", Orange, false, () => { _rbSafe.IsChecked = true; _rbPro.IsChecked = false; RefreshPage4(); }));
        c.Children.Add(OptionCard("专业通行模式 (推荐)", "所有操作无需确认 — 适合高级用户", Green, true, () => { _rbPro.IsChecked = true; _rbSafe.IsChecked = false; RefreshPage4(); }));
        Grid.SetRow(c, 1); g.Children.Add(c);
        return g;
    }
    void RefreshPage4() { Navigate(4); }

    // ── Page 5: API ──────────────────────────────────
    Grid BuildPage5()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var sub = _isSimple ? "第 1 步 / 共 3 步" : null;
        Grid.SetRow(GradientBanner(_isSimple ? "1" : "5", "API 配置", sub), 0); g.Children.Add(GradientBanner(_isSimple ? "1" : "5", "API 配置", sub));

        var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(24, 12, 24, 12) };
        var c = new StackPanel();
        c.Children.Add(OptionCard("Anthropic API", "需要 Claude Code 账号登录，国内访问可能受限", BrandBlue, _selectedApi == "anthropic", () => { _selectedApi = "anthropic"; Navigate(5); }, "不推荐 · 需登录账号", "warn"));
        c.Children.Add(OptionCard("DeepSeek", "deepseek-v4-pro[1m] · 4个模型槽预填 · subagent 用 v4-flash", BrandBlue, _selectedApi == "deepseek", () => { _selectedApi = "deepseek"; Navigate(5); }, "推荐", "rec"));
        c.Children.Add(OptionCard("自定义选择模型", "自行填入 API 密钥、请求地址、模型映射（类似 CC Switch）", BrandBlue, _selectedApi == "custom", () => { _selectedApi = "custom"; Navigate(5); }));

        if (_selectedApi == "deepseek")
        {
            var box = new Border { CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Color.FromRgb(250, 251, 252)), Padding = new Thickness(16), Margin = new Thickness(0, 8, 0, 0), BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 255)), BorderThickness = new Thickness(1.5) };
            var dsSp = new StackPanel();
            dsSp.Children.Add(new TextBlock { Text = "API Key *必填", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Margin = new Thickness(0, 0, 0, 4) });
            _dsKey = new TextBox { Width = 400, Height = 32, Margin = new Thickness(0, 0, 0, 8) };
            dsSp.Children.Add(_dsKey);
            dsSp.Children.Add(new TextBlock { Text = "预设模型: deepseek-v4-pro[1m] (主/Opus/Sonnet) + deepseek-v4-flash (Haiku/Subagent)", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)) });
            box.Child = dsSp; c.Children.Add(box);
        }
        else if (_selectedApi == "custom")
        {
            var box = new Border { CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Color.FromRgb(250, 251, 252)), Padding = new Thickness(16), Margin = new Thickness(0, 8, 0, 0), BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 255)), BorderThickness = new Thickness(1.5) };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            int r = 0;
            void AddField(string label, bool full, out TextBox tb, string ph = "")
            {
                var sp = new StackPanel { Margin = new Thickness(0, 0, full ? 0 : 4, 8) };
                sp.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Margin = new Thickness(0, 0, 0, 4) });
                tb = new TextBox { Height = 32 };
                if (!string.IsNullOrEmpty(ph)) tb.Text = ph;
                sp.Children.Add(tb);
                if (full) { Grid.SetColumnSpan(sp, 2); }
                Grid.SetRow(sp, r); grid.Children.Add(sp); r++;
            }
            AddField("API Key *必填", true, out _custKey);
            AddField("请求地址 *必填", true, out _custUrl, "");
            var hintUrl = new TextBlock { Text = "无需输入 https://，自动补全", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, -4, 0, 8) };
            Grid.SetRow(hintUrl, r); grid.Children.Add(hintUrl); r++;
            // Section
            var sec = new TextBlock { Text = "映射模型（以下 4 项必填）", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 4, 0, 4) };
            Grid.SetColumnSpan(sec, 2); Grid.SetRow(sec, r); grid.Children.Add(sec); r++;
            AddField("主模型 *必填", false, out _custMain);
            Grid.SetColumn(_custHaiku = new TextBox { Height = 32 }, 1);
            {
                var sp = new StackPanel { Margin = new Thickness(4, 0, 0, 8) };
                sp.Children.Add(new TextBlock { Text = "Haiku 默认模型 *必填", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Margin = new Thickness(0, 0, 0, 4) });
                sp.Children.Add(_custHaiku);
                Grid.SetColumn(sp, 1); Grid.SetRow(sp, r); grid.Children.Add(sp);
            }
            r++;
            AddField("Sonnet 默认模型 *必填", false, out _custSonnet);
            Grid.SetColumn(_custOpus = new TextBox { Height = 32 }, 1);
            {
                var sp = new StackPanel { Margin = new Thickness(4, 0, 0, 8) };
                sp.Children.Add(new TextBlock { Text = "Opus 默认模型 *必填", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Margin = new Thickness(0, 0, 0, 4) });
                sp.Children.Add(_custOpus);
                Grid.SetColumn(sp, 1); Grid.SetRow(sp, r); grid.Children.Add(sp);
            }
            r++;
            _chkSubagent = new CheckBox { Content = "配置 Subagent 模型 (选填)", FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) };
            Grid.SetColumnSpan(_chkSubagent, 2); Grid.SetRow(_chkSubagent, r); grid.Children.Add(_chkSubagent); r++;
            _custSub = new TextBox { Height = 32, IsEnabled = false, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetColumnSpan(_custSub, 2); Grid.SetRow(_custSub, r); grid.Children.Add(_custSub); r++;
            _chkSubagent.Checked += (_, _) => _custSub.IsEnabled = true;
            _chkSubagent.Unchecked += (_, _) => _custSub.IsEnabled = false;

            box.Child = grid; c.Children.Add(box);
        }
        sv.Content = c;
        Grid.SetRow(sv, 1); g.Children.Add(sv);
        return g;
    }

    // ── Page 6: Install Config ───────────────────────
    Grid BuildPage6()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var sub = _isSimple ? "第 2 步 / 共 3 步" : null;
        Grid.SetRow(GradientBanner(_isSimple ? "2" : "6", "安装配置", sub), 0); g.Children.Add(GradientBanner(_isSimple ? "2" : "6", "安装配置", sub));

        var c = new StackPanel { Margin = new Thickness(24, 12, 24, 12) };
        c.Children.Add(new TextBlock { Text = "选择安装位置", FontWeight = FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Margin = new Thickness(0, 0, 0, 12) });
        var driveRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        driveRow.Children.Add(new TextBlock { Text = "默认盘符:", FontWeight = FontWeights.Bold, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        _cmbDrive6 = new ComboBox { Width = 80, FontWeight = FontWeights.Bold, FontSize = 14, Foreground = new SolidColorBrush(BrandBlue), Background = new SolidColorBrush(Color.FromRgb(240, 244, 255)) };
        foreach (var d in _drives) _cmbDrive6.Items.Add(d); _cmbDrive6.SelectedIndex = 0;
        _cmbDrive6.SelectionChanged += (_, _) => { _drive = _cmbDrive6.SelectedItem.ToString()!; RefreshPathHint(); };
        driveRow.Children.Add(_cmbDrive6);
        c.Children.Add(driveRow);

        _pathHint = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 16) };
        RefreshPathHint();
        c.Children.Add(_pathHint);

        _chkCustomPaths = new CheckBox { Content = "工具自定义安装位置", FontWeight = FontWeights.Bold, FontSize = 12, Margin = new Thickness(0, 8, 0, 4) };
        _chkCustomPaths.Checked += (_, _) => { _showCustomPaths = true; Navigate(6); };
        _chkCustomPaths.Unchecked += (_, _) => { _showCustomPaths = false; Navigate(6); };
        c.Children.Add(_chkCustomPaths);
        c.Children.Add(new TextBlock { Text = "单独指定每个工具安装到哪个盘符", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 8) });

        if (_showCustomPaths)
        {
            var box = new Border { CornerRadius = new CornerRadius(12), Background = new SolidColorBrush(Color.FromRgb(250, 251, 252)), Padding = new Thickness(16), BorderBrush = new SolidColorBrush(Color.FromRgb(238, 238, 255)), BorderThickness = new Thickness(1.5) };
            var sp = new StackPanel();
            (string id, string name, string sub)[] tools = { ("node","Node.js","NodeJS"), ("git","Git","Git"), ("npm","npm global","npm-global"), ("cctools","Claude Code 工具","cc-tools"), ("python","Python","Python312"), ("tesseract","Tesseract OCR","Tesseract-OCR") };
            foreach (var t in tools)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
                row.Children.Add(new TextBlock { Text = t.name, FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Width = 110, VerticalAlignment = VerticalAlignment.Center });
                var sel = new ComboBox { Width = 70, Margin = new Thickness(0, 0, 8, 0) };
                foreach (var d in _drives) sel.Items.Add(d);
                sel.SelectedItem = _toolDrives.ContainsKey(t.id) ? _toolDrives[t.id] : _drive;
                sel.SelectionChanged += (s, _) => { _toolDrives[t.id] = ((ComboBox)s!).SelectedItem.ToString()!; RefreshPathHint(); };
                row.Children.Add(sel);
                var pathTb = new TextBlock { Text = $"→ {sel.SelectedItem}\\Claude Code tool\\{t.sub}", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(pathTb);
                sp.Children.Add(row);
            }
            box.Child = sp; c.Children.Add(box);
        }

        if (!_isSimple) c.Children.Add(new TextBlock { Text = "点击下一步将自动配置所有选项并开始安装", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(150, 160, 175)), Margin = new Thickness(0, 12, 0, 0) });
        Grid.SetRow(c, 1); g.Children.Add(c);
        return g;
    }

    void RefreshPathHint()
    {
        if (_pathHint == null) return;
        var tools = new[] { ("Node.js","v20.18.0","NodeJS"), ("Git","2.45.2","Git"), ("npm global","","npm-global"), ("Claude Code","","cc-tools"), ("Python 3.12","","Python312"), ("Tesseract OCR","","Tesseract-OCR") };
        var lines = new List<string> { "所有工具将安装在所选盘符:" };
        foreach (var t in tools)
        {
            var d = _showCustomPaths && _toolDrives.ContainsKey(t.Item3.ToLower()) ? _toolDrives[t.Item3.ToLower()] : _drive;
            var ver = string.IsNullOrEmpty(t.Item2) ? "" : $" {t.Item2}";
            lines.Add($"  {t.Item1}{ver} → {d}\\Claude Code tool\\{t.Item3}");
        }
        _pathHint.Text = string.Join("\n", lines);
    }

    // ── Page 7: Install ──────────────────────────────
    Grid BuildPage7()
    {
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var sub = _isSimple ? "第 3 步 / 共 3 步" : null;
        var badge = _isSimple ? "3" : "7";
        Grid.SetRow(GradientBanner(badge, "安装", sub), 0); g.Children.Add(GradientBanner(badge, "安装", sub));

        var c = new StackPanel { Margin = new Thickness(24, 12, 24, 12) };
        var checks = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        _chkThinking = new CheckBox { Content = "启用最大强度思考", IsChecked = true, FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 24, 0) };
        _chkNoUpdate = new CheckBox { Content = "禁用自动升级", FontSize = 12, FontWeight = FontWeights.Bold };
        checks.Children.Add(_chkThinking); checks.Children.Add(_chkNoUpdate);
        c.Children.Add(checks);

        _btnInstall = new Button { Content = "开始安装", Height = 46, Background = new LinearGradientBrush(Green, Color.FromRgb(0, 209, 102), 135), Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 15, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
        _btnInstall.Click += async (_, _) => await DoInstall();
        c.Children.Add(_btnInstall);

        _bar = new ProgressBar { Height = 6, Margin = new Thickness(0, 8, 0, 8), Foreground = new SolidColorBrush(BrandBlue) };
        c.Children.Add(_bar);

        _termLog = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(169, 177, 214)), Background = new SolidColorBrush(Color.FromRgb(26, 27, 38)), Padding = new Thickness(16), Height = 340, TextWrapping = TextWrapping.Wrap };
        c.Children.Add(_termLog);
        Grid.SetRow(c, 1); g.Children.Add(c);
        return g;
    }

    // ═══ NAVIGATION ═════════════════════════════════
    void Navigate(int step)
    {
        _step = step;
        // Rebuild pages that have dynamic state
        if (step == 4) _pages[4] = BuildPage4();
        if (step == 5) _pages[5] = BuildPage5();
        if (step == 6) _pages[6] = BuildPage6();
        if (step == 7) _pages[7] = BuildPage7();

        PageHost.Content = _pages[step];

        NavBar.Visibility = step == 0 ? Visibility.Collapsed : Visibility.Visible;
        BtnBack.Visibility = Visibility.Visible;
        BtnNext.Visibility = step == 7 ? Visibility.Collapsed : Visibility.Visible;
    }

    void OnBack(object s, RoutedEventArgs e)
    {
        if (_installing) return;
        if (_isSimple)
        {
            if (_step == 5) Navigate(0);
            else if (_step == 6) Navigate(5);
            else if (_step == 7) Navigate(6);
        }
        else
        {
            if (_step > 0) Navigate(_step - 1);
        }
    }

    void OnNext(object s, RoutedEventArgs e)
    {
        if (_installing) return;
        if (_step == 5 && !ValidateApi()) return;
        if (_isSimple) { if (_step == 5) Navigate(6); else if (_step == 6) Navigate(7); }
        else { if (_step < 6) Navigate(_step + 1); else if (_step == 6) Navigate(7); }
    }

    // ═══ API VALIDATION ═════════════════════════════
    bool ValidateApi()
    {
        if (_selectedApi == "anthropic") return true;
        if (_selectedApi == "deepseek")
        {
            if (string.IsNullOrWhiteSpace(_dsKey?.Text)) { ShowToast("请输入 DeepSeek API Key"); return false; }
            return true;
        }
        if (_selectedApi == "custom")
        {
            if (string.IsNullOrWhiteSpace(_custKey?.Text)) { ShowToast("请输入 API Key"); return false; }
            if (string.IsNullOrWhiteSpace(_custUrl?.Text)) { ShowToast("请输入请求地址"); return false; }
            if (string.IsNullOrWhiteSpace(_custMain?.Text)) { ShowToast("请输入主模型名称"); return false; }
            if (string.IsNullOrWhiteSpace(_custHaiku?.Text)) { ShowToast("请输入 Haiku 默认模型"); return false; }
            if (string.IsNullOrWhiteSpace(_custSonnet?.Text)) { ShowToast("请输入 Sonnet 默认模型"); return false; }
            if (string.IsNullOrWhiteSpace(_custOpus?.Text)) { ShowToast("请输入 Opus 默认模型"); return false; }
            return true;
        }
        return true;
    }

    void ShowToast(string msg)
    {
        var toast = new Border
        {
            CornerRadius = new CornerRadius(10), Background = new SolidColorBrush(Color.FromRgb(254, 242, 242)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(254, 202, 202)), BorderThickness = new Thickness(1),
            Padding = new Thickness(24, 12, 24, 12), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 20, 0, 0)
        };
        toast.Child = new TextBlock { Text = $"⚠ {msg}", Foreground = new SolidColorBrush(Color.FromRgb(153, 27, 27)), FontSize = 13, FontWeight = FontWeights.Bold };
        var overlay = new Grid();
        overlay.Children.Add(toast);
        var mainGrid = Content as Grid;
        if (mainGrid != null)
        {
            Grid.SetRowSpan(overlay, 3);
            mainGrid.Children.Add(overlay);
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            timer.Tick += (_, _) => { mainGrid.Children.Remove(overlay); timer.Stop(); };
            timer.Start();
        }
    }

    // ═══ INSTALL ════════════════════════════════════
    async Task DoInstall()
    {
        if (_installing) return;
        _installing = true; _btnInstall.IsEnabled = false; _btnInstall.Content = "安装中...";
        _chkThinking.IsEnabled = false; _chkNoUpdate.IsEnabled = false;
        BtnBack.IsEnabled = false; BtnNext.Visibility = Visibility.Collapsed;
        _bar.Value = 0; _termLog.Text = "";

        var log = (Action<string>)(s => Dispatcher.Invoke(() => { _termLog.Text += s + "\n"; }));
        var prog = (Action<int, int>)((c, t) => Dispatcher.Invoke(() => { _bar.Maximum = t; _bar.Value = Math.Min(c, t); }));

        var settings = new InstallSettings
        {
            IsSimple = _isSimple, Drive = _drive,
            NodePath = Path.Combine(_drive, "Claude Code tool", "NodeJS"),
            GitPath = Path.Combine(_drive, "Claude Code tool", "Git"),
            NpmPrefix = Path.Combine(_drive, "Claude Code tool", "npm-global"),
            ToolsPath = Path.Combine(_drive, "Claude Code tool", "cc-tools"),
            PythonPath = Path.Combine(_drive, "Claude Code tool", "Python312"),
            TesseractPath = Path.Combine(_drive, "Claude Code tool", "Tesseract-OCR"),
            PipPath = Path.Combine(_drive, "Python-packages"),
            InstallTools = _chkTools.IsChecked == true,
            InstallLogic = _chkLogic.IsChecked == true,
            InstallCustomLogic = _chkCustomLogic.IsChecked == true,
            CustomLogicContent = _claudeMdContent,
            BypassPermissions = _rbPro?.IsChecked ?? true,
            ApiProvider = _selectedApi,
            ApiKey = _selectedApi == "deepseek" ? _dsKey?.Text ?? "" : _custKey?.Text ?? "",
            ApiBaseUrl = _custUrl?.Text ?? "",
            CustomMainModel = _custMain?.Text ?? "",
            CustomHaikuModel = _custHaiku?.Text ?? "",
            CustomSonnetModel = _custSonnet?.Text ?? "",
            CustomOpusModel = _custOpus?.Text ?? "",
            CustomSubagentModel = _custSub?.Text ?? "",
            MaxThinking = _chkThinking.IsChecked == true,
            NoUpdate = _chkNoUpdate.IsChecked == true,
            Skills = _selectedSkills,
        };

        var engine = new InstallEngine(settings, log, prog);
        try
        {
            await engine.DoInstallAsync();
            log("\n═══════════════════════════════════");
            log("  ✅ 安装完成!");
            log("═══════════════════════════════════");
            ShowCompletionModal(settings);
        }
        catch (Exception ex)
        {
            log($"\n!!! 错误: {ex.Message}");
            MessageBox.Show(ex.Message, "安装失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _installing = false;
            Dispatcher.Invoke(() => { BtnBack.IsEnabled = true; });
        }
    }

    void ShowCompletionModal(InstallSettings s)
    {
        var overlay = new Grid { Background = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0)) };
        var box = new Border { CornerRadius = new CornerRadius(16), Background = Brushes.White, Padding = new Thickness(32), MaxWidth = 500, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = "✅ 安装完成!", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Green), Margin = new Thickness(0, 0, 0, 4) });
        sp.Children.Add(new TextBlock { Text = "Claude Code Installer v1.0.6 · Shimizu", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 16) });

        var installed = new StackPanel();
        installed.Children.Add(new TextBlock { Text = "已安装工具", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(BrandBlue), Margin = new Thickness(0, 4, 0, 4) });
        var tools = new[] { ("Node.js v20.18.0", s.NodePath), ("Git 2.45.2", s.GitPath), ("npm global", s.NpmPrefix), ("Claude Code", s.ToolsPath), ("Python 3.12", s.PythonPath), ("Tesseract OCR 5.3.3", s.TesseractPath) };
        foreach (var t in tools) installed.Children.Add(new TextBlock { Text = $"{t.Item1} → {t.Item2}", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), Margin = new Thickness(0, 0, 0, 2) });
        sp.Children.Add(installed);

        if (s.Skills.Count > 0)
        {
            sp.Children.Add(new TextBlock { Text = $"已安装 Skills ({s.Skills.Count})", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(BrandBlue), Margin = new Thickness(0, 12, 0, 4) });
            sp.Children.Add(new TextBlock { Text = string.Join(", ", s.Skills), FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)), TextWrapping = TextWrapping.Wrap });
        }

        sp.Children.Add(new TextBlock { Text = "配置", FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(BrandBlue), Margin = new Thickness(0, 12, 0, 4) });
        sp.Children.Add(new TextBlock { Text = "settings.json → ~/.claude/", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)) });
        if (s.InstallLogic || s.InstallCustomLogic) sp.Children.Add(new TextBlock { Text = "CLAUDE.md → ~/", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)) });
        sp.Children.Add(new TextBlock { Text = "桌面快捷方式: Claude Code / CC Switch", FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(26, 26, 46)) });

        var btnOk = new Button { Content = "完成", Height = 40, Background = new SolidColorBrush(BrandBlue), Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 13, BorderThickness = new Thickness(0), Cursor = Cursors.Hand, Margin = new Thickness(0, 20, 0, 0) };
        btnOk.Click += (_, _) => { var mainGrid = Content as Grid; if (mainGrid != null) { var ov = mainGrid.Children.OfType<Grid>().FirstOrDefault(g => g.Background is SolidColorBrush b && b.Color.A == 102); if (ov != null) mainGrid.Children.Remove(ov); } };
        sp.Children.Add(btnOk);

        box.Child = sp; overlay.Children.Add(box);
        var mainGrid2 = Content as Grid;
        if (mainGrid2 != null) { Grid.SetRowSpan(overlay, 3); mainGrid2.Children.Add(overlay); }
    }
}
