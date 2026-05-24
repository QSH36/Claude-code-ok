# V17: UI Redesign + Version Check Fix — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade installer UI to Shimizu Brand gradient-modern style across all 8 wizard pages, fix Claude Code version detection with multi-source fallback, set version to 1.0.5.

**Architecture:** Single-file C# WinForms (.NET 8). All UI in `Form1.cs`. Add custom GDI+ helper methods for gradient panels, rounded cards, and styled controls. No new files, no third-party dependencies. Keep all existing install logic unchanged.

**Tech Stack:** C# WinForms .NET 8, GDI+ custom painting, single-file publish

---

### File Map

| File | Action | Purpose |
|------|--------|---------|
| `src/ClaudeCodeInstaller/Form1.cs` | Modify | Main UI rewrite + version check fix |
| `src/ClaudeCodeInstaller/ClaudeCodeInstaller.csproj` | Modify | Version → 1.0.5.0 |
| `versions/V17/` | Create | Archive V17 source + build |

---

### Task 1: Version bump + create V17 folder

**Files:**
- Modify: `src/ClaudeCodeInstaller/ClaudeCodeInstaller.csproj`
- Modify: `src/ClaudeCodeInstaller/Form1.cs:67`
- Create: `versions/V17/`

- [ ] **Step 1: Bump version in csproj**

In `ClaudeCodeInstaller.csproj`, change version lines:
```xml
<Version>1.0.5.0</Version>
<FileVersion>1.0.5.0</FileVersion>
```

- [ ] **Step 2: Update window title in Form1.cs**

In `Form1.cs` line 67, change:
```csharp
Text = "Claude Code 安装器 v1.0.5";
```

- [ ] **Step 3: Update install banner text**

In `Form1.cs` line 342, change:
```csharp
L($"  Claude Code Installer v1.0.5 [{( _isSimple ? "小白" : "专业" )}] | Shimizu");
```

- [ ] **Step 4: Create V17 archive folder**

```powershell
New-Item -ItemType Directory -Force "F:\ClaudeCodeInstaller\versions\V17"
```

---

