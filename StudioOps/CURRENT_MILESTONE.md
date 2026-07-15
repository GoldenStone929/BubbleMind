# 当前里程碑：20 格 3v5 职业测试与刺客后排切入

> 状态：已完成（Generate/核心验证、PlayMode、Windows D3D11 与实机视觉检查均通过）
> 开始日期：2026-07-14
> 完成日期：2026-07-14
> 长期范围权威：`../PROJECT_PLAN.md`

## 本轮用户目标

- 把战斗主轴统一划分为 20 格，避免使用缺少比例参照的 1 / 5 射程。
- Tank / Assassin 使用 2 格攻击距离，其余职业使用 10 格攻击距离。
- Catherine 的 `Wind Wheel: Break` 将目标击退 5 格。
- 保留五槽编队，同时把当前测试战斗改为我方 3 人对敌方 5 人。
- 我方固定包含射手、Catherine UR 坦克和刺客；刺客技能 2 必须切入敌方后排并开始攻击。

## 已锁定战斗契约

### 战场与射程

- 战场逻辑长度为 `20`，左右边界为 `-10 / +10`；所有出生、普通移动、击退与瞬移均限制在边界内。
- Tank / Assassin 射程为 `2 / 20`，Support / Ranged / Mage 射程为 `10 / 20`。
- 单位在最大射程边界停位；当前目标存活期间持续锁定，目标死亡后才重新选择最近目标。
- Catherine 的技能槽 2 `Wind Wheel: Break` 请求沿受击者敌方方向击退 `5 / 20`；中心区域实际移动 5 格，接近地图边缘时由战场边界截断。

### 五槽存档与 3v5 部署

- `PlayerState` 的五槽编队、存档 schema 和 Formation UI 保持不变。
- 本轮战斗部署与五槽存档独立，避免为了测试 3 人职业组合破坏既有五槽功能。
- 我方固定顺序：`Catherine Yuki / Tank`、`Gold Ranger / Ranged`、`Ember Striker / Assassin`。
- 敌方固定五人：`Cyan Warden / Tank`、`Azure Vanguard / Tank`、`Violet Arcanist / Mage`、`Gold Ranger / Ranged`、`Verdant Medic / Support`。
- 敌方运行时实例继续使用 `10× HP / 0.1× ATK`，角色数据资产原值不变。

### 刺客后排切入

- Ember Striker 的技能槽 2 为 `Backline Shift`，按既有技能日程首次在第 5 秒释放。
- 技能优先选择仍存活的 Ranged / Mage / Support 后排；按敌方基地深度、横向接近度和槽位确定性排序。
- 刺客优先落在目标朝敌方基地一侧；若目标已经贴近边界，则沿 Z 轴选择有空间的一侧，始终保持 2 格分离且不越界。瞬移产生独立 `UnitTeleported` 事件，并以 110% ATK 命中。
- 瞬移后持续锁定该后排目标并开始普攻；第 15 秒再次施放仍沿用存活目标，只有目标死亡后才重新选敌。

## UI 与视觉

- 首页明确显示五槽阵容已保存、3v5 职业测试已就绪。
- Formation 保留五个槽位，主按钮改为 `START 3v5 TEST`，并说明实际部署 Catherine、Gold Ranger、Ember Striker。
- 战斗镜头完整容纳 20 格主轴与八名单位；地图边缘装饰同步外移，避免遮挡出生位。
- 真实 Windows 窗口检查确认首页、五槽编队、战斗和结算均无裁切；黑洞聚怪阶段因单位真实重叠会短暂出现姓名牌密集，列为后续碰撞/标签分离打磨项。

## 边界

- 只写入 `GenericGachaRPG/`；`analysis/` 保持只读，未读取、运行或解包任何 XAPK/APK。
- 本轮没有下载、购买或引入新软件、Unity 包、Shader、VFX 或第三方运行依赖。
- 当前部署名单是测试夹具，不是最终自由选人规则；自由三人部署、碰撞分离、分道/NavMesh 和标签避让留待后续里程碑。

## 完成状态

- [x] 20 格战场常量、出生位缩放与全部位移边界限制。
- [x] Tank / Assassin 射程 2，其余职业射程 10，并回写七名角色资产。
- [x] Catherine `Wind Wheel: Break` 击退 5 格。
- [x] 五槽编队保留，运行时战斗改为固定 3v5。
- [x] Ember 技能 2 后排选择、瞬移、命中与持续锁定。
- [x] 数据库、生成器、表现层、UI、核心验证与 PlayMode 冒烟同步更新。
- [x] Windows D3D11 构建及真实窗口首页、Formation、战斗、结算检查。

## 验证证据

| 门槛 | 结果 | 证据与说明 |
|---|---|---|
| Generate / C# / 核心验证 | 通过 | `Artifacts/ThreeVsFiveRange/GenerateFinal.log` 出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]`；验证 20 / 2 / 10 / 5 比例、3v5 名单、八单位、确定性事件、5 / 15 秒同目标切入与边界落点 |
| PlayMode 冒烟 | 通过 | `Artifacts/ThreeVsFiveRange/PlaySmokeFinal.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；真实 UI 流程、八个怒气条、两次同目标瞬移、击退实际位移与 19.3 秒结算通过 |
| Windows D3D11 BuildPipeline | 通过 | `Artifacts/ThreeVsFiveRange/WindowsBuildFinal.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；输出 `114,042,024` bytes / `58.6s` |
| Windows 主程序 | 通过 | `Builds/Windows/GenericGachaRPGDemo.exe` 为 `667,648` bytes，SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| 实机视觉 | 通过 | 1922×1112 捕获窗口检查：首页和五槽 Formation 文本完整；20 格战场无边缘裁切；3v5 胜利结算为 19.3 秒；程序最终留在首页 |

完整记录：`MILESTONES/2026-07-14_THREE_VS_FIVE_RANGE_GRID_AND_ASSASSIN_SHIFT.md`。
