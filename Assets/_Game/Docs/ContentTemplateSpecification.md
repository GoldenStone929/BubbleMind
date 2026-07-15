# BubbleMind 角色内容模板规范

> 版本：1.0
> 日期：2026-07-15
> 范围：原创角色档案、战斗技能映射、养成占位、获取关系与内容校验

## 目标

该模板让一个新角色在不修改角色页、抽卡页、编队页或战斗状态类的前提下，拥有完整且可验证的内容档案。模板扩展现有运行时数据，不建立第二套角色身份。

```text
CharacterDefinition.Id                 唯一角色键
├─ 名称 / 稀有度 / 职业 / 卡面 / Prefab
├─ HP / ATK / DEF / 射程 / 移速 / 怒气
├─ Ultimate / Skill 2 / Skill 3        当前可执行战斗槽
└─ CharacterContentProfile             补充档案
   ├─ 元素 / 阵营 / 称号 / 关键词
   ├─ Basic / Active / Passive / Domain / Awakening
   ├─ 解锁等级 / 技能等级 / 结构化参数
   ├─ 养成阶段 / 获取来源 / 重复规则说明
   ├─ 协同与克制标签
   └─ 版本 / 审批 / 原创性 / 资产来源
```

## 单一数据源

- `CharacterDefinition` 继续拥有 `characterId`、战斗数值、三个运行时技能、卡面和 Prefab。
- `SkillDefinition` 继续拥有运行时目标、倍率、怒气成本和命中时机，并补充 `SkillTag` 与可选技能图标。
- `SkillDefinition.RageCost` 是大招实际消耗的唯一权威；`BattleUnitState` 只在成本为正、当前怒气足够且成本不超过角色 `MaxRage` 时允许释放，并按精确成本扣除，而不是无条件清空怒气。
- `CharacterContentProfile.ownerCharacterId` 是只读归属外键，用来阻止档案误挂；`CharacterDefinition.Id` 仍是唯一身份权威。Profile 不重复名称、稀有度、职业、基础属性、卡面或 Prefab。
- 抽卡返回 `rewardId` 后仍由 `GameDatabase.GetCharacter()` 解析；角色页、编队和战斗不会查询另一张角色表。
- `PlayerState` schema 保持 v3。档案只描述未来养成能力，本里程碑不新增升级、升星、材料消耗或付费命令。

## 能力记录

每个角色至少包含：

1. 一个 `Basic` 档案记录；
2. 一个严格映射 `CharacterDefinition.UltimateSkill` 的 `RuntimeSkillSlot.Ultimate`；
3. 一个严格映射 `Skill2` 的 `RuntimeSkillSlot.Skill2`；
4. 一个严格映射 `Skill3` 的 `RuntimeSkillSlot.Skill3`。

额外能力可使用 `Passive`、`Domain` 或 `Awakening`，并把 `RuntimeSkillSlot` 设为 `None`。未进入当前模拟的能力必须明确写为档案或未来挂点，不能让 UI 暗示它已经生效。

`SkillRankRecord` 必须从 Lv.1 开始逐级连续记录到能力的 `MaxLevel`，不能只写首级和末级。同一等级内的 `SkillValueRecord.Key` 必须唯一，数值必须有限。可用单位包括普通数值、百分比、攻击百分比、伤害转治疗、最大生命百分比、倍率、秒和层数。

## 角色级战斗参数

角色资产中的 `AttackRange`、`MaxRage`、`RagePerAttack` 和 `RageWhenHit` 现在会在战斗开始时被 `BattleUnitState` 快照。Demo 仍使用统一基线：

```text
Tank / Assassin: 2 / 20
Support / Ranged / Mage: 10 / 20
Max Rage: 1000
Basic hit gain: 100
Damage received gain: 50
```

这些值是生成器的默认内容，不再是运行时强制覆盖。未来角色可以在数据层拥有独立射程或怒气曲线，而不改战斗状态代码。

大招常用配置是 `RageCost = 1000` 与 `MaxRage = 1000`，因此释放后表现为归零；这只是当前内容配置，不是引擎限制。模板允许例如 `MaxRage = 777`、`RageCost = 700` 的合法组合，释放后保留 77 点怒气。验证器会拒绝非正成本、超过角色怒气上限的成本，以及档案末级伤害/Power 与运行时 `SkillDefinition` 不一致的配置。

