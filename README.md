<p align="center">
  <img src="src/app.ico" width="96" height="96" alt="Claude Code Installer">
</p>

<h1 align="center">Claude Code Windows 一键安装器</h1>

<p align="center">
  <strong>Shimizu 出品 · 国内网络优化 · 开箱即用</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-1.0.6-blue" alt="version">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-green" alt="platform">
  <img src="https://img.shields.io/badge/.NET-8.0-purple" alt=".net">
  <img src="https://img.shields.io/badge/license-MIT-orange" alt="license">
</p>

---

## 这是什么

在 Windows 上安装 Claude Code 只需要 **双击一个 exe，点几下鼠标**。

无需手动装 Node.js、配 npm 镜像、下载 Git、折腾环境变量。安装器全自动搞定，针对国内网络环境做了深度优化。

---

## 功能亮点

- **原生 claude.exe 安装** — 不依赖 Node.js 运行，从官方 CDN 直接下载独立可执行文件
- **零配置启动** — 自动写入配置文件，首次运行不会弹出登录/OAuth 向导
- **DeepSeek API 支持** — 填入 Key 即可切换到 DeepSeek，自动配置所有模型槽位
- **国内网络优化** — 所有下载源都配了国内镜像链（清华/中科大/npmmirror/ghproxy）
- **中英文双语** — 安装界面支持中文/English 切换
- **专业 & 小白双模式** — 专业用户逐步定制，小白用户一键到底
- **截图自动化工具** — 可选安装 Python 截图/OCR/键鼠/浏览器自动化套件
- **13 个精选 Skills** — 开发工作流、文档处理、代码审查、安全审计等
- **CC Switch 集成** — 自动安装模型管理器，桌面快捷方式一键切换

---

## 安装流程

```
1. Node.js v20.18.0    ──  npmmirror 镜像
2. Git for Windows     ──  npmmirror + ghproxy 镜像
3. npm 全局路径配置
4. Claude Code 原生 exe ──  downloads.claude.ai + GCS + ghproxy
5. CC Switch 模型管理器 ──  GitHub API 自动获取最新版
6. 截图工具 (可选)     ──  Python + Tesseract OCR
7. 选中的 Skills       ──  npx 镜像安装
8. 配置文件写入        ──  settings.json + .claude.json
9. 桌面快捷方式        ──  Claude Code + CC Switch
```

---

## 使用方法

### 专业模式

```
1. 双击 ClaudeCodeInstaller.exe
2. 选择"我是专业用户"
3. 选择安装盘符 → 检测环境
4. 勾选需要的 Skills
5. 选择是否安装截图工具
6. 选择权限模式（推荐专业通行）
7. 配置 API（默认 Anthropic 或 DeepSeek）
8. 点击"开始安装"
```

### 小白模式

```
1. 双击 ClaudeCodeInstaller.exe
2. 选择"我是小白用户"
3. 选择安装盘符
4. 选择 API 提供商（可选填 Key）
5. 点击"开始安装"
```

---

## DeepSeek API 配置

选择 DeepSeek 并填入 API Key 后，安装器自动配置：

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

## 技术栈

| 组件 | 技术 |
|------|------|
| 安装器框架 | C# WinForms (.NET 8) |
| 发布方式 | Single-file self-contained (win-x64) |
| 安装包大小 | ~161 MB |
| Claude Code 安装 | 原生 exe (v2.1.x+) |
| 截图工具 | Python 3.12 + mss/pyautogui/playwright |
| OCR 引擎 | Tesseract 5.3.3 |

---

## 从源码构建

```powershell
# 要求: .NET 8 SDK
cd src/ClaudeCodeInstaller
dotnet publish -c Release -o ../publish
# 输出: ../publish/ClaudeCodeInstaller.exe
```

---

## 项目结构

```
ClaudeCodeInstaller/
├── src/ClaudeCodeInstaller/    # 主源码
│   ├── Form1.cs                # 主界面 + 安装逻辑 (~37KB)
│   ├── Locale.cs               # 中英文双语
│   ├── Program.cs              # 入口
│   ├── app.ico                 # 品牌图标
│   ├── app.manifest            # 应用清单
│   └── Resources/              # 嵌入的 Python 工具脚本
│       ├── scr.py              # 截图
│       ├── ocr.py              # OCR 识别
│       ├── act.py              # 键鼠操作
│       ├── see.py              # 截图+OCR 组合
│       └── browser.py          # 浏览器自动化
├── deploy_to_vm.py             # VM 自动部署 (pyautogui)
├── vix_copy.py                 # VMware VIX API 文件复制
├── publish/                    # 构建输出
└── versions/                   # 历史版本归档
```

---

## 许可

MIT License · Copyright (c) 2026 Shimizu

---

<p align="center">
  <sub>Made with ❤️ by 清水</sub>
</p>
