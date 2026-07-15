# BubbleMind 原创角色基础目录

> 日期：2026-07-15
> 权威数据：`Assets/_Game/Data/Characters/`、`Skills/` 与 `CharacterProfiles/`

本文便于策划和美术快速查阅。数值、卡池与运行时引用仍以 ScriptableObject 为准，本文不作为第二数据源。

## 七名角色

| Character ID | 名称 | 稀有度 | 职业 | 元素 | 射程 / 20 | 移速 | 大招 | Skill 2 | Skill 3 | 获取 |
|---|---|---:|---|---|---:|---:|---|---|---|---|
| `azure_vanguard` | Azure Vanguard | R | Tank | Water | 2 | 3.2 | Pulse Strike | Tactical Impact | Tactical Wave | Standard Signal |
| `ember_striker` | Ember Striker | R | Assassin | Fire | 2 | 4.2 | Pulse Strike | Backline Shift | Tactical Wave | Standard Signal / Demo Starter |
| `verdant_medic` | Verdant Medic | SR | Support | Wind | 10 | 3.0 | Restore Wave | Tactical Impact | Tactical Wave | Standard Signal / Demo Starter |
| `ur_cosmic_slime` | Catherine Yuki | UR Limited | Tank | Void | 2 | 3.3 | Infinite Void | Wind Wheel: Break | Wind Wheel: Dance | Limited Signal / Demo Test Grant |
| `violet_arcanist` | Violet Arcanist | SR | Mage | Lightning | 10 | 3.4 | Spectrum Nova | Tactical Impact | Tactical Wave | Standard Signal / Demo Starter |
| `gold_ranger` | Gold Ranger | SSR | Ranged | Earth | 10 | 3.8 | Pulse Strike | Tactical Impact | Tactical Wave | Standard Signal / Demo Starter |
| `cyan_warden` | Cyan Warden | SSR | Tank | Water | 2 | 3.15 | Spectrum Nova | Tactical Impact | Tactical Wave | Standard Signal |

所有普通角色档案还包含 `Resonant Strike` 基础攻击和一个元素共鸣被动模板。共鸣只作为未来养成挂点，本轮不修改确定性战斗。

## Catherine Yuki 满级测试档案

### 基础攻击

- `Gravity Strike`
- 射程 2 / 20；锁定最近目标，目标存活期间不换敌。
- 普攻命中建立怒气；受到实际伤害也建立怒气。

### Infinite Void

- 能力类型：Ultimate；运行时槽 1。
- 触发：当前配置为怒气达到 1000、消耗 1000，因此释放后归零；模板按 `SkillDefinition.RageCost` 精确扣除，也支持非满额大招消费。
- 目标：全部存活敌人。
- 时间阶段：蓄力、黑洞变形、全体吸附、多段伤害、坍缩、击飞。
- 九级伤害档：720 / 750 / 780 / 810 / 840 / 870 / 900 / 930 / 960% ATK。

### Wind Wheel: Break

- 能力类型：Active；运行时槽 2。
- 解锁语义：Lv.11；Demo 固定按满级。
- 时序：5 秒首次施放，此后每 10 秒。
- 五级伤害档：480 / 510 / 540 / 570 / 600% ATK。
- 直线穿透与控制；请求击退 5 / 20，接近地图边界时截断。

### Wind Wheel: Dance

- 能力类型：Active；运行时槽 3。
- 解锁语义：Lv.41；Demo 固定按满级。
- 时序：10 秒首次施放，此后每 10 秒，与槽 2 错峰。
- 四级总伤害档：400 / 440 / 480 / 520% ATK。
- 满级运行时拆为两段 260% + 260%，总计 520%；按伤害的 140% 回复生命，命中施加 Taunt，技能期间拥有 Super Armor。

### Star Rage

- 能力类型：Domain；解锁语义 Lv.61。
- 敌方主动技能触发时有概率获得 Imaginary Mass。
- 基础上限 30 层；每层提供最终减伤；10 / 20 / 30 层令大招达到 2x / 3x / 4x。
- 大招后最多 20 层转换为最大生命。
- 九级档案依次记录 40 / 50 / 60 / 70 / 80% 触发率、每次 2 层、每层 3% / 4% 最大生命转换和 99% 最终触发率。

### Singularity Awakening

- 第一段：造成伤害 +35%，伤害减免 +35%；堆叠上限可扩展至 50，50 层大招达到 6x；高层数进入 Boss 型状态。
- 第二段：死亡时自动引爆一次最高可用倍率的大招，至少按 6x 结算；随后恢复 99% 生命并获得 20 层 Imaginary Mass。
- 死亡触发与复活每场战斗只允许一次。

## 当前测试部署

```text
玩家：Catherine Yuki / Gold Ranger / Ember Striker
敌方：Cyan Warden / Azure Vanguard / Violet Arcanist / Gold Ranger / Verdant Medic
```

Formation 仍保存五个不重复角色槽位；当前 3v5 是独立的战斗测试名单。敌方测试实例使用 10x HP 和 0.1x ATK，角色资产本身不被改写。

## 内容生产状态

- 七名角色均有唯一卡面、CharacterDefinition、CharacterContentProfile 与三项运行时技能引用。
- Catherine 的 Domain 与 Awakening 已进入档案和专项运行时规则；其余普通角色的元素共鸣与觉醒阶段为未来挂点。
- 正式升级、升星、碎片、装备、羁绊和技能点尚未实现，不能从静态档案推断为已上线功能。
