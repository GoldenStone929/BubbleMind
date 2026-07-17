# BubbleMind Verification Report

> P0 基线验证日期：2026-07-13
> 最新回归日期：2026-07-16
> Unity：6000.5.3f1 / URP 17.5
> 结论：P0 垂直切片、怒气/三技能、历史 20 格 3v5 职业测试、角色档案/2D 卡面、参考系统图谱、2D 像素 5v5、完整系统壳及 2026-07-16 主页表现升级均已通过各自回归。最新权威运行基线提供全幅 Home、明亮 App Shell、World、Characters、Recruit、Formation、Battle/Result、Inventory、Missions 与 Settings，并完成三关离线主线和奖励闭环。

## 交付入口

- Unity 场景：`Assets/_Game/Scenes/GachaRPGDemo.unity`
- Windows 完整系统试玩版：`Builds/FullSystemWindows/BubbleMind.exe`
- Windows 历史像素纵切片：`Builds/Windows/BubbleMind.exe`
- 试玩说明：`Assets/_Game/README_START_HERE.md`
- Play Mode 截图：`Artifacts/UnityQA/PlayMode_Home.png`

Windows Build 必须和同目录的 `BubbleMind_Data`、`MonoBleedingEdge`、`UnityPlayer.dll` 等支持文件一起保留。

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

## 2026-07-14 怒气、三技能与 Catherine 真实凝胶回归

| 验证项 | 结果 | 证据 |
|---|---|---|
| 怒气规则 | 通过 | verifier 与 PlayMode 覆盖初始 `0 / 1000`、普攻命中 +100、受到实际伤害 +50、满怒技能槽 1 大招及释放后清零 |
| 三技能错峰 | 通过 | 技能槽 2 按 5 / 15 秒释放，技能槽 3 在 10 秒释放；验证战斗于 19.3 秒结束，规则中的后续触发均保持各 10 秒周期且不在同 Tick 同放 |
| 职业射程与停位 | 通过 | Tank / Assassin 射程 1，Support / Ranged / Mage 射程 5；单位在自身最大射程边界停位，目标死亡后才重新锁定最近敌人 |
| Catherine 槽位与被动 | 通过 | 槽 1 `Infinite Void`、槽 2 `Wind Wheel: Break`、槽 3 `Wind Wheel: Dance`；`Star Rage` 作为被动领域触发 |
| 敌方测试倍率 | 通过 | 敌方运行时实例继续应用 10× HP / 0.1× ATK，不修改角色资产原始数据 |
| Catherine 几何与 Shape Keys | 通过 | `Artifacts/UR_CosmicSlime/Blender/geometry_audit.json`：19,000 / 20,000 triangles、6 个材质、四个必需 Shape Keys，全部审计项为 true |
| Generate / C# / 核心验证 | 通过 | `Artifacts/CatherineRealism/RageGenerate4.log` 出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]`，没有 C# 编译失败标记 |
| PlayMode 冒烟 | 通过 | `Artifacts/CatherineRealism/RagePlaySmoke.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；完整 UI 流程、世界怒气条与战斗契约均通过 |
| Windows x64 / D3D11 | 通过 | `Artifacts/CatherineRealism/RageWindowsBuild.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；114,033,256 bytes / 24.1s，日志无 `Shader error` |
| 实机视觉 | 通过 | 1920×1080 与 3440×1392 级宽屏窗口完成检查；真实凝胶厚度、纯黑核心、三点布光、怒气 UI 均可读，黑洞吸附由截图确认 |

本轮 Catherine 模型整体尺寸约 `2.896 × 2.131 × 2.019 m`。事件视界使用纯黑 RGBA `0,0,0,1`，吸积盘、光子透镜和螺旋结构在三分之四视角提供体积深度。场景以主光、补光和轮廓光组成三点布光，没有新增或购买第三方 Shader、VFX、Unity 包或运行时依赖。

完整制作记录：`StudioOps/MILESTONES/2026-07-14_RAGE_THREE_SKILL_AND_CATHERINE_REALISM.md`。

