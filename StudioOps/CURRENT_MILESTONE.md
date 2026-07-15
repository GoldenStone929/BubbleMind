# 当前里程碑：腾讯混元 3D 模型候选评估

> 状态：评估已完成；正式接入待授权
> 开始日期：2026-07-15
> 长期范围权威：`../PROJECT_PLAN.md`

## 目标

- 按用户要求，实际使用腾讯官方混元 3D 为 `ART-CHAR-UR-COSMIC-SLIME-001` 生成可比较的几何候选。
- 在不安装本地大模型、不产生费用、不暴露凭据的前提下，验证单视图与多视图结果、面数、体积、方向和低模可行性。
- 明确许可证、分发和 Unity 接入边界，避免候选未经审查进入公开仓库或正式游戏资产。

## 已完成

- 使用腾讯官方账号发布、托管于 Hugging Face 的 `tencent/Hunyuan3D-2` Space 完成单视图无纹理候选。
- 使用腾讯官方账号发布、托管于 Hugging Face 的 `tencent/Hunyuan3D-2mv` Space 完成前视图模型候选和 front/left/back 三视图候选。
- 三组作业均固定 seed `929`，请求、事件 ID、输入/输出 SHA-256 和原始 GLB 全部记录在 `_ProjectTools/Hunyuan3D/jobs/`。
- Blender 5.1 完成三组归一化、front/side/back/three-quarter 渲染和非空体积审计。
- 单视图原始网格从 442,980 triangles 生成 9,000 triangles 的自动减面预览，面数、接地和非空体积检查通过；它不是生产重拓扑。
- 核对 Hunyuan3D-2.1 官方显存需求、本机硬件限制、社区许可证地域限制，以及腾讯云 HY-3D 3.1 的正式候选路线。

## 结果

| 候选 | 输入 | Blender 结果 | 结论 |
|---|---|---|---|
| Hunyuan3D-2 单视图 | 登记正面 PNG | 服务端 737,060 faces；Blender 442,980 triangles；归一化约 `3.000 × 2.950 × 2.177 m` | 三组中最适合作为主体形体 A/B 基线；需要重拓扑与结构重建 |
| Hunyuan3D-2mv 前视图 | 同一正面 PNG | 474,768 triangles；归一化约 `3.000 × 2.994 × 2.128 m` | 漂浮碎片更多，未改善中央黑洞表达 |
| Hunyuan3D-2mv 三视图 | front/left/back | 489,312 triangles；归一化约 `3.000 × 2.860 × 2.212 m` | 软体体积和角方向可读，但轨道/液滴碎片仍多 |
| 单视图低模预览 | 单视图原始 GLB | 9,000 triangles；减少 97.97%；约 `3.001 × 2.949 × 2.178 m` | 可供 Blender 结构讨论，不能直接替换运行时 FBX |

## 视觉与接入判断

- 优点：主体穹顶、底部凝胶裙边、角和整体有机体积比当前程序化主体更自然。
- 缺点：无纹理几何生成无法可靠理解纯黑事件视界；中央黑洞被解释为凹陷或表面细节。轨道环、液滴和碎片会粘连、断裂或悬浮。
- 接入策略：未来只把混元候选当作 `SlimeBody` 的形体起点；继续使用项目已有的黑洞事件视界、吸积盘、轨道、Socket、Slime Toon Shader、VFX 和四个 Blend Shapes。
- 当前权威资产保持 `Assets/_Game/Art/Generated/UR_CosmicSlime/Runtime/UR_CosmicSlime.fbx`，Prefab 与战斗运行时均未更改。

## 权利与隔离

- 本次只调用腾讯官方账号发布、托管于 Hugging Face 的 Spaces；没有账号登录、API Key、SDK、模型权重、Python 包、付费或 credits 消费。
- 实际候选适用 Hunyuan 3D 2.0 社区许可证；仅研究本地部署门槛的 Hunyuan3D-2.1 适用独立的 2.1 社区许可证。两版均说明腾讯不主张用户生成 Output 的权利，但许可仍将 Output/结果纳入使用限制，并将授权地域排除欧盟、英国和韩国。当前候选因此只作内部评估，不进入 Git、Unity `Assets/`、Build 或全球发行包。
- `_ProjectTools/Hunyuan3D/` 被项目 `.gitignore` 忽略；远程仓库只保存输入/输出哈希、参数、质量结论与许可证边界。
- 腾讯云 HY-3D 3.1 的实际账户服务条款尚未取得，不能用开源许可证或其他地区条款代替。

## 验证证据

| 门槛 | 状态 | 证据 |
|---|---|---|
| 三组官方生成 | 通过 | 单视图、前视图模型和三视图作业均完成并下载 GLB；输出哈希见归档里程碑 |
| Blender 网格审计 | 通过 | 三组 `blender_candidate_audit.json` 均确认非空网格与体积，并输出四视图渲染 |
| 低模预览 | 通过 | `retopology_preview_audit.json`：9,000 triangles、grounded、nonzero volume |
| 隔离 | 通过 | 客户端、请求、GLB、渲染与预览均位于 `_ProjectTools/Hunyuan3D/` 并命中 `.gitignore` |
| Unity 回归 | 不适用 | 没有修改运行时、资产或包；沿用阶段 6.7 已通过的 Generate、PlayMode 与 Windows Build 基线 |
| 参考游戏 | 保持只读 | 进程保持开启；没有关闭、重启、登录、购买、抽卡、养成、改队或进入战斗 |

## 下一授权门槛

正式尝试腾讯云 HY-3D 3.1 前，需要用户明确确认：

1. 腾讯云国际站账户已开通 Tencent HY，并接受账户中实际显示的服务条款。
2. 通过安全环境变量提供最小权限的 SecretId/SecretKey 或 TokenHub Key；不得发到聊天或写入项目。
3. 确认项目对输入参考图拥有足够使用权。
4. 授权首轮最多约 85–95 credits，用于 3.1 参考图生成、PBR 和智能拓扑。
5. 确认计划发行地域，并审查云端输出的全球商业分发权。

在上述门槛完成前，本里程碑停在“评估已完成；正式接入待授权”，不会创建付费云任务或替换 Unity 资产。
