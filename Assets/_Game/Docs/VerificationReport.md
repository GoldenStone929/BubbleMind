# P0 Verification Report

> 验证日期：2026-07-13  
> Unity：6000.5.3f1 / URP 17.5  
> 结论：P0 垂直切片通过，可在 Unity Play Mode 与 Windows 独立版试玩。

## 交付入口

- Unity 场景：`Assets/_Game/Scenes/GachaRPGDemo.unity`
- Windows 试玩版：`Builds/Windows/GenericGachaRPGDemo.exe`
- 试玩说明：`Assets/_Game/README_START_HERE.md`
- Play Mode 截图：`Artifacts/UnityQA/PlayMode_Home.png`

Windows Build 必须和同目录的 `GenericGachaRPGDemo_Data`、`MonoBleedingEdge`、`UnityPlayer.dll` 等支持文件一起保留。

## 已通过验证

| 验证层 | 结果 | 证据 |
|---|---|---|
| Unity 实际编译 | 通过 | 最新脚本编译记录为 `Tundra build success`，编译错误为 0 |
| P0 核心验证 | 通过 | `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| Play Mode 全流程冒烟 | 通过 | `[P0_PLAY_SMOKE_PASS_20260713]` |
| Windows x64 Build | 通过 | `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]` |
| Windows 后台启动 | 通过 | Unity Engine、Mono、Input System、Physics 与 Null Graphics 初始化完成，30 秒日志无错误或异常 |

自动化 Play Mode 流程通过真实 UI `Button.onClick` 依次完成：

```text
Home → Gacha → 单抽 → Home → Collection → Home → Formation
→ Battle → Result → Home
```

核心验证覆盖：

- 六名角色、三项技能和一个抽卡池的内容完整性。
- 使用内存存档执行余额、单抽、收藏更新和三人编队验证。
- 同输入、同 Seed 的 3v3 战斗产生完全一致的有序事件序列。
- 普攻、单体技能、群体技能、治疗、能量、死亡、胜负与超时路径。
- 演示场景存在，且被 Windows Build 明确作为唯一构建场景。

## Windows Build 记录

- Unity BuildPipeline 报告体积：`105,426,571` bytes。
- 构建耗时：`126.3s`。
- 当前 Build 目录：187 个文件，总计 `105,635,277` bytes；其中包含 Unity 额外生成的 Burst 调试信息目录。
- 主程序：`GenericGachaRPGDemo.exe`，`667,648` bytes。
- 主程序 SHA-256：`5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`。
- 独立版启动日志：`Artifacts/UnityQA/StandaloneBatchSmoke.log`。后台检查运行 30 秒后由验证脚本主动结束，因为交互式 Player 不会自行退出；初始化日志无错误、异常或崩溃。

## 2026-07-14 长期制作基础设施验证

| 验证项 | 结果 | 证据 |
|---|---|---|
| Unity MCP 包解析与编译 | 通过 | 上游 `CoplayDev/unity-mcp v10.1.0` / `c14de1e6dc01ab42d2bb358730cff954bce0ce6b` 以 `10.1.0-project.1` 嵌入；Unity 6000.5.3f1 Console 无包编译错误 |
| MCP Server 初始化 | 通过 | 完全离线初始化成功并列出 48 个工具；活动场景为 `GachaRPGDemo` |
| 网络、遥测与全局配置隔离 | 通过 | 遥测状态 `false`；MCP 桥接仅监听 `127.0.0.1:6400`，未启用 MCP HTTP `8080` / LAN；自动用户配置扫描/改写和机器级 Unity `EditorPrefs` 写入默认禁用 |
| 场景写入冒烟 | 通过 | 专用临时场景中创建、读取并删除测试对象，恢复 Demo 后删除临时场景；`UNITY_MCP_SMOKE_PASS` |
| 场景路径边界 | 通过 | `manage_scene` 拒绝 `Assets/../_ProjectTools/...` 遍历路径，返回 `Scene paths may not escape the project's Assets directory.`；探针目录未创建 |
| P0 核心回归 | 通过 | `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| Play Mode 回归 | 通过 | `[P0_PLAY_SMOKE_PASS_20260713]`；Demo 已恢复为干净状态 |
| EditMode Runner | 有限通过 | Job `d21e0f029d2a44f88e5582bf40bbcd8e` 为 `succeeded / Passed`，耗时 `0.0034476s`；正式 NUnit 测试数为 0，主要门槛仍是自定义 P0 验证器 |
| Windows 最终增量回归构建 | 通过 | `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildPipeline `106,151,041` bytes，`20.1s` |

最终回归构建后的 `Builds/Windows` 有 189 个文件，共 `106,359,747` bytes（含 Burst 调试信息目录）。主程序仍为 `667,648` bytes，SHA-256 仍为 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`；Unity 增量构建复用了未变化的 Player EXE，并更新游戏数据与程序集。

OneDrive 曾短暂锁定项目生成目录 `Temp/BurstOutput/Data/Plugins/x86_64`，导致历史构建尝试失败。严格确认目标位于本项目后，仅清理该生成目录并重试，最终构建成功；这不是 C#、Unity MCP 包或游戏逻辑编译失败。

兼容性说明：Unity MCP 的 `read_console` 在 Unity 6000.5 下曾把 Play Smoke 的普通 `Debug.Log` PASS 记录分类为 `Exception`；源码调用栈确认它来自 `Debug.Log`，且没有失败标记或运行时异常。

## 依赖与隔离

P0 游戏内容仍没有引入第三方模型、贴图、音频、字体或外部代码资产。2026-07-14 新增的长期制作工具仅包括固定版本 Unity MCP、便携 uv、项目内 CPython 和 Python MCP Server，详见 `ThirdPartyInventory.md`。当前配置的全部运行写入、缓存、状态、验证证据与 Build 都位于 `GenericGachaRPG` 项目目录内；没有系统级安装或 PATH 修改，`analysis` 只作为可选只读研究资料，不是运行依赖。

隔离加固前的一次验证意外创建了 `C:\Users\yshaw\AppData\Local\UnityMCP`。依照用户边界，该目录未被读取或删除，等待用户明确授权清理；这也是当前唯一未闭环的外部残留。加固后的离线协议复测已确认 48 个工具、遥测关闭、活动场景读取与 P0 回归均通过。

## 已知 P1 打磨项

当前战斗事件以约 2.4 倍速度重播，个别高密度事件的视觉动作可能略快于动画观感；战斗结果和模拟时序不受影响，可在 P1 通过表现队列和动画时间线继续打磨。

说明：`Logs/Editor.log` 中可能保留修复前的历史冒烟失败记录；最终成功标记及其后的干净退出才是本次交付基线。
