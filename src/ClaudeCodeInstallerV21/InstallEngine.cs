using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeCodeInstallerV21;

public class InstallSettings
{
    public bool IsSimple { get; set; }
    public string Drive { get; set; } = "C:";
    public string NodePath { get; set; } = "";
    public string GitPath { get; set; } = "";
    public string NpmPrefix { get; set; } = "";
    public string ToolsPath { get; set; } = "";
    public string PythonPath { get; set; } = "";
    public string PipPath { get; set; } = "";
    public string TesseractPath { get; set; } = "";
    public bool NodeOk { get; set; }
    public bool GitOk { get; set; }
    public bool PythonOk { get; set; }
    public bool ClaudeOk { get; set; }
    public bool InstallTools { get; set; } = true;
    public bool InstallGitTool { get; set; } = true;
    public bool InstallPythonTool { get; set; } = true;
    public bool InstallTesseract { get; set; } = true;
    public bool InstallLogic { get; set; }
    public bool InstallCustomLogic { get; set; }
    public string CustomLogicContent { get; set; } = "";
    public List<string> Skills { get; set; } = new();
    public bool BypassPermissions { get; set; } = true;
    public string ApiProvider { get; set; } = "deepseek"; // anthropic|deepseek|custom
    public string ApiKey { get; set; } = "";
    public string ApiBaseUrl { get; set; } = "";
    public string CustomMainModel { get; set; } = "";
    public string CustomHaikuModel { get; set; } = "";
    public string CustomSonnetModel { get; set; } = "";
    public string CustomOpusModel { get; set; } = "";
    public string CustomSubagentModel { get; set; } = "";
    public bool MaxThinking { get; set; } = true;
    public bool NoUpdate { get; set; }
}

public class InstallEngine
{
    private readonly Action<string> _log;
    private readonly Action<int, int> _progress;
    private readonly InstallSettings _s;

    // Skill ID to display name lookup
    private static readonly Dictionary<string, string> SkillNames = new()
    {
        ["obra/superpowers"] = "Superpowers",
        ["vercel-labs/skills@find-skills"] = "Find Skills",
        ["document-skills@anthropic-agent-skills"] = "Document Skills",
        ["obra/frontend-design"] = "Frontend Design",
        ["skill-creator@claude-plugins-official"] = "Skill Creator",
        ["JuliusBrussee/caveman"] = "Caveman",
        ["yize/web-access"] = "Web Access",
        ["claude-mem@thedotmack"] = "Claude-mem",
        ["pua@claude-code-plugins"] = "PUA Skill",
        ["excalidraw-diagram@claude-plugins-official"] = "Excalidraw Diagram",
        ["genskills--code-review"] = "Code Review",
        ["genskills--security-audit"] = "Security Audit",
        ["genskills--test-generator"] = "Test Generator",
    };

    public InstallEngine(InstallSettings settings, Action<string> log, Action<int, int> progress)
    {
        _s = settings;
        _log = log;
        _progress = progress;
    }