## 生成器保留策略

- 当前七角色、十技能和演示卡池是最低必需集合，不是数据库的精确上限。
- 生成器按稳定 ID 替换同一必需定义，并保留数据库中额外角色、技能和卡池。
- 仅在新建 Profile 或其内容 schema 为空时写入默认档案；已有草稿、审查中或作者修改过的档案不会被模板覆盖。生成器只会修复该资产的 `ownerCharacterId` 归属外键。
- 连续生成必须保持权威数据库、角色 Profile、技能与场景的内容哈希稳定。

## 玩家页面投影

- `Combat` 只展示当前可执行的三个运行时技能槽与战斗参数。
- `Archive` 对完整能力档案分页，包含 Basic、Active、Passive、Domain 与 Awakening 的触发、目标、效果和逐级参数。
- `Growth` 对养成阶段与获取来源分页，展示门槛、上限、摘要、可用性和重复获取规则。
- 页面只投影权威资产，不显示内部 schema、审批字段或验证术语。

## Catherine 档案

`Profile_ur_cosmic_slime.asset` 是完整模板样例，包含：

- 基础攻击；
- 9 级 `Infinite Void` 大招；
- 5 级 `Wind Wheel: Break`；
- 4 级 `Wind Wheel: Dance`；
- 9 级 `Star Rage` 领域；
- 2 段觉醒与一次战斗内复活说明；
- 11 / 41 / 61 级解锁阶段；
- Limited Signal 与 Demo Test Grant 两种获取语义；
- 原创文本、独立数值与两项资产台账记录。

当前战斗继续按满级测试夹具执行；档案中的逐级表用于内容制作、角色页和未来养成系统，不改变现有确定性结果。

## 新角色接入清单

1. 创建唯一 `CharacterDefinition`，设置角色 ID、职业、稀有度、卡面、Prefab、数值和三个运行时技能。
2. 创建 `CharacterContentProfile`，填写版本、审批、元素、阵营、称号和关键词。
3. 记录 Basic 与三个运行时槽，并保证引用严格一致。
4. 按需加入被动、领域、觉醒、技能等级表和结构化参数。
5. 填写至少一个养成阶段、一个获取来源、协同/克制标签和资产来源记录。
6. 文本与数值必须原创；资产必须为原创、已登记生成或有明确许可证。
7. 将角色加入 `GameDatabase`；是否加入卡池必须由 Banner 显式配置，限定角色不能被自动放入标准池。
8. 运行 Generate/Core、PlayMode 冒烟和目标平台构建。

## 强制校验

`CharacterContentProfile.TryValidate()` 与 `DemoProjectVerifier` 会阻止以下内容进入试玩构建：

- 缺档案、共用同一档案、归属角色不匹配或档案未批准；
- 缺 Basic/三运行时能力，或运行时槽与角色定义不一致；
- 重复能力 ID、重复运行时槽、逐级表缺级/跳级、非有限参数或同级重复参数键；
- 大招成本非正、超过角色 `MaxRage`，或档案末级伤害/Power 与运行时技能倍率不一致；
- 缺养成阶段或获取来源；
- 缺原创文本、独立数值或资产来源证明；
- Catherine 的 `Star Rage` 或两段觉醒断链；
- 角色自定义射程/怒气在进入模拟时被覆盖。
- 生成器移除数据库额外角色/技能/卡池，或覆盖已有人工 Profile 内容。

## 后续扩展边界

- 真正的升级、升星、觉醒、碎片、装备和技能等级需要 `PlayerState` schema v4 及迁移测试。
- 复杂技能应逐步拆为 Target Rule、Timeline、Formula、Buff 和 Presentation Cue；现有 Catherine/Assassin BattleKit 仍是专项运行时实现。
- 正式在线抽卡、货币、奖励、PVP 和活动资格必须由后端权威服务处理。
- `StudioOps/KnowledgeBase/` 的参考观察只能向通用规律单向流动；运行时 Profile 不得引用 `REF-*`、`OBS-*`、外部专名或外部资产。
