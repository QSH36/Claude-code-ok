using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClaudeCodeInstaller;

public partial class Form1 : Form
{
    string _drive = "C:";
    string NodePath => Path.Combine(_drive, "NodeJS");
    string GitPath => Path.Combine(_drive, "Git");
    string NpmPrefix => Path.Combine(_drive, "npm-global");
    string ToolsPath => Path.Combine(_drive, "cc-tools");
    string ClaudeConfigDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
    string _pythonPath = "python";
    string PipPath => Path.Combine(_drive, "Python-packages");
    bool _nodeOk, _gitOk, _pythonOk, _claudeOk, _installing;
    bool _isSimple;

    Panel[] _pages;
    Panel _navBar;
    Button _btnBack, _btnNext;
    int _step;

    ComboBox _cmbDrive, _cmbLang;
    Label _lblNode, _lblGit, _lblPython, _lblClaude, _lblPath;
    CheckBox[] _skillChecks; CheckBox _chkSelectAll;
    CheckBox _chkTools, _chkLogic, _chkThinking, _chkNoUpdate;
    RadioButton _rbSafe, _rbPro, _rbApiYes, _rbApiNo;
    TextBox _txtApiKey, _txtLogic;
    ProgressBar _bar; RichTextBox _rtbLog;

    // 小白 simplified install page controls
    ComboBox _cmbDriveSimple;
    Button _btnInstallSimple;
    Label _lblSimplePath;

    readonly (string n, string d, string i)[] _skills =
    {
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

    public Form1()
    {
        Text = "Claude Code 安装器 v7";
        Size = new Size(800, 620);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(245, 247, 250);
        BuildWizard();
        this.Shown += (_, _) => DetectEnv();
    }

    void BuildWizard()
    {
        // Nav bar (hidden on page 0)
        _navBar = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(248, 250, 253) };
        _btnBack = new Button { Text = "← 上一步", Left = 16, Top = 8, Width = 90, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(140, 145, 155), ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false };
        _btnBack.Click += (_, _) => GoStep(_step - 1);
        _btnNext = new Button { Text = "下一步 →", Left = 114, Top = 8, Width = 105, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false };
        _btnNext.Click += DoNext;
        _navBar.Controls.Add(_btnBack); _navBar.Controls.Add(_btnNext);
        Controls.Add(_navBar);

        _pages = new Panel[7]; // 0=user type, 1=env, 2=skills, 3=tools, 4=security, 5=api, 6=install
        _pages[0] = BuildPage0();
        _pages[1] = BuildPage1();
        _pages[2] = BuildPage2();
        _pages[3] = BuildPage3();
        _pages[4] = BuildPage4();
        _pages[5] = BuildPage5();
        _pages[6] = BuildPage6();
        foreach (var pg in _pages) { pg.Dock = DockStyle.Fill; pg.BackColor = Color.White; pg.Visible = false; pg.AutoScroll = true; pg.Padding = new Padding(20, 12, 20, 12); Controls.Add(pg); }

        _step = 0; GoStep(0);
    }

    void GoStep(int s)
    {
        if (s < 0 || s >= _pages.Length) return;
        _step = s;
        for (int i = 0; i < _pages.Length; i++) _pages[i].Visible = (i == s);

        // Hide nav bar on page 0 (user type selection) and for 小白 simplified install
        bool hideNav = (s == 0);
        _navBar.Visible = !hideNav;
        _btnBack.Visible = s > 0 && !hideNav;

        // For page 6 (install page), the Next button becomes "开始安装"
        // For 小白, page 6 gets a dedicated button, nav Next also works as backup
        _btnNext.Visible = (s > 0 && s < 6); // only show on pages 1-5
        if (s == 6) _btnNext.Visible = false; // page 6 has its own install button

        // For 小白 simplified: after clicking, jump to page 6
        if (_isSimple && s == 6)
        {
            // Pre-configure everything for 小白
            _chkTools.Checked = true;
            _chkLogic.Checked = true;
            _chkThinking.Checked = true;
            _chkNoUpdate.Checked = false; // auto-update ON
            _rbPro.Checked = true;
            _rbApiNo.Checked = true;
            foreach (var c in _skillChecks) c.Checked = true;
        }
    }

