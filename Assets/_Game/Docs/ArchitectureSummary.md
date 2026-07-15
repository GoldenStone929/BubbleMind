# Architecture Summary

## P0 Vertical Slice

```text
Home → Gacha → Collection → Formation → 5v5 Battle → Result → Home / Restart
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
- `IRandomService`、`ISaveService`、`IGachaService` 和 `IFormationService` 可在未来替换为正式实现。
- `CharacterDefinition` 数据化保存 `AttackRange`、`MoveSpeed`、`IsLimited` 与 `R -> SR -> SSR -> SP -> UR` 稀有度。
- 战斗核心按固定 Tick 运行，分别保存出生槽、当前战场位置和持续锁定目标；同一输入和 Seed 产生相同结果、位置及事件序列。
- 单位每 Tick 先按移动速度接近锁定目标，进入攻击距离后停止；目标存活期间不换锁，死亡后才按当前位置选择最近敌人。
- 表现层只重播位置、攻击、技能、伤害和死亡事件，不反向修改模拟结果，也不得把世界根节点重置到出生槽。
- `CharacterView` 提供统一 Socket 和动作接口；五套基础元素史莱姆与限定 UR 使用正式 Prefab，无 Animator 时由统一回退动作提供呼吸、挤压、攻击、受击和倒地。
- `BasicSlimeVisualController` 只驱动气泡、火焰、岩叶、风带和电弧等元素装饰；`CosmicSlimeVisualController` 只驱动事件视界呼吸、吸积盘和轨道 Transform，不修改战斗模拟或实例化材质。
- `ProceduralCharacterBuilder` 仅作为 Prefab 缺失/损坏时的防崩回退；正常 5v5 由冒烟测试强制验证为 1 名限定 UR + 9 名基础元素史莱姆。

## Editor tooling

- `DemoSceneGenerator`：生成或修复数据、场景、项目设置和 Build Settings。
- `DemoProjectVerifier`：验证数据库、内存存档、抽卡、编队、确定性战斗和场景。
- `DemoPlayModeSmokeOrchestrator`：运行完整页面流程冒烟测试。
- `DemoBuildAutomation`：输出项目内隔离的 Windows x64 试玩版。

## Production boundary

P0 为离线本地 Demo。正式经济、抽卡、奖励、账号、IAP、PvP 和远程内容应迁移到后端权威服务，不能直接沿用本地 Mock 作为生产安全方案。