## 2026-07-14 20 格 3v5 职业测试回归

| 验证项 | 结果 | 证据 |
|---|---|---|
| 战场比例与职业射程 | 通过 | verifier 检查战场长度 20、Tank / Assassin 射程 2、Support / Ranged / Mage 射程 10，七名角色资产一致且所有位移不越界 |
| Catherine 击退 | 通过 | `Wind Wheel: Break` 在中心区域实际位移 5 格、边缘按战场边界截断；verifier 与 PlayMode 均重放并比较真实落点，坍缩大招的 1.3 格击飞保持独立 |
| 五槽与 3v5 分离 | 通过 | Formation 仍保存并显示五槽；运行时严格生成 Catherine / Gold Ranger / Ember Striker 三名玩家和五名敌人，共 8 个单位与 8 个怒气条 |
| 刺客后排切入 | 通过 | Ember 的技能槽 2 在 5 秒选择存活后排、产生 `UnitTeleported` 并保持 2 格分离；15 秒再次施放仍锁定同一存活目标，边界角落改走 Z 轴而不重叠 |
| Generate / C# / 核心验证 | 通过 | `Artifacts/ThreeVsFiveRange/GenerateFinal.log` 出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| PlayMode 冒烟 | 通过 | `Artifacts/ThreeVsFiveRange/PlaySmokeFinal.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；完整 UI 流程、两次同目标切入、击退落点与 19.3 秒战斗结算通过 |
| Windows x64 / D3D11 | 通过 | `Artifacts/ThreeVsFiveRange/WindowsBuildFinal.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildPipeline `114,042,024` bytes / `58.6s` |
| Windows 实机视觉 | 通过 | 1922×1112 捕获窗口检查首页、五槽 Formation、完整 20 格战场和胜利结算；无画面边缘裁切，最终保留在首页 |

主程序 `Builds/Windows/GenericGachaRPGDemo.exe` 为 `667,648` bytes，SHA-256 为 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`。本轮没有下载、购买或引入任何新依赖；`analysis/` 保持只读，未读取、运行或解包 XAPK/APK。

完整制作记录：`StudioOps/MILESTONES/2026-07-14_THREE_VS_FIVE_RANGE_GRID_AND_ASSASSIN_SHIFT.md`。

## 2026-07-15 角色档案、卡面与 UI 重制回归

