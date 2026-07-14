# Architecture Summary

## P0 Vertical Slice

```text
Home → Gacha → Collection → Formation → 3v3 Battle → Result → Home / Restart
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
  └─ procedural characters / bars / numbers / VFX
```

- 静态内容使用 ScriptableObject；玩家拥有状态、余额和编队使用可序列化运行时数据。
- UI 只展示状态并发送意图，不拥有 RNG、扣费、编队合法性或伤害规则。
- `IRandomService`、`ISaveService`、`IGachaService` 和 `IFormationService` 可在未来替换为正式实现。
- 战斗核心按固定 Tick 运行，同一输入和 Seed 产生相同结果及事件序列。
- 表现层只重播事件，不反向修改模拟结果。
- `CharacterView` 提供统一 Socket 和动作接口；无 Animator 或正式模型时自动使用程序化回退。

## Editor tooling

- `DemoSceneGenerator`：生成或修复数据、场景、项目设置和 Build Settings。
- `DemoProjectVerifier`：验证数据库、内存存档、抽卡、编队、确定性战斗和场景。
- `DemoPlayModeSmokeOrchestrator`：运行完整页面流程冒烟测试。
- `DemoBuildAutomation`：输出项目内隔离的 Windows x64 试玩版。

## Production boundary

P0 为离线本地 Demo。正式经济、抽卡、奖励、账号、IAP、PvP 和远程内容应迁移到后端权威服务，不能直接沿用本地 Mock 作为生产安全方案。

