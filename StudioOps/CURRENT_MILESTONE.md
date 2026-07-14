# 当前里程碑：首个 UR 限定角色样板

> 状态：进行中  
> 开始日期：2026-07-14  
> 角色资产 ID：`ART-CHAR-UR-COSMIC-SLIME-001`  
> 工作名：星渊吞噬体 / Abyssal Singularity Slime  
> 规格：`Specs/ART-CHAR-UR-COSMIC-SLIME-001.md`  
> 长期范围权威：`../PROJECT_PLAN.md`

## 目标

把用户提供的紫色星空/黑洞史莱姆参考图制作成首个可在现有 3v3 战斗中显示、可试玩、可验证的 UR 限定角色样板。它用于建立正式 3D 角色管线，不代表全项目美术风格已经锁定。

## 用户最新授权

- 停止 Claude 路线，由 Codex 独立接管角色制作。
- 使用用户提供的 Tripo API 额度制作首个候选模型。
- 使用用户已经安装的 Blender 5.1 进行清理、优化和导出。
- 继续遵守项目隔离、下载预告和中文沟通规则；本项目源码仅获准推送到 `GoldenStone929/BubbleMind`，其他远端与构建发布仍禁止。

## 已批准的外部动作

仅限本角色：

1. 将项目内登记的参考 PNG 上传到 Tripo 官方 API。
2. 查询 API 钱包余额并消耗免费额度生成少量候选。
3. 将生成结果下载到项目内受控目录。

任何新软件、SDK、插件、模型服务、付费额度或额外第三方上传仍须先通知用户。API Key 不得写入项目、Markdown、Unity 资产、命令历史、日志或 Git；生成进程只从现有用户级 `MCPFORUNITY_TRIPO_API_KEY` 读取值，不输出值。该变量由此前流程写入用户配置，属于项目外状态；本轮不会更改或删除。由于 Key 已出现在聊天中，本次生成后必须建议轮换。

## 路径

```text
参考源：Assets/_Game/Art/Source/Characters/UR_CosmicSlime/
Tripo 原始输出：_ProjectTools/Tripo/jobs/<task-id>/
Blender 源文件：Assets/_Game/Art/Generated/UR_CosmicSlime/Source/Blender/
Unity 游戏资产：Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/
角色 Prefab：Assets/_Game/Prefabs/Characters/
验证证据：Artifacts/UR_CosmicSlime/
本地工具/缓存：_ProjectTools/
```

## 执行阶段

| 阶段 | 状态 | 完成门槛 |
|---|---|---|
| 1. 审计与交接 | 进行中 | Claude 日志、Git、Unity、参考图和安全边界均已核实；延期事项写入 Markdown |
| 2. 角色规格与来源登记 | 进行中 | Asset Spec、Art Bible、资产台账和第三方记录一致 |
| 3. Tripo 候选 | 跳过（余额 0） | 余额检查为 available 0 / frozen 0；未上传、未建任务、未消费 |
| 4. Blender 本地建模与清理 | 待开始 | 轮廓、比例、法线、拓扑、UV、材质槽、Pivot 与导出符合规格 |
| 5. Unity 集成 | 待开始 | URP 材质、Prefabs、Sockets、CharacterView 与战斗数据接通 |
| 6. 验证与交付 | 待开始 | 编译、场景显示、Play 冒烟、性能检查和 Windows Build 回归通过 |

## 美术方向

- 主体：半透明深紫色凝胶体，圆润但具有压迫感。
- 识别点：不对称双角、发光斜眼、胸腹部黑洞核心、破碎轨道环。
- 内部：紫黑星云、星点与吸积盘；这些主要由 Unity Shader/VFX 实现，而不是强行烘进网格。
- 战斗轮廓：宽、低、重心稳定；即使关闭透明与 VFX 仍能认出角色。
- 禁止复制任何现有 IP 的角色、符号或招牌表现。

## 技术策略

- 首版由 Blender 5.1 完全本地程序化生成主体、双角、黑洞核心、轨道环和液滴，并完成法线、比例与 FBX 导出。
- Tripo 官方余额为 0，本轮没有上传参考图、创建任务或产生费用；未来有额度时只作为 A/B 候选，不覆盖已验证模型。
- Unity 使用程序化 `CharacterView` 回退动作；首版不因缺少骨架而阻塞。
- 黑洞核心、星空、透明壳与轨道动画由 URP Shader、材质和轻量脚本完成。
- LOD0 总三角面目标不超过 20,000；材质不超过 3 个；单张贴图最大 2048，移动端可降至 1024。

## 权利与发布限制

- 参考图由用户提供，但原始创作工具与商用权属尚未确认，当前只作为内部原型输入。
- Tripo 免费层产物按当前官方条款不得作为商业发布资产；本样板标记为 `prototype_noncommercial`。
- 未来商业发布前必须取得明确权利，或用项目自有/合适付费许可重新制作。

## 本里程碑不包含

- 不安装 Blender MCP、ComfyUI、Meshy 或新的 Unity 包。
- 不制作整套角色阵容。
- 不实现十连、保底、碎片或完整养成。
- 不发布商业构建；除已批准的 `GoldenStone929/BubbleMind` 源码仓库外，不发布其他远端。
- 不清理 `StudioOps/DEFERRED_WORK.md` 中需要额外授权的项目外目录。

## 当前恢复点

如果任务中断，下一代理应先阅读本文件、角色 Asset Spec 与 `DEFERRED_WORK.md`，检查 Git/Unity 实际状态，再从表格中第一个未完成阶段继续；不得重新从空项目开始。
