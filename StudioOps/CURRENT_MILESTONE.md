# 当前里程碑：怒气、三技能轮转与 Catherine 真实凝胶升级

> 状态：已完成（Generate/核心验证、PlayMode、Windows D3D11 与实机视觉检查均通过）
> 开始日期：2026-07-14
> 完成日期：2026-07-14
> 角色资产 ID：`ART-CHAR-UR-COSMIC-SLIME-001`
> 工作名：Catherine Yuki / 黑洞史莱姆
> 长期范围权威：`../PROJECT_PLAN.md`

## 本轮用户目标

- 建立统一怒气系统：上限 1000、初始 0，普攻命中和承受伤害都能积累怒气。
- 把技能槽 1 固定为大招；技能槽 2 与 3 使用互相错开的自动释放节奏。
- 让 Tank / Assassin 保持近战射程，其余职业在远程射程边界停位，不再无意义贴近。
- Catherine 继续担任限定 UR 坦克；大招必须化为黑洞，吸附全部存活敌人后坍缩击飞。
- 改善模型与灯光，使 Catherine 呈现更真实的深色凝胶厚度、纯黑核心和体积层次。
- 保留敌方测试单位 `10× HP / 0.1× ATK`，让技能循环有足够观察时间。

## 战斗契约

### 怒气

- 所有普通单位以 `0 / 1000` 怒气开始战斗。
- 普攻实际命中敌人时，攻击者获得 100 怒气。
- 单位受到实际伤害时，受击者获得 50 怒气。
- 技能槽 1 为大招；只有满 1000 怒气才可释放，释放后怒气清零。
- 世界条显示 `RAGE current/1000`，表现层只读取模拟层事件。

### 三技能错峰轮转

- 技能槽 2 首次在战斗 5 秒释放，此后每 10 秒一次：5 / 15 / 25 秒……
- 技能槽 3 首次在战斗 10 秒释放，此后每 10 秒一次：10 / 20 / 30 秒……
- 两项定时技能不得在同一 Tick 同时释放，也不得用同 Tick 追赶错过的窗口。
- 单位受控或暂时不满足施放条件时保留当前到期技能；只有实际施放成功后才推进该槽位的下一个 10 秒周期。
- 本轮确定性验证战斗在 19.3 秒结束，因此实际观察到技能槽 2 在 5 / 15 秒、技能槽 3 在 10 秒释放；20 秒后的周期没有被这场战斗执行。
- Catherine 的槽位映射为：技能槽 1 `Imaginary Mass: Infinite Void`，技能槽 2 `Wind Wheel: Break`，技能槽 3 `Wind Wheel: Dance`；`Star Rage` 保持被动领域规则。

### 射程与移动

- Tank / Assassin 的攻击距离为 1。
- Support / Ranged / Mage 的攻击距离为 5。
- 单位只移动到自身最大攻击距离边界，在射程内不继续贴近。
- 当前目标存活期间持续锁定；目标死亡后，才按当前战场位置选择最近的新目标。
- 敌方测试实例继续使用 `MaxHealth ×10` 与 `Attack ×0.1`，不修改角色资产原始数值。

## Catherine 视觉升级

- Blender 主体升级为更宽、更低、更不对称的 19,000 面凝胶轮廓，保留 6 个稳定材质槽与 `IdleBreath / Squash / Stretch / UltimateCollapse` 四个 Blend Shapes。
- 外壳使用近黑高密度凝胶语言：厚部更深、边缘受控发亮、正面保持实体感，避免玻璃球观感。
- 胸腹核心为真正纯黑事件视界；白紫吸积盘、光子透镜与多层螺旋提供清晰体积深度。
- 场景使用主光、补光和轮廓光三点布光，强化外壳体积、透明边缘与深色轮廓分离。
- 大招 VFX 保留蓄力、化为黑洞、全体吸附、多段伤害、坍缩和击飞阶段；黑洞吸附已通过实机截图确认。

## 边界

- 只写入 `GenericGachaRPG/`；`analysis/` 只读，不读取、运行或解包 XAPK/APK。
- 本轮没有下载或购买新依赖、Unity 包、Shader 或 VFX 资产。
- 付费 VFX/Toon 候选继续只作参考，未经费用和许可确认不得接入。
- 当前仍是内部原型；模型相交、正式 Animator、碰撞分离与收藏页 3D 预览留待后续里程碑。

## 完成状态

- [x] 0–1000 怒气规则、普攻/受伤增怒与满怒大招清零。
- [x] 三技能槽数据与 Catherine 槽位映射。
- [x] 技能槽 2 / 3 的 5 秒错峰、10 秒周期与同 Tick 互斥。
- [x] Tank / Assassin 射程 1、其余职业射程 5 与最大射程停位。
- [x] 敌方 `10× HP / 0.1× ATK` 测试倍率回归。
- [x] Catherine 19,000 面真实凝胶模型、纯黑核心、材质与三点布光。
- [x] 嘲讽按 4 秒到期并重选目标，取消的大招阶段可正常触发败北结算，UR 外壳恢复阴影与深度通道。
- [x] Generate/核心验证、PlayMode 冒烟与 Windows D3D11 构建。
- [x] 1920×1080 与 3440×1392 级宽屏实机视觉检查，以及黑洞吸附截图确认。

## 验证证据

| 门槛 | 结果 | 证据与说明 |
|---|---|---|
| Blender 几何与 Shape Keys | 通过 | `Artifacts/UR_CosmicSlime/Blender/geometry_audit.json`：19,000 / 20,000 triangles、6 个材质、四个必需 Shape Keys 与全部视觉审计项通过 |
| Generate / C# 编译 / 核心验证 | 通过 | `Artifacts/CatherineRealism/RageGenerate4.log` 出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| PlayMode 冒烟 | 通过 | `Artifacts/CatherineRealism/RagePlaySmoke.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；覆盖 UI、怒气条、技能轮转、射程与 Catherine 战斗契约 |
| Windows D3D11 BuildPipeline | 通过 | `Artifacts/CatherineRealism/RageWindowsBuild.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；输出 114,033,256 bytes / 24.1s |
| Windows 实机视觉 | 通过 | 1920×1080 与 3440×1392 级宽屏窗口完成画面检查；凝胶厚度、纯黑核心、三点布光和 UI 可读，黑洞吸附由截图确认 |

完整记录：`MILESTONES/2026-07-14_RAGE_THREE_SKILL_AND_CATHERINE_REALISM.md`。
