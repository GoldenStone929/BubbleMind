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

## 2026-07-13 P0 历史基线

| 验证层 | 结果 | 证据 |
|---|---|---|
| Unity 实际编译 | 通过 | 当时脚本编译记录为 `Tundra build success`，编译错误为 0 |
| P0 核心验证 | 通过 | `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| Play Mode 全流程冒烟 | 通过 | `[P0_PLAY_SMOKE_PASS_20260713]` |
| Windows x64 Build | 通过 | `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]` |
| Windows 后台启动 | 通过 | Unity Engine、Mono、Input System、Physics 与 Null Graphics 初始化完成，30 秒日志无错误或异常 |

自动化 Play Mode 流程通过真实 UI `Button.onClick` 依次完成：

```text
Home → Gacha → 单抽 → Home → Collection → Home → Formation
→ Battle → Result → Home
```

当时的核心验证覆盖：

- 六名角色、三项技能和一个抽卡池的内容完整性。
- 使用内存存档执行余额、单抽、收藏更新和三人编队验证。
- 同输入、同 Seed 的 3v3 战斗产生完全一致的有序事件序列。
- 普攻、单体技能、群体技能、治疗、能量、死亡、胜负与超时路径。
- 演示场景存在，且被 Windows Build 明确作为唯一构建场景。

## 2026-07-13 Windows Build 历史记录

- Unity BuildPipeline 报告体积：`105,426,571` bytes。
- 构建耗时：`126.3s`。
- 当时的 Build 目录：187 个文件，总计 `105,635,277` bytes；其中包含 Unity 额外生成的 Burst 调试信息目录。
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

## 2026-07-14 BubbleMind 首个试玩版验证

| 验证项 | 结果 | 证据 |
|---|---|---|
| 星渊吞噬体 Unity 集成 | 通过 | 角色 Prefab、7 角色数据库、UR 稀有度、Sockets、3 个 URP 材质与轨道动画均由验证器/Play 冒烟检查 |
| 星渊观测台运行时地图 | 通过 | Play 冒烟检查全画幅背板、运行时材质 Shader 与战斗对象；六个站位由实现审查和真实窗口确认 |
| 响应式 UI 与页面过渡 | 通过 | 首页、抽卡、收藏、编队、战斗、结算及返回路径自动点击完成；真实 1920×1080 窗口无重叠 |
| P0 核心回归 | 通过 | `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| Play Mode 全流程回归 | 通过 | `[P0_PLAY_SMOKE_PASS_20260713]`，见 `Artifacts/FirstDemo/PlaySmokeFinal.log` |
| Windows x64 最终构建 | 通过 | `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`，BuildPipeline `107,946,724` bytes / `10.0s` |
| Windows 真实窗口视觉核验 | 通过 | 标题 `BubbleMind First Demo`；Direct3D 11；首页、收藏、编队、战斗动画与结算均实际打开检查 |

