# GenericGachaRPG 项目主计划（自包含执行 Prompt）

> 计划版本：v2.2-rage-three-skill-realism<br>
> 当前状态：怒气制、三技能错峰轮转、职业射程与最大射程停位已完成；Catherine 真实凝胶外观、纯黑核心与三点布光已接入，Generate/核心验证、PlayMode、Windows D3D11 构建及 1920×1080 / 3440×1392 级宽屏实机视觉检查均通过<br>
> 最近更新：2026-07-14
> 沟通语言：所有面向用户的沟通、进度和交付说明一律使用中文；代码标识符可使用英文
> 当前唯一目标：在 Unity 中交付一个用户可以亲自按 Play 试玩的原创 3D 抽卡 RPG 垂直切片 Demo

---

## 1. 本文件的用途与优先级

本文件同时是：

1. 项目的长期执行计划；
2. 可以直接交给后续 Codex/开发 Agent 的主提示词；
3. 后续记录阶段进度、验证证据和用户决策的唯一主文档。

执行 Agent 必须先完整阅读本文件，再检查项目实际文件。

- 不得假设能够访问以前的 ChatGPT 对话、共享链接、XAPK 或外部分析目录。
- 本文件已内嵌完成当前 Demo 所需的关键架构结论。即使外部 `analysis` 目录不可访问，也不得因此停止、要求用户搬运资料或放弃实施。
- 用户最新指令始终高于本计划。范围发生变化时，先更新本文件的“决策记录”，再实施。
- 用户已于 2026-07-13 明确授权开始开发。P0 已完成；后续 Agent 应把当前项目作为已验证基线，不得重新从空项目开始。

---

## 2. 已确认的环境与当前状态

### 真实路径

```text
工作区：
C:\Users\yshaw\OneDrive\Desktop\BubbleMind

真实 Unity 项目根目录：
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\GenericGachaRPG

可选的只读研究目录：
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis
```

执行前必须确认当前根目录直接包含：

```text
Assets
Packages
ProjectSettings
PROJECT_PLAN.md
```

如果当前工作目录是上一级 `BubbleMind`，应将 Unity 工作目标明确切换到其子目录 `GenericGachaRPG`，不得误把文件创建在上一级或 `analysis` 内。

### 已核实的项目环境

- Unity Editor：`6000.5.3f1`
- Render Pipeline：URP `17.5.0`
- 项目起点是新建的 Unity URP 模板；P0 内容现已完整落在 `Assets/_Game`。
- 已生成自定义游戏脚本、七名角色（六名标准池角色与一名 UR 限定坦克样板）、九项技能资产、抽卡池、本地存档、五人编队、5v5 自动战斗、完整 UI、演示场景和编辑器工具。
- 已生成可试玩场景 `Assets/_Game/Scenes/GachaRPGDemo.unity` 与 Windows 独立版 `Builds/Windows/GenericGachaRPGDemo.exe`。
- 当前项目已在 `GenericGachaRPG` 内建立本地 Git/Git LFS 仓库；`main` 初始基线提交为 `27ac1da`。用户已于 2026-07-14 明确授权把该仓库推送到 `https://github.com/GoldenStone929/BubbleMind.git`；其他远端、构建发布或外部服务仍需另行授权。
- Unity Editor 当前可能处于打开状态。不要强制关闭 Unity，也不要在项目被打开时另启会冲突的第二个 Editor 实例。
- 项目位于 OneDrive。实施时只操作真正需要的源文件，避免修改 `Library`、`Temp`、`Logs` 等生成目录，并留意同步与导入延迟。

### 已确认的工具能力

- Unity `6000.5.3f1` 已安装并可用。
- 项目已包含 URP、uGUI 和 Unity Test Framework。
- Unity 已安装 Windows Standalone 与 WebGL Build Support。
- Git 与 Git LFS 已在本项目本地启用。Python 仅可用于已批准、项目内隔离的工具流程；不得写入系统环境或其他项目。
- Blender `5.1` 已由用户安装在 `C:\Program Files\Blender Foundation\Blender 5.1\`；Codex 已核对启动器、`blender.exe` 和自带 Python 存在。Blender 只作为只读外部可执行文件调用，所有用户配置、缓存、临时文件和输出必须重定向到本项目。
- 长期制作基础设施已固定使用便携 `uv 0.11.28`、uv 管理的 CPython `3.12.13`、`mcpforunityserver==10.1.0` 与嵌入式 `CoplayDev/unity-mcp 10.1.0-project.1`；上游基线固定到 v10.1.0 / 提交 `c14de1e6dc01ab42d2bb358730cff954bce0ce6b`。
- 所有由 Codex 下载的工具、缓存、Python、临时运行状态与 MCP 状态均位于 `_ProjectTools/`；没有修改系统 PATH 或下载 Blender。当前唯一批准的 Git 远端是 `GoldenStone929/BubbleMind`，唯一外部资产服务例外是用户明确批准的首个 UR 角色 Tripo 官方 API 作业。
- Codex 与 Python MCP Server 使用项目内 stdio；Python Server 与 Unity Editor 之间的 MCP 桥接仅使用 `127.0.0.1:6400` 回环端口。未启用 MCP HTTP `8080`、LAN/远程 HTTP 或遥测；用户客户端配置和机器级 Unity 偏好写入默认禁用。

### 用户授予的工具/外部服务权限与强制隔离规则

用户允许为本项目下载确实必要的工具或依赖，但此授权受以下边界约束：

1. 最小依赖优先：Unity 或项目现有包能完成的工作，不得引入额外工具。
2. 只允许下载与当前已批准里程碑直接相关的工具。
3. 优先选择官方来源、许可证清晰、可校验版本和哈希的便携版或项目本地依赖。
4. 所有下载、便携工具、临时安装包、生成物和验证证据必须位于 Unity 项目根目录内的明确子目录：

```text
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\GenericGachaRPG\_ProjectTools\
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\GenericGachaRPG\Builds\
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\GenericGachaRPG\Artifacts\
```

5. 禁止系统级安装、修改系统 PATH、写入其他项目、用户桌面、Downloads、Documents、其他 OneDrive 目录或无关文件夹。
6. 不得扫描、读取或修改本项目范围之外的无关用户数据。调用已经安装的 Unity 可执行文件属于允许范围，但不得修改 Unity 安装目录。
7. 如果某个必要工具无法在项目目录内便携使用，或安装器必然写入 `Program Files`、`AppData`、注册表或系统服务，必须先暂停并用中文向用户说明具体工具、原因、写入位置和替代方案；即使已有宽泛下载授权，也不能静默扩大范围。
8. 每个第三方依赖必须记录名称、版本、来源、许可证、用途、目标路径和校验信息；记录文件放在：

```text
Assets/_Game/Docs/ThirdPartyInventory.md
```

9. 不执行来源不明的二进制文件、脚本或安装器。
10. 不允许任何工具访问或联系原 APK/XAPK 中发现的服务器、接口或域名。

当前窄幅外部服务授权：

- 仅为 `ART-CHAR-UR-COSMIC-SLIME-001`，允许把已登记的参考 PNG 上传到 Tripo 官方 API、查询余额、消耗用户免费 API 额度并把模型下载到 `_ProjectTools/Tripo/jobs/`。
- 不使用第三方公共图床，不安装 Tripo SDK，不把 API Key 写入命令行、项目、Markdown、日志或 Git。
- 免费 Tripo 输出标记为内部、非商用原型；商业发布前必须重制或取得适当许可。
- 任何付费、其他服务、其他资产或新增下载仍需先通知用户。

---

## 3. 产品目标

构建一套原创、干净、可扩展的移动端 3D 抽卡 RPG 框架，并先交付一个可在 Unity Editor 与 Windows 独立版中直接试玩的垂直切片。

### 当前 Demo 的完整玩家流程

```text
Home 主页
  → Gacha 抽卡
  → Character Collection 角色收藏
  → Formation 五人编队
  → 5v5 Automated Battle 自动战斗
  → Result 战斗结果
  → Return Home 或 Restart