    void DoNext(object? _, EventArgs __)
    {
        if (_isSimple) { GoStep(6); return; }
        GoStep(_step + 1);
    }

    // ═══ PAGE 0: 用户类型选择 (no nav bar) ═══════════
    Panel BuildPage0()
    {
        var p = new Panel();
        p.Controls.Add(L("欢迎使用 Claude Code 安装器", 0, 40, 16, FontStyle.Bold, Color.FromArgb(30, 40, 55)));
        p.Controls.Add(L("请选择你的使用方式", 0, 85, 11, FontStyle.Regular, Color.FromArgb(100, 110, 125)));

        // Pro button
        var bPro = new Button { Text = "我是专业用户\r\n逐步选择安装选项", Left = 50, Top = 150, Width = 300, Height = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, Font = new Font(Font.FontFamily, 13F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        bPro.Click += (_, _) => { _isSimple = false; GoStep(1); };
        p.Controls.Add(bPro);
        p.Controls.Add(L("逐步选择 Skills、工具、安全模式、API 配置", 50, 275, 8, FontStyle.Regular, Color.FromArgb(140, 150, 165)));

        // Simple button - jumps DIRECTLY to install page
        var bSimple = new Button { Text = "我是小白用户\r\n一键安装 · 无需配置", Left = 410, Top = 150, Width = 300, Height = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 170, 80), ForeColor = Color.White, Font = new Font(Font.FontFamily, 13F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        bSimple.Click += (_, _) =>
        {
            _isSimple = true;
            // Pre-set all defaults for 小白
            _rbPro.Checked = true;
            _rbApiNo.Checked = true;
            _chkTools.Checked = true;
            _chkLogic.Checked = true;
            _chkThinking.Checked = true;
            _chkNoUpdate.Checked = false; // auto-update ON
            // Skills are all checked by default
            // Jump to simplified install page
            GoStep(6);
        };
        p.Controls.Add(bSimple);
        p.Controls.Add(L("仅选择安装位置 → 一键安装，全部自动配置", 410, 275, 8, FontStyle.Regular, Color.FromArgb(140, 150, 165)));

        return p;
    }

    // ═══ PAGE 1: 环境检测 ═════════════════════════════
    Panel BuildPage1()
    {
        var p = new Panel();
        int y = 4;
        p.Controls.Add(L("语言:", 0, y + 3, 9, FontStyle.Bold, Color.FromArgb(60, 68, 80)));
        _cmbLang = new ComboBox { Left = 44, Top = y, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbLang.Items.AddRange(new[] { "中文", "English" }); _cmbLang.SelectedIndex = 0;
        _cmbLang.SelectedIndexChanged += (_, _) => { Locale.Lang = _cmbLang.Text == "English" ? "en" : "zh"; RefreshLocale(); };
        p.Controls.Add(_cmbLang);

        p.Controls.Add(L("安装盘符:", 140, y + 3, 9, FontStyle.Bold, Color.FromArgb(60, 68, 80)));
        _cmbDrive = new ComboBox { Left = 220, Top = y, Width = 90, Font = new Font(Font.FontFamily, 11F, FontStyle.Bold), DropDownStyle = ComboBoxStyle.DropDownList };
        LoadDrives(_cmbDrive);
        _cmbDrive.SelectedIndexChanged += (_, _) => { _drive = _cmbDrive.Text; DetectEnv(); };
        p.Controls.Add(_cmbDrive);

        p.Controls.Add(NavButton("重新检测", 320, y, 80, 28, Color.FromArgb(70, 80, 90), (_, _) => DetectEnv()));

        y += 38;
        _lblNode = AddCard(p, "Node.js", ref y);
        _lblGit = AddCard(p, "Git", ref y);
        _lblPython = AddCard(p, "Python", ref y);
        _lblClaude = AddCard(p, "Claude Code", ref y);
        _lblPath = L("", 0, y + 4, 8, FontStyle.Regular, Color.FromArgb(140, 150, 165));
        _lblPath.MaximumSize = new Size(740, 36);
        p.Controls.Add(_lblPath);
        return p;
    }

    Label AddCard(Panel p, string title, ref int y)
    {
        var c = new Panel { Left = 0, Top = y, Width = 740, Height = 40, BackColor = Color.FromArgb(250, 252, 254) };
        c.Controls.Add(L("●", 12, 10, 8, FontStyle.Regular, Color.FromArgb(190, 200, 210)));
        c.Controls.Add(L(title, 34, 9, 10, FontStyle.Bold, Color.FromArgb(50, 55, 65)));
        var s = L("检测中...", 180, 11, 9, FontStyle.Regular, Color.Gray); c.Controls.Add(s);
        p.Controls.Add(c); y += 44; return s;
    }

    // ═══ PAGE 2: Skills ═══════════════════════════════
    Panel BuildPage2()
    {
        var p = new Panel();
        _chkSelectAll = new CheckBox { Text = "全选/取消全选", Left = 0, Top = 0, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 100, 200) };
        _chkSelectAll.CheckedChanged += (_, _) => { bool a = _chkSelectAll.Checked; foreach (var c in _skillChecks) c.Checked = a; };
        p.Controls.Add(_chkSelectAll);
        var pl = new FlowLayoutPanel { Left = 0, Top = 28, Width = 740, Height = 470, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _skillChecks = new CheckBox[_skills.Length];
        for (int i = 0; i < _skills.Length; i++)
        {
            var sk = _skills[i];
            var r = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0, 2, 0, 2) };
            var cb = new CheckBox { Text = sk.n, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), Padding = new Padding(4, 3, 0, 0) };
            r.Controls.Add(cb); r.Controls.Add(L(sk.d, 4, 0, 8.5F, FontStyle.Regular, Color.FromArgb(130, 140, 155)));
            _skillChecks[i] = cb; pl.Controls.Add(r);
        }
        p.Controls.Add(pl);
        return p;
    }

    // ═══ PAGE 3: 工具 ═════════════════════════════════
    Panel BuildPage3()
    {
        var p = new Panel();
        _chkTools = new CheckBox { Text = "安装截图操作工具 (Python: scr, ocr, see, act, browser)", Left = 0, Top = 8, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        p.Controls.Add(_chkTools);
        p.Controls.Add(L("包含屏幕截图、OCR、鼠标键盘、浏览器自动化。需要 Python+Tesseract。", 24, 32, 8, FontStyle.Regular, Color.FromArgb(130, 140, 155)));
        _chkLogic = new CheckBox { Text = "添加截图辅助逻辑 (写入 CLAUDE.md)", Left = 0, Top = 84, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        p.Controls.Add(_chkLogic);
        _txtLogic = new TextBox { Left = 24, Top = 114, Width = 700, Height = 55, Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(248, 250, 253), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 8.5F), Text = "规则: 当用户请求不清晰时，主动询问是否需要截图查看。优先于猜测行为。" };
        p.Controls.Add(_txtLogic);
        return p;
    }

    // ═══ PAGE 4: 安全 ═════════════════════════════════
    Panel BuildPage4()
    {
        var p = new Panel();
        _rbSafe = new RadioButton { Text = "安全模式 — 高威胁操作需要用户确认", Left = 4, Top = 12, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        _rbPro = new RadioButton { Text = "专业通行模式 — 所有操作无需确认 (推荐)", Left = 4, Top = 44, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), Checked = true };
        p.Controls.Add(_rbSafe); p.Controls.Add(_rbPro);
        return p;
    }

    // ═══ PAGE 5: API ══════════════════════════════════
    Panel BuildPage5()
    {
        var p = new Panel();
        _rbApiNo = new RadioButton { Text = "使用默认 Anthropic API", Left = 4, Top = 12, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), Checked = true };
        _rbApiYes = new RadioButton { Text = "切换到 DeepSeek (deepseek-v4-pro[1m])", Left = 4, Top = 44, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        _rbApiYes.CheckedChanged += (_, _) => _txtApiKey.Enabled = _rbApiYes.Checked;
        p.Controls.Add(_rbApiNo); p.Controls.Add(_rbApiYes);
        p.Controls.Add(L("API Key:", 24, 78, 9, FontStyle.Bold, Color.FromArgb(60, 68, 80)));
        _txtApiKey = new TextBox { Left = 100, Top = 75, Width = 400, PasswordChar = '*', Enabled = false };
        p.Controls.Add(_txtApiKey);
        p.Controls.Add(L("预设: deepseek-v4-pro[1m] (4个模型槽位全部预填)", 24, 104, 8, FontStyle.Regular, Color.FromArgb(130, 140, 155)));
        return p;
    }

    // ═══ PAGE 6: 安装 ═════════════════════════════════
    Panel BuildPage6()
    {
        var p = new Panel();

        // 专业用户: show options
        if (!_isSimple)
        {
            _chkThinking = new CheckBox { Text = "启用最大强度思考", Left = 0, Top = 4, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold) };
            _chkNoUpdate = new CheckBox { Text = "禁用自动升级", Left = 0, Top = 28, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold) };
            p.Controls.Add(_chkThinking); p.Controls.Add(_chkNoUpdate);
        }
        else
        {
            _chkThinking = new CheckBox { Text = "启用最大强度思考", Left = 0, Top = 4, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), Checked = true };
            _chkNoUpdate = new CheckBox { Text = "禁用自动升级", Left = 0, Top = 28, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), Checked = false };
            p.Controls.Add(_chkThinking); p.Controls.Add(_chkNoUpdate);