| 验证项 | 结果 | 证据 |
|---|---|---|
| 角色数据与卡面导入 | 通过 | verifier 检查 7 个 `CharacterDefinition` 均有非空 `Portrait`、三项技能、稳定 `characterId`，卡面均来自 `Assets/_Game/Art/Generated/UI/Portraits/`；Sprite/Single、sRGB、Clamp、无 mipmap |
| 随包 UI 字体 | 通过 | `NotoSansCJKsc-Regular.otf` 以动态 Font 导入并从 `Resources/Fonts/` 加载；许可证和 SHA-256 已登记，缺失会阻止核心验证通过 |
| 角色档案 | 通过 | PlayMode 检查 Catherine 卡片与大卡面、角色名及恰好三张技能卡；最终 Windows 1922×1112 捕获窗口确认左侧双列卡库、右侧大卡面/属性/技能无重叠 |
| 抽卡卡面结果 | 通过 | PlayMode 检查 `ResultPortrait` 已启用且 Sprite 非空；Windows 实抽显示 Azure Vanguard 2D 卡面、`R`、角色名和 `NEW CHARACTER UNLOCKED`，水晶 3000→2900 |
| 五槽与现有战斗 | 通过 | Formation 继续提供五个固定卡面槽；完整自动流程仍进入 3v5 战斗并结算，既有 20 格射程、怒气、三技能和刺客切后排契约无回归 |
| 旧存档兼容 | 通过 | `PlayerState` schema 保持 v3；本轮静态卡面/UI 不清空旧水晶、拥有角色、重复计数或五槽编队。实抽后执行 Demo Reset，最终留给用户的窗口恢复 3,000 并停在首页 |
| Generate / C# / 核心验证 | 通过 | `Artifacts/CharacterPage/GenerateFinal.log` 出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| PlayMode 冒烟 | 通过 | `Artifacts/CharacterPage/PlaySmokeFinal.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；首页、抽卡、角色档案、编队、3v5 战斗与结算完整通过 |
| Windows x64 / D3D11 | 通过 | `Artifacts/CharacterPage/WindowsBuildFinal.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildReport `142,698,350` bytes / `70.1s`，无 `Shader error` 或 C# 编译错误 |
| Windows 构建目录 | 通过 | 191 个文件，共 `143,115,832` bytes；主程序 `667,648` bytes，SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| 清洁室边界 | 通过 | 外部参考游戏只读查看主界面、角色、技能、招募与概率页并恢复首页；没有购买、抽卡、养成、改队或关闭。`analysis/` 只读，未读取、运行或解包 XAPK/APK |

完整制作记录：`StudioOps/MILESTONES/2026-07-15_CHARACTER_PAGE_AND_CARD_ART.md`。UI 归纳与后续复用规则：`Assets/_Game/Docs/UiReferenceKnowledgeBase.md`。

## 2026-07-15 参考系统图谱与角色内容模板最终验证

本轮建立 `StudioOps/KnowledgeBase/` 三层清洁室知识库，并把原创角色档案模板接入现有七角色、角色页面、生成器、验证器和战斗数据快照。此前实时观察仅覆盖首页、角色、技能、招募和概率页；再次连接时参考游戏处于自身省电模式，窗口保持开启，未关闭、重启、登录或执行账号状态操作。`analysis/` 保持只读，未修改，也未读取、运行或解包 XAPK/APK。

| 验证项 | 结果 | 证据 |
|---|---|---|
| 清洁室知识库 | 通过 | 21 条证据、36 个系统、25 个页面、27 个实体、89 条关系；所有系统/页面/实体至少有一条关系，坏外键与重复边均为 0 |
| 七角色 Profile | 通过 | `Assets/_Game/Data/CharacterProfiles/`；归属外键、能力、连续逐级参数、养成、获取和来源证明均通过 `CharacterContentProfile` 校验 |
| 最终 Generate / C# / 核心验证 | 通过 | 终审后 `Artifacts/ContentTemplate/GenerateTriggerCompile.log` 再次编译并出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| 生成器二次幂等 | 通过 | 连续 Generate 前后四个权威文件 SHA-256 完全一致；数据库额外内容与已有人工作者 Profile 保留 |
| 最终 PlayMode 完整流程 | 通过 | `Artifacts/ContentTemplate/PlaySmokeOwnershipFinal.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；实际点击 Archive/Growth 前后翻页、验证首尾回绕和普通角色切换，并继续覆盖抽取、五槽、3v5、结算与返回首页 |
| Windows x64 / D3D11 | 通过 | `Artifacts/ContentTemplate/WindowsBuildOwnershipFinal.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildReport `142,747,278` bytes / `76.6s` |
| Windows 构建产物 | 通过 | BubbleMind 分发集合 190 个文件、`142,956,016` bytes；`BubbleMind.exe` 为 `667,648` bytes，SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| 1600x900 真实独立窗口 | 通过 | 首页、角色 Combat、Archive、Growth 与 Recruitment 均已打开检查；Archive 翻到 Star Rage 5/6 和 Awakening 6/6；分页箭头修复并重建后可见，长文本无重叠/裁切，七张 2D 卡面正常 |

连续生成幂等的四个权威文件测试时序列化哈希如下：

```text
GameDatabase       2B21B6690C0B9F053D42B44BCA68E989E3C624B6FCC40D9E3162EF92CAE296A3
Catherine Profile  4D107C0654CEFFC1714AB04736B32C66F259B42AC3872BFB4D5980E132B063F5
Dance Skill        5CE92ECA60BCE06890A1EF3588D9836EB7C9C48854DEA489F515BB7ABB339241
Demo Scene         C64F5B293EEBBFFD34DAA6EFA45DEA8A759E13BA653938C6F183BB0D08322174
```

两次 Unity Generate 的前后值完全一致。提交前只规范化了 Unity YAML 的行尾空格，因此当前仓库中的 Demo Scene 文本哈希为 `1A3A6C78E3949BE53A295081342DB9F1BADD2388B4D7133AA652197D7B65C7C0`；其忽略行尾空格的语义差异为空，其余三个文件哈希不变。

`Builds/Windows` 同时保留了上一轮 `GenericGachaRPGDemo` 基线，因为其进程仍在运行，没有强行关闭或删除。整个共存目录为 355 个文件、`231,448,456` bytes；其中本轮 BubbleMind 分发逻辑集合是上表记录的 190 个文件、`142,956,016` bytes。最终 BubbleMind 试玩窗口已用最新构建重新打开并停在首页，参考游戏也始终保持开启且只读。省电模式阻止了本轮继续巡检，未观察项目仍保留为 `unknown`，没有以推测补齐。

模板验证还覆盖：`SkillDefinition.RageCost` 为大招消耗权威，成本必须为正且不超过角色 `MaxRage`，释放时按精确成本扣除；当前常用 1000/1000 因而归零，但非满额大招会保留剩余怒气。Profile 的 `ownerCharacterId` 必须匹配角色，逐级表必须从 Lv.1 连续到 MaxLevel，档案末级伤害/Power 必须与运行时定义一致；生成器保留数据库额外角色、技能、卡池和已有人工作者档案；Archive 与 Growth 的分页和首尾回绕进入了自动与实机检查。

完整制作记录：`StudioOps/MILESTONES/2026-07-15_REFERENCE_ATLAS_AND_CONTENT_TEMPLATE.md`。

## 2026-07-15 腾讯混元 3D 模型候选评估

用户明确要求尝试腾讯混元 3D 后，项目使用腾讯官方账号发布、托管于 Hugging Face 的 `tencent/Hunyuan3D-2` 与 `tencent/Hunyuan3D-2mv` Spaces，对已登记的 Catherine 正面图和确定性三视图裁剪执行三组无纹理、零费用候选生成。没有登录腾讯账户、提供 API Key、下载 SDK/权重/依赖或消费 credits。

| 门槛 | 状态 | 证据 |
|---|---|---|
| 输入可追溯 | 通过 | 正面输入 SHA-256 `FC50F8D8E40F53925C7715F9B70E6452E4900420D9D4AF0BBDA7CE5736333437`；三视图裁剪及源图哈希记录在 `_ProjectTools/Hunyuan3D/inputs/CatherineMultiView/crop_manifest.json` |
| 单视图生成 | 通过 | 30 steps / guidance 5 / seed 929；原始 GLB SHA-256 `58BCB18098171A2BD5C9A24F37C1E5968F72F1179062EC9F061B82F1B68400FA`；服务端显示 737,060 faces，Blender 识别 442,980 triangles / 221,492 vertices |
| 多视图模型前视图生成 | 通过 | 5 steps / seed 929；原始 GLB SHA-256 `A5C8C1E40205DB51DF489C6A27B9E0FCF57EBDB5585E6CBB62694F58D7A3E6AE`；474,768 triangles |
| front/left/back 三视图生成 | 通过 | 5 steps / seed 929；原始 GLB SHA-256 `E21805D34B1C7CA5C1C1FB2F24E03D8B985D1B049712928B626FE1905FC3B536`；489,312 triangles |
| Blender 四视图审计 | 通过 | 三组均包含非空网格和体积，并输出 front/side/back/three-quarter 渲染；均判定需要游戏级重拓扑 |
| 9,000 面自动减面预览 | 通过 | 单视图原始网格减少 97.97%；输出 SHA-256 `F305B7A77CE04C1B58DDA91CE772D25B6CC9E02EBAA8736195F60B60629594BA`；面数、接地和非空体积检查通过；不是生产重拓扑 |
| Git / Unity / Build 隔离 | 通过 | `_ProjectTools/Hunyuan3D/` 命中项目 `.gitignore`；没有混元 GLB、渲染、请求或工具进入已跟踪文件、Unity `Assets/` 或 Windows Build |

视觉结论：混元候选的主体穹顶、凝胶裙边和有机体积明显优于简单程序化球体，但几何阶段无法可靠表达纯黑事件视界，中央黑洞被解释为凹陷/表面结构；轨道和液滴出现断裂或漂浮碎片。现有项目自有黑洞、轨道、Socket、Shader、VFX 与四个 Blend Shapes 必须保留，并在正式候选上通过 Blender 清理和重拓扑重建。

本轮只新增制作记录，没有修改 C#、场景、Prefab、FBX、材质、包清单或 Build，因此没有重复运行 Unity 编译、PlayMode 或 Windows Build；上一节已通过的运行基线仍是权威。正式资产接入需先获得腾讯云 HY-3D 3.1 服务、条款、受限凭据和 credits 授权，并确认其输出可覆盖计划发行地域。

完整制作记录：`StudioOps/MILESTONES/2026-07-15_HUNYUAN3D_MODEL_EVALUATION.md`。

## 2026-07-15 2D 像素 5v5 纵切片验证

本轮只转换运行时美术与部署入口，不重写抽卡、收藏、角色档案、技能、怒气、存档或确定性战斗核心。默认阵容为 Catherine Yuki、Gold Ranger、Ember Striker、Verdant Medic、Violet Arcanist；玩家保存的合法五槽阵容会直接构造五人战斗请求。

| 验证项 | 结果 | 证据 |
|---|---|---|
| 七角色像素 Sprite 与地图导入 | 通过 | `Artifacts/PixelPvp2D/Generate1.log` 出现 `[GenericGachaRPG][PIXEL_PVP_VERIFY_PASS_20260715]`；七张 128×128 Sprite 与 480×270 地图均验证 Point、无 Mipmap、Clamp、Uncompressed 与 Resources 加载 |
| 保存五槽直接参战 | 通过 | verifier 检查默认主角 + 四伙伴顺序；`DemoGameController` 从当前 `draftFormation` 解析玩家队伍，不再使用历史固定三人名单 |
| 正常流程无旧 3D 角色 | 通过 | PlayMode 运行时恰好生成 10 个 `PixelCharacterVisual`、0 个旧 3D 史莱姆控制器，并拒绝程序化回退 |
| 5v5 全流程与重开 | 通过 | `Artifacts/PixelPvp2D/PlaySmoke1.log` 出现 `[GenericGachaRPG][PIXEL_PVP_PLAY_SMOKE_PASS_20260715]`；覆盖 Formation → Battle → Result → Restart 第二局 → Home、10 个怒气条与单一 BattleWorld |
| Windows x64 / D3D11 | 通过 | `Artifacts/PixelPvp2D/WindowsBuild1.log` 出现 `[GenericGachaRPG][PIXEL_PVP_WINDOWS_BUILD_PASS_20260715]`；BuildReport `143,616,984` bytes / `69.7s` |
| 真实独立窗口 | 通过 | 最新 `Builds/Windows/BubbleMind.exe` 已启动；首页明确显示 “Catherine and four slimes” 与 “five-versus-five Pixel PvP trial”，像素星渊观测台覆盖完整宽屏画幅；实战在 19.3 秒进入 Victory 结算并显示存活的 Pixel2D Catherine / Ember、世界条和 Restart / Return Home；后续视觉微调交由玩家当前试玩反馈驱动 |
| 清洁室边界 | 通过 | 只参考“像素 Sprite + 现代 3D 引擎渲染”的广义技术；没有复制参考作品的角色、字体、UI、地图、构图、玩法或资产，也没有新增第三方运行依赖 |

主程序 `BubbleMind.exe` 为 `667,648` bytes，SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`；Unity Player Stub 未变化，更新内容位于同目录数据与程序集，因此必须保留整个 `Builds/Windows` 分发集合。

