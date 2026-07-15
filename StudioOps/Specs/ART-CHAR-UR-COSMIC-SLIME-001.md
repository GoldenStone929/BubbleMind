# Asset Spec：`ART-CHAR-UR-COSMIC-SLIME-001 星渊吞噬体`

> 状态：已通过内部原型验收
> 稀有度/定位：UR 限定坦克 / 非人形前线角色
> 英文工作名：Abyssal Singularity Slime
> 负责人：Codex
> 最后更新：2026-07-14

## 1. 用途与玩家视角

- 游戏内用途：首个正式 3D 角色样板；进入收藏、默认编队首槽和现有 5v5 射程战斗。
- 出现场景与典型屏幕占比：战斗中高度约占画面 12%–20%；收藏页允许近景旋转预览。
- 必须传达的信息：UR 稀有度、宇宙/黑洞主题、沉重引力感、非人形凝胶生命、危险但可读。
- 不在本资产范围内：完整限定池经济规则、商业发布权、整套角色阵容、付费皮肤、最终移动端优化。

## 2. 原创与权属

- 来源/创作方式：用户提供三视角概念 PNG；Blender 5.1 在本地程序化生成并清理全部网格；Unity 制作最终 Shader、VFX、Prefab 与运行时表现。Tripo 仅完成零费用余额检查，未上传或生成。
- 参考资料：`Assets/_Game/Art/Source/Characters/UR_CosmicSlime/BlackHoleSlime_Reference.png`。
- 原始参考哈希：SHA-256 `BB6CAF3698C4DFADFA20B21EF470ECBB21D385519D0CE226C422CB034736AF6E`；1122×1402；1,790,122 bytes。
- 参考权利：由用户提供；原始生成/创作工具与商业权属尚未确认，因此只用于内部原型。
- 生成式工具：本轮未使用 3D 生成服务；Tripo API 余额为 0，未创建任务；API Key 永不记录。
- 商用权利：Blender 程序化网格和 Unity 实现由本项目创建；但用户参考图的来源/商业权利仍待确认，因此整个样板继续限定为内部原型。
- `ASSET_LEDGER.csv` 条目：`ART-SOURCE-UR-COSMIC-SLIME-001` 与 `ART-CHAR-UR-COSMIC-SLIME-001`。

## 3. 视觉规格

- 轮廓：宽、低、圆润穹顶体；底部向外摊开的凝胶裙边；不对称双角；破碎轨道环形成横向识别线。
- 正面识别：白紫色斜眼、胸腹中央纯黑事件视界与白紫吸积盘、额部菱形符号。
- 背面识别：深色星空穹顶、紫色星云带与环绕碎片。
- 色彩：近黑深靛紫外壳、深紫星云、电光紫细节、纯黑事件视界、核心白紫 `#F4E9FF`、轨道暗金 `#9A7554`。
- 材质语言：主体外壳使用项目自有 Slime Toon Shader，呈近黑半透明厚胶、顶部/底部渐变、两段明暗、Fresnel 软边和克制果冻高光；内部星云、星点与核心使用独立层；事件视界保持真正黑色，吸积盘以白紫高对比包围；轨道为暗紫金属带与独立暗金边。
- 禁止：不复制任何现有 IP 的角色、标志、名称、招式或构图；不使用来源不明的模型/贴图。

## 4. 技术预算

- Blender 源：`Assets/_Game/Art/Generated/UR_CosmicSlime/Source/Blender/UR_CosmicSlime.blend`。
- Tripo 原始结果：无；余额为 0，未上传或生成。未来候选才使用 `_ProjectTools/Tripo/jobs/<task-id>/`。
- Unity FBX：`Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/UR_CosmicSlime.fbx`。
- Unity 材质/贴图：`Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/Materials/` 与 `Textures/`。
- 单位与轴：1 Unity unit = 1 m；Y-up；Pivot 位于主体底部中心；最新 Blender 几何审计尺寸约 `2.593 × 1.945 × 1.978 m`（XYZ），Unity Prefab 以 0.92 缩放并使用三分之四展示角。
- LOD0：主体、角、奇点、星云与轨道总计 ≤20,000 triangles；当前为 `11,468`。
- LOD1：≤8,000 triangles；首轮样板可延期，但在移动端发布前必须完成。
- 材质：当前 6 个（Shell、Nebula、Energy、BlackCore、Orbit、OrbitTrim）；Unity 将 FBX 的 Energy 槽重映射到 `MAT_UR_CosmicSlime_Core`，Shell 与 Nebula 使用 Slime Toon 的 AlphaTest/网点透明方案，其余结构保持独立材质。
- 贴图：最多 4 张 2048²；Windows 原型 2048，移动端导入覆盖 1024。
- 骨骼：不强制骨架；通过 `CharacterView` 的无 Animator 回退动作与 FBX Blend Shapes 共同驱动软体表现。
- Blend Shape：必须包含 `IdleBreath`、`Squash`、`Stretch`、`UltimateCollapse`；Unity 运行时验证名称并在移动、攻击、受击与大招阶段驱动。
- 碰撞：单 Capsule/Sphere Collider 近似主体；轨道与液滴无独立碰撞。
- 性能：战斗中透明层、粒子和过度绘制必须受控；关闭 VFX 时仍保持清晰轮廓。

## 5. 组件拆分