最终构建目录含 191 个文件，共 `108,364,206` bytes。主程序仍为 `667,648` bytes，SHA-256 为 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`。

独立包初次实机检查发现运行时 `Shader.Find` 的 Unlit Shader 被构建裁剪，导致战斗初始化异常。现已改为编辑器生成并由 `Resources` 明确引用的材质资产；验证器和 Play 冒烟同时检查材质与运行时 Shader，修复后的独立包完成真实战斗与结算。工作区改名造成的 Bee 路径缓存也通过 `BuildOptions.CleanBuildCache` 正式重建解决，没有手工修改 `Library`。

本轮没有安装或下载新软件、Unity 包、SDK 或第三方依赖。地图概念图由用户明确要求后使用内置图像生成能力制作，来源与 SHA-256 已登记在地图 Asset Spec 和资产台账。

## 2026-07-14 五人射程战斗与史莱姆重制验证

| 验证项 | 结果 | 证据 |
|---|---|---|
| 五槽与 5v5 单一规则 | 通过 | 新存档默认五名不重复角色；Formation、双方生成、Spectrum Nova 目标数及 UI 均使用 `BattleRules.TeamSize = 5`；`[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| 数据化射程与移动 | 通过 | 七名角色均有有限正值 `AttackRange`、`MoveSpeed`；移动步长、入射程后才可攻击及事件序列确定性由 verifier 覆盖 |
| 坦克前线驻留与换敌 | 通过 | Play 冒烟确认首槽坦克前移、短时站稳、攻击后不回出生点、当前目标存活时不换锁及目标死亡后继续接近；`[P0_PLAY_SMOKE_PASS_20260713]` |
| 稀有度与限定身份 | 通过 | 顺序固定为 `R -> SR -> SSR -> SP -> UR`；星渊吞噬体在数据、收藏与编队 UI 中为 `UR / TANK / LIMITED`，且不进入标准池 |
| 黑洞史莱姆几何与材质 | 通过 | `Artifacts/UR_CosmicSlime/Blender/geometry_audit.json`：`19,704 / 20,000` 三角面、6/6 材质、约 `2.435 × 1.945 × 2.199 m`、接地、近黑外壳、纯黑体积事件视界、侧向可读吸积结构和轨道净空全部 PASS |
| Unity 角色集成 | 通过 | Prefab 使用 `Shell / Nebula / Core / BlackCore / Orbit / OrbitTrim` 六个运行时材质，其中 Core 对应 FBX 的 Energy 槽；事件视界、白紫吸积结构及三分之四朝向在最终 Windows 实机可见 |
| 回放节奏与战斗 UI | 通过 | 回放倍率由 `1.8` 降至 `1.6`；姓名牌、血条及 FOV 为五人接战收紧；最终 1920×1080 窗口实际检查首页、五人编队、前线接战与结算 |
| Windows x64 最终构建 | 通过 | `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildPipeline `108,771,048` bytes / `20.5s` |

最终 `Builds/Windows` 含 191 个文件，共 `109,188,530` bytes。主程序 `GenericGachaRPGDemo.exe` 为 `667,648` bytes，SHA-256 为 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`。本轮没有安装、下载或引入新的软件、Unity 包、SDK 或第三方运行依赖；`analysis/` 仅按需只读参考，未修改，也未读取或运行任何 XAPK/APK。

## 2026-07-14 Catherine 满级 UR 与五元素史莱姆最终验证

| 验证项 | 结果 | 证据 |
|---|---|---|
| Catherine 满级战斗契约 | 通过 | 600% Skill 1、两段 200% Skill 2、140% 治疗、减益/质量层数、960% 大招、30 层 4×、死亡大招至少 6×与单次复活均由 verifier / smoke 覆盖 |
| 黑洞吸附与击飞 | 通过 | 确定性事件包含蓄力、变形、五目标吸附、四段伤害、坍缩和逐目标击飞；1920×1080 与 1280×720 实机逐帧确认 |
| 敌方测试倍率 | 通过 | 仅敌方运行时实例应用 10× HP / 0.1× ATK；实机普通受击约 1 点，资产原值不变 |
| UR 模型与动画 | 通过 | `geometry_audit.json`：11,468 / 20,000 triangles、6 个材质、四个 Blend Shapes，全部审计项为 true |
| 五元素基础史莱姆 | 通过 | `Artifacts/BasicElementSlimes/Blender/geometry_audit.json`：五系 1,064–1,324 triangles，FBX/Prefab/默认 5v5 映射均通过 |
| Generate / PlayMode | 通过 | `GenerateCompile3.log` 为 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]`；`PlaySmokeFinal.log` 为 `[P0_PLAY_SMOKE_PASS_20260713]` |
| Windows x64 / D3D11 | 通过 | `WindowsBuildFinal.log` 为 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`，113,729,368 bytes / 22.8s，且无 `Shader error` |
| 双分辨率视觉 | 通过 | 1920×1080 与 1280×720 真实窗口均完成首页、战斗、黑洞、结算检查；无浅青色实心球或 UI 越界 |

初次构建的 BuildPipeline PASS 曾掩盖 D3D11 Shader 编译错误。本轮没有删除该失败证据，而是以 `WindowsBuildFinal.log` 的干净图形编译和真实窗口画面作为最终门槛。回放速度由 1.6 倍降至 1.25 倍，通用技能脉冲从实心球改为透明能量环。

## 已知 P1 打磨项

固定 Tick 直线接近已经满足坦克驻留与换敌规则，但尚无碰撞分离、分道或 NavMesh；多人围攻及黑洞吸附时可能发生模型与姓名牌相交。正式 Animator、表现队列、收藏页 3D 角色预览及其余角色正式模型留待后续里程碑。

说明：`Logs/Editor.log` 中可能保留修复前的历史冒烟失败记录；最终成功标记及其后的干净退出才是本次交付基线。