完整制作记录：`StudioOps/MILESTONES/2026-07-15_PIXEL_PVP_2D_VERTICAL_SLICE.md`。

## 2026-07-15 完整系统壳与离线主线闭环验证

本轮把既有角色、招募、五槽编队和确定性 5v5 像素战斗接入完整元系统。Home 与 App Shell 提供统一资源栏和主导航；World 以三个 `StageDefinition` 驱动关卡详情、顺序解锁、体力消耗与奖励；Inventory、Missions、Settings 使用同一 `PlayerState` schema v4 持久化。Arena、Events、Shop、Mail、Guild 只进入明确锁定页面，没有伪造在线行为。

| 验证项 | 结果 | 证据 |
|---|---|---|
| 数据、关卡与迁移 | 通过 | `Artifacts/FullSystem/Generate.log` 出现 `[GenericGachaRPG][FULL_SYSTEM_VERIFY_PASS_20260715]`；覆盖三关 ID/顺序/前置/敌人引用/奖励/解锁、schema v3 → v4、内存存档、抽卡、编队和确定性战斗 |
| 完整页面导航 | 通过 | PlayMode 覆盖 App Shell、Home、World、三个关卡节点、Characters/Recruit 既有路径、Formation 返回、Inventory、Missions、Settings 和 LockedFeature；路由切换时只有目标页面激活 |
| World → 战斗 → 进度闭环 | 通过 | `PlaySmokeEconomyFinal.log` 连续选择 Stage 1-1、进入 Formation、完成确定性胜利并断言体力精确扣除一次、通关/胜场、首通 Crystal、Gold 与 Echo Gel；返回 Home 后 Continue 显示并打开 1-2，World 的 1-2 节点可交互 |
| Restart 与结算幂等 | 通过 | Restart 创建新的单一 `BattleWorld`，再次扣除 1-1 体力并只发 Gold/Echo Gel/胜场，不重复发首通 Crystal；`battleResultCommitted` 防止同一局完成回调重复写入经济状态 |
| Missions / Settings | 通过 | PlayMode 实际领取 First Signal 任务；把 Music/Effects 设为 0.41/0.63，翻转 Fullscreen/60 FPS 并断言状态保存，同时覆盖全屏模态重置确认层的打开和取消 |
| 最终 UI 修复 | 通过 | Boss `Void Fragment` 进入 `StageRewardGrant.RareMaterials` 并显示在结算摘要；Home Continue 不再保留陈旧关卡；Recruit 概率说明区域扩展，避免文案截断/按钮重叠 |
| PlayMode 完整系统冒烟 | 通过 | `Artifacts/FullSystem/PlaySmokeEconomyFinal.log` 出现 `[FULL_SYSTEM_PLAY_SMOKE_PASS_20260715]`，并保留既有 `[P0_PLAY_SMOKE_PASS_20260713]` 与 `[PIXEL_PVP_PLAY_SMOKE_PASS_20260715]` |
| Windows x64 / D3D11 | 通过 | `Artifacts/FullSystem/WindowsBuildFinal2.log` 出现 `[GenericGachaRPG][FULL_SYSTEM_VERIFY_PASS_20260715]` 与 `[GenericGachaRPG][FULL_SYSTEM_WINDOWS_BUILD_PASS_20260715]`；BuildReport `143,676,760` bytes / `65.4s`，包含最终经济与 UI 修复 |
| 1600×900 独立窗口 | 通过 | `Builds/FullSystemWindows/BubbleMind.exe` 以窗口模式启动；主页玩家/资源栏、核心热点、锁定入口和 Home/World/Heroes/Recruit/Inventory/Missions 底部导航无重叠或裁切 |
| 清洁室与工作区边界 | 通过 | 本轮未下载或增加第三方运行依赖；`analysis/` 未修改，工作区根目录 XAPK/APK 未读取、运行、解包或提交 |