            // Simple user: show drive selector right here
            p.Controls.Add(L("安装盘符:", 0, 60, 10, FontStyle.Bold, Color.FromArgb(60, 68, 80)));
            _cmbDriveSimple = new ComboBox { Left = 80, Top = 56, Width = 90, Font = new Font(Font.FontFamily, 12F, FontStyle.Bold), DropDownStyle = ComboBoxStyle.DropDownList };
            LoadDrives(_cmbDriveSimple);
            _cmbDriveSimple.SelectedIndexChanged += (_, _) => { _drive = _cmbDriveSimple.Text; UpdateSimplePath(); };
            p.Controls.Add(_cmbDriveSimple);

            _lblSimplePath = L("", 180, 60, 8, FontStyle.Regular, Color.FromArgb(120, 140, 155));
            p.Controls.Add(_lblSimplePath);
            UpdateSimplePath();
        }

        // Install button
        var btnInstall = new Button { Text = "开始安装", Left = 0, Top = _isSimple ? 96 : 60, Width = 160, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 170, 80), ForeColor = Color.White, Font = new Font(Font.FontFamily, 11F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        btnInstall.Click += async (_, _) => await DoInstall();
        p.Controls.Add(btnInstall);
        if (_isSimple) _btnInstallSimple = btnInstall;

        _bar = new ProgressBar { Left = _isSimple ? 175 : 170, Top = (_isSimple ? 106 : 68), Width = _isSimple ? 565 : 570, Height = 22, Style = ProgressBarStyle.Continuous };
        p.Controls.Add(_bar);

        _rtbLog = new RichTextBox { Left = 0, Top = _isSimple ? 138 : 104, Width = 740, Height = _isSimple ? 340 : 370, ReadOnly = true, BackColor = Color.FromArgb(28, 30, 35), ForeColor = Color.FromArgb(200, 210, 220), Font = new Font("Consolas", 8.5F), BorderStyle = BorderStyle.None };
        p.Controls.Add(_rtbLog);
        return p;
    }

    void UpdateSimplePath()
    {
        if (_lblSimplePath != null)
            _lblSimplePath.Text = $"→ Node:{NodePath}  Git:{GitPath}  npm:{NpmPrefix}";
    }

    // ═══ DETECT ═══════════════════════════════════════
    void LoadDrives(ComboBox cb)
    {
        cb.Items.Clear();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
                if (d.IsReady && d.DriveType == DriveType.Fixed)
                    cb.Items.Add(d.Name.TrimEnd('\\'));
        }
        catch { cb.Items.AddRange(new[] { "C:", "D:", "F:" }); }
        if (cb.Items.Count > 0) cb.SelectedIndex = 0;
    }

    void DetectEnv()
    {
        if (_cmbDrive != null) _drive = _cmbDrive.Text;
        if (_cmbDriveSimple != null) _drive = _cmbDriveSimple.Text;
        if (string.IsNullOrEmpty(_drive)) _drive = "C:";
        _nodeOk = TryCmd("node", "--version", out var nv);
        _gitOk = TryCmd("git", "--version", out _);
        _pythonOk = TryCmd("python", "--version", out _) || TryCmd("python3", "--version", out _);
        _claudeOk = TryCmd("claude", "--version", out _);
        if (!IsHandleCreated) return;
        BeginInvoke(() =>
        {
            SetStatus(_lblNode, "Node.js", _nodeOk, nv);
            SetStatus(_lblGit, "Git", _gitOk, "");
            SetStatus(_lblPython, "Python", _pythonOk, "");
            SetStatus(_lblClaude, "Claude Code", _claudeOk, "");
            if (_lblPath != null) _lblPath.Text = $"路径: Node→{NodePath}  Git→{GitPath}  npm→{NpmPrefix}  Tools→{ToolsPath}";
        });
    }
    void SetStatus(Label l, string n, bool ok, string v) { l.Text = ok ? $"{n}: 已安装 {v.Trim()}" : $"{n}: 未安装 — 将自动安装"; l.ForeColor = ok ? Color.FromArgb(0, 150, 80) : Color.FromArgb(220, 100, 40); if (l.Parent is Panel p && p.Controls.Count > 1 && p.Controls[0] is Label d) d.ForeColor = ok ? Color.FromArgb(0, 180, 80) : Color.FromArgb(240, 100, 50); }
    bool TryCmd(string c, string a, out string o) { try { var pi = new ProcessStartInfo(c, a) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true }; var p = Process.Start(pi)!; o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd(); p.WaitForExit(3000); return p.ExitCode == 0 && !string.IsNullOrWhiteSpace(o); } catch { o = ""; return false; } }

    void RefreshLocale()
    {
        _btnBack.Text = Locale.Lang == "zh" ? "← 上一步" : "< Back";
        _btnNext.Text = Locale.Lang == "zh" ? "下一步 →" : "Next >";
        _chkSelectAll.Text = Locale.Lang == "zh" ? "全选/取消全选" : "Select All";
        _chkTools.Text = Locale.Lang == "zh" ? "安装截图操作工具" : "Screenshot tools";
        _chkLogic.Text = Locale.Lang == "zh" ? "添加截图辅助逻辑" : "Screenshot-assist logic";
        _rbSafe.Text = Locale.Lang == "zh" ? "安全模式" : "Safe Mode";
        _rbPro.Text = Locale.Lang == "zh" ? "专业通行模式 (推荐)" : "Pro Mode";
        _rbApiNo.Text = Locale.Lang == "zh" ? "使用默认 Anthropic API" : "Default Anthropic API";
        _rbApiYes.Text = Locale.Lang == "zh" ? "切换到 DeepSeek" : "Switch to DeepSeek";
        _chkThinking.Text = Locale.Lang == "zh" ? "启用最大强度思考" : "Max thinking";
        _chkNoUpdate.Text = Locale.Lang == "zh" ? "禁用自动升级" : "Disable auto-update";
    }

    // ═══ INSTALL ══════════════════════════════════════
    async Task DoInstall()
    {
        if (_installing) return;
        _installing = true;
        if (_btnInstallSimple != null) _btnInstallSimple.Enabled = false;
        _btnNext.Enabled = false;
        _bar.Value = 0; _rtbLog.Clear();
        var L = (Action<string>)(s => BeginInvoke(() => { _rtbLog.AppendText(s + "\r\n"); _rtbLog.ScrollToCaret(); }));
        var P = (Action<int, int>)((c, t) => BeginInvoke(() => { _bar.Maximum = t; _bar.Value = Math.Min(c, t); }));

        L("═══════════════════════════════════");
        L($"  Claude Code 安装器 v7  [{( _isSimple ? "小白模式" : "专业模式" )}]");
        L($"  盘符:{_drive}  Node:{NodePath}  Git:{GitPath}");
        L("═══════════════════════════════════\r\n");

        int total = 7 + (_chkTools.Checked ? 3 : 0) + _skillChecks.Count(c => c.Checked) + (_rbApiYes.Checked && _txtApiKey.Text.Length > 0 ? 1 : 0) + (_chkLogic.Checked ? 1 : 0);
        int step = 0;

        try
        {
            step++; P(step, total); L($"[{step}/{total}] Node.js v20.18.0...");
            if (!_nodeOk) { await DL("https://nodejs.org/dist/v20.18.0/node-v20.18.0-x64.msi", "node.msi", L); await RunAsync("msiexec", $"/i \"{Tmp("node.msi")}\" /qn INSTALLDIR=\"{NodePath}\"", L); } else L("  已安装");

            step++; P(step, total); L($"[{step}/{total}] Git...");
            if (!_gitOk) { await DL("https://github.com/git-for-windows/git/releases/download/v2.45.2.windows.1/Git-2.45.2-64-bit.exe", "git.exe", L); await RunAsync(Tmp("git.exe"), $"/VERYSILENT /NORESTART /DIR=\"{GitPath}\"", L); } else L("  已安装");

            step++; P(step, total); L($"[{step}/{total}] npm prefix → {NpmPrefix}");
            if (!Directory.Exists(NpmPrefix)) Directory.CreateDirectory(NpmPrefix);
            await RunAsync("npm", $"config set prefix \"{NpmPrefix}\"", L);
            UpdatePath(NodePath, NpmPrefix, Path.Combine(GitPath, "cmd"));

            step++; P(step, total); L($"[{step}/{total}] Claude Code...");
            await RunAsync("npm", "install -g @anthropic-ai/claude-code", L);

            step++; P(step, total); L($"[{step}/{total}] CC Switch...");
            await InstallCCSwitch(L);

            if (_chkTools.Checked) { step++; P(step, total); L($"[{step}/{total}] 截图工具..."); InstallTools(L); step++; P(step, total); L($"[{step}/{total}] Python依赖..."); await InstallPy(L); step++; P(step, total); L($"[{step}/{total}] Tesseract..."); await InstallTess(L); }

            foreach (var sk in _skills) { int idx = Array.FindIndex(_skills, x => x.n == sk.n); if (!_skillChecks[idx].Checked) continue; step++; P(step, total); L($"[{step}/{total}] Skill: {sk.n}..."); if (!sk.i.StartsWith("genskills--")) await RunAsync("npx", $"-y skills add {sk.i} -g", L); }

            step++; P(step, total); L($"[{step}/{total}] 配置 settings.json..."); WriteSettings(L);
            if (_chkLogic.Checked) { step++; P(step, total); L($"[{step}/{total}] 截图辅助逻辑..."); WriteCLAUDE(L); }
            if (_rbApiYes.Checked && _txtApiKey.Text.Length > 0) { step++; P(step, total); L($"[{step}/{total}] DeepSeek API..."); WriteDS(_txtApiKey.Text.Trim(), L); }

            P(total, total);
            L("\r\n═══════════════════════════════════");
            L("  安装完成! 终端运行: claude");
            L("═══════════════════════════════════");
            MessageBox.Show("安装完成!\n\n打开终端输入: claude", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { L($"\r\n!!! 错误: {ex.Message}"); MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _installing = false; BeginInvoke(() => { if (_btnInstallSimple != null) _btnInstallSimple.Enabled = true; _btnNext.Enabled = true; }); }
    }

    async Task<bool> DL(string url, string fn, Action<string> log)
    {
        var path = Tmp(fn); log($"  下载: {fn}");
        var urls = new List<string> { url };
        if (url.Contains("nodejs.org")) { urls.Add(url.Replace("nodejs.org/dist", "npmmirror.com/mirrors/node")); urls.Add(url.Replace("nodejs.org/dist", "mirrors.tuna.tsinghua.edu.cn/nodejs-release")); }
        else if (url.Contains("github.com")) urls.Add(url.Replace("github.com", "mirror.ghproxy.com/https://github.com"));
        else if (url.Contains("python.org")) { urls.Add(url.Replace("www.python.org/ftp/python", "npmmirror.com/mirrors/python")); urls.Add(url.Replace("www.python.org/ftp/python", "mirrors.huaweicloud.com/python")); }
        foreach (var u in urls) { try { using var wc = new WebClient(); wc.Headers.Add("User-Agent", "CCI/1.0"); var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60)); await wc.DownloadFileTaskAsync(new Uri(u), path).WaitAsync(cts.Token); if (File.Exists(path) && new FileInfo(path).Length > 50000) return true; } catch { } }
        return false;
    }
    async Task InstallCCSwitch(Action<string> log) { try { using var wc = new WebClient(); wc.Headers.Add("User-Agent", "CCI/1.0"); var json = await wc.DownloadStringTaskAsync("https://api.github.com/repos/farion1231/cc-switch/releases/latest"); using var doc = JsonDocument.Parse(json); string? dl = null; foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray()) { var n = a.GetProperty("name").GetString() ?? ""; if (n.EndsWith(".msi")) { dl = a.GetProperty("browser_download_url").GetString(); break; } if (n.EndsWith(".exe") && dl == null) dl = a.GetProperty("browser_download_url").GetString(); } if (dl == null) return; var ext = Path.GetExtension(dl); var p = Tmp($"ccswitch{ext}"); if (await DL(dl, $"ccswitch{ext}", log)) { if (ext == ".msi") await RunAsync("msiexec", $"/i \"{p}\" /qn", log); else await RunAsync(p, "/VERYSILENT", log); } } catch { } }
    void InstallTools(Action<string> log) { Directory.CreateDirectory(ToolsPath); var asm = System.Reflection.Assembly.GetExecutingAssembly(); foreach (var name in new[] { "scr.py", "ocr.py", "act.py", "see.py", "browser.py" }) { var rn = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name)); if (rn == null) continue; using var s = asm.GetManifestResourceStream(rn); if (s == null) continue; using var sr = new StreamReader(s, Encoding.UTF8); File.WriteAllText(Path.Combine(ToolsPath, name), sr.ReadToEnd(), Encoding.UTF8); } log("  截图工具已安装"); }
    async Task InstallPy(Action<string> log) { if (!_pythonOk) { await DL("https://www.python.org/ftp/python/3.12.4/python-3.12.4-amd64.exe", "python.exe", log); var dir = Path.Combine(_drive, "Python312"); await RunAsync(Tmp("python.exe"), $"/quiet InstallAllUsers=1 TargetDir=\"{dir}\" Include_pip=1 Include_test=0", log); _pythonPath = Path.Combine(dir, "python.exe"); _pythonOk = true; } if (_pythonOk) await RunAsync(_pythonPath, $"-m pip install --target \"{PipPath}\" mss pytesseract pyautogui pillow pygetwindow playwright", log); }
    async Task InstallTess(Action<string> log) { if (await DL("https://github.com/UB-Mannheim/tesseract/releases/download/v5.3.3.20231005/tesseract-ocr-w64-setup-5.3.3.20231005.exe", "tesseract.exe", log)) await RunAsync(Tmp("tesseract.exe"), $"/S /D={Path.Combine(_drive, "Tesseract-OCR")}", log); }
    void WriteSettings(Action<string> log) { Directory.CreateDirectory(ClaudeConfigDir); var path = Path.Combine(ClaudeConfigDir, "settings.json"); var perms = @"{ ""allow"":[""Bash(*)"",""PowerShell(*)"",""Read(*)"",""Write(*)"",""Edit(*)"",""Glob(*)"",""Grep(*)"",""WebFetch(*)"",""WebSearch(*)"",""Agent(*)"",""AskUserQuestion(*)"",""TaskCreate"",""TaskUpdate(*)"",""TaskList"",""TaskGet"",""TaskOutput(*)"",""TaskStop(*)"",""Monitor(*)"",""CronCreate(*)"",""CronDelete"",""CronList"",""PushNotification"",""ScheduleWakeup"",""EnterPlanMode"",""ExitPlanMode"",""EnterWorktree"",""ExitWorktree"",""Skill(*)"",""SendMessage(*)"",], ""defaultMode"":""" + (_rbPro.Checked ? "bypassPermissions" : "default") + @"""}"; try { var node = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) : JsonNode.Parse("{}"); if (node != null) { node["permissions"] = JsonNode.Parse(perms); if (_chkThinking.Checked) { node["thinking"] = "enabled"; node["thinkingBudget"] = "maximum"; } if (_chkNoUpdate.Checked) node["autoUpdatesChannel"] = "none"; File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); } } catch { File.WriteAllText(path, "{\n  \"permissions\":" + perms + "\n}\n", Encoding.UTF8); } log("  → " + path); }
    void WriteDS(string key, Action<string> log) { var path = Path.Combine(ClaudeConfigDir, "settings.json"); try { var node = JsonNode.Parse(File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "{}"); if (node != null) { var env = node["env"] as JsonObject ?? new JsonObject(); node["env"] = env; env["ANTHROPIC_BASE_URL"] = "https://api.deepseek.com/anthropic"; env["ANTHROPIC_AUTH_TOKEN"] = key; env["ANTHROPIC_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_SONNET_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_OPUS_MODEL"] = "deepseek-v4-pro[1m]"; File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); } } catch { } log("  DeepSeek API 已配置"); }
    void WriteCLAUDE(Action<string> log) { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CLAUDE.md"); try { var rule = "\n\n# Screenshot Assistance\nWhen user request is ambiguous, ask:\n\"Would you like me to take a screenshot to understand better?\"\n"; if (File.Exists(path)) { var e = File.ReadAllText(path, Encoding.UTF8); if (!e.Contains("Screenshot Assistance")) File.WriteAllText(path, e + rule, Encoding.UTF8); } else File.WriteAllText(path, rule.Trim(), Encoding.UTF8); log("  → " + path); } catch { } }
    async Task<string> RunAsync(string cmd, string args, Action<string> log) { try { var p = new Process { StartInfo = new ProcessStartInfo(cmd, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } }; p.Start(); var oTask = p.StandardOutput.ReadToEndAsync(); var eTask = p.StandardError.ReadToEndAsync(); var results = await Task.WhenAll(oTask, eTask); await p.WaitForExitAsync(); var r = (results[0] + results[1]).Trim(); if (r.Length > 0) log(r); return r; } catch (Exception ex) { log($"[ERR] {ex.Message}"); return ""; } }
    void UpdatePath(params string[] dirs) { try { var cur = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? ""; foreach (var d in dirs) { var c = d.TrimEnd('\\', '/'); if (!cur.Split(';').Any(e => e.TrimEnd('\\', '/').Equals(c, StringComparison.OrdinalIgnoreCase))) cur = c + ";" + cur; } Environment.SetEnvironmentVariable("PATH", cur, EnvironmentVariableTarget.User); } catch { } }
    string Tmp(string fn) => Path.Combine(Path.GetTempPath(), fn);

    Label L(string t, int x, int y, float sz, FontStyle fs, Color c) => new() { Text = t, Location = new Point(x, y), AutoSize = true, Font = new Font(Font.FontFamily, sz, fs), ForeColor = c };
    Button NavButton(string t, int x, int y, int w, int h, Color bg, EventHandler cb) { var b = new Button { Text = t, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false }; b.Click += cb; return b; }
}
