# 当前里程碑：长期制作基础设施

> 状态：进行中  
> 开始日期：2026-07-14  
> 用户授权：用户在审阅方案后回复“go ahead”  
> 长期范围权威：`../PROJECT_PLAN.md`

## 目标

在不改变 P0 玩法与场景的前提下，为长期 Unity 制作建立可靠的本地版本基线、Codex 原生管理入口和受隔离的 Unity MCP 操作桥梁。

## 已批准范围

- 仅在 `GenericGachaRPG` 内初始化本地 Git 与 Git LFS。
- 建立 `AGENTS.md` 与最小 `StudioOps` 文档层。
- 下载便携 `uv 0.11.28` 到 `_ProjectTools/`。
- 固定安装 `CoplayDev/unity-mcp v10.1.0`。
- 只使用本机 stdio，关闭遥测，保持写操作审批。
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
| Codex 原生入口与 StudioOps | 进行中 | `AGENTS.md`、本目录 |
| 便携 uv 隔离环境 | 未开始 | `_ProjectTools/uv/0.11.28/` |
| Unity MCP 固定包 | 未开始 | `Packages/manifest.json`、`packages-lock.json` |
| Unity/MCP 验证 | 未开始 | Console、连接与临时对象冒烟证据 |

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
- MCP 仅绑定本地 stdio，遥测关闭，不写入其他项目目录。
- 能读取项目/Unity 状态；在专用测试场景创建、读取并删除临时对象。
- 既有 P0 验证仍通过；任何兼容性限制必须明确记录。

## 下一交接

本里程碑完成后，等待用户试玩 P0 并选择 P1 的玩法、美术和目标平台优先级。