关卡配置为：1-1 Fracture Gate（6 Energy / 1,000 Power / 首通 100 Crystal / 250 Gold / 2 Echo Gel）、1-2 Resonance Gallery（8 / 1,800 / 120 / 350 / 3）、1-3 Event Horizon（10 / 2,600 / 200 / 500 / 5，Boss 首通另得 1 Void Fragment）。1-2 依赖 1-1，1-3 依赖 1-2。

完整制作记录：`StudioOps/MILESTONES/2026-07-15_FULL_SYSTEM_SHELL.md`。

## 2026-07-16 参考审计 Session 02 与主页表现升级验证

本轮没有重写存档、抽卡、战斗或关卡服务。UI 修改只落在 `DemoUiFactory`、`AppShellView` 与 HomeHub：新增共享视觉令牌和命令入口，全幅展示原创星渊观测台，并让卡池、收藏和五槽状态由 `Refresh` 实时读取权威数据。参考游戏的精确截图与专有文本继续只留在 Git 忽略的 Artifacts，版本库知识库只记录中性模块、状态和关系。

| 验证项 | 结果 | 证据 |
|---|---|---|
| 参考证据连续性 | 通过 | `Artifacts/ReferenceAudit/2026-07-16_session-02/`：Manifest 67 行、Navigation 67 行，编号 0001–0067 连续；截图缺失、ID 缺失和重复均为 0 |
| Clean-room 知识库 | 通过 | 23 条证据、36 个模块、92 条关系；新增观察只转化为通用入口分组、规则页、静态目录/持有实例和多队伍配置规律 |
| UI 编译与核心验证 | 通过 | `Artifacts/HomeShellPresentation/P2Generate.log` 出现 P0、PixelPVP、FullSystem 三组 VERIFY PASS |
| PlayMode 全流程 | 通过 | `Artifacts/HomeShellPresentation/P2PlaySmoke.log` 出现 P0、PixelPVP、FullSystem 三组 PLAY SMOKE PASS |
| 审查后修复 | 通过 | Home 三张命令卡状态改为动态刷新；App Shell 重排 Formation/Mail/Menu，1024px 横屏估算下 Mail 约 47.4px、Menu 约 46.4px；三层卡片文字启用 best-fit 且锚区不交叠 |
| Windows x64 / D3D11 | 通过 | `Artifacts/HomeShellPresentation/WindowsBuildFinal.log` 出现 FullSystem VERIFY 与 WINDOWS BUILD PASS；BuildReport `143,680,856` bytes / `64.4s` |
| Windows 分发集合 | 通过 | `Builds/FullSystemWindows/` 共 190 个文件、`143,889,594` bytes；EXE 为 `667,648` bytes，SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| 真实窗口视觉 | 通过 | 3440×1440 全屏兼容检查；1920×1080 窗口下 Home、World、Catherine Character 与 Gacha 2D 结果无重叠或裁切，截图位于 `Artifacts/HomeShellPresentation/screenshots/` |
| 动态状态 | 通过 | Windows Player 中执行一次本地单抽：Crystal 3,000 → 2,900，2D 获得卡显示，返回 Home 后收藏 5/7 → 6/7，资源栏和任务徽标同步刷新 |
| 运行窗口保留 | 通过 | BubbleMind 最终 Player 停在 Home；参考游戏仍保持开启，未关闭、重启或触发领取、购买、开始战斗、保存阵容等变更性控件 |

完整制作记录：`StudioOps/MILESTONES/2026-07-16_HOME_SHELL_PRESENTATION_AND_AUDIT.md`。

## 已知 P1 打磨项

固定 Tick 直线接近已经满足坦克驻留与换敌规则，但尚无碰撞分离、分道或 NavMesh；多人围攻及黑洞吸附时可能发生 Sprite 与姓名牌相交。逐帧像素动画、表现队列、分层地图、音频和联网 PvP 留待后续里程碑。

说明：`Logs/Editor.log` 中可能保留修复前的历史冒烟失败记录；最终成功标记及其后的干净退出才是本次交付基线。