### Task 2: Add UI helper methods + color constants

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` (after field declarations, before constructor)

- [ ] **Step 1: Add color constants and helper methods**

Insert after `bool _detecting;` (line 29):

```csharp
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
        var p = new Panel { Width = 760, Height = 52, Margin = new Padding(0) };
        p.Paint += (_, e) => {
            using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, p.Width, p.Height), BrandBlue, BrandBlueEnd, 135f);
            e.Graphics.FillRectangle(br, new Rectangle(0, 0, p.Width, p.Height));
        };
        if (stepNum != "") {
            var badge = new Label { Text = stepNum, Left = 24, Top = 12, Width = 28, Height = 28,
                TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 11f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(255, 255, 255, 40) };
            p.Controls.Add(badge);
            p.Controls.Add(new Label { Text = title, Left = 62, Top = 14, AutoSize = true,
                Font = new Font(Font.FontFamily, 13f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent });
        } else {
            p.Controls.Add(new Label { Text = title, Left = 24, Top = 14, AutoSize = true,
                Font = new Font(Font.FontFamily, 13f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent });
        }
        return p;
    }

    Panel Card(int w, int h, Color leftBar) { var c = new Panel { Width = w, Height = h, BackColor = CardBg }; c.Paint += (_, e) => { using var br = new SolidBrush(leftBar); e.Graphics.FillRectangle(br, 0, 0, 3, c.Height); }; return c; }

    Button AccentBtn(string text, int x, int y, int w, int h, Color bg, EventHandler cb) {
        var b = new Button { Text = text, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat,
            BackColor = bg, ForeColor = Color.White, Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
            UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
        b.Paint += (_, e) => { using var br = new SolidBrush(bg); e.Graphics.FillRectangle(br, 0, 0, b.Width, b.Height); };
        b.Click += cb; return b;
    }

    Label StyledLabel(string text, int x, int y, float sz, FontStyle fs, Color c) =>
        new() { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font(Font.FontFamily, sz, fs), ForeColor = c };
```

- [ ] **Step 2: Add GDI+ smoothing to form constructor**

In `Form1()` constructor, after `BackColor = Color.FromArgb(245, 247, 250);`, add line 72b:
```csharp
        this.Paint += (_, e) => { e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; };
```

---

### Task 3: Redesign Welcome page (Page 0)

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `BuildPage0()` method (lines 138-150)

- [ ] **Step 1: Rewrite BuildPage0()**

Replace lines 138-150 with:

```csharp
    Panel BuildPage0()
    {
        var p = new Panel();
        var banner = new Panel { Left = 0, Top = 0, Width = 760, Height = 180, Margin = new Padding(0) };
        banner.Paint += (_, e) => {
            using var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, banner.Width, banner.Height), BrandBlue, BrandBlueEnd, 135f);
            e.Graphics.FillRectangle(br, new Rectangle(0, 0, banner.Width, banner.Height));
        };
        banner.Controls.Add(StyledLabel("SHIMIZU", 0, 32, 10f, FontStyle.Regular, Color.FromArgb(255, 255, 255, 128)) { TextAlign = ContentAlignment.MiddleCenter, Width = 760 });
        banner.Controls.Add(StyledLabel("Claude Code 安装器", 0, 62, 20f, FontStyle.Bold, Color.White) { TextAlign = ContentAlignment.MiddleCenter, Width = 760 });
        banner.Controls.Add(StyledLabel("一键安装 · 永久免费", 0, 100, 11f, FontStyle.Regular, Color.FromArgb(255, 255, 255, 180)) { TextAlign = ContentAlignment.MiddleCenter, Width = 760 });
        banner.Controls.Add(StyledLabel("v1.0.5", 0, 138, 9f, FontStyle.Regular, Color.FromArgb(255, 255, 255, 120)) { TextAlign = ContentAlignment.MiddleCenter, Width = 760 });
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
```

---

### Task 4: Redesign Environment Detection page (Page 1)

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `BuildPage1()` and `AddCard()` (lines 153-179)

- [ ] **Step 1: Rewrite BuildPage1()**

Replace lines 153-172 with:

```csharp
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
```

- [ ] **Step 2: Update SetStatus to style card background**

Modify `SetStatus()` method (line 329):

```csharp
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
```

Also remove the old `AddCard()` method (lines 174-179):

```csharp
    // REMOVED: old AddCard() — replaced by AddStatusCard() above
```

---

### Task 5: Redesign Skills page (Page 2)

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `BuildPage2()` (lines 182-192)

- [ ] **Step 1: Rewrite BuildPage2()**

Replace lines 182-192 with:

```csharp
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
```

---

### Task 6: Redesign Tools, Security, API pages (Page 3-5)

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `BuildPage3()`, `BuildPage4()`, `BuildPage5()` (lines 193-218)

- [ ] **Step 1: Rewrite BuildPage3() — Tools & Logic**

Replace lines 193-202:

```csharp
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
```

- [ ] **Step 2: Rewrite BuildPage4() — Security**

Replace lines 203-209:

```csharp
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
```

- [ ] **Step 3: Rewrite BuildPage5() — API**

Replace lines 210-218:

```csharp
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
```

---

### Task 7: Redesign Simple mode API page (Page 6)

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `BuildPageSimpleApi()` (lines 220-248)

- [ ] **Step 1: Rewrite BuildPageSimpleApi()**

Replace lines 220-248:

```csharp
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
```

---

### Task 8: Redesign Install page (Page 7)

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `BuildPageInstall()` (lines 254-266)

- [ ] **Step 1: Rewrite BuildPageInstall()**

Replace lines 254-266:

```csharp
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
```

---

### Task 9: Redesign navigation bar

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `BuildWizard()` nav bar section (lines 79-85)

- [ ] **Step 1: Rewrite nav bar styling**

Replace lines 79-85:

```csharp
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
```

---

### Task 10: Fix version detection bug

**Files:**
- Modify: `src/ClaudeCodeInstaller/Form1.cs` — `InstallClaudeNative()` (lines 476-531)

- [ ] **Step 1: Rewrite version detection in InstallClaudeNative()**

Replace lines 482-497 (the version detection block) with:

```csharp
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
```

---

### Task 11: Build, test, and archive

**Files:**
- Create: `versions/V17/*`
- Modify: `publish/ClaudeCodeInstaller.exe` (build output)

- [ ] **Step 1: Build the project**

```powershell
dotnet publish -c Release -o F:\ClaudeCodeInstaller\publish "F:\ClaudeCodeInstaller\src\ClaudeCodeInstaller\ClaudeCodeInstaller.csproj"
```

Expected: Build succeeds, `ClaudeCodeInstaller.exe` created in `publish/`

- [ ] **Step 2: Copy artifacts to V17 archive**

```powershell
$v17 = "F:\ClaudeCodeInstaller\versions\V17"
Copy-Item "F:\ClaudeCodeInstaller\publish\ClaudeCodeInstaller.exe" $v17 -Force
Copy-Item "F:\ClaudeCodeInstaller\src\ClaudeCodeInstaller\Form1.cs" $v17 -Force
Copy-Item "F:\ClaudeCodeInstaller\src\ClaudeCodeInstaller\ClaudeCodeInstaller.csproj" $v17 -Force
Copy-Item "F:\ClaudeCodeInstaller\src\ClaudeCodeInstaller\app.manifest" $v17 -Force
Copy-Item "F:\ClaudeCodeInstaller\src\ClaudeCodeInstaller\app.ico" $v17 -Force
Copy-Item "F:\ClaudeCodeInstaller\src\ClaudeCodeInstaller\Locale.cs" $v17 -Force
Copy-Item "F:\ClaudeCodeInstaller\src\ClaudeCodeInstaller\Program.cs" $v17 -Force
```

- [ ] **Step 3: Verify file sizes**

```powershell
Get-ChildItem "F:\ClaudeCodeInstaller\publish\ClaudeCodeInstaller.exe" | Select Name, Length
Get-ChildItem "F:\ClaudeCodeInstaller\versions\V17" | Select Name, Length
```

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "V17: Shimizu Brand UI redesign + version check multi-source fallback"
```

---

### Task 12: Launch and verify UI

- [ ] **Step 1: Run the installer**

```powershell
Start-Process "F:\ClaudeCodeInstaller\publish\ClaudeCodeInstaller.exe"
```

- [ ] **Step 2: Check each page visually**
  - Welcome page: Gradient banner + two styled cards
  - Environment: Step badge + colored status cards
  - Skills: Left-accent cards + pills
  - Security/API: Cards with click-to-select
  - Install: Dark terminal log

- [ ] **Step 3: Verify version detection (check logs)**

Click through to install page, start install, watch log output for version detection messages showing mirror attempts.
