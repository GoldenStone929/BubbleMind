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

## 依赖与隔离

本轮没有下载、安装或引入新工具、插件、模型、贴图、音频、字体或第三方代码。所有新增源文件、数据、验证证据与 Build 都位于 `GenericGachaRPG` 项目目录内；`analysis` 只作为可选只读研究资料，不是运行依赖。

## 已知 P1 打磨项

当前战斗事件以约 2.4 倍速度重播，个别高密度事件的视觉动作可能略快于动画观感；战斗结果和模拟时序不受影响，可在 P1 通过表现队列和动画时间线继续打磨。

说明：`Logs/Editor.log` 中可能保留修复前的历史冒烟失败记录；最终成功标记及其后的干净退出才是本次交付基线。