```

### 长期方向

- iOS 与 Android；横屏显示。
- 固定站位的队伍战斗。
- 自动普通攻击与角色专属技能、动作和 VFX。
- 抽卡、角色收藏、编队和基础养成。
- 角色、技能、关卡和卡池全部数据驱动。
- 将来支持 AI 辅助生成的 3D 角色。
- 通过统一骨架、动画接口和 Socket 契约量产角色。
- 新增角色主要依靠数据与资产导入，不修改战斗核心源码。

### 本轮不追求

- 不做完整商业手游。
- 不做真实登录、联网后端、数据库、PvP、公会、邮件、排行榜或活动系统。
- 不做真钱充值、IAP 或商店支付。
- P0 默认不下载第三方角色、插件或模板；如后续确有必要，只能依照“项目内隔离、官方来源、最小依赖”的规则处理。
- P0 已不依赖 Blender、正式模型、配音或最终商业美术；当前后继里程碑只制作一个正式 3D 角色样板，不扩展整套阵容。
- 不在 Windows 上尝试最终 iOS 构建。

---

## 4. 外部研究的自包含摘要

以下内容仅解释为什么采用当前架构，不是待还原的实现：

- 被研究的公开安装包使用 Unity `2022.3.62f2`、IL2CPP ARM64、SLua/Lua 5.3 桥接和 AssetBundle 管线。
- 离线分析识别出启动、资源更新、登录、主页、角色、技能、编队、抽卡、背包、关卡、战斗、任务、活动、商店、公会等约 36 个高层模块。
- 找到 761 个 Lua 路径字符串，但脚本与主要 AssetBundle 加密且不可读。
- 战斗、抽卡和角色管线只能恢复到“高层系统边界/中等可信度”，无法恢复原始公式、数值、AI、技能时间线、抽卡概率、保底、后端协议或源代码。
- 指纹更接近一个私有白标产品家族；没有找到可信、合法、可购买的公开原始模板。
- 结论是：研究资料足够指导独立架构设计，但不能把原包变成合法、可编译的 Unity 模板。

因此，本项目必须独立设计和实现。不要声称正在“恢复”“移植”或“复刻”原游戏。

### 可选只读资料

如果执行环境允许，可只读参考：

```text
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\module_inventory.csv
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\game_flow.md
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\data_model.md
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\battle_system.md
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\gacha_system.md
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\character_pipeline.md
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\clean_room_unity_architecture.md
C:\Users\yshaw\OneDrive\Desktop\BubbleMind\analysis\architecture\missing_information.md
```

规则：

- 只读，不得修改 `analysis`。
- 若无法访问，记录事实后立即继续；不是阻塞条件。
- 不需要读取 XAPK 或大型提取目录来完成 Demo。

---

## 5. 强制 Clean-room、版权与安全边界

Demo 中所有内容必须新创并保持通用。

### 严禁使用或复制

- Jujutsu Kaisen、Pokémon 或其他现有 IP 的角色、名称、世界观和品牌。
- 原包中的模型、贴图、动画、UI 布局、图标、字体、美术、音频、配音、VFX 或 Shader。
- 原始 C#、Lua、配置表、数据、数值、公式、AI、技能表现、抽卡概率或保底规则。
- 原始包名、内部 ID、服务器地址、协议、消息 ID、密钥、凭证、产品 SKU 或反作弊逻辑。
- 未经授权的源码、反编译资产或来源不清的模板。

### 严禁执行

- 不解密 Lua 或 AssetBundle。
- 不搜索密钥、不暴力破解、不绕过 DRM/认证/反作弊。
- 不运行原 APK/XAPK，不连接研究中发现的服务器。
- 用户已原则性批准为本项目下载必要工具，但只能依照本计划的项目隔离规则执行；不得静默进行系统级安装或引入与当前里程碑无关的依赖。

所有角色、技能、名称、数值、公式、抽卡规则和界面都必须独立创作。

---

## 6. Demo 内容规格

### 6.1 原创占位角色

创建至少 6 名原创通用角色，使用 Unity Primitive 程序化组装的统一 3D 彩色火柴人/低多边形角色：

- Sphere：头部
- Capsule/Cube：躯干
- Cylinder/Capsule：手臂和腿
- 可选 Primitive：武器或饰件
- 圆形 Ground Marker：脚下标记

建议通用角色定位：

1. Vanguard（先锋）
2. Guardian（守卫）
3. Striker（斗士）
4. Arcanist（术士）
5. Ranger（游侠）
6. Medic（医师）

要求：

- 使用原创通用名称。
- 通过蓝、青、绿、黄、橙、红、紫等不同配色和简单形体/武器差异区分。
- 保持统一身体比例和视觉体系。
- 使用 URP 兼容材质。
- 初始解锁 5 名角色；其余可通过 Demo 抽卡获得。
- 不搜索或下载免费角色模型。

### 6.2 Home 主页

包含：

- 游戏标题：`Generic Gacha RPG Demo`
- 当前 Demo 货币
- Gacha 按钮
- Characters 按钮
- Formation 按钮
- Battle 按钮
- Reset Demo Data 按钮

使用 Unity 内置 uGUI。适配 `1920×1080` 横屏和安全区域，鼠标点击与触摸式点击都可用，不依赖键盘。

### 6.3 Gacha 抽卡

P0 必须实现：

- 一个可配置本地标准卡池；当前包含 R / SR / SSR，限定 UR 不进入标准池，全局稀有度顺序为 R / SR / SSR / SP / UR。
- 单抽。
- 可配置 R / SR / SSR 权重。
- 扣除 Demo 货币。
- 首次获取解锁角色。
- 结果展示与返回。
- 概率详情入口。
- 明确显示：`DEMO / NOT SERVER AUTHORITATIVE`。
- 抽卡结果先由服务生成并写入玩家状态，揭示动画只负责展示。

P1 才实现：

- 十连抽。
- 十连最低稀有度保证。
- 重复角色转换为通用碎片。
- 更完整的保底与结果摘要。

所有权重、保底阈值和重复转换数量均为新创、可配置的 Demo 数值。

### 6.4 Character Collection 角色收藏

显示全部 7 名角色：

- 已锁定/已解锁状态
- 稀有度
- 等级（Demo 可固定为 1）
- 基础属性
- 角色颜色/简单预览
- P1 可显示碎片与基础升级按钮

抽卡完成后，收藏状态必须立即更新。

### 6.5 Formation 编队

- 五个玩家编队槽位。
- 只能选择已经拥有的角色。
- 必须正好选择 5 名角色才能开始战斗。
- 禁止重复角色。
- 提供合法默认编队。
- 保存选定编队。
- 当前编队与战斗统一为 5 人；队伍人数只由共享规则定义。

### 6.6 5v5 射程自动战斗

场景要求：

- 玩家 5 人在左，敌方 5 人在右。
- 固定出生槽位、持续逻辑位置与侧向移动游戏镜头。
- 简单地面、背景、灯光和 Ground Marker。
- 角色名、HP Bar、Rage Bar、Damage Number。
- 自动普通攻击、怒气、三技能、受击、死亡、胜负结果。
- Restart 和 Return Home。
- 最大战斗时长保护，绝不允许无限战斗。

每个单位必须：

1. 在固定 Formation Slot 生成；
2. 选择仍然存活的合法目标；
3. 按配置间隔自动普通攻击；
4. 初始怒气为 0，上限为 1000；普攻命中获得 100 怒气，受到实际伤害获得 50 怒气；
5. 技能槽 1 为大招，满 1000 怒气后自动释放并清零；
6. 技能槽 2 与技能槽 3 各自按 10 秒周期释放，首次分别在 5 秒与 10 秒触发，错峰且不同时释放；
7. 技能拥有清晰不同于普攻的动作和 VFX；
8. 以职业规则决定攻击距离：Tank / Assassin 为 1，其余职业为 5；单位只接近到自身最大攻击距离并在那里停位；
9. 受击时有视觉反馈；
10. HP 为零时死亡并停止行动；
11. 死亡单位不可再被选为目标；当前目标死亡后按当前位置重选最近敌人；
12. 受击、普攻和技能表现不得把角色世界位置重置到出生槽；
13. 全灭或超时时生成稳定结果。

P0 至少支持三类通用技能效果：

- 单体伤害
- 多目标伤害
- 治疗

---

## 7. 核心技术架构

### 7.1 分层原则

必须分离：

1. 静态数据定义
2. 玩家运行时状态
3. 服务与存档
4. 战斗模拟/计算
5. 角色表现与程序化动画
6. UI 表现
7. 编辑器生成和验证工具
8. 未来后端接口

UI 只展示状态和发出意图，不直接承担权威 RNG、货币扣除、奖励生成或伤害公式。

### 7.2 数据类型

至少定义：

```text
CharacterDefinition
CharacterInstance / OwnedCharacter
SkillDefinition
SkillTimeline / SkillEvent
TeamFormation
StageDefinition / EnemyDefinition
GachaBannerDefinition
GachaPoolEntry
GachaResult / DrawResult
PlayerState / CurrencyState
BattleContext
BattleResult
```

`CharacterDefinition` 与玩家拥有/等级状态必须分开；静态定义使用 ScriptableObject，玩家状态使用可序列化运行时数据。

### 7.3 服务接口

至少保留以下可替换边界：

```text
ISaveService
IGachaService
IRandomService
ICollectionService
IFormationService
IBattleRewardService
```

Demo 使用本地离线实现。正式产品中的账号、货币、抽卡、购买、奖励和 PvP 必须由后端权威处理，但本轮不建设生产后端。

### 7.4 本地存档

可使用 PlayerPrefs + JSON 或 `Application.persistentDataPath` JSON，但必须隐藏在 `ISaveService` 后。

至少保存：

- Demo 货币
- 已拥有角色
- 角色等级（P0 可全部为 1）
- 当前五人编队
- P1 的碎片数据
- `schemaVersion`

要求：

- 缺失或损坏存档时安全恢复默认状态。
- Reset Demo Data 可恢复初始货币、初始角色和默认编队。

### 7.5 战斗模拟

- 使用固定逻辑 Tick 和项目拥有的 Seeded RNG。
- 相同 BattleContext、内容数据和随机种子应产生相同的模拟结果。
- 出生 Formation Slot、持续逻辑位置与演出 Transform 分离；模拟层权威决定移动后位置，表现层只做插值和局部动作。
- 战斗资源统一为 Rage：每名单位从 0 / 1000 开始，普攻命中 +100，受到实际伤害 +50；满怒释放技能槽 1 后清零。
- 技能槽 2 与技能槽 3 使用独立的 10 秒确定性周期，首次触发分别为战斗 5 秒与 10 秒；错过窗口时不允许在同一 Tick 补放两项技能。
- 攻击距离由职业统一约束：Tank / Assassin 为 1，Support / Ranged / Mage 为 5；移动只关闭到攻击距离边界，不额外贴近目标。
- 目标选择、伤害、治疗和技能效果由通用规则/数据驱动，不写角色专属条件分支。
- 技能时间线决定命中时刻；模拟逻辑不得等待动画播放完成回调。
- 新角色或技能不得要求修改 `BattleUnit`/战斗核心源码。
- P0 无需实现完整服务器回放系统，但架构不得阻止未来替换为服务器权威模拟。

### 7.6 角色表现契约

程序化角色与未来 FBX/正式 Prefab 都通过 `CharacterView` 接入。

统一层级/Socket 契约：

```text
CharacterRoot
├── ModelRoot
├── RightHandSocket
├── LeftHandSocket
├── SkillVfxSocket
├── ProjectileSocket
├── GroundVfxSocket
├── TargetSocket
└── HealthBarSocket
```

`CharacterView` 支持：

- 可选 Animator
- Idle / Attack / Skill / Hit / Death 通用接口
- 武器、投射物、VFX、地面和目标挂点
- 无模型或无 Animator 时的程序化回退动画

程序化动画至少包括：

- Idle：轻微呼吸/摆动
- Normal Attack：在当前射程位置做局部蓄力/前探并恢复，不重置世界位置
- Skill：更明显的缩放、跳跃、旋转或蓄力效果
- Hit：短暂后退和材质闪烁
- Death：倒下、旋转、缩小或淡出

不要引入 DOTween 或其他外部 Tween 包。

### 7.7 资源策略

- P0 只使用现有 Unity/URP/uGUI 与本地直接引用。
- 当前项目没有 Addressables；不要为 Demo 擅自安装。
- 通过清晰的内容键和数据边界保留未来迁移到 Addressables/远程内容的可能性。

---

## 8. 项目目录与命名

根命名空间：

```text
GenericGachaRPG
```

所有新游戏文件默认只创建在：

```text
Assets/_Game/
```

必要例外：

- 横屏/移动设置所需的 `ProjectSettings`
- 将 Demo Scene 加入 Build Settings 的 `EditorBuildSettings`
- Codex 持久入口 `AGENTS.md`、项目级 `.codex/config.toml` 与制作管理目录 `StudioOps/`
- 版本控制文件 `.gitignore`、`.gitattributes` 与本地 `.git/`
- 经隔离规则批准的项目本地工具目录 `_ProjectTools/`
- 本项目构建输出目录 `Builds/`
- 本项目验证证据目录 `Artifacts/`

建议结构：

```text
Assets/_Game/
├── Art/
│   ├── Materials/
│   └── Generated/
├── Data/
│   ├── Characters/
│   ├── Skills/
│   ├── Gacha/
│   └── Stages/
├── Prefabs/
│   ├── Characters/
│   ├── UI/
│   └── VFX/
├── Scenes/
├── Scripts/
│   ├── Core/
│   ├── Data/
│   ├── Services/
│   ├── Gacha/
│   ├── Collection/
│   ├── Formation/
│   ├── Battle/
│   ├── Characters/
│   ├── Skills/
│   ├── UI/
│   └── Editor/
├── Docs/
└── Tests/
```

主场景与菜单的唯一权威名称：

```text
Scene:
Assets/_Game/Scenes/GachaRPGDemo.unity

