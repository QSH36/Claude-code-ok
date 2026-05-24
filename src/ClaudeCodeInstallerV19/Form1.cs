using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ClaudeCodeInstallerV19;

public partial class Form1 : Form
{
    HttpListener _http = new();
    int _port;
    WebView2? _webView;
    bool _useBrowser;
    InstallEngine? _engine;

    // Progress state (updated by engine, read by poll)
    StringBuilder _logBuffer = new();
    int _progressPercent;
    int _progressTotal;
    bool _installDone;
    string? _installError;

    public Form1()
    {
        // Window setup
        Text = "Claude Code Installer v1.0.6 — Shimizu";
        Size = new Size(960, 740);
        MinimumSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = Color.FromArgb(0x1E, 0x1E, 0x2E);

        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            // Ignore if no icon available
        }

        // Find free port
        _port = FindFreePort();

        // Start HTTP server
        StartHttpServer();

        // Try WebView2, fall back to browser
        this.Load += async (_, _) => await InitializeAsync();
    }

    int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    void StartHttpServer()
    {
        _http.Prefixes.Add($"http://localhost:{_port}/");
        _http.Start();
        _ = Task.Run(ListenLoop);
    }

    async Task ListenLoop()
    {
        while (_http.IsListening)
        {
            try
            {
                var ctx = await _http.GetContextAsync();
                _ = Task.Run(() => HandleRequest(ctx));
            }
            catch (HttpListenerException)
            {
                // Listener was closed
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                break;
            }
        }
    }

    void HandleRequest(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url!.AbsolutePath;
        var response = ctx.Response;

        try
        {
            if (path == "/" || path == "/index.html")
            {
                // Serve embedded HTML
                var html = GetEmbeddedResource("index.html");
                var data = Encoding.UTF8.GetBytes(html);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = data.Length;
                response.OutputStream.Write(data, 0, data.Length);
            }
            else if (path == "/api/check-cmd")
            {
                // Parse query manually
                var query = ctx.Request.Url!.Query;
                var cmd = GetQueryParam(query, "cmd");
                var args = GetQueryParam(query, "args");
                try
                {
                    var psi = new ProcessStartInfo(cmd, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                    using var p = Process.Start(psi)!;
                    var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                    p.WaitForExit(5000);
                    var ok = p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
                    var json = JsonSerializer.Serialize(new { ok, ver = output.Trim() });
                    var data = Encoding.UTF8.GetBytes(json);
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentLength64 = data.Length;
                    response.OutputStream.Write(data, 0, data.Length);
                }
                catch
                {
                    var json = JsonSerializer.Serialize(new { ok = false, ver = "" });
                    var data = Encoding.UTF8.GetBytes(json);
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentLength64 = data.Length;
                    response.OutputStream.Write(data, 0, data.Length);
                }
            }
            else if (path == "/api/drives")
            {
                // Return available drives
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => d.Name.TrimEnd('\\'))
                    .ToList();
                if (drives.Count == 0)
                {
                    drives = new List<string> { "C:", "D:", "F:" };
                }

                var json = JsonSerializer.Serialize(drives);
                var data = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = data.Length;
                response.OutputStream.Write(data, 0, data.Length);
            }
            else if (path == "/api/install" && ctx.Request.HttpMethod == "POST")
            {
                // Read settings JSON, start install
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                var json = reader.ReadToEnd();
                var settings = JsonSerializer.Deserialize<InstallSettings>(json);

                if (settings == null)
                {
                    var bad = Encoding.UTF8.GetBytes("{\"status\":\"error\",\"message\":\"Invalid settings\"}");
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentLength64 = bad.Length;
                    response.OutputStream.Write(bad, 0, bad.Length);
                    response.Close();
                    return;
                }

                // Start install on background thread
                _ = Task.Run(async () =>
                {
                    await DoInstallAsync(settings);
                });

                var ok = Encoding.UTF8.GetBytes("{\"status\":\"started\"}");
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = ok.Length;
                response.OutputStream.Write(ok, 0, ok.Length);
            }
            else if (path == "/api/progress")
            {
                // Return current progress state as JSON
                var progress = new
                {
                    logs = _logBuffer.ToString(),
                    percent = _progressPercent,
                    total = _progressTotal,
                    done = _installDone,
                    error = _installError
                };
                var json = JsonSerializer.Serialize(progress);
                var data = Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = data.Length;
                response.OutputStream.Write(data, 0, data.Length);
            }
            else
            {
                response.StatusCode = 404;
                var notFound = Encoding.UTF8.GetBytes("{\"error\":\"Not found\"}");
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = notFound.Length;
                response.OutputStream.Write(notFound, 0, notFound.Length);
            }
        }
        catch (Exception ex)
        {
            try
            {
                response.StatusCode = 500;
                var err = Encoding.UTF8.GetBytes($"{{\"error\":\"{EscapeJson(ex.Message)}\"}}");
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = err.Length;
                response.OutputStream.Write(err, 0, err.Length);
            }
            catch
            {
                // Response already sent or stream broken
            }
        }
        finally
        {
            try
            {
                response.Close();
            }
            catch
            {
                // Already closed
            }
        }
    }

    static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    static string GetQueryParam(string query, string key)
    {
        if (string.IsNullOrEmpty(query)) return "";
        query = query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == key)
                return Uri.UnescapeDataString(parts[1]);
        }
        return "";
    }

    string GetEmbeddedResource(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames();
        var match = names.FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            return $"<!-- Resource {fileName} not found among: {string.Join(", ", names)} -->";
        }
        using var stream = asm.GetManifestResourceStream(match);
        if (stream == null)
        {
            return $"<!-- Resource stream is null for: {match} -->";
        }
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    async Task InitializeAsync()
    {
        try
        {
            // Check if WebView2 runtime is available
            string? wv2Path = null;
            try
            {
                wv2Path = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch
            {
                // WebView2 runtime not found
            }

            if (!string.IsNullOrEmpty(wv2Path))
            {
                // WebView2 mode
                _webView = new WebView2 { Dock = DockStyle.Fill };
                Controls.Add(_webView);

                var env = await CoreWebView2Environment.CreateAsync();
                await _webView.EnsureCoreWebView2Async(env);
                _webView.CoreWebView2.Navigate($"http://localhost:{_port}/");
                return;
            }
        }
        catch
        {
            // Any failure in WebView2 initialization falls back to browser
        }

        // Fallback: open browser
        _useBrowser = true;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"http://localhost:{_port}/",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"无法打开浏览器: {ex.Message}\n\n请手动打开: http://localhost:{_port}/",
                "启动失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        var lbl = new Label
        {
            Text = "安装已在浏览器中打开。\n\n你可以关闭此窗口，在浏览器中继续安装。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei UI", 12),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(0x1E, 0x1E, 0x2E)
        };
        Controls.Add(lbl);
    }

    async Task DoInstallAsync(InstallSettings settings)
    {
        _logBuffer.Clear();
        _progressPercent = 0;
        _progressTotal = 0;
        _installDone = false;
        _installError = null;

        var log = (Action<string>)(s =>
        {
            lock (_logBuffer)
            {
                _logBuffer.AppendLine(s);
            }
        });

        var prog = (Action<int, int>)((current, total) =>
        {
            _progressPercent = current;
            _progressTotal = total;
        });

        _engine = new InstallEngine(settings, log, prog);
        try
        {
            await _engine.DoInstallAsync();
            lock (_logBuffer)
            {
                _logBuffer.AppendLine("\n安装完成!");
            }
            _progressPercent = _progressTotal;
        }
        catch (Exception ex)
        {
            lock (_logBuffer)
            {
                _logBuffer.AppendLine($"\n!!! 错误: {ex.Message}");
            }
            _installError = ex.Message;
        }
        finally
        {
            _installDone = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _http.Stop();
                _http.Close();
            }
            catch
            {
                // Ignore disposal errors
            }

            _webView?.Dispose();
        }
        base.Dispose(disposing);
    }
}
