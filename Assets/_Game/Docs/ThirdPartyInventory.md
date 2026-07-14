# Third-Party Inventory

## 2026-07-13 P0 基线

P0 没有下载、安装或引入任何新的第三方工具、插件、模型、贴图、音频、字体或代码。

使用的运行环境仅为项目原本已有的 Unity `6000.5.3f1` 及其已安装包，包括 URP、uGUI、Input System 和 Unity Test Framework。程序化角色、UI、数据、逻辑与文档均在本项目内原创实现。

## 2026-07-14 长期制作工具

| 组件 | 版本/固定值 | 官方来源与许可证 | 用途与项目内路径 | 校验 |
|---|---|---|---|---|
| CoplayDev/unity-mcp | 上游 v10.1.0 / commit `c14de1e6dc01ab42d2bb358730cff954bce0ce6b`；项目补丁版 `10.1.0-project.1` | GitHub；MIT；原许可证保存在嵌入包 `LICENSE` | Unity Editor 集成；源码嵌入 `Packages/com.coplaydev.unity-mcp/`，隔离差异记录于 `UPSTREAM.md` | `manifest.json` 保留精确上游提交；`packages-lock.json` 解析为 `source: embedded` / `file:com.coplaydev.unity-mcp`，嵌入源码优先 |
| mcpforunityserver | `10.1.0` | PyPI / CoplayDev GitHub；MIT | Codex 与 Unity 的 Python MCP Server；由 uvx 运行，缓存位于 `_ProjectTools/uv/cache/` | uvx 使用精确版本约束 `mcpforunityserver==10.1.0` |
| uv | `0.11.28` Windows x64 | Astral GitHub 官方发布；MIT 或 Apache-2.0 | 便携 Python/工具运行器；`_ProjectTools/uv/0.11.28/` | 下载 ZIP SHA-256：`0A23463216D09C6A72FF80EF5DC5A795F07DC1575CB84D24596C2F124A441B7B` |
| CPython | `3.12.13` Windows x64 | uv 官方托管的 Python 发行物；Python Software Foundation License | MCP Server 运行时；`_ProjectTools/uv/python/cpython-3.12.13-windows-x86_64-none/` | 由 uv 版本解析并以 `only-managed` 模式使用 |

## 2026-07-14 首个 UR 角色样板

| 组件/服务 | 版本/接口 | 官方来源与许可/条款 | 用途与路径 | 限制与校验 |
|---|---|---|---|---|
| Blender | `5.1` | https://www.blender.org/；GPL-3.0-or-later | 用户已安装于 `C:\Program Files\Blender Foundation\Blender 5.1\`；用于清理 Tripo 网格、建立独立轨道环并导出 FBX | Codex 未下载或安装；已核对 `blender.exe` 与自带 Python 存在；运行配置/缓存/临时文件必须重定向到 `_ProjectTools/`；不随游戏分发 |
| Tripo OpenAPI | API v2 | https://platform.tripo3d.ai/docs/；受 Tripo Terms 约束 | 仅调用余额接口评估 `ART-CHAR-UR-COSMIC-SLIME-001` 候选路线 | available 0 / frozen 0；没有上传参考图、创建任务、下载产物或消费额度；不安装 SDK、不使用公共图床；未来如使用仍只作内部非商用原型 |

## 隔离声明

- 预期运行路径中，所有下载、缓存、Python、临时文件、AppData 映射和状态文件均位于 `_ProjectTools/`；没有修改系统 PATH 或进行系统级安装。
- 隔离加固前的一次验证意外创建了 `C:\Users\yshaw\AppData\Local\UnityMCP`。该目录未被读取或删除，正等待用户明确授权清理；当前配置已把未来的 `LOCALAPPDATA`、HOME、XDG、TEMP 与 uv 写入全部重定向回项目内。
- 当前唯一外部资产例外是用户明确批准的 `ART-CHAR-UR-COSMIC-SLIME-001` Tripo 官方 API 作业；没有接入 Meshy、Sketchfab、FAL、公共图床或其他服务。
- Tripo Key 已由此前流程写入用户级环境变量/凭据存储；本项目不保存其值，Codex 不会输出或提交它。该项目外状态的轮换和删除记录在 `StudioOps/DEFERRED_WORK.md`，需要用户另行授权。
- 工具不属于游戏内容资产，因此不写入 `StudioOps/ASSET_LEDGER.csv`。
- `_ProjectTools/` 被 Git 忽略，不随游戏源代码提交，也不包含在 Windows 游戏 Build 中。
