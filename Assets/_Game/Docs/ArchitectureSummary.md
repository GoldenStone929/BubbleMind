# Architecture Summary

## Full Offline System Loop

```text
Home
├─ World → Stage Detail → Formation → 5v5 Pixel Battle → Result → Rewards → World / Home / Restart
├─ Characters / Hero Archive
├─ Recruit
├─ Formation
├─ Inventory
├─ Missions
└─ Settings
```

## Runtime boundaries

```text
ScriptableObject definitions
  └─ GameDatabase / Character / Skill / Banner / Stage
       ↓
Local services
  ├─ GameState + schema v4 JSON save
  ├─ Seeded gacha
  ├─ Formation validation
  └─ Stage entry / rewards / missions / settings
       ↓
DemoUiRouter + DemoGameController
  ├─ AppShell + registered uGUI screens
  ├─ route history / back / locked feature context
  └─ stage-selected Battle request
       ↓
Deterministic BattleSimulation
  └─ ordered BattleEvent stream
       ↓
DemoBattlePresenter
  └─ pixel sprites / compatibility fallback / bars / numbers / modern VFX
```

- 静态内容使用 ScriptableObject；玩家拥有状态、余额和编队使用可序列化运行时数据。
- UI 只展示状态并发送意图，不拥有 RNG、扣费、编队合法性或伤害规则。
- `DemoUiRouter` 注册 Home、World、Recruit、Characters、Formation、Inventory、Missions、Settings、LockedFeature 与 Battle 路由，并统一处理前进、替换、返回和重置；Battle 不进入普通页面历史，结算使用明确的 World/Home/Restart 目标。
- `AppShellView` 在主要元系统页面复用玩家档案、水晶、金币、体力、编队、邮件、设置与底部导航；路由高亮和任务可领取徽标均从当前状态刷新，不各自维护副本。
- World 从 `GameDatabase.Stages` 读取三个 `StageDefinition`，按稳定 ID 和 `prerequisiteStageId` 顺序解锁；进入 Formation 前选择关卡，开始战斗时由 `GameStateService` 验证开放状态、五槽阵容和体力并扣除消耗。
- 胜利结算记录通关与胜场，常规发放 Gold/Echo Gel，首次通关额外发放 Crystal，Boss 首通再发放 Void Fragment；Result 展示包含 `RareMaterials` 的 `StageRewardGrant`，不直接写入玩家状态。`DemoGameController` 使用每局 `battleResultCommitted` 守卫，保证完成回调只结算一次；Restart 开启新局并再次支付本关体力。
- `PlayerState` schema v4 保存玩家档案、Crystal、Gold、Energy、抽卡次数、胜场、拥有角色、五槽阵容、背包、通关关卡、已领取任务和设置；v3 → v4 迁移保留旧水晶、角色和阵容并补入新字段默认值。
- Missions 通过静态 `DemoMissionCatalog` 计算招募、拥有角色、胜场和 1-3 通关进度；领取动作在服务层进行幂等检查并保存奖励。Inventory 通过 `DemoInventoryCatalog` 映射本地物品，重复抽卡会增加 Universal Shard。
- Home Continue 每次刷新都从 `GameDatabase.GetCurrentStage` 与最新玩家进度解析目标，通关后不会沿用陈旧 `selectedStageId`。Settings 的音乐/效果音量、全屏和 60 FPS 选项写回同一存档；本地数据重置必须经过覆盖安全区域的模态确认层。竞技场、活动、商店、邮件和公会只进入带上下文的锁定页面，不伪造在线状态或奖励。
- `CharacterDefinition.Id` 是角色在抽卡结果、角色档案、编队槽位、存档与战斗请求之间的唯一身份键；所有页面从同一 `CharacterDefinition` 解析名称、稀有度、职业、三技能与 `Portrait`，不维护第二套角色资料。
- `CharacterContentProfile` 是由 `CharacterDefinition` 单向引用的补充档案，保存元素、阵营、称号、Basic/Passive/Domain/Awakening、逐级参数、解锁、养成阶段、获取关系与来源证明；其中 `ownerCharacterId` 只用于阻止档案误挂，角色身份权威仍是 `CharacterDefinition.Id`，Profile 不重复运行时基础数据。角色页通过 `Combat / Archive / Growth` 三个模式读取同一条数据链。
- `CharacterDefinition.Portrait` 是当前 2D 卡面单一来源。抽卡服务只返回权威 `rewardId` 与数量，UI 再通过数据库映射为卡面；展示层不得改变抽卡概率、扣费、重复角色转化或拥有状态。
- 首页、抽卡、角色档案与编队使用独立 Screen View；`DemoGameController` 只协调导航和服务，卡面揭示动画属于纯表现层。
- `IRandomService`、`ISaveService`、`IGachaService` 和 `IFormationService` 可在未来替换为正式实现。
- `CharacterDefinition` 数据化保存职业、`MoveSpeed`、`AttackRange`、三个技能槽、怒气参数、`IsLimited` 与 `R -> SR -> SSR -> SP -> UR` 稀有度。生成内容仍按职业给出默认射程，但 `BattleUnitState` 会快照角色资产中的射程与怒气参数，不再用全局常量覆盖角色级调优。
- 战斗核心按固定 Tick 运行，分别保存出生槽、当前战场位置和持续锁定目标；同一输入和 Seed 产生相同结果、位置及事件序列。
- 战场长度统一为 20 个逻辑格并限制所有位移不越界。当前 Demo 为 Tank / Assassin 配置 2 格、Support / Ranged / Mage 配置 10 格；单位每 Tick 按移动速度接近锁定目标，在角色自己的最大射程边界停止，目标存活期间不换锁，死亡后才按当前位置选择最近敌人。
- 当前 Demo 单位从 `0 / 1000` 怒气开始；普攻命中令攻击者 +100，受到实际伤害令受击者 +50。技能槽 1 是大招，在怒气达到其 `SkillDefinition.RageCost` 时释放并扣除精确成本；当前 1000/1000 配置因此表现为清零。
- 技能槽 2 首次在 5 秒、此后每 10 秒释放；技能槽 3 首次在 10 秒、此后每 10 秒释放。两个定时槽位使用独立日程并禁止同 Tick 同时释放；受控或条件不足时保留到期技能，实际施放后才推进下一周期。
- Catherine 的技能槽 1 / 2 / 3 分别映射 `Infinite Void / Wind Wheel: Break / Wind Wheel: Dance`，`Star Rage` 是由战斗事件驱动的被动领域。
- 大招可用性与扣除量读取 `SkillDefinition.RageCost`，并要求消耗不高于角色 `MaxRage`；默认 1000 上限/1000 消耗保持用户规则，扩展角色不再被错误地按上限提前施放。
- `Wind Wheel: Dance` 的满级 520% 总倍率与档案统一，运行时拆为两段 260%，治疗继续按实际伤害的 140% 结算。
- Catherine 的 `Wind Wheel: Break` 请求把命中目标沿敌方方向击退 5 格；中心区域实际移动 5 格，接近地图边缘时按战场边界截断。
- 当前战斗部署直接读取玩家保存的合法五槽阵容并生成 5 名玩家单位，对抗 5 名测试敌人；新存档默认顺序是 Catherine、Gold Ranger、Ember Striker、Verdant Medic、Violet Arcanist。Ember 的技能槽 2 在第 5 秒选择仍存活的敌方后排，瞬移后保持精确 2 格分离；第 15 秒再次释放仍沿用存活目标，直到目标死亡才重新选择。
- 表现层只重播位置、攻击、技能、怒气、伤害和死亡事件，不反向修改模拟结果，也不得把世界根节点重置到出生槽。
- `CharacterView` 提供统一 Socket 和动作接口；`PixelCharacterBuilder` 从稳定角色 ID 加载 128×128 Point Filter Sprite，`PixelCharacterVisual` 负责镜头朝向、像素级位置吸附、阵营翻转、挤压、攻击、受击和倒地表现。
- Catherine 的纯黑事件视界、白紫吸积盘与技能阶段继续使用 `CatherineSkillVfxController` 和现代 Shader/VFX；表现组件不修改战斗模拟。
- 旧 `BasicSlimeVisualController`、`CosmicSlimeVisualController` 和 authored 3D Prefab 只作为素材缺失时的兼容回退。`ProceduralCharacterBuilder` 是最后一级防崩回退；正常 5v5 冒烟强制验证恰好 10 个 `PixelCharacterVisual`、0 个旧 3D 控制器。

