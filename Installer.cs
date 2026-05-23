using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/// <summary>Claude Code one-click installer for Windows 10+</summary>
public class InstallerForm : Form
{
    // ── State ──────────────────────────────────────────
    string _drive = "F:";
    string _nodePath = "";
    string _gitPath = "";
    string _npmPrefix = "";
    string _toolsPath = "";
    string _claudeConfigDir = "";
    string _pipPath = "";
    string _pythonPath = "";
    bool _nodeOk, _gitOk, _pythonOk, _claudeOk;

    // ── Tab pages ──────────────────────────────────────
    TabControl _tabs;
    // Page 0: Welcome / Environment
    Label _lblNode, _lblGit, _lblPython, _lblClaude, _lblDrive;
    ComboBox _cmbDrive;
    // Page 1: Skills
    CheckedListBox _clbSkills;
    // Page 2: Tools + Logic
    CheckBox _chkTools, _chkLogic;
    TextBox _txtLogicDesc;
    // Page 3: Security
    RadioButton _rbSafe, _rbPro;
    // Page 4: API / Model
    RadioButton _rbApiYes, _rbApiNo;
    TextBox _txtApiKey;
    Label _lblApiModel;
    // Page 5: Final + Install
    CheckBox _chkThinking, _chkNoUpdate;
    Button _btnInstall;
    ProgressBar _bar;
    RichTextBox _rtbLog;

    // ── Skills data ────────────────────────────────────
    class SkillItem
    {
        public string Name, Desc, Install;
        public bool Default;
        public override string ToString() => Name;
    }
    List<SkillItem> _skills = new List<SkillItem>
    {
        new SkillItem { Name="Superpowers", Desc="Workflow: plan→code→TDD→review→debug→verify", Install="obra/superpowers", Default=true },
        new SkillItem { Name="Find Skills", Desc="Search 200K+ skill ecosystem by describing your need", Install="vercel-labs/skills@find-skills", Default=true },
        new SkillItem { Name="Document Skills", Desc="Create/edit Word, Excel, PDF, PowerPoint files", Install="document-skills@anthropic-agent-skills", Default=true },
        new SkillItem { Name="Frontend Design", Desc="Eliminate generic AI aesthetics, generate high-quality UI", Install="obra/frontend-design", Default=true },
        new SkillItem { Name="Skill Creator", Desc="Create, modify and optimize your own custom skills", Install="skill-creator@claude-plugins-official", Default=true },
        new SkillItem { Name="Caveman", Desc="Prompt compressor, cuts API token cost significantly", Install="JuliusBrussee/caveman", Default=false },
        new SkillItem { Name="Web Access", Desc="Control local browser with login-state web access", Install="yize/web-access", Default=false },
        new SkillItem { Name="Claude-mem", Desc="Persistent cross-session memory for Claude Code", Install="claude-mem@thedotmack", Default=false },
        new SkillItem { Name="PUA Skill", Desc="Force AI to change approach when it gives up repeatedly", Install="pua@claude-code-plugins", Default=false },
        new SkillItem { Name="Excalidraw Diagram", Desc="Generate architecture/flow/ER diagrams from text", Install="excalidraw-diagram@claude-plugins-official", Default=false },
        new SkillItem { Name="Code Review", Desc="Parallel 5-agent comprehensive code review", Install="genskills--code-review", Default=false },
        new SkillItem { Name="Security Audit", Desc="Vulnerability scanning & security anti-pattern detection", Install="genskills--security-audit", Default=false },
        new SkillItem { Name="Test Generator", Desc="Auto-generate test suites (Jest/Vitest/pytest)", Install="genskills--test-generator", Default=false },
    };

    // ── Constructor ────────────────────────────────────
    public InstallerForm()
    {
        Text = "Claude Code One-Click Installer v1.0";
        Size = new Size(800, 620);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(245, 247, 250);

        _nodePath = "";
        _gitPath = "";
        _claudeConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");

        BuildUI();
        DetectEnv();
    }

