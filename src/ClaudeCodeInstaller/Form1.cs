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

    ComboBox _cmbDriveSimple; Label _lblSimplePath;
    RadioButton _rbApiYesSimple, _rbApiNoSimple;
    TextBox _txtApiKeySimple;

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
        Text = "Claude Code 安装器 v10";
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
        _navBar = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(248, 250, 253) };
        _btnBack = new Button { Text = "← 上一步", Left = 16, Top = 8, Width = 90, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(140, 145, 155), ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false };
        _btnBack.Click += (s, e) => OnBack(s, e);
        _btnNext = new Button { Text = "下一步 →", Left = 114, Top = 8, Width = 105, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false };
        _btnNext.Click += (s, e) => OnNext(s, e);
        _navBar.Controls.Add(_btnBack); _navBar.Controls.Add(_btnNext);
        Controls.Add(_navBar);

        _pages = new Panel[8];
        _pages[0] = BuildPage0();
        _pages[1] = BuildPage1();
        _pages[2] = BuildPage2();
        _pages[3] = BuildPage3();
        _pages[4] = BuildPage4();
        _pages[5] = BuildPage5();
        _pages[6] = BuildPageSimpleApi();
        _pages[7] = BuildPageInstall();
        foreach (var pg in _pages) { pg.Dock = DockStyle.Fill; pg.BackColor = Color.White; pg.Visible = false; pg.AutoScroll = true; pg.Padding = new Padding(20, 12, 20, 12); Controls.Add(pg); }

        ShowPage(0);
    }

    void ShowPage(int s)
    {
        _step = s;
        for (int i = 0; i < _pages.Length; i++) _pages[i].Visible = (i == s);

        // During install, hide nav entirely
        if (_installing) { _navBar.Visible = false; return; }

        _navBar.Visible = (s != 0);
        _btnBack.Visible = (s > 0);

        if (s == 7) { _btnNext.Visible = false; }
        else if (_isSimple && s == 6) { _btnNext.Text = "下一步 →"; _btnNext.BackColor = Color.FromArgb(0, 120, 212); _btnNext.Visible = true; }
        else if (!_isSimple && s == 5) { _btnNext.Text = "下一步 →"; _btnNext.BackColor = Color.FromArgb(0, 120, 212); _btnNext.Visible = true; }
        else if (s == 0) { _btnNext.Visible = false; }
        else { _btnNext.Text = "下一步 →"; _btnNext.BackColor = Color.FromArgb(0, 120, 212); _btnNext.Visible = true; }

        // Install page pre-config
        if (s == 7 && _isSimple) { _rbPro.Checked = true; _chkTools.Checked = true; _chkLogic.Checked = true; _chkThinking.Checked = true; _chkNoUpdate.Checked = false; _rbApiNo.Checked = _rbApiNoSimple.Checked; _rbApiYes.Checked = _rbApiYesSimple.Checked; _txtApiKey.Text = _txtApiKeySimple.Text; foreach (var c in _skillChecks) c.Checked = true; }
    }

    void OnBack(object? _, EventArgs __)
    {
        if (_installing) return;
        if (_isSimple) { if (_step == 6) { ShowPage(0); return; } if (_step == 7) { ShowPage(6); return; } }
        else { if (_step == 7) { ShowPage(5); return; } }
        if (_step > 0) ShowPage(_step - 1);
    }

    void OnNext(object? _, EventArgs __)
    {
        if (_installing) return;
        if (_isSimple) { if (_step == 6) { ShowPage(7); return; } }
        else { if (_step < 5) { ShowPage(_step + 1); return; } if (_step == 5) { ShowPage(7); return; } }
    }

    // ═══ PAGE 0 ═══════════════════════════════════════
    Panel BuildPage0()
    {
        var p = new Panel();
        p.Controls.Add(L("欢迎使用 Claude Code 安装器", 0, 40, 16, FontStyle.Bold, Color.FromArgb(30, 40, 55)));
        p.Controls.Add(L("请选择你的使用方式", 0, 85, 11, FontStyle.Regular, Color.FromArgb(100, 110, 125)));
        var bPro = new Button { Text = "我是专业用户\r\n逐步选择安装选项", Left = 50, Top = 150, Width = 300, Height = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, Font = new Font(Font.FontFamily, 13F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        bPro.Click += (_, _) => { _isSimple = false; ShowPage(1); };
        p.Controls.Add(bPro); p.Controls.Add(L("逐步选择 Skills、工具、安全模式、API 配置", 50, 275, 8, FontStyle.Regular, Color.FromArgb(140, 150, 165)));
        var bSimple = new Button { Text = "我是小白用户\r\n简易安装 · 快速配置", Left = 410, Top = 150, Width = 300, Height = 110, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 170, 80), ForeColor = Color.White, Font = new Font(Font.FontFamily, 13F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        bSimple.Click += (_, _) => { _isSimple = true; ShowPage(6); };
        p.Controls.Add(bSimple); p.Controls.Add(L("选择安装位置 → API 配置 → 一键安装", 410, 275, 8, FontStyle.Regular, Color.FromArgb(140, 150, 165)));
        return p;
    }

    // ═══ PAGE 1: 环境 ═════════════════════════════════
    Panel BuildPage1()
    {
        var p = new Panel(); int y = 4;
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
        p.Controls.Add(NBtn("重新检测", 320, y, 80, 28, Color.FromArgb(70, 80, 90), (_, _) => DetectEnv()));
        y += 38;
        _lblNode = AddCard(p, "Node.js", ref y); _lblGit = AddCard(p, "Git", ref y);
        _lblPython = AddCard(p, "Python", ref y); _lblClaude = AddCard(p, "Claude Code", ref y);
        _lblPath = L("", 0, y + 4, 8, FontStyle.Regular, Color.FromArgb(140, 150, 165)); _lblPath.MaximumSize = new Size(740, 36); p.Controls.Add(_lblPath);
        return p;
    }

    Label AddCard(Panel p, string title, ref int y)
    {
        var c = new Panel { Left = 0, Top = y, Width = 740, Height = 40, BackColor = Color.FromArgb(250, 252, 254) };
        c.Controls.Add(L("●", 12, 10, 8, FontStyle.Regular, Color.FromArgb(190, 200, 210))); c.Controls.Add(L(title, 34, 9, 10, FontStyle.Bold, Color.FromArgb(50, 55, 65)));
        var s = L("检测中...", 180, 11, 9, FontStyle.Regular, Color.Gray); c.Controls.Add(s); p.Controls.Add(c); y += 44; return s;
    }

    // ═══ PAGE 2-5 unchanged ═══════════════════════════
    Panel BuildPage2()
    {
        var p = new Panel();
        _chkSelectAll = new CheckBox { Text = "全选/取消全选", Left = 0, Top = 0, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 100, 200) };
        _chkSelectAll.CheckedChanged += (_, _) => { bool a = _chkSelectAll.Checked; foreach (var c in _skillChecks) c.Checked = a; };
        p.Controls.Add(_chkSelectAll);
        var pl = new FlowLayoutPanel { Left = 0, Top = 28, Width = 740, Height = 470, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _skillChecks = new CheckBox[_skills.Length];
        for (int i = 0; i < _skills.Length; i++) { var sk = _skills[i]; var r = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Margin = new Padding(0, 2, 0, 2) }; var cb = new CheckBox { Text = sk.n, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), Padding = new Padding(4, 3, 0, 0) }; r.Controls.Add(cb); r.Controls.Add(L(sk.d, 4, 0, 8.5F, FontStyle.Regular, Color.FromArgb(130, 140, 155))); _skillChecks[i] = cb; pl.Controls.Add(r); }
        p.Controls.Add(pl); return p;
    }
    Panel BuildPage3()
    {
        var p = new Panel();
        _chkTools = new CheckBox { Text = "安装截图操作工具 (Python: scr, ocr, see, act, browser)", Left = 0, Top = 8, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        p.Controls.Add(_chkTools); p.Controls.Add(L("需要 Python + Tesseract OCR。包含屏幕截图、OCR、鼠标键盘、浏览器自动化。", 24, 32, 8, FontStyle.Regular, Color.FromArgb(130, 140, 155)));
        _chkLogic = new CheckBox { Text = "添加截图辅助逻辑 (写入 CLAUDE.md)", Left = 0, Top = 84, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        p.Controls.Add(_chkLogic);
        _txtLogic = new TextBox { Left = 24, Top = 114, Width = 700, Height = 55, Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(248, 250, 253), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 8.5F), Text = "当用户请求不清晰时，主动询问是否需要截图查看。优先于猜测行为。" };
        p.Controls.Add(_txtLogic); return p;
    }
    Panel BuildPage4()
    {
        var p = new Panel();
        _rbSafe = new RadioButton { Text = "安全模式 — 高威胁操作需要用户确认", Left = 4, Top = 12, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        _rbPro = new RadioButton { Text = "专业通行模式 — 所有操作无需确认 (推荐)", Left = 4, Top = 44, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), Checked = true };
        p.Controls.Add(_rbSafe); p.Controls.Add(_rbPro); return p;
    }
    Panel BuildPage5()
    {
        var p = new Panel();
        _rbApiNo = new RadioButton { Text = "使用默认 Anthropic API", Left = 4, Top = 12, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), Checked = true };
        _rbApiYes = new RadioButton { Text = "切换到 DeepSeek (deepseek-v4-pro[1m])", Left = 4, Top = 44, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        _rbApiYes.CheckedChanged += (_, _) => _txtApiKey.Enabled = _rbApiYes.Checked; p.Controls.Add(_rbApiNo); p.Controls.Add(_rbApiYes);
        p.Controls.Add(L("API Key:", 24, 78, 9, FontStyle.Bold, Color.FromArgb(60, 68, 80))); _txtApiKey = new TextBox { Left = 100, Top = 75, Width = 400, PasswordChar = '*', Enabled = false }; p.Controls.Add(_txtApiKey);
        p.Controls.Add(L("预设: deepseek-v4-pro[1m] (4个模型槽位全部预填)", 24, 104, 8, FontStyle.Regular, Color.FromArgb(130, 140, 155))); return p;
    }

    // ═══ PAGE 6: 小白 API + 盘符 ══════════════════════
    Panel BuildPageSimpleApi()
    {
        var p = new Panel();
        p.Controls.Add(L("安装配置", 0, 8, 14, FontStyle.Bold, Color.FromArgb(30, 40, 55)));
        // Drive
        p.Controls.Add(L("安装盘符:", 0, 44, 10, FontStyle.Bold, Color.FromArgb(60, 68, 80)));
        _cmbDriveSimple = new ComboBox { Left = 80, Top = 40, Width = 90, Font = new Font(Font.FontFamily, 12F, FontStyle.Bold), DropDownStyle = ComboBoxStyle.DropDownList };
        LoadDrives(_cmbDriveSimple);
        _cmbDriveSimple.SelectedIndexChanged += (_, _) => { _drive = _cmbDriveSimple.Text; UpdateSimplePath(); };
        p.Controls.Add(_cmbDriveSimple);
        _lblSimplePath = L("", 180, 44, 8, FontStyle.Regular, Color.FromArgb(120, 140, 155)); p.Controls.Add(_lblSimplePath);

        // API
        p.Controls.Add(L("API 提供商:", 0, 88, 10, FontStyle.Bold, Color.FromArgb(60, 68, 80)));
        _rbApiNoSimple = new RadioButton { Text = "默认 Anthropic API（无需配置）", Left = 4, Top = 116, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), Checked = true };
        _rbApiYesSimple = new RadioButton { Text = "DeepSeek API（deepseek-v4-pro[1m]）", Left = 4, Top = 146, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold) };
        _rbApiYesSimple.CheckedChanged += (_, _) => _txtApiKeySimple.Enabled = _rbApiYesSimple.Checked;
        p.Controls.Add(_rbApiNoSimple); p.Controls.Add(_rbApiYesSimple);
        p.Controls.Add(L("API Key:", 24, 178, 9, FontStyle.Bold, Color.FromArgb(60, 68, 80)));
        _txtApiKeySimple = new TextBox { Left = 100, Top = 175, Width = 400, PasswordChar = '*', Enabled = false };
        p.Controls.Add(_txtApiKeySimple);
        p.Controls.Add(L("填入 Key 即可，4 个模型槽全预填 deepseek-v4-pro[1m]", 24, 204, 8, FontStyle.Regular, Color.FromArgb(130, 140, 155)));

        // Info
        p.Controls.Add(L("点击下一步将自动配置：Skills全选 + 专业模式 + 截图工具 + 最强算力 + 自动更新", 0, 250, 8, FontStyle.Regular, Color.FromArgb(140, 150, 165)));
        UpdateSimplePath();
        return p;
    }

    void UpdateSimplePath() { if (_lblSimplePath != null) _lblSimplePath.Text = $"→ Node:{NodePath}  Git:{GitPath}  npm:{NpmPrefix}"; }

    // ═══ PAGE 7: 安装 ═════════════════════════════════
    Panel BuildPageInstall()
    {
        var p = new Panel();
        _chkThinking = new CheckBox { Text = "启用最大强度思考", Left = 0, Top = 4, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold) };
        _chkNoUpdate = new CheckBox { Text = "禁用自动升级", Left = 0, Top = 28, AutoSize = true, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold) };
        p.Controls.Add(_chkThinking); p.Controls.Add(_chkNoUpdate);
        var btnInstall = new Button { Text = "开始安装", Left = 0, Top = 56, Width = 160, Height = 44, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 170, 80), ForeColor = Color.White, Font = new Font(Font.FontFamily, 11F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        btnInstall.Click += async (_, _) => await DoInstall();
        p.Controls.Add(btnInstall);
        _bar = new ProgressBar { Left = 175, Top = 66, Width = 565, Height = 22, Style = ProgressBarStyle.Continuous }; p.Controls.Add(_bar);
        _rtbLog = new RichTextBox { Left = 0, Top = 110, Width = 740, Height = 350, ReadOnly = true, BackColor = Color.FromArgb(28, 30, 35), ForeColor = Color.FromArgb(200, 210, 220), Font = new Font("Consolas", 8.5F), BorderStyle = BorderStyle.None }; p.Controls.Add(_rtbLog);
        return p;
    }

    // ═══ DETECT ═══════════════════════════════════════
    void LoadDrives(ComboBox cb) { cb.Items.Clear(); try { foreach (var d in DriveInfo.GetDrives()) if (d.IsReady && d.DriveType == DriveType.Fixed) cb.Items.Add(d.Name.TrimEnd('\\')); } catch { cb.Items.AddRange(new[] { "C:", "D:", "F:" }); } if (cb.Items.Count > 0) cb.SelectedIndex = 0; }
    void DetectEnv()
    {
        if (_cmbDrive != null && !_isSimple) _drive = _cmbDrive.Text;
        if (_cmbDriveSimple != null && _isSimple) _drive = _cmbDriveSimple.Text;
        if (string.IsNullOrEmpty(_drive)) _drive = "C:";
        _nodeOk = TryCmd("node", "--version", out var nv); _gitOk = TryCmd("git", "--version", out _);
        _pythonOk = TryCmd("python", "--version", out _) || TryCmd("python3", "--version", out _);
        _claudeOk = TryCmd("claude", "--version", out _);
        if (!IsHandleCreated) return;
        BeginInvoke(() => { SetStatus(_lblNode, "Node.js", _nodeOk, nv); SetStatus(_lblGit, "Git", _gitOk, ""); SetStatus(_lblPython, "Python", _pythonOk, ""); SetStatus(_lblClaude, "Claude Code", _claudeOk, ""); if (_lblPath != null) _lblPath.Text = $"路径: Node→{NodePath}  Git→{GitPath}  npm→{NpmPrefix}  Tools→{ToolsPath}"; UpdateSimplePath(); });
    }
    void SetStatus(Label l, string n, bool ok, string v) { l.Text = ok ? $"{n}: 已安装 {v.Trim()}" : $"{n}: 未安装"; l.ForeColor = ok ? Color.FromArgb(0, 150, 80) : Color.FromArgb(220, 100, 40); if (l.Parent is Panel p && p.Controls.Count > 1 && p.Controls[0] is Label d) d.ForeColor = ok ? Color.FromArgb(0, 180, 80) : Color.FromArgb(240, 100, 50); }
    bool TryCmd(string c, string a, out string o) { try { var pi = new ProcessStartInfo(c, a) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true }; var p = Process.Start(pi)!; o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd(); p.WaitForExit(3000); return p.ExitCode == 0 && !string.IsNullOrWhiteSpace(o); } catch { o = ""; return false; } }
    void RefreshLocale() { _btnBack.Text = Locale.Lang == "zh" ? "← 上一步" : "< Back"; _btnNext.Text = Locale.Lang == "zh" ? "下一步 →" : "Next >"; _chkSelectAll.Text = Locale.Lang == "zh" ? "全选/取消全选" : "Select All"; }

    // ═══ INSTALL ══════════════════════════════════════
    async Task DoInstall()
    {
        if (_installing) return;
        _installing = true; _navBar.Visible = false;
        _bar.Value = 0; _rtbLog.Clear();
        var L = (Action<string>)(s => BeginInvoke(() => { _rtbLog.AppendText(s + "\r\n"); _rtbLog.ScrollToCaret(); }));
        var P = (Action<int, int>)((c, t) => BeginInvoke(() => { _bar.Maximum = t; _bar.Value = Math.Min(c, t); }));

        L("═══════════════════════════════════");
        L($"  Claude Code Installer v10 [{( _isSimple ? "小白" : "专业" )}] | Shimizu");
        L($"  Drive:{_drive}  Node:{NodePath}  Git:{GitPath}");
        L("═══════════════════════════════════\r\n");

        int total = 7 + (_chkTools.Checked ? 4 : 0) + _skillChecks.Count(c => c.Checked) + ((_rbApiYes.Checked && _txtApiKey.Text.Length > 0) || (_rbApiYesSimple.Checked && _txtApiKeySimple.Text.Length > 0) ? 1 : 0) + (_chkLogic.Checked ? 1 : 0) + 1;
        int step = 0;

        try
        {
            step++; P(step, total); L($"[{step}/{total}] Node.js v20.18.0..."); if (!_nodeOk) { await DL("https://nodejs.org/dist/v20.18.0/node-v20.18.0-x64.msi", "node.msi", L); await RunAsync("msiexec", $"/i \"{Tmp("node.msi")}\" /qn INSTALLDIR=\"{NodePath}\"", L); } else L("  已安装");
            step++; P(step, total); L($"[{step}/{total}] Git..."); if (!_gitOk) { await DL("https://github.com/git-for-windows/git/releases/download/v2.45.2.windows.1/Git-2.45.2-64-bit.exe", "git.exe", L); await RunAsync(Tmp("git.exe"), $"/VERYSILENT /NORESTART /DIR=\"{GitPath}\"", L); } else L("  已安装");
            step++; P(step, total); L($"[{step}/{total}] npm prefix → {NpmPrefix}"); if (!Directory.Exists(NpmPrefix)) Directory.CreateDirectory(NpmPrefix); await RunAsync("npm", $"config set prefix \"{NpmPrefix}\"", L); UpdatePath(NodePath, NpmPrefix, Path.Combine(GitPath, "cmd"));
            step++; P(step, total); L($"[{step}/{total}] Claude Code (npm install -g)..."); await RunAsync("npm", "install -g @anthropic-ai/claude-code", L);
            step++; P(step, total); L($"[{step}/{total}] CC Switch..."); await InstallCCSwitch(L);

            if (_chkTools.Checked)
            {
                step++; P(step, total); L($"[{step}/{total}] 截图工具..."); InstallTools(L);
                step++; P(step, total); L($"[{step}/{total}] Python 依赖安装 ⚠ 需约 5-8 分钟，请耐心等待...");
                L("  (mss + pytesseract + pyautogui + pillow + pygetwindow + playwright ≈ 200MB)");
                await InstallPy(L);
                step++; P(step, total); L($"[{step}/{total}] Tesseract OCR..."); await InstallTess(L);
            }

            foreach (var sk in _skills) { int idx = Array.FindIndex(_skills, x => x.n == sk.n); if (!_skillChecks[idx].Checked) continue; step++; P(step, total); L($"[{step}/{total}] Skill: {sk.n}..."); if (!sk.i.StartsWith("genskills--")) await RunAsync("npx", $"-y skills add {sk.i} -g", L); }

            step++; P(step, total); L($"[{step}/{total}] 配置 settings.json..."); WriteSettings(L);
            if (_chkLogic.Checked) { step++; P(step, total); L($"[{step}/{total}] 截图辅助逻辑..."); WriteCLAUDE(L); }
            var apiKey = _isSimple ? _txtApiKeySimple.Text.Trim() : _txtApiKey.Text.Trim();
            if ((_rbApiYes.Checked || _rbApiYesSimple.Checked) && apiKey.Length > 0) { step++; P(step, total); L($"[{step}/{total}] DeepSeek API..."); WriteDS(apiKey, L); }

            step++; P(step, total); L($"[{step}/{total}] 创建桌面快捷方式..."); CreateShortcuts(L);

            P(total, total);
            L("\r\n═══════════════════════════════════");
            L("  安装完成! 终端: claude | 桌面: Claude Code / CC Switch");
            L("═══════════════════════════════════");
            MessageBox.Show("安装完成!\n\n终端: claude\n桌面快捷方式: Claude Code / CC Switch", "Shimizu Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { L($"\r\n!!! 错误: {ex.Message}"); MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { _installing = false; BeginInvoke(() => { _navBar.Visible = false; _btnBack.Visible = false; }); }
    }

    void CreateShortcuts(Action<string> log)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            var claudeBin = Path.Combine(NpmPrefix, "claude.cmd");
            if (!File.Exists(claudeBin)) claudeBin = Path.Combine(NpmPrefix, "node_modules", ".bin", "claude.cmd");
            if (!File.Exists(claudeBin)) claudeBin = "claude";
            dynamic cl = shell.CreateShortcut(Path.Combine(desktop, "Claude Code.lnk"));
            cl.TargetPath = "cmd.exe"; cl.Arguments = $"/k \"{claudeBin}\""; cl.Description = "Claude Code"; cl.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); cl.Save();
            log("  ✓ Claude Code.lnk");
            string? ccExe = null;
            foreach (var p in new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CC Switch", "CC Switch.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CC Switch", "CC Switch.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CC Switch", "CC Switch.exe") }) { if (File.Exists(p)) { ccExe = p; break; } }
            if (ccExe != null) { dynamic cc = shell.CreateShortcut(Path.Combine(desktop, "CC Switch.lnk")); cc.TargetPath = ccExe; cc.Description = "CC Switch"; cc.Save(); log("  ✓ CC Switch.lnk"); }
            else log("  ⚠ CC Switch path not found");
        }
        catch (Exception ex) { log($"  快捷方式: {ex.Message}"); }
    }

    // ── Install helpers ───────────────────────────────
    async Task<string> RunAsync(string cmd, string args, Action<string> log) { try { var p = new Process { StartInfo = new ProcessStartInfo(cmd, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } }; p.Start(); var oT = p.StandardOutput.ReadToEndAsync(); var eT = p.StandardError.ReadToEndAsync(); var r = await Task.WhenAll(oT, eT); await p.WaitForExitAsync(); var txt = (r[0] + r[1]).Trim(); if (txt.Length > 0) log(txt); return txt; } catch (Exception ex) { log($"[ERR] {ex.Message}"); return ""; } }
    async Task<bool> DL(string url, string fn, Action<string> log) { var path = Tmp(fn); log($"  下载: {fn}"); var urls = new List<string> { url }; if (url.Contains("nodejs.org")) { urls.Add(url.Replace("nodejs.org/dist", "npmmirror.com/mirrors/node")); urls.Add(url.Replace("nodejs.org/dist", "mirrors.tuna.tsinghua.edu.cn/nodejs-release")); } else if (url.Contains("github.com")) urls.Add(url.Replace("github.com", "mirror.ghproxy.com/https://github.com")); else if (url.Contains("python.org")) { urls.Add(url.Replace("www.python.org/ftp/python", "npmmirror.com/mirrors/python")); urls.Add(url.Replace("www.python.org/ftp/python", "mirrors.huaweicloud.com/python")); } foreach (var u in urls) { try { using var wc = new WebClient(); wc.Headers.Add("User-Agent", "CCI/1.0"); await wc.DownloadFileTaskAsync(new Uri(u), path); if (File.Exists(path) && new FileInfo(path).Length > 50000) return true; } catch { } } return false; }
    async Task InstallCCSwitch(Action<string> log) { try { using var wc = new WebClient(); wc.Headers.Add("User-Agent", "CCI/1.0"); var json = await wc.DownloadStringTaskAsync("https://api.github.com/repos/farion1231/cc-switch/releases/latest"); using var doc = JsonDocument.Parse(json); string? dl = null; foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray()) { var n = a.GetProperty("name").GetString() ?? ""; if (n.EndsWith(".msi")) { dl = a.GetProperty("browser_download_url").GetString(); break; } if (n.EndsWith(".exe") && dl == null) dl = a.GetProperty("browser_download_url").GetString(); } if (dl == null) return; var ext = Path.GetExtension(dl); var p = Tmp($"ccswitch{ext}"); if (await DL(dl, $"ccswitch{ext}", log)) { if (ext == ".msi") await RunAsync("msiexec", $"/i \"{p}\" /qn", log); else await RunAsync(p, "/VERYSILENT", log); } } catch { } }
    void InstallTools(Action<string> log) { Directory.CreateDirectory(ToolsPath); var asm = System.Reflection.Assembly.GetExecutingAssembly(); foreach (var name in new[] { "scr.py", "ocr.py", "act.py", "see.py", "browser.py" }) { var rn = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name)); if (rn == null) continue; using var s = asm.GetManifestResourceStream(rn); if (s == null) continue; using var sr = new StreamReader(s, Encoding.UTF8); File.WriteAllText(Path.Combine(ToolsPath, name), sr.ReadToEnd(), Encoding.UTF8); } log("  截图工具已安装"); }
    async Task InstallPy(Action<string> log) { if (!_pythonOk) { await DL("https://www.python.org/ftp/python/3.12.4/python-3.12.4-amd64.exe", "python.exe", log); var dir = Path.Combine(_drive, "Python312"); await RunAsync(Tmp("python.exe"), $"/quiet InstallAllUsers=1 TargetDir=\"{dir}\" Include_pip=1 Include_test=0", log); _pythonPath = Path.Combine(dir, "python.exe"); _pythonOk = true; } if (_pythonOk) await RunAsync(_pythonPath, $"-m pip install --target \"{PipPath}\" mss pytesseract pyautogui pillow pygetwindow playwright", log); }
    async Task InstallTess(Action<string> log) { if (await DL("https://github.com/UB-Mannheim/tesseract/releases/download/v5.3.3.20231005/tesseract-ocr-w64-setup-5.3.3.20231005.exe", "tesseract.exe", log)) await RunAsync(Tmp("tesseract.exe"), $"/S /D={Path.Combine(_drive, "Tesseract-OCR")}", log); }
    void WriteSettings(Action<string> log) { Directory.CreateDirectory(ClaudeConfigDir); var path = Path.Combine(ClaudeConfigDir, "settings.json"); var perms = @"{ ""allow"":[""Bash(*)"",""PowerShell(*)"",""Read(*)"",""Write(*)"",""Edit(*)"",""Glob(*)"",""Grep(*)"",""WebFetch(*)"",""WebSearch(*)"",""Agent(*)"",""AskUserQuestion(*)"",""TaskCreate"",""TaskUpdate(*)"",""TaskList"",""TaskGet"",""TaskOutput(*)"",""TaskStop(*)"",""Monitor(*)"",""CronCreate(*)"",""CronDelete"",""CronList"",""PushNotification"",""ScheduleWakeup"",""EnterPlanMode"",""ExitPlanMode"",""EnterWorktree"",""ExitWorktree"",""Skill(*)"",""SendMessage(*)"",], ""defaultMode"":""" + (_rbPro.Checked ? "bypassPermissions" : "default") + @"""}"; try { var node = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) : JsonNode.Parse("{}"); if (node != null) { node["permissions"] = JsonNode.Parse(perms); if (_chkThinking.Checked) { node["thinking"] = "enabled"; node["thinkingBudget"] = "maximum"; } if (_chkNoUpdate.Checked) node["autoUpdatesChannel"] = "none"; File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); } } catch { File.WriteAllText(path, "{\n  \"permissions\":" + perms + "\n}\n", Encoding.UTF8); } log("  → " + path); }
    void WriteDS(string key, Action<string> log) { var path = Path.Combine(ClaudeConfigDir, "settings.json"); try { var node = JsonNode.Parse(File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "{}"); if (node != null) { var env = node["env"] as JsonObject ?? new JsonObject(); node["env"] = env; env["ANTHROPIC_BASE_URL"] = "https://api.deepseek.com/anthropic"; env["ANTHROPIC_AUTH_TOKEN"] = key; env["ANTHROPIC_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_SONNET_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_OPUS_MODEL"] = "deepseek-v4-pro[1m]"; File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); } } catch { } log("  DeepSeek API 已配置"); }
    void WriteCLAUDE(Action<string> log) { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CLAUDE.md"); try { var rule = "\n\n# Screenshot Assist\nWhen user request is ambiguous, ask:\n\"Would you like me to take a screenshot?\"\n"; if (File.Exists(path)) { var e = File.ReadAllText(path, Encoding.UTF8); if (!e.Contains("Screenshot Assist")) File.WriteAllText(path, e + rule, Encoding.UTF8); } else File.WriteAllText(path, rule.Trim(), Encoding.UTF8); log("  → " + path); } catch { } }
    void UpdatePath(params string[] dirs) { try { var cur = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? ""; foreach (var d in dirs) { var c = d.TrimEnd('\\', '/'); if (!cur.Split(';').Any(e => e.TrimEnd('\\', '/').Equals(c, StringComparison.OrdinalIgnoreCase))) cur = c + ";" + cur; } Environment.SetEnvironmentVariable("PATH", cur, EnvironmentVariableTarget.User); } catch { } }
    string Tmp(string fn) => Path.Combine(Path.GetTempPath(), fn);
    Label L(string t, int x, int y, float sz, FontStyle fs, Color c) => new() { Text = t, Location = new Point(x, y), AutoSize = true, Font = new Font(Font.FontFamily, sz, fs), ForeColor = c };
    Button NBtn(string t, int x, int y, int w, int h, Color bg, EventHandler cb) { var b = new Button { Text = t, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false }; b.Click += cb; return b; }
}
