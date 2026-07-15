# Architecture Summary

## P0 Vertical Slice

```text
Home → Gacha → Character Archive → Formation → 3v5 Battle → Result → Home / Restart
```

## Runtime boundaries

```text
ScriptableObject definitions
  └─ GameDatabase / Character / Skill / Banner
       ↓
Local services
  ├─ GameState + JSON save
  ├─ Seeded gacha
  └─ Formation validation
       ↓
DemoGameController
  ├─ uGUI screens
  └─ Battle request
       ↓
Deterministic BattleSimulation
  └─ ordered BattleEvent stream
       ↓
DemoBattlePresenter
  └─ authored slime prefabs / fallback characters / bars / numbers / VFX
```

- 静态内容使用 ScriptableObject；玩家拥有状态、余额和编队使用可序列化运行时数据。
- UI 只展示状态并发送意图，不拥有 RNG、扣费、编队合法性或伤害规则。
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
- 当前战斗部署与五槽存档分离：玩家保存五名角色，但测试战斗固定部署 Catherine、Gold Ranger、Ember Striker 三人，对抗五名敌人。Ember 的技能槽 2 在第 5 秒选择仍存活的敌方后排，瞬移后保持精确 2 格分离；第 15 秒再次释放仍沿用存活目标，直到目标死亡才重新选择。
- 表现层只重播位置、攻击、技能、怒气、伤害和死亡事件，不反向修改模拟结果，也不得把世界根节点重置到出生槽。
- `CharacterView` 提供统一 Socket 和动作接口；五套基础元素史莱姆与限定 UR 使用正式 Prefab，无 Animator 时由统一回退动作提供呼吸、挤压、攻击、受击和倒地。
- `BasicSlimeVisualController` 只驱动气泡、火焰、岩叶、风带和电弧等元素装饰；`CosmicSlimeVisualController` 只驱动事件视界呼吸、吸积盘和轨道 Transform，不修改战斗模拟或实例化材质。Catherine 使用近黑真实凝胶外壳、纯黑核心与三点场景布光。
- `ProceduralCharacterBuilder` 仅作为 Prefab 缺失/损坏时的防崩回退；正常 3v5 由冒烟测试强制验证为 1 名限定 UR + 7 名基础元素史莱姆。

## Editor tooling

- `DemoSceneGenerator`：生成或修复数据、场景、项目设置和 Build Settings。
- `DemoProjectVerifier`：验证数据库、内存存档、抽卡、编队、怒气、三技能错峰、职业射程、Catherine 契约、确定性战斗和场景。
- `CharacterContentProfile.TryValidate`：验证角色归属、档案审批、原创/来源、能力 ID、三运行时槽、从 Lv.1 到 MaxLevel 的连续逐级参数、养成与获取关系；七份生成档案必须全部通过。
- `DemoPlayModeSmokeOrchestrator`：运行完整页面流程，并检查 8 个世界怒气条和 3v5 战斗事件契约。
- `DemoBuildAutomation`：输出项目内隔离的 Windows x64 试玩版。

## Production boundary

P0 为离线本地 Demo。正式经济、抽卡、奖励、账号、IAP、PvP 和远程内容应迁移到后端权威服务，不能直接沿用本地 Mock 作为生产安全方案。
