# V17: UI Redesign + Version Check Fix

## 概述

在 V16 功能基础上进行全面的 UI 视觉升级，同时修复安装过程中版本号获取失败的问题。功能逻辑不变。

## 1. UI 重新设计 — Shimizu Brand 渐变现代风

### 1.1 整体设计语言

- 每页顶部：蓝色渐变 Banner（135deg, #0052D9 → #0078FF → #4FACFE），包含步骤编号 + 标题
- 内容区：白色背景，圆角卡片（R=10px），轻阴影
- 品牌色：#0052D9（主蓝）、#00AA50（成功绿）、#F97316（警告橙）
- 字体：Microsoft YaHei UI，标题加粗
- 底部导航栏：融入渐变风格，按钮圆角

### 1.2 各页面改动

**欢迎页 (Page 0):**
- 顶部大 Banner：Shimizu 品牌名 + "Claude Code 安装器" + 副标题
- 两个入口改为圆角卡片（带图标色条）而非实心矩形按钮
- 专业用户：蓝色调卡片 + 左边框
- 小白用户：绿色调卡片 + 左边框

**环境检测页 (Page 1):**
- 顶部小 Banner：步骤编号圆形徽章 + "环境检测"
- 盘符选择器改为圆角标签样式
- 检测结果：圆角卡片 + 左边色条（绿色=已安装，橙色=未安装）
- "重新检测"按钮改为小圆角药丸

**Skills 页 (Page 2):**
- 顶部 Banner + 步骤编号
- 全选按钮改为圆角药丸样式
- 每个 Skill 改为圆角卡片，左边自定义复选框
- 不可安装的 Skill（genskills--）灰色显示

**工具/安全/API 页 (Page 3-5):**
- 统一顶部 Banner
- Radio 选项改为卡片式选择（类似欢迎页入口卡片）
- API Key 输入框加圆角边框

**小白配置页 (Page 6):**
- 顶部 Banner
- 同样卡片化处理

**安装页 (Page 7):**
- 顶部 Banner
- 安装按钮绿色渐变
- 进度条蓝色渐变
- 日志保持深色终端风格（Tokyonight 配色）

### 1.3 WinForms 实现要点

- 使用 `Panel.Paint` 事件绘制渐变背景和圆角
- 自定义 `GradientPanel` 和 `RoundedPanel` 类
- 不用第三方库，纯 GDI+ 绘制
- 保持 Single-file exe，不增加额外依赖

## 2. Bug 修复 — 版本号获取失败

### 2.1 问题

`InstallClaudeNative()` 只有 2 个版本检测 URL:
1. `downloads.claude.ai/claude-code-releases/latest`
2. `storage.googleapis.com/.../latest`

两者在国内都可能不可达，8 秒超时后直接抛异常终止安装。

### 2.2 修复方案

**多源版本检测（与下载逻辑统一）:**
1. 原有 2 个 URL 各加 ghproxy 镜像包装（4 个 URL）
2. 超时从 8s → 12s
3. 所有 URL 都失败时，使用硬编码回退版本 `"1.0.36"`（写死在代码里）
4. 如果回退版本也下载不到对应的 exe，再报错

**版本 URL 列表:**
```
ghproxy.net/https://downloads.claude.ai/claude-code-releases/latest
mirror.ghproxy.com/https://downloads.claude.ai/claude-code-releases/latest
ghproxy.net/https://storage.googleapis.com/.../latest
mirror.ghproxy.com/https://storage.googleapis.com/.../latest
downloads.claude.ai/claude-code-releases/latest
storage.googleapis.com/.../latest
```
（保持原始 URL 在最后，镜像优先）

## 3. 非目标

- 不改变任何安装逻辑
- 不改变菜单/选项的默认值
- 不改变 Skills 列表
- 不改变任何配置写入逻辑
