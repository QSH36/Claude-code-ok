<p align="center">
  <img src="src/app.ico" width="96" height="96" alt="Claude Code Installer">
</p>

<h1 align="center">Claude Code Windows 一键安装器</h1>

<p align="center">
  <strong>Shimizu 出品 · 国内网络优化 · 开箱即用</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-1.0.7-blue" alt="version">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-green" alt="platform">
  <img src="https://img.shields.io/badge/.NET-8.0-purple" alt=".net">
  <img src="https://img.shields.io/badge/size-~161MB-orange" alt="size">
  <img src="https://img.shields.io/badge/license-MIT-red" alt="license">
</p>

---

## 这是什么

在 Windows 上安装 Claude Code，**双击一个 exe，点几下鼠标**，等几分钟，就好了。

不用装 Node.js、不用配 npm 镜像、不用折腾环境变量、不用手动下 Git。安装器全自动搞定。

针对国内网络做了深度优化——所有下载源都有镜像加速，GitHub 不可达也能装。

---

## 功能

- **原生 claude.exe** — 不依赖 Node.js 运行，从官方 CDN 直接下载独立可执行文件
- **7 源下载链** — 官方 CDN + GCS + 多个 CDN 镜像 + 本地文件，每源自动重试
- **零配置启动** — 自动写入 `.claude.json` 跳过登录/OAuth，自动配置 `settings.json` 权限和思考模式
- **DeepSeek API** — 填入 Key 即切换到 DeepSeek，4 个模型槽位全预填 `deepseek-v4-pro[1m]`
- **自定义 API** — 支持任意 Anthropic 兼容的 API 端点，5 个模型槽 + Subagent 可选
- **工具选择** — 安装前可自由勾选/取消 Git、Python、Tesseract、截图脚本，Node.js 和 Claude Code 强制安装
- **已安装检测** — 自动检测系统已有的 Node.js / Git / Python / Claude Code，已安装的自动跳过，也可点"仍要安装"强制重装
- **专业 & 小白双模式** — 专业用户 8 步完整定制，小白用户 4 步快速安装
- **中英文双语** — 所有页面 8 页完整 i18n，切语言不丢已填内容
- **下载 99% 假死修复** — 每次读取 120s 独立超时，死连接快速检测自动切源
- **截图自动化套件** — 可选安装 Python 截图 / OCR / 键鼠 / 浏览器自动化
- **13 个精选 Skills** — 开发工作流、文档处理、代码审查、安全审计等，一键批量安装
- **CC Switch 集成** — 自动安装模型管理器，桌面快捷方式一键切换模型
- **桌面快捷方式** — 安装完成自动创建 Claude Code 和 CC Switch 快捷方式

---

## 安装流程

```
 1. Node.js v20.18.0       npm 中国镜像链
 2. Git for Windows         npmmirror + 官方
 3. npm 全局路径 + PATH
 4. Claude Code 原生 exe    7 源下载链 + 每源 2 次重试
 5. CC Switch 模型管理器    GitHub API + 备用直链
 6. 截图工具 (可选)         嵌入 Python 脚本
 7. Tesseract OCR (可选)    70MB 中英文识别引擎
 8. Skills                  npx 多镜像安装
 9. .claude.json            跳过登录
10. settings.json           权限 + 主题 + 思考模式
11. API 配置                DeepSeek / 自定义 / Anthropic
12. CLAUDE.md (可选)        截图辅助逻辑
13. claude install          注册 PATH
14. 桌面快捷方式             Claude Code + CC Switch
```

> Git 和 Python 后台并行安装，不阻塞主流程。下载全部走国内镜像加速。

---

## 使用方法

### 专业模式（8 页）