    public async Task DoInstallAsync()
    {
        _log("═══════════════════════════════════");
        _log($"  Claude Code Installer v1.0.7 [{(_s.IsSimple ? "小白" : "专业")}] | Shimizu");
        _log($"  Drive:{_s.Drive}  Node:{_s.NodePath}  Git:{_s.GitPath}");
        _log("═══════════════════════════════════");
        _log("提示: 安装过程中如弹出 UAC/权限提示窗口，请点击 [是] 以继续安装");

        int total = 9
            + (_s.InstallTools ? 1 : 0)
            + (_s.InstallTesseract ? 1 : 0)
            + _s.Skills.Count
            + ((_s.ApiProvider != "anthropic" && _s.ApiKey.Length > 0) ? 1 : 0)
            + ((_s.InstallLogic || _s.InstallCustomLogic) ? 1 : 0);
        int step = 0;

        // ── Launch Git + Python as BACKGROUND tasks ──
        Task? gitTask = null, pyTask = null;
        if (_s.InstallGitTool && !_s.GitOk)
        {
            gitTask = Task.Run(async () =>
            {
                _log("  [后台] Git 开始下载安装 (约 60MB)...");
                await DL("https://github.com/git-for-windows/git/releases/download/v2.45.2.windows.1/Git-2.45.2-64-bit.exe", "git.exe");
                await RunAsync(Tmp("git.exe"), $"/VERYSILENT /NORESTART /DIR=\"{_s.GitPath}\"");
                _log("  [后台] Git 安装完成");
            });
        }
        if (_s.InstallPythonTool && !_s.PythonOk)
        {
            pyTask = Task.Run(async () =>
            {
                _log("  [后台] Python 开始下载安装 (约 30MB)...");
                await DL("https://www.python.org/ftp/python/3.12.4/python-3.12.4-amd64.exe", "python.exe");
                var dir = Path.Combine(_s.Drive, "Python312");
                await RunAsync(Tmp("python.exe"), $"/quiet InstallAllUsers=1 TargetDir=\"{dir}\" Include_pip=1 Include_test=0");
                _log("  [后台] Python 安装完成，安装 pip 依赖 (约 10 分钟，pip 会实时输出进度)...");
                await RunAsync(Path.Combine(dir, "python.exe"),
                    $"-m pip install --no-input --disable-pip-version-check --target \"{_s.PipPath}\" -i https://pypi.tuna.tsinghua.edu.cn/simple --trusted-host pypi.tuna.tsinghua.edu.cn mss pytesseract pyautogui pillow pygetwindow playwright",
                    timeoutSeconds: 1800);
                _log("  [后台] Python 依赖安装完成");
            });
        }

        try
        {
            // Step: Node.js v20.18.0
            step++; _progress(step, total);
            _log($"[{step}/{total}] Node.js v20.18.0...");
            if (!_s.NodeOk)
            {
                await DL("https://nodejs.org/dist/v20.18.0/node-v20.18.0-x64.msi", "node.msi");
                await RunAsync("msiexec", $"/i \"{Tmp("node.msi")}\" /qn INSTALLDIR=\"{_s.NodePath}\"");
            }
            else _log("  已安装");

            // Step: npm prefix
            step++; _progress(step, total);
            _log($"[{step}/{total}] npm prefix → {_s.NpmPrefix}");
            if (!Directory.Exists(_s.NpmPrefix)) Directory.CreateDirectory(_s.NpmPrefix);
            await RunAsync("npm", $"config set prefix \"{_s.NpmPrefix}\"");
            UpdatePath(_s.NodePath, _s.NpmPrefix, Path.Combine(_s.GitPath, "cmd"));

            // Step: Claude Code (native .exe)
            step++; _progress(step, total);
            _log($"[{step}/{total}] Claude Code (原生 .exe 下载)...");
            if (!_s.ClaudeOk)
            {
                _log("  下载约 150MB，预计需 3-8 分钟");
                await InstallClaudeNative();
            }
            else _log("  已安装");

            // Step: CC Switch
            step++; _progress(step, total);
            _log($"[{step}/{total}] CC Switch...");
            _log("  从 GitHub 下载最新版，请稍候~");
            await InstallCCSwitch();

            // Optional: Screenshot tools (Python scripts only)
            if (_s.InstallTools)
            {
                step++; _progress(step, total);
                _log($"[{step}/{total}] 截图工具...");
                InstallTools();
            }

            // Optional: Tesseract OCR
            if (_s.InstallTesseract)
            {
                step++; _progress(step, total);
                _log($"[{step}/{total}] Tesseract OCR...");
                _log("  下载约 70MB，安装过程可能弹出 UAC 提示请点 [是]");
                await InstallTess();
            }

            // Skills loop
            foreach (var skillId in _s.Skills)
            {
                step++; _progress(step, total);
                var name = SkillNames.TryGetValue(skillId, out var n) ? n : skillId;
                _log($"[{step}/{total}] Skill: {name}...");
                await InstallSkillAsync(skillId, name);
            }

            // Step: .claude.json
            step++; _progress(step, total);
            _log($"[{step}/{total}] 写入 .claude.json (跳过登录)...");
            WriteClaudeJson();

            // Step: settings.json
            step++; _progress(step, total);
            _log($"[{step}/{total}] 配置 settings.json...");
            WriteSettings();

            // Step: API config (DeepSeek or Custom)
            if (_s.ApiProvider != "anthropic" && _s.ApiKey.Length > 0)
            {
                step++; _progress(step, total);
                _log($"[{step}/{total}] {(_s.ApiProvider == "deepseek" ? "DeepSeek" : "自定义")} API...");
                WriteApiConfig();
            }

            // Step: CLAUDE.md logic
            if (_s.InstallLogic || _s.InstallCustomLogic)
            {
                step++; _progress(step, total);
                _log($"[{step}/{total}] 截图辅助逻辑...");
                WriteCLAUDE();
            }

            // Step: claude install
            step++; _progress(step, total);
            _log($"[{step}/{total}] claude install (配置启动器 + PATH)...");
            await RunAsync(Path.Combine(_s.ToolsPath, "claude.exe"), "install");

            // Step: Desktop shortcuts
            step++; _progress(step, total);
            _log($"[{step}/{total}] 创建桌面快捷方式...");
            CreateShortcuts();

            // ── Wait for background tasks (Git + Python) ──
            if (gitTask != null || pyTask != null)
            {
                _log("");
                _log("  等待后台安装完成...");
                var bgTasks = new List<Task>();
                if (gitTask != null) bgTasks.Add(gitTask);
                if (pyTask != null) bgTasks.Add(pyTask);
                try { await Task.WhenAll(bgTasks); _log("  ✓ 后台安装全部完成"); }
                catch (Exception ex) { _log($"  ⚠ 后台安装异常: {ex.Message}"); }
            }

            _progress(total, total);
            _log("");
            _log("═══════════════════════════════════");
            _log("  安装完成! 终端: claude | 桌面: Claude Code / CC Switch");
            _log("═══════════════════════════════════");
        }
        catch (Exception ex)
        {
            _log($"");
            _log($"!!! 错误: {ex.Message}");
            throw;
        }
    }

