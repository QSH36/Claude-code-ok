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
    string _cachedDrive = "";
    bool _detecting;

    // ── V17 Brand Colors ────────────────────────────
    static readonly Color BrandBlue     = Color.FromArgb(0, 82, 217);
    static readonly Color BrandBlueEnd  = Color.FromArgb(79, 172, 254);
    static readonly Color SuccessGreen  = Color.FromArgb(0, 170, 80);
    static readonly Color WarnOrange    = Color.FromArgb(249, 115, 22);
    static readonly Color CardBg        = Color.FromArgb(250, 251, 253);
    static readonly Color TextDark      = Color.FromArgb(26, 26, 46);
    static readonly Color TextGray      = Color.FromArgb(100, 110, 130);
    static readonly Color BorderLight   = Color.FromArgb(225, 230, 240);

    // ── V17 Styled Control Builders ──────────────────
    Panel Banner(string stepNum, string title)
    {
        var b = new Panel { Width = 760, Height = 52, Margin = new Padding(0) };
        b.Paint += (_, e) => {
            using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, b.Width, b.Height), BrandBlue, BrandBlueEnd, 135f);
            e.Graphics.FillRectangle(br, new Rectangle(0, 0, b.Width, b.Height));
        };
        if (stepNum != "") {
            var badge = new Label { Text = stepNum, Left = 24, Top = 12, Width = 28, Height = 28,
                TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(40, 255, 255, 255) };
            b.Controls.Add(badge);
            b.Controls.Add(new Label { Text = title, Left = 62, Top = 14, AutoSize = true,
                Font = new Font(Font.FontFamily, 13f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent });
        } else {
            b.Controls.Add(new Label { Text = title, Left = 24, Top = 14, AutoSize = true,
                Font = new Font(Font.FontFamily, 13f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent });
        }
        return b;
    }

    Button AccentBtn(string text, int x, int y, int w, int h, Color bg, EventHandler cb)
    {
        var btn = new Button { Text = text, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat,
            BackColor = bg, ForeColor = Color.White, Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
            UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        btn.Click += cb; return btn;
    }

    Label StyledLabel(string text, int x, int y, float sz, FontStyle fs, Color c) =>
        new() { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font(Font.FontFamily, sz, fs), ForeColor = c };

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
        Text = "Claude Code 安装器 v1.0.5";
        Size = new Size(800, 620);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(245, 247, 250);
        this.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; };
        BuildWizard();
        this.Shown += (_, _) => DetectEnvAsync();
    }

    void BuildWizard()
    {
        _navBar = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(30, 35, 45) };
        _navBar.Paint += (_, e) => {
            using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, _navBar.Width, _navBar.Height), Color.FromArgb(30, 35, 48), Color.FromArgb(22, 26, 36), 90f);
            e.Graphics.FillRectangle(br, new Rectangle(0, 0, _navBar.Width, _navBar.Height));
        };
        _btnBack = new Button { Text = "← 上一步", Left = 16, Top = 10, Width = 95, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(255, 255, 255, 25), ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        _btnBack.Click += (s, e) => OnBack(s, e);
        _btnNext = new Button { Text = "下一步 →", Left = 120, Top = 10, Width = 110, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = BrandBlue, ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
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
        var banner = new Panel { Left = 0, Top = 0, Width = 760, Height = 180, Margin = new Padding(0) };
        banner.Paint += (_, e) => {
            using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, banner.Width, banner.Height), BrandBlue, BrandBlueEnd, 135f);
            e.Graphics.FillRectangle(br, new Rectangle(0, 0, banner.Width, banner.Height));
        };
        var lblBrand = StyledLabel("SHIMIZU", 0, 32, 10f, FontStyle.Regular, Color.FromArgb(128, 255, 255, 255)); lblBrand.TextAlign = ContentAlignment.MiddleCenter; lblBrand.Width = 760; banner.Controls.Add(lblBrand);
        var lblTitle = StyledLabel("Claude Code 安装器", 0, 62, 20f, FontStyle.Bold, Color.White); lblTitle.TextAlign = ContentAlignment.MiddleCenter; lblTitle.Width = 760; banner.Controls.Add(lblTitle);
        var lblSubtitle = StyledLabel("一键安装 · 永久免费", 0, 100, 11f, FontStyle.Regular, Color.FromArgb(180, 255, 255, 255)); lblSubtitle.TextAlign = ContentAlignment.MiddleCenter; lblSubtitle.Width = 760; banner.Controls.Add(lblSubtitle);
        var lblVer = StyledLabel("v1.0.5", 0, 138, 9f, FontStyle.Regular, Color.FromArgb(120, 255, 255, 255)); lblVer.TextAlign = ContentAlignment.MiddleCenter; lblVer.Width = 760; banner.Controls.Add(lblVer);
        p.Controls.Add(banner);

        p.Controls.Add(StyledLabel("选择安装方式", 20, 200, 13f, FontStyle.Bold, TextDark));

        var cardPro = new Panel { Left = 20, Top = 236, Width = 350, Height = 72, BackColor = Color.FromArgb(240, 244, 255), Cursor = Cursors.Hand };
        cardPro.Paint += (_, e) => { using var b = new SolidBrush(BrandBlue); e.Graphics.FillRectangle(b, 0, 0, 4, cardPro.Height); };
        cardPro.Click += (_, _) => { _isSimple = false; ShowPage(1); };
        cardPro.Controls.Add(StyledLabel("我是专业用户", 20, 14, 13f, FontStyle.Bold, BrandBlue));
        cardPro.Controls.Add(StyledLabel("逐步选择 Skills、安全模式、API 配置", 20, 38, 9f, FontStyle.Regular, TextGray));
        p.Controls.Add(cardPro);

        var cardSimple = new Panel { Left = 400, Top = 236, Width = 350, Height = 72, BackColor = Color.FromArgb(237, 255, 245), Cursor = Cursors.Hand };
        cardSimple.Paint += (_, e) => { using var b = new SolidBrush(SuccessGreen); e.Graphics.FillRectangle(b, 0, 0, 4, cardSimple.Height); };
        cardSimple.Click += (_, _) => { _isSimple = true; ShowPage(6); };
        cardSimple.Controls.Add(StyledLabel("我是小白用户", 20, 14, 13f, FontStyle.Bold, SuccessGreen));
        cardSimple.Controls.Add(StyledLabel("简易安装 · 快速配置", 20, 38, 9f, FontStyle.Regular, TextGray));
        p.Controls.Add(cardSimple);

        p.Controls.Add(StyledLabel("专业模式：完整控制安装过程 | 小白模式：选择盘符 + API 一键到底", 20, 320, 8f, FontStyle.Regular, Color.FromArgb(150, 160, 175)));
        return p;
    }

    // ═══ PAGE 1: 环境 ═════════════════════════════════
    Panel BuildPage1()
    {
        var p = new Panel();
        p.Controls.Add(Banner("1", "环境检测"));

        int y = 64;
        p.Controls.Add(StyledLabel("语言:", 4, y + 5, 9f, FontStyle.Bold, TextDark));
        _cmbLang = new ComboBox { Left = 55, Top = y, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font(Font.FontFamily, 9f) };
        _cmbLang.Items.AddRange(new[] { "中文", "English" }); _cmbLang.SelectedIndex = 0;
        _cmbLang.SelectedIndexChanged += (_, _) => { Locale.Lang = _cmbLang.Text == "English" ? "en" : "zh"; RefreshLocale(); };
        p.Controls.Add(_cmbLang);

        p.Controls.Add(StyledLabel("安装盘符:", 155, y + 5, 9f, FontStyle.Bold, TextDark));
        _cmbDrive = new ComboBox { Left = 235, Top = y, Width = 80, Font = new Font(Font.FontFamily, 11F, FontStyle.Bold), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(240, 244, 255), ForeColor = BrandBlue };
        LoadDrives(_cmbDrive);
        _cmbDrive.SelectedIndexChanged += (_, _) => { _drive = _cmbDrive.Text; DetectEnvAsync(); };
        p.Controls.Add(_cmbDrive);

        var btnReDetect = new Button { Text = "重新检测", Left = 330, Top = y, Width = 90, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(80, 90, 110), ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        btnReDetect.Click += (_, _) => DetectEnvAsync();
        p.Controls.Add(btnReDetect);

        y += 44;
        _lblNode = AddStatusCard(p, "Node.js", ref y); _lblGit = AddStatusCard(p, "Git", ref y);
        _lblPython = AddStatusCard(p, "Python", ref y); _lblClaude = AddStatusCard(p, "Claude Code", ref y);
        _lblPath = StyledLabel("", 4, y + 6, 8f, FontStyle.Regular, TextGray); _lblPath.MaximumSize = new Size(750, 36); p.Controls.Add(_lblPath);
        return p;
    }

    Label AddStatusCard(Panel p, string title, ref int y)
    {
        var card = new Panel { Left = 0, Top = y, Width = 750, Height = 42, BackColor = CardBg };
        card.Paint += (_, e) => { using var br = new SolidBrush(BorderLight); e.Graphics.FillRectangle(br, 0, 0, 3, card.Height); };
        var dot = new Label { Text = "●", Left = 16, Top = 10, AutoSize = true, Font = new Font(Font.FontFamily, 8f), ForeColor = Color.FromArgb(190, 200, 210) };
        card.Controls.Add(dot);
        card.Controls.Add(StyledLabel(title, 34, 10, 10f, FontStyle.Bold, Color.FromArgb(50, 55, 65)));
        var status = StyledLabel("检测中...", 160, 12, 9f, FontStyle.Regular, Color.Gray);
        card.Controls.Add(status); p.Controls.Add(card); y += 48;
        return status;
    }

    // ═══ PAGE 2-5 unchanged ═══════════════════════════
    Panel BuildPage2()
    {
        var p = new Panel();
        p.Controls.Add(Banner("2", "Skills"));

        _chkSelectAll = new CheckBox { Text = "全选 / 取消全选", Left = 4, Top = 62, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), ForeColor = BrandBlue };
        _chkSelectAll.CheckedChanged += (_, _) => { bool a = _chkSelectAll.Checked; foreach (var c in _skillChecks) c.Checked = a; };
        p.Controls.Add(_chkSelectAll);

        var pl = new FlowLayoutPanel { Left = 4, Top = 92, Width = 750, Height = 440, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _skillChecks = new CheckBox[_skills.Length];
        for (int i = 0; i < _skills.Length; i++) {
            var sk = _skills[i];
            var row = new Panel { Width = 730, Height = 44, Margin = new Padding(0, 2, 0, 2), BackColor = sk.i.StartsWith("genskills--") ? Color.FromArgb(248, 248, 250) : CardBg, Cursor = Cursors.Hand };
            row.Paint += (_, e) => { if (!sk.i.StartsWith("genskills--")) using (var br = new SolidBrush(BrandBlue)) e.Graphics.FillRectangle(br, 0, 0, 3, row.Height); };
            var cb = new CheckBox { Text = sk.n, Checked = !sk.i.StartsWith("genskills--"), Left = 16, Top = 10, AutoSize = true, Font = new Font(Font.FontFamily, 10F, FontStyle.Bold), Enabled = !sk.i.StartsWith("genskills--") };
            row.Controls.Add(cb);
            row.Controls.Add(StyledLabel(sk.d, 20, 26, 8f, FontStyle.Regular, sk.i.StartsWith("genskills--") ? Color.FromArgb(180,185,195) : TextGray));
            cb.Click += (_, _) => { if (sk.i.StartsWith("genskills--")) cb.Checked = false; };
            _skillChecks[i] = cb; pl.Controls.Add(row);
        }
        p.Controls.Add(pl); return p;
    }
    Panel BuildPage3()
    {
        var p = new Panel();
        p.Controls.Add(Banner("3", "工具与逻辑"));
        int y = 64;

        _chkTools = new CheckBox { Text = "安装截图操作工具 (Python: scr, ocr, see, act, browser)", Left = 4, Top = y, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 10F, FontStyle.Bold), ForeColor = TextDark };
        p.Controls.Add(_chkTools);
        p.Controls.Add(StyledLabel("需要 Python + Tesseract OCR。包含屏幕截图、OCR、鼠标键盘、浏览器自动化。", 28, y + 26, 8.5f, FontStyle.Regular, TextGray));

        y += 76;
        _chkLogic = new CheckBox { Text = "添加截图辅助逻辑 (写入 CLAUDE.md)", Left = 4, Top = y, Checked = true, AutoSize = true, Font = new Font(Font.FontFamily, 10F, FontStyle.Bold), ForeColor = TextDark };
        p.Controls.Add(_chkLogic);
        _txtLogic = new TextBox { Left = 28, Top = y + 30, Width = 720, Height = 55, Multiline = true, ReadOnly = true, BackColor = Color.FromArgb(248, 250, 253), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 8.5F), Text = "当用户请求不清晰时，主动询问是否需要截图查看。优先于猜测行为。" };
        p.Controls.Add(_txtLogic);
        return p;
    }
    Panel BuildPage4()
    {
        var p = new Panel();
        p.Controls.Add(Banner("4", "安全模式"));
        int y = 64;

        var cardSafe = new Panel { Left = 4, Top = y, Width = 360, Height = 64, BackColor = CardBg, Cursor = Cursors.Hand };
        cardSafe.Paint += (_, e) => { using var b = new SolidBrush(WarnOrange); e.Graphics.FillRectangle(b, 0, 0, 4, cardSafe.Height); };
        _rbSafe = new RadioButton { Text = "安全模式", Left = 20, Top = 8, AutoSize = true, Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold), ForeColor = TextDark };
        cardSafe.Controls.Add(_rbSafe);
        cardSafe.Controls.Add(StyledLabel("高威胁操作需要用户确认", 20, 34, 8f, FontStyle.Regular, TextGray));
        cardSafe.Click += (_, _) => _rbSafe.Checked = true; p.Controls.Add(cardSafe);

        y += 76;
        var cardPro = new Panel { Left = 4, Top = y, Width = 360, Height = 64, BackColor = Color.FromArgb(240, 253, 244), Cursor = Cursors.Hand };
        cardPro.Paint += (_, e) => { using var b = new SolidBrush(SuccessGreen); e.Graphics.FillRectangle(b, 0, 0, 4, cardPro.Height); };
        _rbPro = new RadioButton { Text = "专业通行模式 (推荐)", Left = 20, Top = 8, AutoSize = true, Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold), ForeColor = TextDark, Checked = true };
        cardPro.Controls.Add(_rbPro);
        cardPro.Controls.Add(StyledLabel("所有操作无需确认 — 适合高级用户", 20, 34, 8f, FontStyle.Regular, TextGray));
        cardPro.Click += (_, _) => _rbPro.Checked = true; p.Controls.Add(cardPro);
        return p;
    }
    Panel BuildPage5()
    {
        var p = new Panel();
        p.Controls.Add(Banner("5", "API 配置"));
        int y = 64;

        var cardDefault = new Panel { Left = 4, Top = y, Width = 360, Height = 52, BackColor = CardBg, Cursor = Cursors.Hand };
        cardDefault.Paint += (_, e) => { using var b = new SolidBrush(BrandBlue); e.Graphics.FillRectangle(b, 0, 0, 4, cardDefault.Height); };
        _rbApiNo = new RadioButton { Text = "默认 Anthropic API", Left = 20, Top = 14, AutoSize = true, Font = new Font(Font.FontFamily, 10F, FontStyle.Bold), ForeColor = TextDark, Checked = true };
        cardDefault.Controls.Add(_rbApiNo);
        cardDefault.Click += (_, _) => _rbApiNo.Checked = true; p.Controls.Add(cardDefault);

        y += 64;
        var cardDS = new Panel { Left = 4, Top = y, Width = 360, Height = 52, BackColor = CardBg, Cursor = Cursors.Hand };
        cardDS.Paint += (_, e) => { using var b = new SolidBrush(Color.FromArgb(175, 82, 222)); e.Graphics.FillRectangle(b, 0, 0, 4, cardDS.Height); };
        _rbApiYes = new RadioButton { Text = "DeepSeek (deepseek-v4-pro[1m])", Left = 20, Top = 14, AutoSize = true, Font = new Font(Font.FontFamily, 10F, FontStyle.Bold), ForeColor = TextDark };
        _rbApiYes.CheckedChanged += (_, _) => _txtApiKey.Enabled = _rbApiYes.Checked;
        cardDS.Controls.Add(_rbApiYes);
        cardDS.Click += (_, _) => _rbApiYes.Checked = true; p.Controls.Add(cardDS);

        y += 68;
        p.Controls.Add(StyledLabel("API Key:", 8, y + 5, 9f, FontStyle.Bold, TextDark));
        _txtApiKey = new TextBox { Left = 80, Top = y, Width = 420, PasswordChar = '*', Enabled = false, BackColor = Color.FromArgb(248, 250, 253), BorderStyle = BorderStyle.FixedSingle, Font = new Font(Font.FontFamily, 10f) };
        p.Controls.Add(_txtApiKey);
        p.Controls.Add(StyledLabel("预设: deepseek-v4-pro[1m] (4个模型槽位全部预填, subagent 用 v4-flash)", 80, y + 30, 8f, FontStyle.Regular, TextGray));
        return p;
    }

    // ═══ PAGE 6: 小白 API + 盘符 ══════════════════════
    Panel BuildPageSimpleApi()
    {
        var p = new Panel();
        p.Controls.Add(Banner("", "安装配置"));
        int y = 64;

        p.Controls.Add(StyledLabel("安装盘符:", 4, y + 5, 10f, FontStyle.Bold, TextDark));
        _cmbDriveSimple = new ComboBox { Left = 90, Top = y, Width = 90, Font = new Font(Font.FontFamily, 12F, FontStyle.Bold), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(240, 244, 255), ForeColor = BrandBlue };
        LoadDrives(_cmbDriveSimple);
        _cmbDriveSimple.SelectedIndexChanged += (_, _) => { _drive = _cmbDriveSimple.Text; UpdateSimplePath(); };
        p.Controls.Add(_cmbDriveSimple);
        _lblSimplePath = StyledLabel("", 195, y + 3, 8f, FontStyle.Regular, TextGray); p.Controls.Add(_lblSimplePath);

        y += 48;
        p.Controls.Add(StyledLabel("API 提供商:", 4, y + 5, 10f, FontStyle.Bold, TextDark));

        _rbApiNoSimple = new RadioButton { Text = "默认 Anthropic API（无需配置）", Left = 8, Top = y + 32, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), ForeColor = TextDark, Checked = true };
        _rbApiYesSimple = new RadioButton { Text = "DeepSeek API（deepseek-v4-pro[1m]）", Left = 8, Top = y + 60, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), ForeColor = TextDark };
        _rbApiYesSimple.CheckedChanged += (_, _) => _txtApiKeySimple.Enabled = _rbApiYesSimple.Checked;
        p.Controls.Add(_rbApiNoSimple); p.Controls.Add(_rbApiYesSimple);

        p.Controls.Add(StyledLabel("API Key:", 28, y + 92, 9f, FontStyle.Bold, TextDark));
        _txtApiKeySimple = new TextBox { Left = 100, Top = y + 89, Width = 420, PasswordChar = '*', Enabled = false, BackColor = Color.FromArgb(248, 250, 253), BorderStyle = BorderStyle.FixedSingle };
        p.Controls.Add(_txtApiKeySimple);
        p.Controls.Add(StyledLabel("填入 Key 即可，4 个模型槽全预填 + v4-flash subagent", 100, y + 118, 8f, FontStyle.Regular, TextGray));

        p.Controls.Add(StyledLabel("点击下一步将自动配置：Skills 全选 + 专业模式 + 截图工具 + 最大强度思考", 4, y + 146, 8f, FontStyle.Regular, Color.FromArgb(140, 150, 165)));
        UpdateSimplePath();
        return p;
    }

    void UpdateSimplePath() { if (_lblSimplePath != null) _lblSimplePath.Text = $"→ Node:{NodePath}  Git:{GitPath}  npm:{NpmPrefix}"; }

    // ═══ PAGE 7: 安装 ═════════════════════════════════
    Panel BuildPageInstall()
    {
        var p = new Panel();
        p.Controls.Add(Banner("", "安装"));

        int y = 62;
        _chkThinking = new CheckBox { Text = "启用最大强度思考", Left = 4, Top = y, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), ForeColor = TextDark, Checked = true };
        _chkNoUpdate = new CheckBox { Text = "禁用自动升级", Left = 320, Top = y, AutoSize = true, Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold), ForeColor = TextDark };
        p.Controls.Add(_chkThinking); p.Controls.Add(_chkNoUpdate);

        var btnInstall = new Button { Text = "开始安装", Left = 0, Top = y + 34, Width = 180, Height = 46, FlatStyle = FlatStyle.Flat, BackColor = SuccessGreen, ForeColor = Color.White, Font = new Font(Font.FontFamily, 12F, FontStyle.Bold), UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        btnInstall.Click += async (_, _) => await DoInstall();
        p.Controls.Add(btnInstall);

        _bar = new ProgressBar { Left = 195, Top = y + 44, Width = 555, Height = 24, Style = ProgressBarStyle.Continuous };
        p.Controls.Add(_bar);

        _rtbLog = new RichTextBox { Left = 0, Top = y + 94, Width = 750, Height = 360, ReadOnly = true, BackColor = Color.FromArgb(26, 27, 38), ForeColor = Color.FromArgb(169, 177, 214), Font = new Font("Consolas", 9F), BorderStyle = BorderStyle.None };
        p.Controls.Add(_rtbLog);
        return p;
    }

    // ═══ DETECT ═══════════════════════════════════════
    void LoadDrives(ComboBox cb) { cb.Items.Clear(); try { foreach (var d in DriveInfo.GetDrives()) if (d.IsReady && d.DriveType == DriveType.Fixed) cb.Items.Add(d.Name.TrimEnd('\\')); } catch { cb.Items.AddRange(new[] { "C:", "D:", "F:" }); } if (cb.Items.Count > 0) cb.SelectedIndex = 0; }
    async Task DetectEnvAsync()
    {
        if (_detecting) return;
        if (_cmbDrive != null && !_isSimple) _drive = _cmbDrive.Text;
        if (_cmbDriveSimple != null && _isSimple) _drive = _cmbDriveSimple.Text;
        if (string.IsNullOrEmpty(_drive)) _drive = "C:";
        _detecting = true;
        const int timeoutMs = 5000;

        if (IsHandleCreated)
            BeginInvoke(() => {
                foreach (var lbl in new[] { _lblNode, _lblGit, _lblPython, _lblClaude })
                    if (lbl != null) { lbl.Text = lbl.Text.Split(':')[0] + ": 检测中..."; lbl.ForeColor = Color.Gray; }
            });

        var tasks = new[] {
            Task.Run(async () => { var (ok, ver) = await TryCmdAsync("node", "--version", timeoutMs); return (name: "Node.js", ok, ver); }),
            Task.Run(async () => { var (ok, ver) = await TryCmdAsync("git", "--version", timeoutMs); return (name: "Git", ok, ver); }),
            Task.Run(async () => { var (ok, ver) = await TryCmdAsync("python", "--version", timeoutMs); if (!ok) { var r2 = await TryCmdAsync("python3", "--version", timeoutMs); if (r2.ok) return (name: "Python", ok: true, ver: r2.ver.Trim() + " (python3)"); } return (name: "Python", ok, ver); }),
            Task.Run(async () => { var (ok, ver) = await TryCmdAsync("claude", "--version", timeoutMs); return (name: "Claude Code", ok, ver); }),
        };

        foreach (var t in tasks)
        {
            _ = t.ContinueWith(t2 => {
                if (!IsHandleCreated) return;
                var (name, ok, ver) = t2.Result;
                BeginInvoke(() => {
                    switch (name) {
                        case "Node.js": _nodeOk = ok; SetStatus(_lblNode, "Node.js", ok, ver); break;
                        case "Git": _gitOk = ok; SetStatus(_lblGit, "Git", ok, ""); break;
                        case "Python": _pythonOk = ok; SetStatus(_lblPython, "Python", ok, ver.Trim()); break;
                        case "Claude Code": _claudeOk = ok; SetStatus(_lblClaude, "Claude Code", ok, ""); break;
                    }
                    if (_lblPath != null) _lblPath.Text = $"路径: Node→{NodePath}  Git→{GitPath}  npm→{NpmPrefix}  Tools→{ToolsPath}";
                    UpdateSimplePath();
                });
            }, TaskScheduler.Default);
        }

        await Task.WhenAll(tasks);
        _cachedDrive = _drive;
        _detecting = false;
    }

    async Task<(bool ok, string ver)> TryCmdAsync(string cmd, string args, int timeoutMs = 5000)
    {
        try {
            var psi = new ProcessStartInfo(cmd, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi)!;
            var readTask = p.StandardOutput.ReadToEndAsync();
            var cts = new CancellationTokenSource(timeoutMs);
            try { await p.WaitForExitAsync(cts.Token); }
            catch (OperationCanceledException) { try { p.Kill(entireProcessTree: true); } catch { } return (false, ""); }
            var output = (await readTask + p.StandardError.ReadToEnd()).Trim();
            return (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output), output);
        }
        catch { return (false, ""); }
    }
    void SetStatus(Label l, string n, bool ok, string v)
    {
        l.Text = ok ? $"{n}: 已安装 {v.Trim()}" : $"{n}: 未安装";
        l.ForeColor = ok ? Color.FromArgb(0, 150, 80) : Color.FromArgb(220, 100, 40);
        if (l.Parent is Panel p && p.Controls.Count > 1 && p.Controls[0] is Label d) {
            d.ForeColor = ok ? SuccessGreen : WarnOrange;
            p.BackColor = ok ? Color.FromArgb(240, 253, 244) : Color.FromArgb(255, 247, 237);
            p.Invalidate();
        }
    }
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
        L($"  Claude Code Installer v1.0.5 [{( _isSimple ? "小白" : "专业" )}] | Shimizu");
        L($"  Drive:{_drive}  Node:{NodePath}  Git:{GitPath}");
        L("═══════════════════════════════════");
        L("💡 提示: 安装过程中如弹出 UAC/权限提示窗口，请点击 [是] 以继续安装");

        int total = 9 + (_chkTools.Checked ? 3 : 0) + _skillChecks.Count(c => c.Checked) + ((_rbApiYes.Checked && _txtApiKey.Text.Length > 0) || (_rbApiYesSimple.Checked && _txtApiKeySimple.Text.Length > 0) ? 1 : 0) + (_chkLogic.Checked ? 1 : 0);
        int step = 0;

        try
        {
            step++; P(step, total); L($"[{step}/{total}] Node.js v20.18.0..."); if (!_nodeOk) { await DL("https://nodejs.org/dist/v20.18.0/node-v20.18.0-x64.msi", "node.msi", L); await RunAsync("msiexec", $"/i \"{Tmp("node.msi")}\" /qn INSTALLDIR=\"{NodePath}\"", L); } else L("  已安装");
            step++; P(step, total); L($"[{step}/{total}] Git..."); if (!_gitOk) { L("  🕐 Git 安装包约 60MB，预计需 2-5 分钟，请去喝杯咖啡吧~");
    L("  ⚠ 安装过程中如弹出 [更改硬盘驱动器] 或 UAC 提示，请点击 [是]"); await DL("https://github.com/git-for-windows/git/releases/download/v2.45.2.windows.1/Git-2.45.2-64-bit.exe", "git.exe", L); L("  🕐 正在静默安装 Git..."); await RunAsync(Tmp("git.exe"), $"/VERYSILENT /NORESTART /DIR=\"{GitPath}\"", L); } else L("  已安装");
            step++; P(step, total); L($"[{step}/{total}] npm prefix → {NpmPrefix}"); if (!Directory.Exists(NpmPrefix)) Directory.CreateDirectory(NpmPrefix); await RunAsync("npm", $"config set prefix \"{NpmPrefix}\"", L); UpdatePath(NodePath, NpmPrefix, Path.Combine(GitPath, "cmd"));
            step++; P(step, total); L($"[{step}/{total}] Claude Code (原生 .exe 下载)..."); L("  🕐 下载约 150MB，预计需 3-8 分钟"); await InstallClaudeNative(L);
            step++; P(step, total); L($"[{step}/{total}] CC Switch..."); L("  🕐 从 GitHub 下载最新版，请稍候~"); await InstallCCSwitch(L);

            if (_chkTools.Checked)
            {
                step++; P(step, total); L($"[{step}/{total}] 截图工具..."); InstallTools(L);
                step++; P(step, total); L($"[{step}/{total}] Python 依赖安装 ⚠ 需约 5-8 分钟，请耐心等待...");
                L("  (mss + pytesseract + pyautogui + pillow + pygetwindow + playwright ≈ 200MB)");
                await InstallPy(L);
                step++; P(step, total); L($"[{step}/{total}] Tesseract OCR..."); L("  🕐 下载约 70MB，安装过程可能弹出 UAC 提示请点 [是]"); await InstallTess(L);
            }

            foreach (var sk in _skills) { int idx = Array.FindIndex(_skills, x => x.n == sk.n); if (!_skillChecks[idx].Checked) continue; step++; P(step, total); L($"[{step}/{total}] Skill: {sk.n}..."); await InstallSkillAsync(sk.i, sk.n, L); }

            var apiKey = _isSimple ? _txtApiKeySimple.Text.Trim() : _txtApiKey.Text.Trim();
            step++; P(step, total); L($"[{step}/{total}] 写入 .claude.json (跳过登录)..."); WriteClaudeJson(L);
            step++; P(step, total); L($"[{step}/{total}] 配置 settings.json..."); WriteSettings(L);
            if ((_rbApiYes.Checked || _rbApiYesSimple.Checked) && apiKey.Length > 0) { step++; P(step, total); L($"[{step}/{total}] DeepSeek API..."); WriteDS(apiKey, L); }
            if (_chkLogic.Checked) { step++; P(step, total); L($"[{step}/{total}] 截图辅助逻辑..."); WriteCLAUDE(L); }

            step++; P(step, total); L($"[{step}/{total}] claude install (配置启动器 + PATH)..."); await RunAsync(Path.Combine(ToolsPath, "claude.exe"), "install", L);

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

            // Find claude executable — prefer .exe, fall back to .cmd, direct shortcut (no cmd.exe /k)
            string? claudeTarget = null;
            var searchPaths = new[] {
                Path.Combine(ToolsPath, "claude.exe"),
                Path.Combine(NpmPrefix, "claude.exe"),
                Path.Combine(NpmPrefix, "claude.cmd"),
                Path.Combine(NpmPrefix, "node_modules", ".bin", "claude.exe"),
                Path.Combine(NpmPrefix, "node_modules", ".bin", "claude.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Claude Code", "claude.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Claude Code", "claude.exe"),
            };
            foreach (var p in searchPaths) { if (File.Exists(p)) { claudeTarget = p; break; } }
            // Fallback: check claude.cmd in PATH, or just "claude"
            if (claudeTarget == null) claudeTarget = "claude";

            dynamic cl = shell.CreateShortcut(Path.Combine(desktop, "Claude Code.lnk"));
            cl.TargetPath = claudeTarget;
            cl.Description = "Claude Code";
            cl.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            cl.Save();
            log($"  ✓ Claude Code.lnk → {claudeTarget}");

            // Find CC Switch — search common install paths
            string? ccExe = null;
            var ccSearch = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CC Switch", "cc-switch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "CC Switch", "CC Switch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CC Switch", "cc-switch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "CC Switch", "CC Switch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CC Switch", "cc-switch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "CC Switch", "CC Switch.exe"),
            };
            foreach (var p in ccSearch) { if (File.Exists(p)) { ccExe = p; break; } }

            if (ccExe != null)
            {
                dynamic cc = shell.CreateShortcut(Path.Combine(desktop, "CC Switch.lnk"));
                cc.TargetPath = ccExe;
                cc.Description = "CC Switch";
                cc.Save();
                log($"  ✓ CC Switch.lnk → {ccExe}");
            }
            else log("  ⚠ CC Switch not found — install from https://github.com/farion1231/cc-switch");
        }
        catch (Exception ex) { log($"  快捷方式: {ex.Message}"); }
    }

    // ── Install helpers ───────────────────────────────
    async Task<string> RunAsync(string cmd, string args, Action<string> log) { try { var p = new Process { StartInfo = new ProcessStartInfo(cmd, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } }; p.Start(); var oT = p.StandardOutput.ReadToEndAsync(); var eT = p.StandardError.ReadToEndAsync(); var r = await Task.WhenAll(oT, eT); await p.WaitForExitAsync(); var txt = (r[0] + r[1]).Trim(); if (txt.Length > 0) log(txt); return txt; } catch (Exception ex) { log($"[ERR] {ex.Message}"); return ""; } }
    async Task<bool> DL(string url, string fn, Action<string> log) { var path = Tmp(fn); log($"  下载: {fn}"); var urls = new List<string>(); if (url.Contains("nodejs.org/dist")) { urls.Add(url.Replace("nodejs.org/dist", "npmmirror.com/mirrors/node")); urls.Add(url.Replace("nodejs.org/dist", "mirrors.tuna.tsinghua.edu.cn/nodejs-release")); urls.Add(url.Replace("nodejs.org/dist", "mirrors.ustc.edu.cn/node")); urls.Add(url); } else if (url.Contains("github.com/git-for-windows")) { urls.Add(url.Replace("github.com/git-for-windows/git/releases/download/v2.45.2.windows.1", "npmmirror.com/mirrors/git-for-windows/v2.45.2.windows.1")); urls.Add(url.Replace("github.com", "mirror.ghproxy.com/https://github.com")); urls.Add(url); } else if (url.Contains("github.com")) { urls.Add(url.Replace("github.com", "mirror.ghproxy.com/https://github.com")); urls.Add(url.Replace("github.com", "ghproxy.net/https://github.com")); urls.Add(url.Replace("github.com", "gitclone.com/github.com")); urls.Add(url); } else if (url.Contains("python.org")) { urls.Add(url.Replace("www.python.org/ftp/python", "npmmirror.com/mirrors/python")); urls.Add(url.Replace("www.python.org/ftp/python", "mirrors.huaweicloud.com/python")); urls.Add(url.Replace("www.python.org/ftp/python", "mirrors.tuna.tsinghua.edu.cn/python")); urls.Add(url); } else { urls.Add(url); } int idx=0; foreach (var u in urls) { idx++; try { using var wc = new WebClient(); wc.Headers.Add("User-Agent", "CCI/1.0"); var lastPct = 0; wc.DownloadProgressChanged += (_, e) => { var pct = e.ProgressPercentage; if (pct > lastPct+15) { lastPct = pct; log($"    [{idx}/{urls.Count}] {pct}%"); } }; var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(15)); await wc.DownloadFileTaskAsync(new Uri(u), path).WaitAsync(cts.Token); if (File.Exists(path) && new FileInfo(path).Length > 50000) { log($"    ✓ 完成"); return true; } } catch (Exception ex) { log($"    ✗ [{idx}/{urls.Count}] {ex.Message}"); } } return false; }
    async Task InstallSkillAsync(string skillId, string skillName, Action<string> log)
    {
        var registries = new[] {
            "https://registry.npmmirror.com",
            "https://registry.npmjs.org",
            "https://mirrors.cloud.tencent.com/npm",
            "https://mirrors.huaweicloud.com/repository/npm",
        };
        for (int ri = 0; ri < registries.Length; ri++)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    if (attempt > 0) { var delay = (int)Math.Pow(2, attempt) * 1000; log($"  重试 {attempt}/2, 等待 {delay / 1000}s..."); await Task.Delay(delay); }
                    var reg = registries[ri];
                    log($"  注册源: {new Uri(reg).Host} (尝试 {attempt + 1}/3)");
                    var result = await RunAsync("npx", $"-y --registry={reg} skills add {skillId} -g", log);
                    if (result.Contains("error", StringComparison.OrdinalIgnoreCase) || result.Contains("ERR!", StringComparison.OrdinalIgnoreCase)) { log($"  npx 返回错误，准备下一次尝试..."); continue; }
                    log($"  ✓ {skillName} 安装成功");
                    return;
                }
                catch (Exception ex) { log($"  尝试 {attempt + 1} 失败: {ex.Message}"); }
            }
            log($"  注册源 {new Uri(registries[ri]).Host} 所有重试耗尽，切换下一个...");
        }
        log($"  ✗ {skillName} 全部 {registries.Length} 个注册源均失败");
    }

    async Task InstallClaudeNative(Action<string> log)
    {
        var targetExe = Path.Combine(ToolsPath, "claude.exe");
        if (File.Exists(targetExe)) { log("  claude.exe 已存在"); return; }
        Directory.CreateDirectory(ToolsPath);

        // 1. Resolve version (multi-source with mirror fallback + hardcoded fallback)
        string? version = null;
        var verCandidates = new[] {
            "https://ghproxy.net/https://downloads.claude.ai/claude-code-releases/latest",
            "https://mirror.ghproxy.com/https://downloads.claude.ai/claude-code-releases/latest",
            "https://ghproxy.net/https://storage.googleapis.com/claude-code-dist-86c565f3-f756-42ad-8dfa-d59b1c096819/claude-code-releases/latest",
            "https://mirror.ghproxy.com/https://storage.googleapis.com/claude-code-dist-86c565f3-f756-42ad-8dfa-d59b1c096819/claude-code-releases/latest",
            "https://downloads.claude.ai/claude-code-releases/latest",
            "https://storage.googleapis.com/claude-code-dist-86c565f3-f756-42ad-8dfa-d59b1c096819/claude-code-releases/latest",
        };
        foreach (var vu in verCandidates) {
            try {
                log($"  检测版本: {new Uri(vu).Host}...");
                using var wc = new System.Net.WebClient();
                wc.Headers.Add("User-Agent", "CCI/1.0");
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                var v = (await wc.DownloadStringTaskAsync(vu).WaitAsync(cts.Token)).Trim();
                if (!string.IsNullOrEmpty(v) && v.Length < 30) { version = v; log($"  ✓ 版本: {v}"); break; }
            } catch (Exception ex) { log($"  ✗ {new Uri(vu).Host}: {ex.Message}"); }
        }
        if (string.IsNullOrEmpty(version)) {
            version = "1.0.36";
            log($"  ⚠ 所有版本检测源均失败，使用硬编码回退版本: {version}");
        }

        // 2. Build download URL chain (original + GCS + ghproxy mirrors)
        var plat = "win32-x64";
        var urls = new List<string> {
            $"https://downloads.claude.ai/claude-code-releases/{version}/{plat}/claude.exe",
            $"https://storage.googleapis.com/claude-code-dist-86c565f3-f756-42ad-8dfa-d59b1c096819/claude-code-releases/{version}/{plat}/claude.exe",
        };
        // prepend mirror-wrapped URLs for China
        var mirrors = new[] { "https://ghproxy.net/", "https://mirror.ghproxy.com/" };
        var org = urls.ToList();
        foreach (var m in mirrors) foreach (var u in org) urls.Insert(0, m + u);

        // 3. Download with 30-min timeout (≈150MB)
        var downloaded = false;
        foreach (var u in urls) {
            try {
                log($"  下载: {new Uri(u).Host}...");
                var tmp = Path.GetTempFileName();
                using var wc = new System.Net.WebClient();
                wc.Headers.Add("User-Agent", "CCI/1.0");
                var lastPct = 0;
                wc.DownloadProgressChanged += (_, e) => { if (e.ProgressPercentage > lastPct + 10) { lastPct = e.ProgressPercentage; log($"    {e.ProgressPercentage}%"); } };
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
                await wc.DownloadFileTaskAsync(new Uri(u), tmp).WaitAsync(cts.Token);
                if (File.Exists(tmp) && new FileInfo(tmp).Length > 50_000_000) { File.Move(tmp, targetExe, true); downloaded = true; break; }
                try { File.Delete(tmp); } catch { }
            } catch (Exception ex) { log($"    ✗ {ex.Message}"); }
        }
        if (!downloaded) throw new Exception("claude.exe 下载失败，请检查网络或使用代理。可从 https://claude.ai/install 手动下载。");

        log($"  → {targetExe}");
        UpdatePath(ToolsPath);
    }

    async Task InstallCCSwitch(Action<string> log) { string? dl = null; try { using var wc = new WebClient(); wc.Headers.Add("User-Agent", "CCI/1.0"); var json = await wc.DownloadStringTaskAsync("https://api.github.com/repos/farion1231/cc-switch/releases/latest"); using var doc = JsonDocument.Parse(json); foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray()) { var n = a.GetProperty("name").GetString() ?? ""; if (n.EndsWith(".msi")) { dl = a.GetProperty("browser_download_url").GetString(); break; } if (n.EndsWith(".exe") && dl == null) dl = a.GetProperty("browser_download_url").GetString(); } } catch { log("  GitHub API 不可达，使用备用源..."); } if (dl == null) { dl = "https://www.axwsd.cn/cc/1.msi"; log("  备用源: axwsd.cn"); } else { log("  GitHub: " + dl); } try { var ext = Path.GetExtension(dl); var p = Tmp($"ccswitch{ext}"); if (await DL(dl, $"ccswitch{ext}", log)) { if (ext == ".msi") await RunAsync("msiexec", $"/i \"{p}\" /qn", log); else await RunAsync(p, "/VERYSILENT", log); } } catch { } }
    void InstallTools(Action<string> log) { Directory.CreateDirectory(ToolsPath); var asm = System.Reflection.Assembly.GetExecutingAssembly(); foreach (var name in new[] { "scr.py", "ocr.py", "act.py", "see.py", "browser.py" }) { var rn = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name)); if (rn == null) continue; using var s = asm.GetManifestResourceStream(rn); if (s == null) continue; using var sr = new StreamReader(s, Encoding.UTF8); File.WriteAllText(Path.Combine(ToolsPath, name), sr.ReadToEnd(), Encoding.UTF8); } log("  截图工具已安装"); }
    async Task InstallPy(Action<string> log) { if (!_pythonOk) { await DL("https://www.python.org/ftp/python/3.12.4/python-3.12.4-amd64.exe", "python.exe", log); var dir = Path.Combine(_drive, "Python312"); await RunAsync(Tmp("python.exe"), $"/quiet InstallAllUsers=1 TargetDir=\"{dir}\" Include_pip=1 Include_test=0", log); _pythonPath = Path.Combine(dir, "python.exe"); _pythonOk = true; } if (_pythonOk) await RunAsync(_pythonPath, $"-m pip install --target \"{PipPath}\" -i https://pypi.tuna.tsinghua.edu.cn/simple --trusted-host pypi.tuna.tsinghua.edu.cn mss pytesseract pyautogui pillow pygetwindow playwright", log); }
    async Task InstallTess(Action<string> log) { if (await DL("https://github.com/UB-Mannheim/tesseract/releases/download/v5.3.3.20231005/tesseract-ocr-w64-setup-5.3.3.20231005.exe", "tesseract.exe", log)) await RunAsync(Tmp("tesseract.exe"), $"/S /D={Path.Combine(_drive, "Tesseract-OCR")}", log); }
    void WriteSettings(Action<string> log) { Directory.CreateDirectory(ClaudeConfigDir); var path = Path.Combine(ClaudeConfigDir, "settings.json"); var perms = @"{ ""allow"":[""Bash(*)"",""PowerShell(*)"",""Read(*)"",""Write(*)"",""Edit(*)"",""Glob(*)"",""Grep(*)"",""WebFetch(*)"",""WebSearch(*)"",""Agent(*)"",""AskUserQuestion(*)"",""TaskCreate"",""TaskUpdate(*)"",""TaskList"",""TaskGet"",""TaskOutput(*)"",""TaskStop(*)"",""Monitor(*)"",""CronCreate(*)"",""CronDelete"",""CronList"",""PushNotification"",""ScheduleWakeup"",""EnterPlanMode"",""ExitPlanMode"",""EnterWorktree"",""ExitWorktree"",""Skill(*)"",""SendMessage(*)"",""SkillIssue(*)"",""NotebookEdit(*)"",""BashOutput(*)"",""KillShell(*)"",""TodoWrite(*)"",""mcp__plugin_playwright_playwright__*""], ""defaultMode"":""" + (_rbPro.Checked ? "bypassPermissions" : "default") + @"""}"; try { JsonNode node; if (File.Exists(path)) { node = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) ?? JsonNode.Parse("{}")!; } else { node = JsonNode.Parse("{}")!; } node["permissions"] = JsonNode.Parse(perms); node["theme"] = "dark"; if (_chkThinking.Checked) { node["thinking"] = "enabled"; node["thinkingBudget"] = "maximum"; } if (_chkNoUpdate.Checked) node["autoUpdatesChannel"] = "none"; File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); } catch { var fallback = new JsonObject { ["theme"] = "dark", ["permissions"] = JsonNode.Parse(perms) }; if (_chkThinking.Checked) { fallback["thinking"] = "enabled"; fallback["thinkingBudget"] = "maximum"; } if (_chkNoUpdate.Checked) fallback["autoUpdatesChannel"] = "none"; File.WriteAllText(path, fallback.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); } log("  → " + path); }
    void WriteDS(string key, Action<string> log) { var path = Path.Combine(ClaudeConfigDir, "settings.json"); try { JsonNode node; if (File.Exists(path)) { node = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) ?? new JsonObject(); } else { node = new JsonObject(); } var env = node["env"] as JsonObject ?? new JsonObject(); node["env"] = env; env["ANTHROPIC_BASE_URL"] = "https://api.deepseek.com/anthropic"; env["ANTHROPIC_AUTH_TOKEN"] = key; env["ANTHROPIC_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_OPUS_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_SONNET_MODEL"] = "deepseek-v4-pro[1m]"; env["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = "deepseek-v4-flash"; env["CLAUDE_CODE_SUBAGENT_MODEL"] = "deepseek-v4-flash"; env["CLAUDE_CODE_EFFORT_LEVEL"] = "max"; File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); log("  DeepSeek API 已配置 (v4-pro + v4-flash subagent)"); } catch (Exception ex) { log($"  DeepSeek 配置失败: {ex.Message}"); } }
    void WriteClaudeJson(Action<string> log) { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude.json"); try { var node = new JsonObject(); node["hasCompletedOnboarding"] = true; File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8); log("  → " + path); } catch (Exception ex) { log($"  .claude.json failed: {ex.Message}"); } }
    void WriteCLAUDE(Action<string> log) { var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CLAUDE.md"); try { var rule = "\n\n# Screenshot Assist\nWhen user request is ambiguous, ask:\n\"Would you like me to take a screenshot?\"\n"; if (File.Exists(path)) { var e = File.ReadAllText(path, Encoding.UTF8); if (!e.Contains("Screenshot Assist")) File.WriteAllText(path, e + rule, Encoding.UTF8); } else File.WriteAllText(path, rule.Trim(), Encoding.UTF8); log("  → " + path); } catch { } }
    void UpdatePath(params string[] dirs) { try { var cur = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? ""; foreach (var d in dirs) { var c = d.TrimEnd('\\', '/'); if (!cur.Split(';').Any(e => e.TrimEnd('\\', '/').Equals(c, StringComparison.OrdinalIgnoreCase))) cur = c + ";" + cur; } Environment.SetEnvironmentVariable("PATH", cur, EnvironmentVariableTarget.User); } catch { } }
    string Tmp(string fn) => Path.Combine(Path.GetTempPath(), fn);
    Label L(string t, int x, int y, float sz, FontStyle fs, Color c) => new() { Text = t, Location = new Point(x, y), AutoSize = true, Font = new Font(Font.FontFamily, sz, fs), ForeColor = c };
    Button NBtn(string t, int x, int y, int w, int h, Color bg, EventHandler cb) { var b = new Button { Text = t, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Font = new Font(Font.FontFamily, 9F, FontStyle.Bold), UseVisualStyleBackColor = false }; b.Click += cb; return b; }
}
