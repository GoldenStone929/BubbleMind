# 当前里程碑：Catherine 满级技能与 UR 视觉重制

> 状态：已完成（自动化、D3D11 与双分辨率实机视觉均通过）
> 开始日期：2026-07-14
> 角色资产 ID：`ART-CHAR-UR-COSMIC-SLIME-001`
> 工作名：Catherine Yuki / 黑洞史莱姆
> 长期范围权威：`../PROJECT_PLAN.md`

## 用户验收反馈

- 当前 UR 模型仍然不合格：轮廓像占位模型，颜色、半透明厚度、果冻高光、内部层次和生命感不足。
- 角色在 Demo 中始终视为满级；不能只配置一个通用 AoE 技能。
- 大招必须让 Catherine 化为黑洞，把所有存活敌人吸到黑洞位置，持续伤害，坍缩后全体击飞。
- 敌方测试单位需要 10 倍生命、0.1 倍攻击，确保一场战斗能观察完整技能循环。
- 三类测试技能必须明确覆盖击飞、直接命中与减益。
- 史莱姆品质来自统一 Master Shader、内部渐变、软边/厚度、Squash & Stretch 与 VFX；模型、Shader、动画、VFX 必须作为一套完成。
- 初次 Windows 实机检查发现回放偏快、浅青色实心球遮挡，以及 D3D11 Shader 编译失败；这些问题均保留在里程碑复盘中。
- 最终版本将回放降至 1.25 倍，用透明能量环替换通用实心球，并修复两个 Shader 的 D3D11 保留字冲突；事件视界、吸积盘、坍缩和击飞已清楚可读。

## 满级 Catherine 契约

1. `Wind Wheel: Break`：600% ATK 直线穿透、破防语义和击飞事件。
2. `Wind Wheel: Dance`：总计 400% ATK（200% 首击 + 200% 冲锋）、按伤害 140% 治疗、命中嘲讽、技能期间霸体。
3. `Star Rage`：默认满级演示以 30 层虚拟质量开始；每层 1.5% 最终减伤，30 层封顶；本轮用减益/层数事件验证 UI 与表现。
4. `Imaginary Mass: Infinite Void`：满级基础 960% ATK，30 层按 4×倍率；短蓄力后将全部存活敌人拉到 Catherine 位置，多段伤害，坍缩爆炸并击飞。
5. 觉醒：20 层后开放 50 层上限与 6×上限；死亡时按当前层数自动释放且至少 6×，随后恢复 99% HP、获得 20 层，每战一次。
6. 所有技能和觉醒必须保持固定 Tick、同 Seed 可复现；表现层不得反向修改模拟结果。

## 视觉重制契约

- Blender 主体底部压扁、顶部不规则且左右不完全对称；禁止再用球体/胶囊作为最终主轮廓。
- FBX 至少包含 `IdleBreath`、`Squash`、`Stretch`、`UltimateCollapse` 四个 Blend Shapes，配合 CharacterView 的位移与技能阶段。
- Slime Master Shader 提供顶部/底部渐变、Toon 阴影、Fresnel 软边、果冻高光、内部星点/核心和可控半透明；正面不能像玻璃。
- 黑洞 VFX 至少覆盖蓄力、事件视界、吸积盘、引力波、拉拽轨迹、多段命中、坍缩闪光与击飞余波。
- 付费候选只作为品质参考；未经费用与许可确认，不下载、不购买、不接入。

## 测试战斗契约

- 仅在 Demo 敌方实例层应用 `MaxHealth ×10` 与 `Attack ×0.1`；ScriptableObject 原始数值、玩家角色、收藏和卡池不变。
- 默认战斗必须实际出现至少一次击飞、一次直接命中、一次减益、一次全体拉拽与一次大招坍缩击飞。
- 自动验证覆盖 Catherine 满级倍率、虚拟质量、死亡觉醒只触发一次、敌方倍率、拉拽目标数量、阶段顺序和确定性。

## 边界

- 只写入 `GenericGachaRPG/`；`analysis/` 只读，不读取、运行或解包 XAPK/APK。
- 不改变既有五槽、5v5、持续锁敌、射程与目标死亡后重选规则。
- 现有 Unity 6、URP 17.5 与 Blender 5.1 足以实施；不引入未批准付费资产或外部运行时依赖。

## 当前进度

- [x] 五元素基础史莱姆、柔和漫画地图与 Windows 可构建基线保留。
- [x] 用户满级技能、觉醒、敌方测试倍率与视觉标准已记录。
- [x] Catherine 确定性技能/大招/觉醒与测试敌方倍率已接入并由自动化事件断言覆盖。
- [x] UR Blender 轮廓、四个 Blend Shapes、FBX 与几何审计已生成；`11,468 / 20,000` triangles，6 个材质槽，审计全部通过。
- [x] Slime Toon Shader、黑洞 VFX、技能阶段 API、Prefab 与表现层调用已接入。
- [x] 修正后的 Generate/核心验证、PlayMode 冒烟与 Windows BuildPipeline 已完成并留下 PASS 标记。
- [x] 修复 D3D11 Shader 编译错误，以及实机浅青色大球遮挡/黑洞不可读问题。
- [x] 调整战斗实时回放节奏，并重新完成 1280×720、1920×1080 实机视觉验收。
- [x] 视觉门槛通过后完成最终文档状态、提交与推送。

## 当前验证状态

| 门槛 | 当前结果 | 证据与说明 |
|---|---|---|
| Blender 几何与 Shape Keys | 通过 | `Artifacts/UR_CosmicSlime/Blender/geometry_audit.json`：11,468 triangles、6 个材质、`IdleBreath / Squash / Stretch / UltimateCollapse` 均存在且位移阈值通过 |
| Generate / C# 编译 / 核心验证 | 通过 | `Artifacts/CatherineRework/GenerateCompile3.log` 出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]`，无 C# 编译错误或异常 |
| PlayMode 冒烟 | 通过 | `Artifacts/CatherineRework/PlaySmokeFinal.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；覆盖完整 UI 流程与 Catherine 战斗事件契约 |
| Windows BuildPipeline | 通过 | `Artifacts/CatherineRework/WindowsBuildFinal.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildPipeline 输出 113,729,368 bytes / 22.8s |
| URP Shader / D3D11 | 通过 | 最终 Windows 构建完整编译 `Slime Toon` 与 `Black Hole VFX`，日志无 `Shader error` |
| Windows 实机视觉 | 通过 | 1920×1080 与 1280×720 真实窗口均检查首页、战斗、事件视界、五目标吸附、坍缩击飞与结算；无浅青色遮挡球或 UI 越界 |

完整本轮记录：`MILESTONES/2026-07-14_CATHERINE_MAXED_UR_REWORK.md`。