```
双击 ClaudeCodeInstaller.exe
 → 用户分流选择 → 我是专业用户
 → 环境检测（自动识别已安装软件）
 → Skills 选择（勾选需要的）
 → 辅助逻辑（截图规则 / 自定义 CLAUDE.md）
 → 工具选择（Git / Python / Tesseract / 截图脚本）
 → 安全模式（推荐专业通行）
 → API 配置（Anthropic / DeepSeek / 自定义）
 → 安装配置（盘符 / 自定义路径）
 → 开始安装
```

### 小白模式（4 页）

```
双击 ClaudeCodeInstaller.exe
 → 用户分流选择 → 我是小白用户
 → 工具选择（后台自动检测已安装软件）
 → API 配置（可选填 Key）
 → 安装配置（选盘符）
 → 开始安装
```

---

## DeepSeek API 配置

选择 DeepSeek 并填入 API Key，安装器自动配置：

| 配置项 | 值 |
|--------|-----|
| API 端点 | `https://api.deepseek.com/anthropic` |
| 主模型 | `deepseek-v4-pro[1m]` |
| Opus 槽位 | `deepseek-v4-pro[1m]` |
| Sonnet 槽位 | `deepseek-v4-pro[1m]` |
| Haiku 槽位 | `deepseek-v4-flash` |
| Subagent 模型 | `deepseek-v4-flash` |
| 思考强度 | max |

> API Key 获取: [platform.deepseek.com/api_keys](https://platform.deepseek.com/api_keys)

---

## 镜像加速

| 资源 | 镜像链 |
|------|--------|
| Node.js | npmmirror → tsinghua → ustc → 官方 |
| Git | npmmirror → 官方 |
| Python | npmmirror → huaweicloud → tsinghua → 官方 |
| Claude Code | downloads.claude.ai → GCS → CDN 镜像 → 本地 |
| npm registry | npmmirror → npmjs.org → tencent → huawei |
| pip | tsinghua |

---

## 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | C# WinForms (.NET 8) |
| UI 渲染 | WebView2 嵌入式浏览器 + HTML5 单页应用 |
| 发布 | Single-file self-contained win-x64, ReadyToRun |
| Node.js | v20.18.0 LTS |
| Git | v2.45.2 for Windows |
| Python | 3.12.4 + mss / pytesseract / pyautogui / playwright |
| OCR | Tesseract 5.3.3 |
| 安装包 | ~161 MB |

---

## 从源码构建

```powershell
# 要求: .NET 8 SDK
cd src/ClaudeCodeInstallerV21
dotnet publish -c Release -o ../../publish
# 输出: ../../publish/ClaudeCodeInstaller.exe
```

---

## 项目结构

```
ClaudeCodeInstaller/
├── src/ClaudeCodeInstallerV21/    # 主源码 (V21)
│   ├── Program.cs                 # .NET 8 WinForms 入口
│   ├── Form1.cs                   # 主窗口 + HTTP API + WebView2 宿主
│   ├── InstallEngine.cs           # 安装引擎 (~1000 行)
│   ├── Locale.cs                  # 中英文双语
│   ├── app.ico                    # 品牌图标
│   ├── wwwroot/
│   │   └── index.html             # 嵌入式 Web UI (单页应用)
│   └── Resources/                 # 嵌入的 Python 工具脚本
│       ├── scr.py                 # 截图
│       ├── ocr.py                 # OCR 识别
│       ├── act.py                 # 键鼠操作
│       ├── see.py                 # 截图 + OCR 组合
│       └── browser.py             # 浏览器自动化
├── landing.html                   # 项目首页
├── publish/                       # 构建输出
└── versions/                      # 历史版本归档
```

---

## 下载

| 方式 | 链接 |
|------|------|
| GitHub Releases | [Releases 页面](https://github.com/QSH36/Claude-code-ok/releases) |
| 直链下载 | [ClaudeCodeInstaller.exe](https://github.com/QSH36/Claude-code-ok/releases/latest) |

---

## 许可

MIT License · Copyright © 2026 Shimizu

---

<p align="center">
  <sub>Made with ❤️ by 清水</sub>
</p>