    // ── Core helpers ────────────────────────────────────

    async Task<string> RunAsync(string cmd, string args, int timeoutSeconds = 0)
    {
        try
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo(cmd, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };
            p.Start();

            var sb = new StringBuilder();
            var readTasks = new List<Task>();

            // Stream stdout line-by-line in real time (no buffering)
            readTasks.Add(Task.Run(async () =>
            {
                while (true)
                {
                    var line = await p.StandardOutput.ReadLineAsync();
                    if (line == null) break;
                    if (line.Length > 0 && !line.StartsWith("  ")) _log(line);
                    sb.AppendLine(line);
                }
            }));

            // Stream stderr line-by-line (pip progress bars go to stderr)
            readTasks.Add(Task.Run(async () =>
            {
                while (true)
                {
                    var line = await p.StandardError.ReadLineAsync();
                    if (line == null) break;
                    // Filter noisy pip stderr
                    var t = line.Trim();
                    if (t.Length > 0) _log(t);
                    sb.AppendLine(line);
                }
            }));

            var readDone = Task.WhenAll(readTasks);

            if (timeoutSeconds > 0)
            {
                var completed = await Task.WhenAny(readDone, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
                if (completed != readDone)
                {
                    _log($"  ⚠ 进程超时 ({timeoutSeconds}s)，强制结束...");
                    try { p.Kill(entireProcessTree: true); } catch { }
                    return sb.ToString().Trim();
                }
            }

            await readDone;
            await p.WaitForExitAsync();
            var txt = sb.ToString().Trim();
            return txt;
        }
        catch (Exception ex)
        {
            _log($"[ERR] {ex.Message}");
            return "";
        }
    }