## Editor tooling

- `DemoSceneGenerator`：生成或修复数据、三关、场景、项目设置和 Build Settings；已有额外作者关卡保持不变。
- `DemoProjectVerifier`：除既有数据库、抽卡、编队和战斗契约外，还验证三关 ID/顺序/前置/奖励/敌人引用、关卡解锁、任务与 schema v3 → v4 迁移。
- `CharacterContentProfile.TryValidate`：验证角色归属、档案审批、原创/来源、能力 ID、三运行时槽、从 Lv.1 到 MaxLevel 的连续逐级参数、养成与获取关系；七份生成档案必须全部通过。
- `DemoPlayModeSmokeOrchestrator`：连续运行 1-1 Formation → 5v5 Battle → 首通奖励 → Restart 重复奖励 → Home Continue → 1-2 解锁，精确断言两次体力/Crystal/Gold/Echo Gel/胜场差值；同时覆盖 App Shell、Inventory、Mission Claim、四项 Settings 持久化、重置确认/取消和锁定 Arena。
- `DemoBuildAutomation`：输出项目内隔离的 Windows x64 试玩版 `Builds/FullSystemWindows/BubbleMind.exe`，避免覆盖仍在运行的历史构建。

## Production boundary

当前完整系统仍是离线本地 Demo。正式经济、抽卡、奖励、账号、IAP、PvP、邮件、公会、活动和远程内容应迁移到后端权威服务；锁定入口是产品信息架构占位，不能把本地 Mock 直接当作生产安全方案。