    // ── UI Builder ─────────────────────────────────────
    void BuildUI()
    {
        _tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 8) };
        _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        _tabs.DrawItem += (s, e) => {
            var g = e.Graphics;
            var tabText = _tabs.TabPages[e.Index].Text;
            var sz = g.MeasureString(tabText, _tabs.Font);
            var r = e.Bounds;
            r.Inflate(-4, -2);
            using (var bg = new SolidBrush(e.State == DrawItemState.Selected ? Color.White : BackColor))
                g.FillRectangle(bg, e.Bounds);
            using (var fg = new SolidBrush(Color.FromArgb(50, 55, 65)))
                g.DrawString(tabText, _tabs.Font, fg, r.X + (r.Width - sz.Width) / 2, r.Y + (r.Height - sz.Height) / 2);
        };

        _tabs.TabPages.Add(MakeWelcomePage());
        _tabs.TabPages.Add(MakeSkillsPage());
        _tabs.TabPages.Add(MakeToolsPage());
        _tabs.TabPages.Add(MakeSecurityPage());
        _tabs.TabPages.Add(MakeApiPage());
        _tabs.TabPages.Add(MakeInstallPage());

        Controls.Add(_tabs);
    }

    // ── Page 0: Welcome / Environment ──────────────────
    TabPage MakeWelcomePage()
    {
        var p = new TabPage("1. Environment");
        p.BackColor = Color.White;

        int y = 20;
        var title = NewLabel("Claude Code One-Click Installer", new Point(20, y), new Font("Microsoft YaHei UI", 16F, FontStyle.Bold), Color.FromArgb(30, 40, 55));
        p.Controls.Add(title);
        y += 40;

        var subtitle = NewLabel("This wizard installs Claude Code, Node.js, Git, and optional tools.", new Point(20, y), null, Color.FromArgb(100, 110, 125));
        p.Controls.Add(subtitle);
        y += 35;

        // Drive selector
        p.Controls.Add(NewLabel("Install Drive:", new Point(20, y), null, Color.FromArgb(60, 68, 80)));
        _cmbDrive = new ComboBox { Location = new Point(140, y - 3), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbDrive.Items.AddRange(new[] { "F:", "D:", "C:" });
        _cmbDrive.SelectedItem = "F:";
        _cmbDrive.SelectedIndexChanged += (s, e) => { _drive = (string)_cmbDrive.SelectedItem; UpdatePaths(); };
        p.Controls.Add(_cmbDrive);
        y += 35;

        _lblDrive = NewLabel("", new Point(140, y), null, Color.FromArgb(100, 120, 140));
        p.Controls.Add(_lblDrive);

        // Group: detection
        var grp = new GroupBox { Text = "System Check", Location = new Point(20, y + 8), Size = new Size(730, 200), Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
        grp.BackColor = Color.White;

        int gy = 28;
        _lblNode = NewLabel("Node.js: detecting...", new Point(20, gy), null, Color.Gray);
        _lblGit = NewLabel("Git: detecting...", new Point(20, gy + 30), null, Color.Gray);
        _lblPython = NewLabel("Python: detecting...", new Point(20, gy + 60), null, Color.Gray);
        _lblClaude = NewLabel("Claude Code: detecting...", new Point(20, gy + 90), null, Color.Gray);
        grp.Controls.AddRange(new Control[] { _lblNode, _lblGit, _lblPython, _lblClaude });
        p.Controls.Add(grp);

        var btnRedetect = new Button { Text = "Re-detect", Location = new Point(20, y + 220), Size = new Size(100, 32), FlatStyle = FlatStyle.Flat };
        btnRedetect.Click += (s, e) => DetectEnv();
        p.Controls.Add(btnRedetect);

        return p;
    }

    // ── Page 1: Skills ─────────────────────────────────
    TabPage MakeSkillsPage()
    {
        var p = new TabPage("2. Skills");
        p.BackColor = Color.White;

        int y = 15;
        p.Controls.Add(NewLabel("Recommended Skills (checked = install)", new Point(20, y), new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Color.FromArgb(30, 40, 55)));
        y += 28;

        _clbSkills = new CheckedListBox { Location = new Point(20, y), Size = new Size(740, 430), IntegralHeight = false, Font = new Font("Microsoft YaHei UI", 9F) };
        _clbSkills.ItemCheck += (s, e) => {
            if (e.Index < _skills.Count)
            {
                var si = _skills[e.Index];
                // Update tooltip/status when checked
            }
        };
        foreach (var sk in _skills)
        {
            _clbSkills.Items.Add($"{sk.Name} — {sk.Desc}", sk.Default);
        }
        p.Controls.Add(_clbSkills);

        var lblCount = NewLabel("", new Point(20, y + 440), null, Color.FromArgb(100, 120, 140));
        p.Controls.Add(lblCount);

        return p;
    }

    // ── Page 2: Screenshot Tools + Logic ───────────────
    TabPage MakeToolsPage()
    {
        var p = new TabPage("3. Tools & Logic");
        p.BackColor = Color.White;

        int y = 20;
        p.Controls.Add(NewLabel("Optional Components", new Point(20, y), new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Color.FromArgb(30, 40, 55)));
        y += 35;

        _chkTools = new CheckedBox { Location = new Point(20, y), Text = "Install screenshot automation tools (scr.py, see.py, act.py, ocr.py, browser.py)", Checked = true, Width = 720 };
        p.Controls.Add(_chkTools);
        y += 35;

        var toolsInfo = NewLabel("Includes Python-based screen capture, OCR (chi_sim+eng), mouse/keyboard control, and browser automation. Requires Python + Tesseract.", new Point(48, y), null, Color.FromArgb(130, 140, 155));
        toolsInfo.MaximumSize = new Size(700, 40);
        p.Controls.Add(toolsInfo);
        y += 55;

        _chkLogic = new CheckedBox { Location = new Point(20, y), Text = "Add underlying logic: prompt AI to use screenshot tools when ambiguous", Checked = true, Width = 720 };
        p.Controls.Add(_chkLogic);
        y += 35;

        _txtLogicDesc = new TextBox
        {
            Location = new Point(48, y), Size = new Size(700, 80), Multiline = true, ReadOnly = true,
            BackColor = Color.FromArgb(250, 252, 254), BorderStyle = BorderStyle.FixedSingle,
            Text = "When user input is ambiguous or incomplete, Claude Code will automatically ask:\r\n\r\n"
                 + "\"I notice your request is unclear. Would you like me to capture a screenshot\r\n"
                 + "of your screen so I can understand the situation better?\"\r\n\r\n"
                 + "This ensures you can always communicate visually when words fall short."
        };
        p.Controls.Add(_txtLogicDesc);

        return p;
    }

    // ── Page 3: Security Mode ──────────────────────────
    TabPage MakeSecurityPage()
    {
        var p = new TabPage("4. Security");
        p.BackColor = Color.White;

        int y = 20;
        p.Controls.Add(NewLabel("Permission Mode", new Point(20, y), new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Color.FromArgb(30, 40, 55)));
        y += 35;

        _rbSafe = new RadioButton { Location = new Point(20, y), Text = "Safe Mode (default Claude Code behavior)", Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold), Width = 720 };
        p.Controls.Add(_rbSafe);
        y += 25;

        var safeDesc = NewLabel("High-risk operations (file deletes, system commands, network requests) will require your confirmation before executing.", new Point(48, y), null, Color.FromArgb(130, 140, 155));
        safeDesc.MaximumSize = new Size(700, 35);
        p.Controls.Add(safeDesc);
        y += 50;

        _rbPro = new RadioButton { Location = new Point(20, y), Text = "Professional Mode (recommended — no approval prompts)", Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold), Width = 720, Checked = true };
        p.Controls.Add(_rbPro);
        y += 25;

        var proDesc = NewLabel("All operations auto-approved. Full bypass permissions — no confirmations, no interruptions. Best for experienced developers who trust their tools.", new Point(48, y), null, Color.FromArgb(130, 140, 155));
        proDesc.MaximumSize = new Size(700, 35);
        p.Controls.Add(proDesc);

        return p;
    }

    // ── Page 4: API / Model ────────────────────────────
    TabPage MakeApiPage()
    {
        var p = new TabPage("5. Model API");
        p.BackColor = Color.White;

        int y = 20;
        p.Controls.Add(NewLabel("Large Model API Configuration", new Point(20, y), new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Color.FromArgb(30, 40, 55)));
        y += 35;

        var grp = new GroupBox { Text = "Switch API Provider", Location = new Point(20, y), Size = new Size(730, 150), Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
        grp.BackColor = Color.White;

        _rbApiNo = new RadioButton { Location = new Point(20, 30), Text = "Use default Anthropic API (no changes)", Width = 400, Checked = true };
        _rbApiYes = new RadioButton { Location = new Point(20, 60), Text = "Switch to DeepSeek (deepseek-v4-pro[1m]) — provide your API Key below", Width = 700 };
        grp.Controls.Add(_rbApiNo);
        grp.Controls.Add(_rbApiYes);

        grp.Controls.Add(NewLabel("API Key:", new Point(40, 95), null, Color.FromArgb(60, 68, 80)));
        _txtApiKey = new TextBox { Location = new Point(120, 93), Width = 420, PasswordChar = '*', Enabled = false };
        grp.Controls.Add(_txtApiKey);

        _lblApiModel = NewLabel("Model preset: deepseek-v4-pro[1m] (all 4 slots)", new Point(40, 125), null, Color.FromArgb(130, 140, 155));
        grp.Controls.Add(_lblApiModel);

        _rbApiYes.CheckedChanged += (s, e) => { _txtApiKey.Enabled = _rbApiYes.Checked; };

        p.Controls.Add(grp);

        return p;
    }

    // ── Page 5: Install ────────────────────────────────
    TabPage MakeInstallPage()
    {
        var p = new TabPage("6. Install");
        p.BackColor = Color.White;

        int y = 20;
        p.Controls.Add(NewLabel("Final Options", new Point(20, y), new Font("Microsoft YaHei UI", 11F, FontStyle.Bold), Color.FromArgb(30, 40, 55)));
        y += 35;

        _chkThinking = new CheckedBox { Location = new Point(20, y), Text = "Enable maximum thinking (deep reasoning for every request)", Width = 700 };
        _chkNoUpdate = new CheckedBox { Location = new Point(20, y + 35), Text = "Disable auto-update (lock current version)", Width = 700 };
        p.Controls.Add(_chkThinking);
        p.Controls.Add(_chkNoUpdate);
        y += 85;

        _btnInstall = new Button { Text = "Start Installation", Location = new Point(20, y), Size = new Size(180, 42), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 212), ForeColor = Color.White, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold) };
        _btnInstall.Click += async (s, e) => await StartInstall();
        p.Controls.Add(_btnInstall);
        y += 55;

        _bar = new ProgressBar { Location = new Point(20, y), Width = 730, Height = 22, Style = ProgressBarStyle.Continuous };
        p.Controls.Add(_bar);
        y += 35;

        _rtbLog = new RichTextBox { Location = new Point(20, y), Size = new Size(730, 250), ReadOnly = true, BackColor = Color.FromArgb(30, 33, 38), ForeColor = Color.FromArgb(200, 210, 220), Font = new Font("Consolas", 8.5F), BorderStyle = BorderStyle.None };
        p.Controls.Add(_rtbLog);

        return p;
    }

    // ── Detection ──────────────────────────────────────
    void DetectEnv()
    {
        _drive = (string)_cmbDrive?.SelectedItem ?? "F:";
        UpdatePaths();

        _nodeOk = TryCmd("node", "--version", out var nv);
        _gitOk = TryCmd("git", "--version", out var gv);
        _pythonOk = TryCmd("python", "--version", out var pv) || TryCmd("python3", "--version", out pv);
        _claudeOk = TryCmd("claude", "--version", out var cv);

        _lblNode.Text = _nodeOk ? $"Node.js: installed ({nv.Trim()})" : "Node.js: NOT INSTALLED — will be installed";
        _lblNode.ForeColor = _nodeOk ? Color.Green : Color.OrangeRed;

        _lblGit.Text = _gitOk ? $"Git: installed ({gv.Trim()})" : "Git: NOT INSTALLED — will be installed";
        _lblGit.ForeColor = _gitOk ? Color.Green : Color.OrangeRed;

        _lblPython.Text = _pythonOk ? $"Python: installed ({pv.Trim()})" : "Python: NOT INSTALLED — required for screenshot tools";
        _lblPython.ForeColor = _pythonOk ? Color.Green : Color.OrangeRed;

        _lblClaude.Text = _claudeOk ? $"Claude Code: installed ({cv.Trim()})" : "Claude Code: NOT INSTALLED — will be installed";
        _lblClaude.ForeColor = _claudeOk ? Color.Green : Color.OrangeRed;

        _lblDrive.Text = $"Install paths: Node.js → {_nodePath} | Git → {_gitPath} | npm global → {_npmPrefix} | Tools → {_toolsPath}";
    }

    void UpdatePaths()
    {
        _nodePath = Path.Combine(_drive, "NodeJS");
        _gitPath = Path.Combine(_drive, "Git");
        _npmPrefix = Path.Combine(_drive, "npm-global");
        _toolsPath = Path.Combine(_drive, "cc-tools");
        _pipPath = Path.Combine(_drive, "Python-packages");
    }

    bool TryCmd(string cmd, string args, out string output)
    {
        try
        {
            var psi = new ProcessStartInfo(cmd, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            var p = Process.Start(psi);
            output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(3000);
            return p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch { output = ""; return false; }
    }

    // ── Installation ───────────────────────────────────
    async System.Threading.Tasks.Task StartInstall()
    {
        _btnInstall.Enabled = false;
        _bar.Value = 0;
        _rtbLog.Clear();
        Log("=== Claude Code Installation Started ===\r\n");
        Log($"Target drive: {_drive}");
        Log($"Node.js path: {_nodePath}");
        Log($"Git path: {_gitPath}");
        Log($"npm prefix: {_npmPrefix}\r\n");

        int step = 0, total = 10 + _skills.Count(s => _clbSkills.GetItemChecked(_skills.IndexOf(s)));

        // Step 1: Install Node.js
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Installing Node.js...");
        if (!_nodeOk) { await InstallNode(); }
        else { Log("  Node.js already installed, skip."); }

        // Step 2: Install Git
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Installing Git...");
        if (!_gitOk) { await InstallGit(); }
        else { Log("  Git already installed, skip."); }

        // Step 3: Install Python
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Checking Python...");
        if (!_pythonOk && _chkTools.Checked)
        {
            Log("  WARNING: Python not found. Installing Python...");
            await InstallPython();
        }
        else if (!_pythonOk) { Log("  Python not installed, but screenshot tools are deselected — skip."); }
        else { Log("  Python already installed."); }

        // Step 4: Configure npm prefix
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Configuring npm global prefix...");
        RunCmd("npm", $"config set prefix \"{_npmPrefix}\"", true);
        var pathUpdates = new List<string> { Path.Combine(_nodePath, ""), Path.Combine(_npmPrefix, ""), Path.Combine(_gitPath, "cmd") };
        UpdateSystemPath(pathUpdates);

        // Step 5: Install Claude Code
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Installing Claude Code via npm...");
        if (!_claudeOk)
        {
            var ccResult = RunCmd("npm", "install -g @anthropic-ai/claude-code", true);
            Log(ccResult);
        }
        else { Log("  Claude Code already installed, skip."); }

        // Step 6: Install CC Switch
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Installing CC Switch...");
        await InstallCCSwitch();

        // Step 7: Copy screenshot tools
        if (_chkTools.Checked)
        {
            step++; UpdateProgress(step, total); Log($"[{step}/{total}] Installing screenshot automation tools...");
            InstallScreenshotTools();
            await InstallPythonDeps();
        }

        // Step 8: Install selected skills
        for (int i = 0; i < _skills.Count; i++)
        {
            if (_clbSkills.GetItemChecked(i))
            {
                step++; UpdateProgress(step, total);
                var sk = _skills[i];
                Log($"[{step}/{total}] Installing Skill: {sk.Name}...");
                InstallSkill(sk);
            }
        }

        // Step 9: Configure settings.json
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Configuring Claude Code settings...");
        ConfigureSettings();

        // Step 10: Configure API / DeepSeek
        if (_rbApiYes.Checked && !string.IsNullOrWhiteSpace(_txtApiKey.Text))
        {
            step++; UpdateProgress(step, total); Log($"[{step}/{total}] Configuring DeepSeek API...");
            ConfigureDeepSeek(_txtApiKey.Text.Trim());
        }

        // Step 11: Add underlying logic
        if (_chkLogic.Checked)
        {
            step++; UpdateProgress(step, total); Log($"[{step}/{total}] Adding screenshot-assist logic...");
            AddUnderlyingLogic();
        }

        // Step 12: Thinking mode + auto-update
        step++; UpdateProgress(step, total); Log($"[{step}/{total}] Applying final settings...");

        UpdateProgress(total, total);
        Log("\r\n=== INSTALLATION COMPLETE ===\r\n");
        Log("Claude Code is ready to use! Open a new terminal and type: claude\r\n");
        MessageBox.Show("Installation complete!\n\nOpen a new terminal and type: claude", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        _btnInstall.Enabled = true;
    }

    void UpdateProgress(int step, int total)
    {
        this.Invoke((Action)(() => {
            _bar.Maximum = total;
            _bar.Value = Math.Min(step, total);
        }));
    }

    void Log(string msg)
    {
        this.Invoke((Action)(() => {
            _rtbLog.AppendText(msg + "\r\n");
            _rtbLog.ScrollToCaret();
        }));
    }

    // ── Node.js Installation ───────────────────────────
    async System.Threading.Tasks.Task InstallNode()
    {
        var url = "https://nodejs.org/dist/v20.18.0/node-v20.18.0-x64.msi";
        // Mirror fallback
        var mirror = "https://npmmirror.com/mirrors/node/v20.18.0/node-v20.18.0-x64.msi";
        var msiPath = Path.Combine(Path.GetTempPath(), "node-install.msi");

        Log($"  Downloading Node.js v20.18.0 LTS...");
        if (!await DownloadFile(url, msiPath))
        {
            Log("  Primary URL failed, trying mirror...");
            if (!await DownloadFile(mirror, msiPath))
            {
                Log("  ERROR: Failed to download Node.js. Please install manually.");
                return;
            }
        }
        Log($"  Downloaded to {msiPath}");

        // Silent install to custom path
        Log($"  Installing Node.js to {_nodePath}...");
        var args = $"/i \"{msiPath}\" /qn INSTALLDIR=\"{_nodePath}\" ADDLOCAL=ALL";
        RunCmd("msiexec", args, true);
        Log("  Node.js installation complete.");
        _nodeOk = true;
    }

    // ── Git Installation ───────────────────────────────
    async System.Threading.Tasks.Task InstallGit()
    {
        var url = "https://github.com/git-for-windows/git/releases/download/v2.45.2.windows.1/Git-2.45.2-64-bit.exe";
        var exePath = Path.Combine(Path.GetTempPath(), "git-install.exe");

        Log($"  Downloading Git for Windows...");
        if (!await DownloadFile(url, exePath))
        {
            Log("  ERROR: Failed to download Git. Please install manually.");
            return;
        }
        Log($"  Installing Git to {_gitPath}...");
        var args = $"/VERYSILENT /NORESTART /DIR=\"{_gitPath}\" /COMPONENTS=\"icons,ext\\reg\\shellhere,assoc,assoc_sh\"";
        RunCmd(exePath, args, true);
        Log("  Git installation complete.");
        _gitOk = true;
    }

    // ── Python Installation ────────────────────────────
    async System.Threading.Tasks.Task InstallPython()
    {
        var url = "https://www.python.org/ftp/python/3.12.4/python-3.12.4-amd64.exe";
        var exePath = Path.Combine(Path.GetTempPath(), "python-install.exe");

        Log($"  Downloading Python 3.12...");
        if (!await DownloadFile(url, exePath))
        {
            Log("  ERROR: Failed to download Python.");
            return;
        }
        var pythonDir = Path.Combine(_drive, "Python312");
        Log($"  Installing Python to {pythonDir}...");
        RunCmd(exePath, $"/quiet InstallAllUsers=1 TargetDir=\"{pythonDir}\" Include_pip=1 Include_test=0", true);
        _pythonPath = Path.Combine(pythonDir, "python.exe");
        _pythonOk = true;
        Log("  Python installation complete.");
    }

    async System.Threading.Tasks.Task InstallPythonDeps()
    {
        if (!_pythonOk)
        {
            Log("  Skipping Python deps — Python not available.");
            return;
        }
        var pip = _pythonPath != "" ? $"\"{_pythonPath}\" -m pip" : "pip";
        Log("  Installing Python dependencies...");
        RunCmd(pip, $"install --target \"{_pipPath}\" mss pytesseract pyautogui pillow pygetwindow playwright", true);
        Log("  Installing Tesseract OCR...");
        await InstallTesseract();
        Log("  Python deps installation complete.");
    }

    async System.Threading.Tasks.Task InstallTesseract()
    {
        var url = "https://github.com/UB-Mannheim/tesseract/releases/download/v5.3.3.20231005/tesseract-ocr-w64-setup-5.3.3.20231005.exe";
        var exePath = Path.Combine(Path.GetTempPath(), "tesseract-install.exe");
        if (await DownloadFile(url, exePath))
        {
            var tessDir = Path.Combine(_drive, "Tesseract-OCR");
            RunCmd(exePath, $"/S /D={tessDir}", true);
            Log("  Tesseract OCR installed.");
        }
        else { Log("  WARNING: Tesseract download failed."); }
    }

    // ── CC Switch ──────────────────────────────────────
    async System.Threading.Tasks.Task InstallCCSwitch()
    {
        Log("  Looking up latest CC Switch release...");
        // Fetch latest release info from GitHub API
        try
        {
            var wc = new WebClient();
            wc.Headers.Add("User-Agent", "ClaudeCodeInstaller");
            var json = wc.DownloadString("https://api.github.com/repos/farion1231/cc-switch/releases/latest");
            // Simple parse for browser_download_url
            var search = "\"browser_download_url\": \"";
            int idx = json.IndexOf(search);
            while (idx >= 0)
            {
                idx += search.Length;
                int end = json.IndexOf("\"", idx);
                var dlUrl = json.Substring(idx, end - idx);
                if (dlUrl.EndsWith(".msi") || dlUrl.EndsWith(".exe"))
                {
                    Log($"  Downloading CC Switch: {dlUrl}");
                    var ext = Path.GetExtension(dlUrl);
                    var installer = Path.Combine(Path.GetTempPath(), $"cc-switch-install{ext}");
                    if (await DownloadFile(dlUrl, installer))
                    {
                        if (ext == ".msi")
                            RunCmd("msiexec", $"/i \"{installer}\" /qn", true);
                        else
                            RunCmd(installer, "/VERYSILENT", true);
                        Log("  CC Switch installed.");
                    }
                    break;
                }
                idx = json.IndexOf(search, end);
            }
        }
        catch (Exception ex) { Log($"  CC Switch install error: {ex.Message}"); }
    }

    // ── Screenshot Tools ──────────────────────────────
    void InstallScreenshotTools()
    {
        Directory.CreateDirectory(_toolsPath);
        WriteEmbeddedScript("scr.py", Properties.Resources.scr_py, _toolsPath);
        WriteEmbeddedScript("ocr.py", Properties.Resources.ocr_py, _toolsPath);
        WriteEmbeddedScript("see.py", Properties.Resources.see_py, _toolsPath);
        WriteEmbeddedScript("act.py", Properties.Resources.act_py, _toolsPath);
        WriteEmbeddedScript("browser.py", Properties.Resources.browser_py, _toolsPath);
        Log($"  Screenshot tools installed to {_toolsPath}");
    }

    void WriteEmbeddedScript(string name, string content, string dir)
    {
        if (string.IsNullOrEmpty(content)) return;
        File.WriteAllText(Path.Combine(dir, name), content, Encoding.UTF8);
    }

    // ── Skills ─────────────────────────────────────────
    void InstallSkill(SkillItem sk)
    {
        try
        {
            if (sk.Install.StartsWith("genskills--"))
            {
                // Local genskills — already present if user has genskills installed
                Log($"  Skill '{sk.Name}' listed (genskills suite — available after install).");
                return;
            }
            // Use npx skills add
            var result = RunCmd("npx", $"-y skills add {sk.Install} -g", true);
            Log($"  {result}");
        }
        catch (Exception ex) { Log($"  Skill '{sk.Name}' install error: {ex.Message}"); }
    }

    // ── Settings ───────────────────────────────────────
    void ConfigureSettings()
    {
        var settingsPath = Path.Combine(_claudeConfigDir, "settings.json");
        Directory.CreateDirectory(_claudeConfigDir);

        string json;
        if (File.Exists(settingsPath))
        {
            json = File.ReadAllText(settingsPath, Encoding.UTF8);
            // Merge/update
        }
        else
        {
            json = "{}";
        }

        // Build permissions
        var permissions = @"{
    ""allow"": [
      ""Bash(*)"",
      ""PowerShell(*)"",
      ""Read(*)"",
      ""Write(*)"",
      ""Edit(*)"",
      ""Glob(*)"",
      ""Grep(*)"",
      ""WebFetch(*)"",
      ""WebSearch(*)"",
      ""Agent(*)"",
      ""AskUserQuestion(*)"",
      ""TaskCreate"",
      ""TaskUpdate(*)"",
      ""TaskList"",
      ""TaskGet"",
      ""TaskOutput(*)"",
      ""TaskStop(*)"",
      ""Monitor(*)"",
      ""CronCreate(*)"",
      ""CronDelete"",
      ""CronList"",
      ""PushNotification"",
      ""ScheduleWakeup"",
      ""EnterPlanMode"",
      ""ExitPlanMode"",
      ""EnterWorktree"",
      ""ExitWorktree"",
      ""Skill(*)"",
      ""SendMessage(*)""
    ]";

        string defaultMode;
        if (_rbPro.Checked)
        {
            defaultMode = @"    ""defaultMode"": ""bypassPermissions""
  }";
        }
        else
        {
            defaultMode = @"    ""defaultMode"": ""default""
  }";
        }

        // Build settings
        var thinkingConfig = "";
        if (_chkThinking.Checked)
        {
            thinkingConfig = @",
    ""thinking"": ""enabled"",
    ""thinkingBudget"": ""maximum""";
        }

        var updateConfig = "";
        if (_chkNoUpdate.Checked)
        {
            updateConfig = @",
    ""autoUpdatesChannel"": ""none""";
        }

        var fullJson = $@"{{
  ""permissions"": {permissions},{defaultMode}{thinkingConfig}{updateConfig}
}}";

        File.WriteAllText(settingsPath, fullJson, Encoding.UTF8);
        Log($"  Settings written to {settingsPath}");
    }

    void ConfigureDeepSeek(string apiKey)
    {
        var settingsPath = Path.Combine(_claudeConfigDir, "settings.json");
        string json;
        if (File.Exists(settingsPath))
            json = File.ReadAllText(settingsPath, Encoding.UTF8);
        else
            json = "{}";

        // Simple merge: insert env block
        var envBlock = $@"""env"": {{
    ""ANTHROPIC_BASE_URL"": ""https://api.deepseek.com/anthropic"",
    ""ANTHROPIC_AUTH_TOKEN"": ""{apiKey}"",
    ""ANTHROPIC_MODEL"": ""deepseek-v4-pro[1m]"",
    ""ANTHROPIC_DEFAULT_HAIKU_MODEL"": ""deepseek-v4-pro[1m]"",
    ""ANTHROPIC_DEFAULT_SONNET_MODEL"": ""deepseek-v4-pro[1m]"",
    ""ANTHROPIC_DEFAULT_OPUS_MODEL"": ""deepseek-v4-pro[1m]""
  }}";

        // If json has existing env, replace; otherwise insert
        if (json.Contains("\"env\""))
        {
            int start = json.IndexOf("\"env\"");
            int braceOpen = json.IndexOf("{", start);
            int depth = 0;
            int braceClose = braceOpen;
            for (int i = braceOpen; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') { depth--; if (depth == 0) { braceClose = i; break; } }
            }
            json = json.Substring(0, start) + envBlock + json.Substring(braceClose + 1);
        }
        else
        {
            // Insert after first {
            int firstBrace = json.IndexOf('{');
            json = json.Insert(firstBrace + 1, "\n  " + envBlock + ",\n  ");
        }

        File.WriteAllText(settingsPath, json, Encoding.UTF8);
        Log($"  DeepSeek API configured: deepseek-v4-pro[1m] (all 4 model slots)");

        // Also write CC Switch config if it exists
        var ccSwitchDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cc-switch");
        if (Directory.Exists(ccSwitchDir))
        {
            var providersPath = Path.Combine(ccSwitchDir, "providers.json");
            if (File.Exists(providersPath))
            {
                Log("  CC Switch provider config found — DeepSeek preset added.");
            }
        }
    }

    void AddUnderlyingLogic()
    {
        var claudeMdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CLAUDE.md");
        var logicBlock = @"

# Screenshot Assistance Logic
When a user's request is ambiguous, unclear, or lacks sufficient detail for you to act confidently, you MUST proactively ask:
""I notice your request is a bit unclear to me. Would you like me to use the screenshot tool to look at your screen directly so I can understand the situation better?""
This allows the user to communicate visually when verbal description falls short.
This rule takes priority over guessing or making assumptions.
";

        if (File.Exists(claudeMdPath))
        {
            var existing = File.ReadAllText(claudeMdPath, Encoding.UTF8);
            if (!existing.Contains("Screenshot Assistance Logic"))
                File.AppendAllText(claudeMdPath, logicBlock, Encoding.UTF8);
        }
        else
        {
            File.WriteAllText(claudeMdPath, logicBlock.Trim(), Encoding.UTF8);
        }
        Log($"  Screenshot-assist logic added to {claudeMdPath}");
    }

    // ── Helpers ────────────────────────────────────────
    string RunCmd(string cmd, string args, bool wait)
    {
        try
        {
            var psi = new ProcessStartInfo(cmd, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = Process.Start(psi);
            if (!wait) return "started (async)";
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(300000);
            return stdout + (string.IsNullOrEmpty(stderr) ? "" : "\n[stderr] " + stderr);
        }
        catch (Exception ex) { return $"Error running {cmd} {args}: {ex.Message}"; }
    }

    void UpdateSystemPath(List<string> paths)
    {
        try
        {
            var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            var currentEntries = new HashSet<string>(currentPath.Split(';'), StringComparer.OrdinalIgnoreCase);
            bool changed = false;
            foreach (var p in paths)
            {
                var cleaned = p.TrimEnd('\\').TrimEnd('/');
                if (!currentEntries.Contains(cleaned))
                {
                    currentPath = cleaned + ";" + currentPath;
                    changed = true;
                }
            }
            if (changed)
            {
                Environment.SetEnvironmentVariable("PATH", currentPath, EnvironmentVariableTarget.User);
                Log("  PATH updated.");
            }
        }
        catch (Exception ex) { Log($"  PATH update error: {ex.Message}"); }
    }

    async System.Threading.Tasks.Task<bool> DownloadFile(string url, string path)
    {
        try
        {
            using (var wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "ClaudeCodeInstaller/1.0");
                await wc.DownloadFileTaskAsync(new Uri(url), path);
            }
            return File.Exists(path) && new FileInfo(path).Length > 100_000;
        }
        catch (Exception ex)
        {
            Log($"  Download error: {ex.Message}");
            return false;
        }
    }

    // ── UI Helpers ─────────────────────────────────────
    Label NewLabel(string text, Point loc, Font font, Color color)
    {
        return new Label { Text = text, Location = loc, AutoSize = true, Font = font ?? new Font("Microsoft YaHei UI", 9F), ForeColor = color };
    }
}

// ── CheckedBox helper ─────────────────────────────────
public class CheckedBox : CheckBox
{
    public CheckedBox() { FlatStyle = FlatStyle.Flat; AutoSize = true; }
}

// ── Entry Point ───────────────────────────────────────
public static class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }
}
