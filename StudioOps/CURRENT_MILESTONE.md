# 当前里程碑：长期制作基础设施

> 状态：项目内交付已完成；1 个外部残留待用户授权清理<br>
> 开始日期：2026-07-14  
> 完成日期：2026-07-14<br>
> 用户授权：用户在审阅方案后回复“go ahead”  
> 长期范围权威：`../PROJECT_PLAN.md`

## 目标

在不改变 P0 玩法与场景的前提下，为长期 Unity 制作建立可靠的本地版本基线、Codex 原生管理入口和受隔离的 Unity MCP 操作桥梁。

## 已批准范围

- 仅在 `GenericGachaRPG` 内初始化本地 Git 与 Git LFS。
- 建立 `AGENTS.md` 与最小 `StudioOps` 文档层。
- 下载便携 `uv 0.11.28` 到 `_ProjectTools/`。
- 固定安装 `CoplayDev/unity-mcp v10.1.0`。
- Codex 侧使用项目内 stdio，Unity 内部桥接仅绑定 `127.0.0.1`，关闭遥测，保持写操作审批。
- 验证包解析、Unity 编译、MCP 连接及既有 Demo 完整性。

## 非目标

- 不安装 Blender、Blender MCP、ComfyUI、BMAD 或 Claude-Code-Game-Studios。
- 不接入 Meshy、Tripo、Sketchfab、FAL 或其他外部资产服务。
- 不创建远程 GitHub 仓库、不推送代码、不发布构建。
- 不开始 P1 玩法、美术重制或正式 3D 资产制作。
- 不修改系统 PATH，不进行系统级安装。

## 交付物与状态

| 交付物 | 状态 | 证据 |
|---|---|---|
| 工程审计与 Unity 版本控制设置核验 | 已完成 | Force Text、Visible Meta Files、`.meta` 完整 |
| 本地 Git/LFS 基线 | 已完成 | `main` 根提交 `27ac1da` |
| Codex 原生入口与 StudioOps | 已完成 | 提交 `83ce206`；`AGENTS.md`、本目录 |
| 便携 uv 隔离环境 | 已完成 | `uv 0.11.28`、CPython `3.12.13` 与缓存全部位于 `_ProjectTools/` |
| Unity MCP 固定包 | 已完成 | 上游 v10.1.0 / commit `c14de1e6dc01ab42d2bb358730cff954bce0ce6b`；嵌入式项目补丁版 `10.1.0-project.1`；lock 为 `source: embedded` |
| Unity/MCP 验证 | 已完成 | 48 个工具；活动场景读取；临时场景对象创建/读取/删除；P0、Play 与最终 Windows Build 回归通过 |
| 项目外残留清理 | 待用户授权 | 隔离加固前意外创建 `C:\Users\yshaw\AppData\Local\UnityMCP`；未读取或删除，未来写入已封堵 |

## 下载许可与限制

已批准下载：

1. `uv 0.11.28` Windows x64 官方 ZIP；必须校验 SHA-256。
2. `CoplayDev/unity-mcp v10.1.0`，固定到发布标签/提交。
3. Unity MCP Python 服务 `mcpforunityserver==10.1.0`，其缓存与 Python 环境必须在 `_ProjectTools/`。

除此以外的下载必须重新通知用户。

## 验收门槛

- Git 工作区可明确解释，生成目录未被跟踪。
- Unity MCP 版本固定，依赖来源与许可证已登记。
- Unity `6000.5.3f1` Console 无编译错误。
- Codex/Python Server 使用项目内 stdio；Unity 的 MCP 桥接端口仅绑定 `127.0.0.1:6400` 回环，遥测关闭，不写入其他项目目录或机器级 Unity 偏好。
- 能读取项目/Unity 状态；在专用测试场景创建、读取并删除临时对象。
- 场景创建、加载与保存路径必须经规范化并限制在本项目 `Assets/` 内，拒绝绝对路径与 `..` 越界。
- 既有 P0 验证仍通过；任何兼容性限制必须明确记录。

## 验证结论

- Unity `6000.5.3f1` 无 MCP 包编译错误；MCP Server 初始化后列出 48 个工具。
- `UNITY_MCP_SMOKE_PASS`、`P0_VERIFY_PASS_20260713`、`P0_PLAY_SMOKE_PASS_20260713` 与最终 `WINDOWS_BUILD_PASS_20260713` 均已确认。
- 遥测状态为 `false`；MCP 桥接仅监听 `127.0.0.1:6400`，未启用 MCP HTTP `8080` 或 LAN 绑定。

## 已知兼容性说明

- Unity MCP 在 Unity 6000.5 下曾把 Play Smoke 的普通 `Debug.Log` PASS 记录归类为 `Exception`；调用栈确认日志来自 `Debug.Log`，测试没有抛出异常。
- EditMode Runner 任务状态为 `succeeded / Passed`，但正式 NUnit 测试数为 0；项目自定义 P0 验证器仍是当前主要自动化门槛。
- 当前 Codex 任务不会热加载新建的 `.codex/config.toml`；在新任务或重启应用并信任本项目后使用原生 MCP 入口。
- OneDrive 曾短暂锁定 `Temp/BurstOutput`；只清理该项目内生成目录后，最终 Windows 增量构建成功。该历史失败不属于代码或 MCP 包编译错误。
- 上游 MCP 设置窗口在项目隔离版中只显示隔离提示，自动更新检查、用户客户端配置扫描/改写、HTTP 自动启动和机器级 `EditorPrefs` 写入均默认禁用。

## 下一交接

本里程碑完成后，等待用户试玩 P0 并选择 P1 的玩法、美术和目标平台优先级。
