# 已归档里程碑：长期制作基础设施

> 状态：项目内交付已完成；1 个外部残留待用户授权清理  
> 开始日期：2026-07-14  
> 完成日期：2026-07-14  
> 本地提交：`ccff9c976252`  
> 后继里程碑：`../CURRENT_MILESTONE.md`

## 目标

在不改变 P0 玩法与场景的前提下，为长期 Unity 制作建立可靠的本地版本基线、Codex 原生管理入口和受隔离的 Unity MCP 操作桥梁。

## 已完成交付

- 仅在 `GenericGachaRPG` 内建立本地 Git 与 Git LFS；没有远程仓库。
- 建立 `AGENTS.md` 与最小 `StudioOps` 文档层。
- 下载并隔离便携 `uv 0.11.28`、CPython `3.12.13` 与 `mcpforunityserver==10.1.0`。
- 嵌入 `CoplayDev/unity-mcp v10.1.0`，形成项目补丁版 `10.1.0-project.1`。
- Codex 使用项目内 stdio；Unity MCP 桥接仅绑定 `127.0.0.1:6400`，遥测、HTTP 自动启动、用户配置改写和机器级偏好写入默认禁用。
- Unity `6000.5.3f1` 编译、48 个 MCP 工具、P0、Play Mode 与 Windows Build 回归通过。
- 场景路径已加固，绝对路径和 `..` 越界被拒绝。

## 验证证据

- `UNITY_MCP_SMOKE_PASS`
- `P0_VERIFY_PASS_20260713`
- `P0_PLAY_SMOKE_PASS_20260713`
- `WINDOWS_BUILD_PASS_20260713`
- `Assets/_Game/Docs/VerificationReport.md`

## 未在本里程碑处理

- 没有制作正式 3D 角色。
- 没有接入 Tripo、Meshy 或其他外部资产服务。
- 没有安装 Blender；之后用户自行安装的 Blender 5.1 属于后继里程碑环境。
- 没有开始 P1 玩法扩展。

## 唯一外部残留

隔离加固前的一次验证意外创建了 `C:\Users\yshaw\AppData\Local\UnityMCP`。该目录没有被读取或删除；未来写入已经封堵。是否清理由用户另行明确授权，不能由后续角色制作授权推定。