    async Task<bool> DL(string url, string fn)
    {
        var path = Tmp(fn);
        _log($"  下载: {fn}");

        var urls = new List<string>();
        if (url.Contains("nodejs.org/dist"))
        {
            urls.Add(url.Replace("nodejs.org/dist", "npmmirror.com/mirrors/node"));
            urls.Add(url.Replace("nodejs.org/dist", "mirrors.tuna.tsinghua.edu.cn/nodejs-release"));
            urls.Add(url.Replace("nodejs.org/dist", "mirrors.ustc.edu.cn/node"));
            urls.Add(url);
        }
        else if (url.Contains("github.com/git-for-windows"))
        {
            urls.Add(url.Replace("github.com/git-for-windows/git/releases/download/v2.45.2.windows.1", "npmmirror.com/mirrors/git-for-windows/v2.45.2.windows.1"));
            urls.Add(url);
        }
        else if (url.Contains("github.com"))
        {
            urls.Add(url);
        }
        else if (url.Contains("python.org"))
        {
            urls.Add(url.Replace("www.python.org/ftp/python", "npmmirror.com/mirrors/python"));
            urls.Add(url.Replace("www.python.org/ftp/python", "mirrors.huaweicloud.com/python"));
            urls.Add(url.Replace("www.python.org/ftp/python", "mirrors.tuna.tsinghua.edu.cn/python"));
            urls.Add(url);
        }
        else
        {
            urls.Add(url);
        }

        int idx = 0;
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CCI/1.0");
        foreach (var u in urls)
        {
            idx++;
            try
            {
                // Phase 1: connect with short timeout
                using var connCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var resp = await http.GetAsync(u, HttpCompletionOption.ResponseHeadersRead, connCts.Token);
                resp.EnsureSuccessStatusCode();
                var total = resp.Content.Headers.ContentLength ?? -1;

                // Phase 2: download with generous timeout, starting AFTER headers received
                using var dlCts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
                using var fs = File.Create(path);
                using var stream = await resp.Content.ReadAsStreamAsync(dlCts.Token);
                var buffer = new byte[65536];
                long read = 0;
                int lastPct = 0;
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, dlCts.Token)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, bytesRead);
                    read += bytesRead;
                    if (total > 0) {
                        var pct = (int)(read * 100 / total);
                        if (pct > lastPct + 15) { lastPct = pct; _log($"    [{idx}/{urls.Count}] {pct}%"); }
                    }
                }
                if (File.Exists(path) && new FileInfo(path).Length > 50000)
                {
                    _log("    ✓ 完成");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log($"    ✗ [{idx}/{urls.Count}] {ex.Message}");
            }
        }
        return false;
    }

    // ── Skill installer ─────────────────────────────────

    async Task InstallSkillAsync(string skillId, string skillName)
    {
        var registries = new[]
        {
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
                    if (attempt > 0)
                    {
                        var delay = (int)Math.Pow(2, attempt) * 1000;
                        _log($"  重试 {attempt}/2, 等待 {delay / 1000}s...");
                        await Task.Delay(delay);
                    }
                    var reg = registries[ri];
                    _log($"  注册源: {new Uri(reg).Host} (尝试 {attempt + 1}/3)");
                    var result = await RunAsync("npx", $"-y --registry={reg} skills add {skillId} -g");
                    if (result.Contains("error", StringComparison.OrdinalIgnoreCase) || result.Contains("ERR!", StringComparison.OrdinalIgnoreCase))
                    {
                        _log("  npx 返回错误，准备下一次尝试...");
                        continue;
                    }
                    _log($"  ✓ {skillName} 安装成功");
                    return;
                }
                catch (Exception ex)
                {
                    _log($"  尝试 {attempt + 1} 失败: {ex.Message}");
                }
            }
            _log($"  注册源 {new Uri(registries[ri]).Host} 所有重试耗尽，切换下一个...");
        }
        _log($"  ✗ {skillName} 全部 {registries.Length} 个注册源均失败");
    }

    // ── Claude Code native .exe ─────────────────────────

    async Task InstallClaudeNative()
    {
        var targetExe = Path.Combine(_s.ToolsPath, "claude.exe");
        if (File.Exists(targetExe)) { _log("  claude.exe 已存在"); return; }
        Directory.CreateDirectory(_s.ToolsPath);

        // 1. Resolve version (official CDN + GCS + hardcoded fallback)
        string? version = null;
        var verCandidates = new[]
        {
            "https://downloads.claude.ai/claude-code-releases/latest",
            "https://storage.googleapis.com/claude-code-dist-86c565f3-f756-42ad-8dfa-d59b1c096819/claude-code-releases/latest",
        };
        foreach (var vu in verCandidates)
        {
            try
            {
                _log($"  检测版本: {new Uri(vu).Host}...");
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("CCI/1.0");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                var v = (await http.GetStringAsync(vu, cts.Token)).Trim();
                if (!string.IsNullOrEmpty(v) && v.Length < 30) { version = v; _log($"  ✓ 版本: {v}"); break; }
            }
            catch (Exception ex) { _log($"  ✗ {new Uri(vu).Host}: {ex.Message}"); }
        }
        if (string.IsNullOrEmpty(version)) { version = "2.1.150"; _log($"  ⚠ 使用回退版本: {version}"); }

        // 2. Build download URL chain (所有可用源依次尝试)
        var plat = "win32-x64";
        var urls = new List<string>
        {
            "https://links.8uid.com/d/55155fa7ea3799c152203007e7a4a92c",
            "https://fj.ly93.cc/478/44180484703/claude.exe",
            $"https://downloads.claude.ai/claude-code-releases/{version}/{plat}/claude.exe",
            "https://www.panurl.cn/down.php/ea77e60cade1fd320733930f9a7534d7.exe",
            "https://dl-b.feejii.com/storage/files/2026/05/24/8/5028555288/17796212017041.gz?t=6a12e7a2&rlimit=20&us=2FWc7rDlUZ&sign=4417596bbf4be6acf93af484c514f80f&download_name=claude.exe&p=null-3480982-44180484703",
            $"https://storage.googleapis.com/claude-code-dist-86c565f3-f756-42ad-8dfa-d59b1c096819/claude-code-releases/{version}/{plat}/claude.exe",
            $"file:///F:/ClaudeCodeLocal/claude.exe",
        };

        // 3. Download with retries per source
        var downloaded = false;
        for (int si = 0; si < urls.Count; si++)
        {
            var u = urls[si];
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var tmp = "";
                try
                {
                    // Handle local file copy (skip retries, local file won't change)
                    if (u.StartsWith("file:///"))
                    {
                        if (attempt > 0) break;
                        var localPath = u.Replace("file:///", "").Replace("/", "\\");
                        _log($"  本地复制: {localPath}");
                        if (File.Exists(localPath) && new FileInfo(localPath).Length > 50_000_000)
                        {
                            File.Copy(localPath, targetExe, true);
                            downloaded = true; _log("  ✓ 本地复制完成"); break;
                        }
                        _log("  本地文件不存在或无效");
                        break;
                    }

                    if (attempt == 0) _log($"  下载 [{si + 1}/{urls.Count}] (源 {si + 1}): {new Uri(u).Host}...");
                    else _log($"  重试 {new Uri(u).Host}...");
                    tmp = Path.GetTempFileName();

                    // No HttpClient.Timeout — use explicit CancellationToken for each phase
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("CCI/1.0");

                    // Phase 1: connect + headers with 60s timeout
                    using var connCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    using var resp = await http.GetAsync(u, HttpCompletionOption.ResponseHeadersRead, connCts.Token);
                    resp.EnsureSuccessStatusCode();
                    var total = resp.Content.Headers.ContentLength ?? -1;
                    _log(total > 0
                        ? $"    文件大小: {total / 1024.0 / 1024.0:F0} MB"
                        : "    文件大小未知");

                    // Phase 2: download body — per-read timeout to detect dead connections fast
                    using var dlCts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
                    using var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 65536, useAsync: true);
                    using var stream = await resp.Content.ReadAsStreamAsync();
                    var buffer = new byte[65536];
                    long read = 0;
                    int lastPct = 0;
                    int bytesRead;
                    var stallSeconds = TimeSpan.FromSeconds(120);
                    while (true)
                    {
                        using var readCts = new CancellationTokenSource(stallSeconds);
                        using var linked = CancellationTokenSource.CreateLinkedTokenSource(readCts.Token, dlCts.Token);
                        try { bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, linked.Token); }
                        catch (OperationCanceledException) when (!dlCts.IsCancellationRequested)
                        {
                            throw new HttpRequestException($"读取停滞超过 {stallSeconds.TotalSeconds:F0}s (已下载 {read}/{total} bytes)，连接可能已断开");
                        }
                        if (bytesRead == 0) break;
                        await fs.WriteAsync(buffer, 0, bytesRead, dlCts.Token);
                        read += bytesRead;
                        if (total > 0) {
                            var pct = (int)(read * 100 / total);
                            if (pct > lastPct + 10) { lastPct = pct; _log($"    {pct}% ({read / 1024.0 / 1024.0:F0}/{total / 1024.0 / 1024.0:F0} MB)"); }
                        }
                    }
                    await fs.FlushAsync();
                    if (fs.Length > 50_000_000)
                    {
                        fs.Close();
                        File.Move(tmp, targetExe, true);
                        downloaded = true; _log("  ✓ 下载完成"); break;
                    }
                    _log($"    文件大小不足 ({fs.Length} bytes), 重试...");
                    try { File.Delete(tmp); } catch { }
                }
                catch (OperationCanceledException) { _log($"    ✗ 超时 (源 {si + 1}, 尝试 {attempt + 1}/2)"); try { if (tmp != "") File.Delete(tmp); } catch { } }
                catch (HttpRequestException ex) { _log($"    ✗ 网络错误: {ex.Message}"); try { if (tmp != "") File.Delete(tmp); } catch { } }
                catch (Exception ex) { _log($"    ✗ {ex.Message}"); try { if (tmp != "") File.Delete(tmp); } catch { } }
            }
            if (downloaded) break;
        }
        if (!downloaded) { _log("  ✗ 所有下载源均失败"); throw new Exception("网络有问题，无法下载 Claude Code。请检查网络连接后重试。"); }

        _log($"  → {targetExe}");
        UpdatePath(_s.ToolsPath);
    }

    // ── CC Switch ───────────────────────────────────────

    async Task InstallCCSwitch()
    {
        // 1. Resolve primary URL from GitHub API
        string? primary = null;
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("CCI/1.0");
            var json = await http.GetStringAsync("https://api.github.com/repos/farion1231/cc-switch/releases/latest");
            using var doc = JsonDocument.Parse(json);
            foreach (var a in doc.RootElement.GetProperty("assets").EnumerateArray())
            {
                var n = a.GetProperty("name").GetString() ?? "";
                if (n.EndsWith(".msi")) { primary = a.GetProperty("browser_download_url").GetString(); break; }
                if (n.EndsWith(".exe") && primary == null) primary = a.GetProperty("browser_download_url").GetString();
            }
        }
        catch { _log("  GitHub API 不可达，使用备用源..."); }

        // 2. Build candidate list: primary + fallbacks (deduplicated)
        var seen = new HashSet<string>();
        var candidates = new List<string>();
        void AddCandidate(string? url)
        {
            if (!string.IsNullOrEmpty(url) && seen.Add(url)) candidates.Add(url);
        }
        AddCandidate(primary);
        AddCandidate("https://links.8uid.com/d/29da208a3c999a03089ea063c468b1d8");
        AddCandidate("https://fj.ly93.cc/478/44183061300/CC-Switch-v3.14.1-Windows.msi");
        AddCandidate("https://fj.ly93.cc/478/44183061300");

        // 3. Try each candidate until one works
        string? dl = null;
        using var dlHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        dlHttp.DefaultRequestHeaders.UserAgent.ParseAdd("CCI/1.0");
        for (int i = 0; i < candidates.Count; i++)
        {
            var cu = candidates[i];
            try
            {
                _log($"  CC Switch [{i + 1}/{candidates.Count}]: {new Uri(cu).Host}...");
                var tmpFile = Tmp("ccswitch.msi");
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                using var resp = await dlHttp.GetAsync(cu, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                resp.EnsureSuccessStatusCode();
                using var fs = File.Create(tmpFile);
                await resp.Content.CopyToAsync(fs, cts.Token);
                if (File.Exists(tmpFile) && new FileInfo(tmpFile).Length > 100000)
                {
                    dl = cu; _log($"    ✓ OK");
                    // Install from tmpFile
                    var ext = Path.GetExtension(cu);
                    if (ext == ".msi") await RunAsync("msiexec", $"/i \"{tmpFile}\" /qn");
                    else await RunAsync(tmpFile, "/VERYSILENT");
                    _log("  ✓ CC Switch 安装完成");
                    return;
                }
                _log($"    ✗ 文件太小 ({new FileInfo(tmpFile).Length} bytes)");
            }
            catch (Exception ex) { _log($"    ✗ {ex.Message}"); }
        }

        _log("  ✗ CC Switch 所有下载源均失败");
        _log("  网络有问题，跳过 CC Switch 安装。可稍后手动安装。");
    }

    // ── Tools ───────────────────────────────────────────

    void InstallTools()
    {
        Directory.CreateDirectory(_s.ToolsPath);
        var asm = Assembly.GetExecutingAssembly();
        foreach (var name in new[] { "scr.py", "ocr.py", "act.py", "see.py", "browser.py" })
        {
            var rn = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(name));
            if (rn == null) continue;
            using var s = asm.GetManifestResourceStream(rn);
            if (s == null) continue;
            using var sr = new StreamReader(s, Encoding.UTF8);
            File.WriteAllText(Path.Combine(_s.ToolsPath, name), sr.ReadToEnd(), Encoding.UTF8);
        }
        _log("  截图工具已安装");
    }

    // ── Python ──────────────────────────────────────────

    async Task InstallPy()
    {
        var pythonExe = "python";
        if (!_s.PythonOk)
        {
            var pythonDir = Path.Combine(_s.Drive, "Claude Code tool", "Python312");
            await DL("https://www.python.org/ftp/python/3.12.4/python-3.12.4-amd64.exe", "python.exe");
            await RunAsync(Tmp("python.exe"), $"/quiet InstallAllUsers=1 TargetDir=\"{pythonDir}\" Include_pip=1 Include_test=0");
            pythonExe = Path.Combine(pythonDir, "python.exe");
        }
        else if (!string.IsNullOrWhiteSpace(_s.PythonPath) && File.Exists(_s.PythonPath))
        {
            pythonExe = _s.PythonPath;
        }

        await RunAsync(pythonExe,
            $"-m pip install --target \"{_s.PipPath}\" -i https://pypi.tuna.tsinghua.edu.cn/simple --trusted-host pypi.tuna.tsinghua.edu.cn mss pytesseract pyautogui pillow pygetwindow playwright");
    }

    // ── Tesseract ───────────────────────────────────────

    async Task InstallTess()
    {
        var tessDir = Path.Combine(_s.Drive, "Claude Code tool", "Tesseract-OCR");
        if (await DL("https://github.com/UB-Mannheim/tesseract/releases/download/v5.3.3.20231005/tesseract-ocr-w64-setup-5.3.3.20231005.exe", "tesseract.exe"))
        {
            await RunAsync(Tmp("tesseract.exe"), $"/S /D={tessDir}");
        }
    }

    // ── settings.json ───────────────────────────────────

    void WriteSettings()
    {
        var claudeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
        Directory.CreateDirectory(claudeDir);
        var path = Path.Combine(claudeDir, "settings.json");

        var perms = @"{ ""allow"":[""Bash(*)"",""PowerShell(*)"",""Read(*)"",""Write(*)"",""Edit(*)"",""Glob(*)"",""Grep(*)"",""WebFetch(*)"",""WebSearch(*)"",""Agent(*)"",""AskUserQuestion(*)"",""TaskCreate"",""TaskUpdate(*)"",""TaskList"",""TaskGet"",""TaskOutput(*)"",""TaskStop(*)"",""Monitor(*)"",""CronCreate(*)"",""CronDelete"",""CronList"",""PushNotification"",""ScheduleWakeup"",""EnterPlanMode"",""ExitPlanMode"",""EnterWorktree"",""ExitWorktree"",""Skill(*)"",""SendMessage(*)"",""SkillIssue(*)"",""NotebookEdit(*)"",""BashOutput(*)"",""KillShell(*)"",""TodoWrite(*)"",""mcp__plugin_playwright_playwright__*""], ""defaultMode"":""" + (_s.BypassPermissions ? "bypassPermissions" : "default") + @"""}";

        try
        {
            JsonNode node;
            if (File.Exists(path))
            {
                node = JsonNode.Parse(File.ReadAllText(path, Encoding.UTF8)) ?? JsonNode.Parse("{}")!;
            }
            else
            {
                node = JsonNode.Parse("{}")!;
            }
            node["permissions"] = JsonNode.Parse(perms);
            node["theme"] = "dark";
            if (_s.MaxThinking) { node["thinking"] = "enabled"; node["thinkingBudget"] = "maximum"; }
            if (_s.NoUpdate) node["autoUpdatesChannel"] = "none";
            File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }
        catch
        {
            var fallback = new JsonObject
            {
                ["theme"] = "dark",
                ["permissions"] = JsonNode.Parse(perms),
            };
            if (_s.MaxThinking) { fallback["thinking"] = "enabled"; fallback["thinkingBudget"] = "maximum"; }
            if (_s.NoUpdate) fallback["autoUpdatesChannel"] = "none";
            File.WriteAllText(path, fallback.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }
        _log("  → " + path);
    }

    // ── .claude.json ────────────────────────────────────

    void WriteClaudeJson()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude.json");
        try
        {
            var node = new JsonObject();
            node["hasCompletedOnboarding"] = true;
            File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
            _log("  → " + path);
        }
        catch (Exception ex) { _log($"  .claude.json failed: {ex.Message}"); }
    }

    // ── API config (3 modes) ────────────────────────────

    void WriteApiConfig()
    {
        var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "settings.json");
        try
        {
            JsonNode node;
            if (File.Exists(settingsPath))
                node = JsonNode.Parse(File.ReadAllText(settingsPath)) ?? new JsonObject();
            else
                node = new JsonObject();
            var env = node["env"] as JsonObject ?? new JsonObject();
            node["env"] = env;

            if (_s.ApiProvider == "deepseek")
            {
                env["ANTHROPIC_BASE_URL"] = "https://api.deepseek.com/anthropic";
                env["ANTHROPIC_AUTH_TOKEN"] = _s.ApiKey;
                env["ANTHROPIC_MODEL"] = "deepseek-v4-pro[1m]";
                env["ANTHROPIC_DEFAULT_OPUS_MODEL"] = "deepseek-v4-pro[1m]";
                env["ANTHROPIC_DEFAULT_SONNET_MODEL"] = "deepseek-v4-pro[1m]";
                env["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = "deepseek-v4-flash";
                env["CLAUDE_CODE_SUBAGENT_MODEL"] = "deepseek-v4-flash";
                env["CLAUDE_CODE_EFFORT_LEVEL"] = "max";
            }
            else if (_s.ApiProvider == "custom")
            {
                var url = _s.ApiBaseUrl.Trim();
                if (!url.StartsWith("https://") && !url.StartsWith("http://")) url = "https://" + url;
                env["ANTHROPIC_BASE_URL"] = url;
                env["ANTHROPIC_AUTH_TOKEN"] = _s.ApiKey;
                env["ANTHROPIC_MODEL"] = _s.CustomMainModel;
                env["ANTHROPIC_DEFAULT_OPUS_MODEL"] = _s.CustomOpusModel;
                env["ANTHROPIC_DEFAULT_SONNET_MODEL"] = _s.CustomSonnetModel;
                env["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = _s.CustomHaikuModel;
                if (!string.IsNullOrWhiteSpace(_s.CustomSubagentModel))
                    env["CLAUDE_CODE_SUBAGENT_MODEL"] = _s.CustomSubagentModel;
            }
            // anthropic: do nothing (use default)

            File.WriteAllText(settingsPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            _log("  API 配置已写入");
        }
        catch (Exception ex) { _log($"  API 配置失败: {ex.Message}"); }
    }

    // ── CLAUDE.md logic ─────────────────────────────────

    void WriteCLAUDE()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "CLAUDE.md");
        try
        {
            if (_s.InstallCustomLogic && !string.IsNullOrWhiteSpace(_s.CustomLogicContent))
            {
                var customRule = "\n\n# Screenshot Assist\n" + _s.CustomLogicContent + "\n";
                if (File.Exists(path))
                {
                    var existing = File.ReadAllText(path, Encoding.UTF8);
                    if (!existing.Contains(_s.CustomLogicContent))
                        File.WriteAllText(path, existing + customRule, Encoding.UTF8);
                }
                else
                {
                    File.WriteAllText(path, customRule.Trim(), Encoding.UTF8);
                }
            }
            else if (_s.InstallLogic)
            {
                var rule = "\n\n# Screenshot Assist\nWhen user request is ambiguous, ask:\n\"Would you like me to take a screenshot?\"\n";
                if (File.Exists(path))
                {
                    var existing = File.ReadAllText(path, Encoding.UTF8);
                    if (!existing.Contains("Screenshot Assist"))
                        File.WriteAllText(path, existing + rule, Encoding.UTF8);
                }
                else
                {
                    File.WriteAllText(path, rule.Trim(), Encoding.UTF8);
                }
            }
            _log("  → " + path);
        }
        catch { }
    }

    // ── Desktop shortcuts ───────────────────────────────

    void CreateShortcuts()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;

            // Find claude executable - prefer .exe, fall back to .cmd
            string? claudeTarget = null;
            var searchPaths = new[]
            {
                Path.Combine(_s.ToolsPath, "claude.exe"),
                Path.Combine(_s.NpmPrefix, "claude.exe"),
                Path.Combine(_s.NpmPrefix, "claude.cmd"),
                Path.Combine(_s.NpmPrefix, "node_modules", ".bin", "claude.exe"),
                Path.Combine(_s.NpmPrefix, "node_modules", ".bin", "claude.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Claude Code", "claude.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Claude Code", "claude.exe"),
            };
            foreach (var p in searchPaths) { if (File.Exists(p)) { claudeTarget = p; break; } }
            if (claudeTarget == null) claudeTarget = "claude";

            dynamic cl = shell.CreateShortcut(Path.Combine(desktop, "Claude Code.lnk"));
            cl.TargetPath = claudeTarget;
            cl.Description = "Claude Code";
            cl.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            cl.Save();
            _log($"  ✓ Claude Code.lnk → {claudeTarget}");

            // Find CC Switch
            string? ccExe = null;
            var ccSearch = new[]
            {
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
                _log($"  ✓ CC Switch.lnk → {ccExe}");
            }
            else
            {
                _log("  ⚠ CC Switch not found — install from https://github.com/farion1231/cc-switch");
            }
        }
        catch (Exception ex) { _log($"  快捷方式: {ex.Message}"); }
    }

    // ── PATH helper ─────────────────────────────────────

    void UpdatePath(params string[] dirs)
    {
        try
        {
            var cur = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            foreach (var d in dirs)
            {
                var c = d.TrimEnd('\\', '/');
                if (!cur.Split(';').Any(e => e.TrimEnd('\\', '/').Equals(c, StringComparison.OrdinalIgnoreCase)))
                    cur = c + ";" + cur;
            }
            Environment.SetEnvironmentVariable("PATH", cur, EnvironmentVariableTarget.User);
        }
        catch { }
    }

    // ── Temp file helper ────────────────────────────────

    string Tmp(string fn) => Path.Combine(Path.GetTempPath(), fn);
}
