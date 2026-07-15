# 2026-07-14：20 格 3v5 职业测试与刺客后排切入

## 摘要

本轮把此前缺少统一比例参照的近战 1 / 远程 5 射程改为 20 格战场契约：Tank / Assassin 为 2 格，其余职业为 10 格，Catherine 的 `Wind Wheel: Break` 击退 5 格。五槽收藏与编队继续保留，但当前战斗部署改为我方固定坦克、射手、刺客三人对敌方五人，便于集中验证职业差异。

Ember Striker 获得专属技能槽 2 `Backline Shift`：第 5 秒选择敌方存活后排、瞬移到 2 格射程内、造成一次 110% ATK 命中，并持续攻击同一目标直到其死亡。瞬移被记录为确定性模拟事件，表现层只负责重播。

## 实现明细

### 模拟与数据

- `BattleRules` 统一定义 20 格主轴、2 / 10 格职业射程、3 / 5 人测试部署和缩放后的出生坐标。
- `BattleTeam` 支持 1–5 人实际成员数；模拟层不再假定双方人数相同。
- `GameContentCatalog` 保存独立的玩家/敌人测试名单，生成器重复运行时会规范化顺序。
- 七名角色资产的 `AttackRange` 已同步为 2 或 10；Ember 的技能槽 2 指向新技能资产。
- Catherine 直线技能击退统一为 5 格；大招坍缩击飞保留原来的 1.3 格演出距离。

### 刺客技能

- 新增 `AssassinBattleKit`，集中保存角色 ID、技能 ID、2 格落点和 0.16 秒表现时长。
- 后排候选只包括 Ranged / Mage / Support；若仍有多个候选，按敌方基地方向的最深位置、Z 距离和槽位稳定排序。
- 瞬移位置经过战场边界限制；`UnitTeleported` 事件同时携带起点、终点与锁定目标。
- 后续普攻与第 15 秒再次施放必须继续命中该存活目标，避免因最近距离或技能重选跳回前排；只有目标死亡后才重新选择。
- 目标接近 X 边界时，落点会沿 Z 轴自动寻找空间，在矩形战场任一角仍保持精确 2 格分离。

### 表现与 UI

- Presenter 分别按 3 名玩家和 5 名敌人生成单位、标记、怒气条与位置回放，并响应瞬移事件。
- 固定镜头和地面尺寸按 20 格主轴校准，边缘装饰外移，保持八单位都在安全画面区。
- Formation 继续显示五槽保存阵容；按钮和提示明确区分“保存五人”和“本场部署三人”。

## 验证过程

1. 首轮 Generate 揭示旧测试强制要求在本场观察嘲讽自然到期；该规则与 19.3 秒固定战斗的实际节奏无关。保留所有实际嘲讽锁定验证，仅移除“本场必须自然到期”的脆弱观察条件。
2. 第二轮 Generate 揭示旧测试强制坦克在本场击杀后观察第二目标；3v5 高血量测试不保证该事件发生。保留“活目标期间不得换锁”和首次接战位置检查，仅移除非必然的第二目标要求。
3. 最终 Generate、PlayMode 和 Windows Build 均留下成功标记；失败日志保留为调试证据，没有覆盖或伪装。

## 最终证据

| 验证 | 结果 |
|---|---|
| Generate / 核心验证 | `Artifacts/ThreeVsFiveRange/GenerateFinal.log`：`[GenericGachaRPG][P0_VERIFY_PASS_20260713]`；包含第二次同目标切入、边界瞬移和击退真实落点检查 |
| PlayMode UI 冒烟 | `Artifacts/ThreeVsFiveRange/PlaySmokeFinal.log`：`[P0_PLAY_SMOKE_PASS_20260713]` |
| Windows D3D11 构建 | `Artifacts/ThreeVsFiveRange/WindowsBuildFinal.log`：`[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`，`114,042,024` bytes / `58.6s` |
| 主程序哈希 | `Builds/Windows/GenericGachaRPGDemo.exe`：`667,648` bytes；SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| 实机检查 | 1922×1112 捕获窗口完成首页、五槽 Formation、完整 20 格战场、3v5 战斗与胜利结算检查；最终停留首页 |

## 未引入与后续项

- 本轮没有下载、购买或新增任何依赖。
- 未修改 `analysis/`，未读取、运行或解包 XAPK/APK。
- 多人近战和黑洞聚拢仍可能造成模型/姓名牌短暂相交；正式碰撞分离、分道/NavMesh 与世界标签避让留待后续。
- 当前三人名单是测试夹具；玩家从五槽阵容中自由选择三人部署尚未实现。