Menu:
Tools > Generic Gacha RPG > Generate or Repair Demo
```

旧对话中出现的 `BattleDemo.unity` 与 `Tools > Gacha RPG > Generate Battle Demo` 已废弃，不再使用。

---

## 9. Editor 生成工具

创建一个安全、幂等的 Editor Generator：

```text
Tools > Generic Gacha RPG > Generate or Repair Demo
```

负责生成或修复：

- 6 个示例 CharacterDefinition
- P0 所需 SkillDefinition
- Demo Gacha Banner
- 原创 URP 材质
- 程序化角色配置/Prefab
- Home、Gacha、Collection、Formation、Battle、Result UI
- Camera、Light、Floor、Background、Formation Slots
- Managers、EventSystem
- 完整 `GachaRPGDemo.unity`
- Build Settings 场景项

要求：

- 可重复运行，不产生重复对象或重复资产。
- 使用版本标记。
- 首次生成后不得静默覆盖用户手工修改的资产。
- 不手写 Unity Scene YAML；通过 Unity Editor API 创建。
- 脚本首次成功编译且场景不存在时，可尝试安全的一次性自动生成。
- 如果自动生成不可行，菜单命令必须作为可靠后备。
- Unity 已打开时，不要为了生成场景启动第二个冲突的 Editor 进程。

---

## 10. 分阶段执行计划

每一阶段都必须满足：有实际文件、Unity 编译通过、保存证据，再进入下一阶段。不得一次创建大量未编译代码后才检查。

### 阶段 0 — 基线与安全检查

交付：

- 确认真正项目根目录和 Unity 版本。
- 检查并保留用户已有修改。
- 确认 `analysis` 只读且非依赖。
- 建立 `Assets/_Game` 基础目录和最小文档。

完成条件：

- 未修改 `analysis`、XAPK、`Library`、`Temp`、`Logs`。
- Unity 项目仍可正常打开且无新增编译错误。

### 阶段 1 — 数据、服务与本地状态

交付：

- 核心数据定义。
- 6 名原创角色和 P0 技能数据。
- 本地 `ISaveService`、默认状态、加载/保存/重置。
- 本地随机数、抽卡和编队服务接口。

完成条件：

- 存档往返、损坏恢复和重置通过验证。
- Unity 编译错误为 0。

### 阶段 2 — Home、抽卡、收藏、编队

交付：

- Home Screen。
- 单抽与结果展示。
- 收藏状态实时更新。
- 三人编队与合法性检查。

完成条件：

- 页面可前进和返回。
- 货币不足不会改变状态。
- 抽卡会扣费、解锁角色并保存。
- 只有 3 名已拥有且不重复的角色才能进入战斗。
- Unity 编译错误为 0。

### 阶段 3 — 3v3 战斗模拟

交付：

- 两支 3 人队伍。
- 固定 Tick、Seeded RNG、目标选择、普攻、能量、技能、治疗、死亡、结果和超时。
- 与表现层分离的纯逻辑战斗核心。

完成条件：

- 相同 Seed 产生相同核心结果。
- 死亡单位不会继续行动或被选为目标。
- 战斗必定以胜/负/超时结束。
- Unity 编译错误为 0。

### 阶段 4 — 角色表现、UI 和完整场景

交付：

- 彩色程序化角色。
- HP/Energy Bar、Damage Number。
- Idle/Attack/Skill/Hit/Death 程序化动作。
- 简单 VFX。
- Result、Restart、Return Home。
- 幂等 Editor Generator 与 `GachaRPGDemo.unity`。

完成条件：

- 从 Home 到 Result 的完整流程可以在 Play Mode 走通。
- 场景引用无缺失，Console 无运行时错误。

### 阶段 5 — P0 验证与用户试玩交付

交付：

- 所有 P0 功能修复完成。
- `README_START_HERE.md`。
- 架构摘要、Clean-room 可追溯说明和 Demo 限制。
- 编译、Play Mode、自动化全流程与 Windows Build 验证记录。
- `Builds/Windows/GenericGachaRPGDemo.exe` 及其 Unity 运行支持文件。

完成条件：

- 用户只需打开 `Assets/_Game/Scenes/GachaRPGDemo.unity` 并点击 Play。
- 或直接运行 `Builds/Windows/GenericGachaRPGDemo.exe`。
- 完整流程可重复试玩。
- 不得用“代码已写但未编译”或“场景未生成”宣称完成。

### 阶段 6 — P1（仅在 P0 稳定后）

候选：

- 首个正式 3D 角色样板、Prefab/CharacterView 接入与 URP 表现。
- 十连和最低稀有度保证。
- 重复角色碎片。
- 更丰富技能类型。
- 角色详情和基础升级。
- 视觉与音效润色。
- 更完整的 Windows/WebGL/移动平台发布与输入适配。

---

## 11. P0 强制验收标准

只有全部满足才能声明 P0 Demo 完成：

- Unity 项目编译错误为 0。
- `Assets/_Game/Scenes/GachaRPGDemo.unity` 确实存在并可打开。
- Home → Gacha → Collection → Formation → Battle → Result → Home/Restart 全流程可完成。
- 新存档有足够 Demo 货币，至少可以多次单抽。
- 单抽正确扣费并更新收藏；重新进入页面状态仍正确。
- 编队只能保存 5 名已拥有且不重复的角色。
- 玩家与敌方各生成 5 个单位。
- 普攻、怒气、技能槽 1 大招与错峰技能槽 2 / 3 可见且有效。
- 怒气上限 1000、初始 0；普攻命中 +100、受伤 +50，满怒大招后归零。
- Tank / Assassin 在射程 1 停位，其余职业在射程 5 停位。
- 单位能够死亡，死亡后不行动、不被选为目标。
- 战斗能够稳定产生结果，超时保护有效。
- Restart 和 Return Home 有实际行为。
- 存档可重新加载；Reset Demo Data 能恢复默认状态。
- Generator 连续运行两次不会复制内容或破坏场景。
- 无未处理异常、持续 NullReference 或 MissingReference。
- 没有任何原应用专有代码、资产、品牌、数据或服务器依赖。

---

## 12. 验证与证据规则

执行 Agent 必须区分：

```text
静态检查完成
Unity 实际编译完成
Editor Play Mode 已验证
用户仍需手工试玩
```

不得在没有证据时声称编译或运行成功。

至少验证：

- 文件名与 MonoBehaviour/ScriptableObject 类名匹配。
- Runtime 不引用 UnityEditor。
- 无重复类、缺失 Namespace 或循环程序集引用。
- 缺少 Animator/正式模型时程序化回退正常。
- 货币不足不会产生抽卡结果或负余额。
- 编队无重复且只允许已拥有角色。
- 同 Seed 的抽卡/战斗核心结果可复现。
- 技能槽 1 满怒释放并清零，技能槽 2 / 3 的 5 秒与 10 秒错峰时序稳定。
- 战斗胜负、全灭和超时均正确。
- 存档保存/加载/损坏恢复/重置正确。
- Scene Generator 可重复运行。

如果执行环境不能直接控制 Play Mode，应完成所有可验证项目，并明确告知用户唯一剩余动作；不能假装已运行。

---

## 13. 文档交付

实施时创建：

```text
Assets/_Game/README_START_HERE.md
Assets/_Game/Docs/ArchitectureSummary.md
Assets/_Game/Docs/AnalysisTraceability.md
Assets/_Game/Docs/DemoLimitations.md
```

`README_START_HERE.md` 必须简短说明：

1. Demo 场景路径；
2. 如何生成/修复场景；
3. 如何按 Play；
4. Demo 当前包含什么；
5. 在哪里编辑角色、技能和卡池数据；
6. 将来如何用正式 3D 模型替换程序化角色。

`AnalysisTraceability.md` 仅记录：

- 使用了哪些高层通用概念；
- 采用了什么独立 Clean-room 实现；
- 哪些原始内容明确没有复制；
- 哪些系统被推迟。

不要复制外部分析文档的长段落。

---

## 14. 执行 Agent 的工作规则

1. 所有用户沟通用中文。
2. 先检查实际文件与现有改动，不覆盖用户工作。
3. 先建立最小可编译纵切，再增量实现。
4. 每完成一组脚本就让 Unity 导入并检查编译；错误未清零前不扩展新功能。
5. 不只写计划，用户授权执行后必须直接实施并验证。
6. 不要求用户手工创建文件夹、脚本、数据资产或场景对象。
7. 只在真正需要用户权限、选择或无法自动解决的 Unity 阻塞时提问。
8. 不因为外部 `analysis` 无法访问而停工。
9. 不因缺少最终美术而停工，使用原创程序化占位内容。
10. P0 默认不增加依赖；用户已允许下载必要工具，但必须限定在项目目录、使用可信官方来源、记录版本/许可证/哈希，并遵守本计划的隔离规则。
11. 不修改 `analysis`、XAPK 或 Unity 生成缓存。
12. 不强制关闭 Unity，不启动冲突的第二个 Editor。
13. 不牺牲可编译、可试玩的 P0 去追求 P1 润色。
14. 每阶段完成后更新本文件的进度表和验证证据。

---

## 15. 最终实施报告格式

实施完成后用中文返回：

```text
项目根目录：
计划版本：
实际读取的可选研究文件：
P0 Demo 完成：是 / 否
Unity 实际编译验证：是 / 否
编译错误数量：
Agent 实际 Play Mode 验证：是 / 否
主场景：
已实现系统：
延期到 P1 的系统：
已知问题：
验证证据：
用户下一步唯一需要做的操作：
```

理想情况下，用户唯一需要做的是：

```text
打开 Assets/_Game/Scenes/GachaRPGDemo.unity，然后点击 Play。
```

---

## 16. 阶段进度表

| 阶段 | 状态 | 主要交付物 | 验证证据 | 更新时间 |
|---|---|---|---|---|
| 计划制定 | 已完成 | `PROJECT_PLAN.md` | v2.2 已同步怒气、三技能错峰、职业射程与 Catherine 真实凝胶里程碑 | 2026-07-14 |
| 阶段 0：基线检查 | 已完成 | 路径、版本、现有改动、目录基础 | Unity 6000.5.3f1、URP/uGUI/Input System/Test Framework 与构建支持已确认 | 2026-07-13 |
| 阶段 1：数据与服务 | 已完成 | 定义、存档、默认数据、服务接口 | 七角色/三技能/一组六角色标准抽卡池；内存存档、抽卡与编队验证通过 | 2026-07-14 |
| 阶段 2：主页/抽卡/收藏/编队 | 已完成 | 完整非战斗流程 | 自动化 UI 冒烟测试已走通 Home、单抽、收藏与编队 | 2026-07-13 |
| 阶段 3：3v3 战斗核心 | 已完成 | 确定性自动战斗 | 固定 Tick、同 Seed 完整事件序列一致；胜负与超时验证通过 | 2026-07-13 |
| 阶段 4：表现与场景 | 已完成 | 角色、UI、VFX、Generator、Scene | 程序化角色、运行时 UI、表现层及场景已在 Unity Play Mode 实测 | 2026-07-13 |
| 阶段 5：P0 验证与试玩 | 已完成 | 可重复试玩的完整流程与 Windows Build | `P0_VERIFY_PASS`、`P0_PLAY_SMOKE_PASS`、`WINDOWS_BUILD_PASS`；详见 `VerificationReport.md` | 2026-07-13 |
| 长期制作基础设施 | 已完成（清理待授权） | 本地 Git/LFS、`AGENTS.md`、StudioOps、项目级 `.codex/config.toml`、嵌入式隔离 Unity MCP、隔离 uv/Python 与 Editor Bootstrap | 48 工具离线协议复测、`UNITY_MCP_SMOKE_PASS`、P0/Play 回归与最终 Windows 构建均通过；项目外残留详见 `VerificationReport.md` | 2026-07-14 |
| 阶段 6：P1 | 进行中 | 首个地图、五人战斗、怒气/三技能与 UR 真实凝胶样板已完成；正式角色动画、十连、碎片、升级仍延期 | `StudioOps/MILESTONES/2026-07-14_RAGE_THREE_SKILL_AND_CATHERINE_REALISM.md` | 2026-07-14 |
| 阶段 6.1：五人射程战斗与史莱姆重制 | 已完成 | 五槽/5v5、攻击距离、持续位置与锁敌、1.6 倍回放、六材质黑洞史莱姆及紧凑战斗 UI | `P0_VERIFY_PASS`、`P0_PLAY_SMOKE_PASS`、`WINDOWS_BUILD_PASS` 与真实 1920×1080 窗口核验 | 2026-07-14 |
| 阶段 6.2：柔和漫画地图与元素测试史莱姆 | 已完成 | 水火土风雷五套基础史莱姆、柔和漫画星渊观测台、限定黑洞动画与统一视觉集成 | GPT Image 设定底稿、五系 Blender/FBX/Prefab、几何审计、PlayMode、Windows Build 与双分辨率实机核验均完成 | 2026-07-14 |
| 阶段 6.3：Catherine 满级技能与 UR 视觉重制 | 已完成 | 满级三技能/领域/觉醒、大招全体拉拽与击飞、10×HP/0.1×ATK 测试敌人、UR Blend Shapes、Slime Toon Shader 与原创黑洞 VFX | `GenerateCompile3.log`、`PlaySmokeFinal.log`、`WindowsBuildFinal.log` 均通过；D3D11 无 Shader error，1920×1080 / 1280×720 实机逐帧确认吸附、坍缩与击飞 | 2026-07-14 |
| 阶段 6.4：怒气、三技能轮转与 Catherine 真实凝胶升级 | 已完成 | 0–1000 怒气制、槽位 1 大招、槽位 2 / 3 错峰 10 秒轮转、职业射程 1 / 5 与最大射程停位；Catherine 真实凝胶材质、纯黑核心、19,000 面轮廓和三点布光 | `Artifacts/CatherineRealism/RageGenerate4.log`、`RagePlaySmoke.log`、`RageWindowsBuild.log` 均通过；D3D11 构建 114,033,256 bytes / 24.1s，1920×1080 与 3440×1392 级宽屏实机检查及黑洞吸附截图确认 | 2026-07-14 |

状态仅使用：`未开始 / 进行中 / 修复中 / 已完成 / 阻塞`。

---

## 17. 决策记录

| 日期 | 决定 | 原因 | 影响 |
|---|---|---|---|
| 2026-07-13 | 本计划存入真实 Unity 项目根目录 | 后续 Agent 可能无法访问上级 `analysis` | 计划必须自包含，外部分析仅为可选只读资料 |
| 2026-07-13 | 使用 Unity `6000.5.3f1` 的现有 URP 项目 | 这是本机实际项目版本 | 不沿用旧提示词中的错误版本号 |
| 2026-07-13 | P0 采用完整垂直切片，而非仅战斗 Demo | 用户希望亲自试玩“游戏 Demo” | 必须包含主页、单抽、收藏、编队、3v3 战斗和结果 |
| 2026-07-13 | 使用原创程序化彩色角色 | 快速、无版权依赖、无需外部资产 | 正式 3D 角色管线延期，但保留统一 CharacterView 契约 |
| 2026-07-13 | P0 使用本地 Mock 服务与存档 | 优先得到离线可玩 Demo | 正式抽卡、货币和奖励未来改为后端权威 |
| 2026-07-13 | P1 才做十连、碎片和扩展养成 | 保护 P0 的编译与可玩性 | 单抽与收藏更新仍为 P0 强制功能 |
| 2026-07-13 | 允许下载必要工具，但必须项目内隔离 | 用户授权补充工具，同时要求不得越界到其他目录 | P0 先只用现有 Unity；未来依赖优先放入 `_ProjectTools`，系统级写入必须再次说明 |
| 2026-07-13 | 用户授权启动长期开发任务 | 用户明确说“shall we start”并允许长期运行 | 计划由讨论状态切换到实施；P0 已完成 |
| 2026-07-13 | P0 不增加任何下载或第三方依赖 | 现有 Unity、URP、uGUI 与 Input System 已足够 | 所有实现和程序化表现均保留在项目内；详见 `ThirdPartyInventory.md` |
| 2026-07-13 | 同时交付 Windows 独立试玩版 | 用户希望能够亲自体验 Demo | 已生成 `Builds/Windows/GenericGachaRPGDemo.exe`，并通过 Unity BuildPipeline |
| 2026-07-13 | P0 以三组自动验证作为完成门槛 | 防止只写代码但未编译或未实际走流程 | 核心验证、Play Mode UI 冒烟和 Windows Build 全部留下 PASS 标记 |
| 2026-07-14 | 仅在 `GenericGachaRPG` 内建立 Git/Git LFS 基线 | 长期项目必须先具备可靠回滚，同时隔离同级分析与 XAPK | 已创建 `main` 基线提交 `27ac1da`；没有远程仓库，生成目录和本地工具被忽略 |
| 2026-07-14 | 采用精简的 Codex 原生 `StudioOps`，不安装 Claude-Code-Game-Studios/BMAD | 现有主计划已经完整，项目真正缺少当前工作包、美术契约和资产台账 | 以 `AGENTS.md`、五个 StudioOps 文件和八类按需职责替代重型虚拟工作室框架 |
| 2026-07-14 | 以 `10.1.0-project.1` 嵌入并隔离 `CoplayDev/unity-mcp v10.1.0` | 上游自动设置、更新、HTTP 与用户级偏好路径不满足严格项目边界 | 保留上游提交和 MIT 许可证；在 Unity 6000.5.3f1 完成 48 工具离线连接、P0 与 Windows Build 回归；仅保留项目 stdio + `127.0.0.1:6400` MCP 回环，遥测和全局配置写入默认禁用 |
| 2026-07-14 | 不擅自清理项目外残留 | 隔离加固前的验证意外创建 `C:\Users\yshaw\AppData\Local\UnityMCP`，用户要求不得越界操作 | 已封堵未来外部写入；未读取或删除该目录，等待用户明确授权后只清理这一精确路径 |
| 2026-07-14 | 项目级 Codex MCP 配置作为后续任务入口 | 当前任务不会热加载新建的 `.codex/config.toml` | 新 Codex 任务或应用重启并信任项目后加载；当前 Unity Bridge 与手动 MCP 协议验证已通过 |
| 2026-07-14 | 首个正式 3D 样板锁定为紫色星空/黑洞史莱姆 | 用户提供了唯一三视角参考图并明确要求制作首个 UR 限定角色 | 资产 ID 为 `ART-CHAR-UR-COSMIC-SLIME-001`；先建立样板管线，不自动锁定全项目风格 |
| 2026-07-14 | 停止 Claude 角色生成路线，由 Codex 独立接管 | Claude 使用了未加载本项目隔离配置的多套 MCP Server，未产生模型或消耗 Tripo 额度 | 所有后续证据以项目文件、官方 API 响应、Blender 和 Unity 验证为准 |
| 2026-07-14 | 使用用户已安装的 Blender 5.1 | 安装已存在，无需下载；适合清理 AI 网格、重建轨道环并导出 Unity 原生 FBX | 运行时把 HOME、AppData、配置、缓存和 TEMP 重定向到 `_ProjectTools/`；不修改安装目录 |
| 2026-07-14 | 仅为首个角色评估 Tripo 官方上传/生成接口 | 当前 Unity Tripo 适配器未实现本地文件上传；官方 API 支持 multipart 上传后使用 `image_token` | 余额检查为 available 0 / frozen 0，因此未上传、未建任务、未消费；首版改为本地 Blender 程序化建模，未来有额度才做 A/B 候选 |
| 2026-07-14 | 公开过的 Tripo Key 视为已暴露 | Key 出现在聊天中，且此前流程创建了用户级环境变量/凭据 | 本次只从现有存储读入内存；生成后建议用户轮换；清理用户级状态需另行授权，见 `StudioOps/DEFERRED_WORK.md` |
| 2026-07-14 | 工作区正式保留名称 `BubbleMind` | 用户明确偏好该名称并同意修正此前文档中的旧路径判断 | 唯一可写工程路径统一为 `C:\Users\yshaw\OneDrive\Desktop\BubbleMind\GenericGachaRPG`；后续任务不得再使用不存在的 `Game-jjk` 路径 |
| 2026-07-14 | 授权发布到 `GoldenStone929/BubbleMind` | 用户要求把现有空仓库作为项目远端，并同意当前路径判断 | 允许为本地 `main` 配置该唯一远端并推送 Git/Git LFS 内容；不包含构建发布或其他远端授权 |
| 2026-07-14 | 首张地图采用“先生成概念图、再应用”的星渊观测台方案 | 用户要求先看地图图像，再把地图用于首个试玩版 | 原创 16:9 概念图以全画幅 URP 背板接入首页与战斗；另记录翠空铸园、镜潮陨坑两个后续方向 |
| 2026-07-14 | Windows 试玩固定 Direct3D 11 与 60 FPS 目标 | 首个试玩更重视广泛兼容、稳定演示和一致帧节奏 | 产品名统一为 `BubbleMind First Demo`；真实窗口完成首页、收藏、编队、战斗和结算检查 |
| 2026-07-14 | P1 扩展为五人编队与 5v5 自动战斗 | 用户试玩后明确要求五个槽位，并要求角色主动接近目标 | 存档、生成器、UI、战斗布局、目标选择、验证与构建统一使用五人规则；旧三人存档按新 schema 安全重置 |
| 2026-07-14 | 稀有度统一为 `R -> SR -> SSR -> SP -> UR` | 用户明确指定完整顺序 | 枚举值固定为 0 到 4；所有数据资产重新生成，避免旧 `UltraRare=3` 被误解释为 `SP` |
| 2026-07-14 | 星渊吞噬体定为首位 `UR` 限定坦克 | 用户要求首个角色承担坦克定位且是首位限定角色 | 新增正式限定数据字段；角色放入默认编队首槽、排除标准池，并按坦克数值与最近目标接战规则运行 |
| 2026-07-14 | 重制星渊吞噬体而非局部调色 | 用户指出当前模型外形与颜色均不接近参考图 | Blender 主体、角、眼、黑洞核心、星点与分段金属轨道重新制作；Unity 材质改为近黑星空胶体、白紫发光与紫金属结构 |
| 2026-07-14 | 五人战斗采用数据化攻击距离与持续逻辑位置 | 用户要求坦克进入射程后留在敌人身边，直到目标死亡才重新接近最近敌人；只读 PvP 架构资料也把 AI、Movement、Targeting 分为确定性系统 | `CharacterDefinition` 增加攻击距离与移动速度；模拟层保存当前位置和锁定目标，每 tick 移动，目标死亡后才按当前位置重选；表现层只回放位置事件 |
| 2026-07-14 | 继续修正史莱姆近黑配色与正面黑洞可读性 | Windows 实机中模型仍偏亮，侧向站位使事件视界和吸积环不明显 | Unity 材质、FBX 朝向、核心层级和战斗三分之四朝向共同校准，不能只依赖 Blender 正面预览 |
| 2026-07-14 | 全局地图方向改为柔和漫画风 | 用户指出当前地图偏写实，希望整体更柔和、更接近漫画与动画渲染 | 星渊观测台保留玩法身份和资源 GUID，但替换为低频色块、柔和明暗与克制高光；前景材质同步降低金属感 |
| 2026-07-14 | 六名标准测试单位改用五元素基础史莱姆 | 用户要求测试单元使用最基础史莱姆，并在完成时交付水、火、土、风、雷角色基础表与模型 | 保留角色稳定 ID、技能、数值、稀有度和卡池权重；元素本轮只作为视觉分类，不新增克制或伤害规则 |
| 2026-07-14 | 五元素设定表与新地图使用 GPT Image，再由 Blender 程序化建模 | 用户明确要求先用 GPT Image 生成基础表，再生成模型 | 生成图、提示意图、SHA-256、Blender 源文件、FBX、审计和 Unity Prefab 全部写入项目资产台账；现有工具足够，本轮不下载新依赖 |
| 2026-07-14 | 黑洞演出采用纯 Transform 动画 | 用户要求制作 black hole animation，同时当前材质实例化会增加运行时负担 | 事件视界保持固定朝向并轻微呼吸，吸积盘与螺旋错相旋转；不通过 `Renderer.material` 创建运行时材质副本 |
| 2026-07-14 | Catherine Yuki 在 Demo 中始终按满级技能与觉醒测试 | 用户提供了完整技能等级、领域与觉醒规则，并明确要求默认满级 | 三技能、30 层虚拟质量、960% 大招、4×层数倍率、死亡大招/复活只触发一次；演示数据不建立升级界面 |
| 2026-07-14 | 大招必须改变战场位置并完成拉拽、持续伤害、坍缩与击飞 | 用户指出当前 UR 没有兑现技能描述 | 确定性模拟新增明确阶段事件；所有存活敌人先被拉到黑洞位置，随后多段受伤并在坍缩时击飞，表现层只能重播结果 |
| 2026-07-14 | 敌方测试单位使用 10×生命与 0.1×攻击 | 用户需要足够长的战斗观察击飞、命中和减益 | 倍率只应用于 Demo 敌方战斗实例，不修改角色资产、收藏数据、卡池或玩家同角色数值 |
| 2026-07-14 | UR 视觉基线改为 Slime Master Shader + Blend Shapes + 原创 VFX | 用户认为当前模型不合格，并明确指出渐变、软边、厚度、果冻高光、内部结构与 Squash & Stretch 才是关键 | 重做模型轮廓与形变；新增项目自有 URP Toon 果冻 Shader、黑洞 VFX 和技能演出，不再以单纯 FBX/纯色材质作为完成标准 |
| 2026-07-14 | 付费 VFX/Toon 资产只作为品质参考，未经单独确认不购买 | 用户列出 Stylized VFX URP、Anime Effects Pack 等候选，但未明确授权费用与具体商品 | 本轮先用现有 Unity 6/URP 自制；如后续购买，必须先列出准确商品、当前价格、许可、版本、来源与写入路径并等待确认 |
| 2026-07-14 | Catherine 自动化门槛通过不等于视觉验收完成 | 本轮 Generate/核心验证、PlayMode 冒烟和 Windows BuildPipeline 均返回 PASS，但 Windows 实机仍出现节奏偏快与浅青色遮挡球，构建日志同时保留 D3D11 Shader 编译错误 | 阶段 6.3 保持“修复中”；必须修复 Shader/VFX 与实时节奏并重新完成双分辨率实机核验后，才允许标记最终通过 |
| 2026-07-14 | Catherine D3D11 与双分辨率视觉门槛完成 | HLSL 局部名 `point` 在 D3D11 下被解析为保留字；通用实心球脉冲也遮挡角色 | 改名 Shader 变量、用透明能量环替换实心球、把回放降至 1.25 倍；最终 Build 日志无 Shader error，1920×1080 与 1280×720 均确认黑洞吸附、坍缩、击飞和 UI 可读 |
| 2026-07-14 | 战斗资源统一为 0–1000 怒气，技能槽 1 固定为大招 | 用户要求普攻与受伤都能积累怒气，并让大招只在满怒时释放 | 单位初始怒气 0；普攻命中 +100，受到实际伤害 +50；满 1000 后释放大招并清零，世界条显示 `RAGE current/1000` |
| 2026-07-14 | 技能槽 2 / 3 采用 5 秒错峰、各 10 秒周期 | 用户要求两项主动技能定时释放但不得同时触发 | 槽 2 在 5 / 15 / 25 秒，槽 3 在 10 / 20 / 30 秒；本轮 19.3 秒验证战斗实际观察到槽 2 的 5 / 15 秒与槽 3 的 10 秒触发 |
| 2026-07-14 | 职业射程改为 Tank / Assassin = 1、其余 = 5，并把 Catherine 升级为真实凝胶灯光样板 | 用户要求单位停在最大攻击距离，同时认为 Catherine 仍欠缺真实体积与灯光质感 | 移动模拟停在自身射程边界；UR 模型重建为 19,000 面不对称凝胶体、纯黑事件视界与体积吸积结构，战斗场景使用主光/补光/轮廓光三点布光 |

---

## 18. 待用户讨论的 P1 方向

P0 已完成。以下选项不会阻塞当前试玩版，可在用户试玩后决定：

1. P1 玩法优先级：首个 UR 样板、怒气与三技能轮转已经完成；下一步决定十连/保底/碎片或角色升级与养成。
2. 美术方向：星空/黑洞史莱姆的真实凝胶与三点布光样板已经通过内部原型门槛；下一步决定是否扩展到全项目。
3. 战斗方向：优先加入碰撞/分道与正式动作，或扩展更多技能机制和目标策略。
4. 下一目标平台：继续 Windows，或优先 WebGL/Android 适配。
5. 是否在玩法扩展前先加入音效、正式模型或更完整的输入/无障碍设置。
