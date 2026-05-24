# V18: WPF 重写 + 自定义模型 + 工具自定义路径

## 概述

用 WPF (.NET 8) 完全重写安装器，保留所有安装逻辑，新增自定义模型配置和工具自定义安装路径。版本 1.0.6。

## 技术栈

- WPF .NET 8, XAML + code-behind
- Single-file publish, self-contained win-x64
- 品牌: Shimizu, 蓝色渐变主题

## 页面流程

**专业模式 (8 页):**
0. 欢迎 → 1. 环境检测 → 2. Skills → 3. 工具与逻辑 → 4. 安全模式 → 5. API 配置 → 6. 安装配置 → 7. 安装

**小白模式 (3 页，步骤编号 1/3 2/3 3/3):**
0. 欢迎 → 5. API 配置 → 6. 安装配置 → 7. 安装

## 新增功能

### API 配置 (Page 5)
- 三个选项: Anthropic(不推荐)/DeepSeek(推荐,默认)/自定义
- 自定义展开: API Key* + 请求地址* + 主模型* + Haiku* + Sonnet* + Opus* + Subagent(选填,勾选框)
- 请求地址自动补 https://
- 验证: 必填字段为空时禁止下一步

### 工具与逻辑 (Page 3)
- 第3个勾选框: "为 Claude Code 添加底层逻辑"
- 勾选后展开 CLAUDE.md 文本编辑区

### 安装配置 (Page 6)
- 默认盘符选择器
- 勾选框"工具自定义安装位置": 展开6个工具的独立盘符选择器
- 路径格式: `{盘符}\Claude Code tool\{子文件夹}`
- 路径实时更新显示
- 不勾选则全部使用默认盘符

### 安装完成
- 弹窗列出安装内容 (文字 → 路径格式)

## 保留的现有功能
- 全部安装逻辑 (Node.js, Git, npm, Claude Code, CC Switch, Python, Tesseract, Skills, settings, CLAUDE.md, DeepSeek, 快捷方式)
- 异步环境检测
- 多源下载镜像
- Skills 多源 fallback
- 版本检测 6 源镜像链
- 中英文双语
- 所有 genskills-- 静默跳过