```text
CharacterRoot
├── ModelRoot
│   ├── SlimeBody
│   ├── NebulaInner / StarCloudPoints
│   ├── Horn_L / Horn_Center / Horn_RightFluid
│   ├── SingularityAccretionRig
│   │   ├── SingularityCore
│   │   ├── SingularityAccretion
│   │   └── AccretionSpiral_*
│   ├── OrbitRig_Lower
│   │   └── OrbitBand_Lower / OrbitTrim_Lower
│   └── OrbitRig_Upper
│       └── OrbitBand_Upper / OrbitTrim_Upper
├── RightHandSocket
├── LeftHandSocket
├── SkillVfxSocket
├── ProjectileSocket
├── GroundVfxSocket
├── TargetSocket
└── HealthBarSocket
```

- `SlimeBody/Horns` 首版全部由项目内 Blender 脚本生成。
- 黑洞奇点、内部星空与透明外壳主要在 Unity 中重建。
- 轨道环优先独立网格，允许由 Blender 程序化重建，不依赖 Tripo 生成质量。

## 6. Unity 导入契约

- Model Importer：scale 1；Read/Write 默认关闭；Generate Colliders 关闭；Normals 从文件导入；Tangents 自动。
- Rig：None；无 Animator 时必须正常工作。
- Prefab：`Assets/_Game/Prefabs/Characters/PF_UR_CosmicSlime.prefab`。
- 运行时连接：`CharacterDefinition.characterPrefab` 指向 Prefab；`DemoBattlePresenter` 优先实例化 Prefab，缺失时回退 `ProceduralCharacterBuilder`。
- 数据：新增稳定 ID `ur_cosmic_slime`；现有 Generator 必须纳入该定义，重复运行不能把它移除。
- UR 显示：全局稀有度为 `R -> SR -> SSR -> SP -> UR`；本角色显示 `UR / TANK / LIMITED`，进入默认编队但排除标准池。
- 战斗数据：近战攻击距离与移动速度由 `CharacterDefinition` 配置；进入射程后留在前线，锁定目标死亡后才重新接近最近敌人。

## 7. 满级战斗契约

- Demo 中角色始终按满级测试，不建立本轮升级界面。
- Skill 1：600% ATK 直线穿透、破防语义与击飞事件。
- Skill 2：200% + 200% 两段伤害，按实际伤害 140% 恢复生命，命中嘲讽，技能阶段具有霸体语义。
- Skill 3 / 领域：默认 30 层虚拟质量，每层提供 1.5% 最终减伤；本轮必须发出可验证的减益与层数事件。
- Ultimate：960% ATK 基础，30 层按 4×；蓄力后把所有存活敌人拉至黑洞位置，持续多段伤害，坍缩后全体击飞。
- 觉醒：死亡时自动大招且最低 6×，恢复 99% HP并获得 20 层，每战最多一次。
- 技能事件与位置变化必须由固定 Tick 模拟先行决定；VFX 只能重播，不得改变结果。

## 8. 验收与证据

- [x] 角色身份、参考图与工作范围锁定
- [x] 参考源复制到项目并有 `.meta`
- [x] Tripo 余额检查已记录；0 额度，未上传/未建任务/未消费，且无密钥落盘
- [x] Blender 源文件可打开并可重复导出
- [x] 网格、法线、材质槽、比例与 Pivot 合格；首版仅依赖程序化材质，不需要贴图 UV
- [x] Unity 6000.5.3f1 中 FBX 导入、Prefab 生成与 C# 核心验证成功
- [x] Prefab、Sockets 与 `CharacterView` 正确
- [x] 收藏/编队条目与战斗模型均可见
- [x] URP 材质、黑洞核心与轨道动画通过 1920×1080 / 1280×720 最终实机视觉验收
- [x] Blender 几何与视觉预算通过：11,468 / 20,000 triangles，6 / 6 materials
- [x] 纯黑体积事件视界、侧视吸积盘、下轨净空、近黑外壳与接地审计全部通过
- [x] Meta 文件与 Git LFS 状态正确
- [x] P0 Play 与 Windows Build 回归通过
- [x] 四个 Blend Shapes 在 FBX 和 Unity 导入后均存在，并由 Idle、三技能与大招阶段驱动
- [x] Slime Toon Shader 与黑洞阶段 VFX 通过 D3D11 编译和实机运行验证
- [x] 满级三技能、30 层大招、死亡觉醒、全体拉拽与击飞通过确定性验证
- [x] Generate/核心验证、PlayMode 冒烟与 Windows BuildPipeline 均留下 PASS 标记
- [x] 战斗 1.25 倍实时回放与 1280×720、1920×1080 视觉验收通过

证据目录：`Artifacts/UR_CosmicSlime/`、`Artifacts/CatherineRework/` 与 `StudioOps/MILESTONES/2026-07-14_CATHERINE_MAXED_UR_REWORK.md`

## 9. 已知限制与交接

- 正式中英文商品名、技能名称、限定池概率/期限/保底尚未锁定。
- 免费 Tripo 输出不得被描述为商业发布资产。
- API Key 已在聊天中暴露且存在用户级环境变量；完成本次生成后需轮换，并在用户单独授权后清理该变量。
- 当前没有 Tripo 候选；未来有额度时只做一次受控 A/B 测试，不覆盖本地 Blender 基线。
- 初次 Windows 构建曾出现 D3D11 保留字冲突，且通用实心技能球遮挡画面；最终构建已修复并保留前后日志供复盘。
- 当前仍是内部原型：多人近战会发生模型相交，正式 Animator、碰撞分离和收藏页 3D 近景预览留待后续里程碑。
