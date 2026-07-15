# 2026-07-15 腾讯混元 3D 模型候选评估

> 状态：候选评估完成；正式资产未替换
> 资产：`ART-CHAR-UR-COSMIC-SLIME-001` / `ART-CANDIDATE-UR-COSMIC-SLIME-HY3D-001`
> 用户指令：尝试使用腾讯混元 3D 生成角色模型

## 1. 执行摘要

本轮实际调用腾讯官方账号发布、托管于 Hugging Face 的 Hunyuan3D Spaces，为 Catherine 的已登记项目参考图生成三组无纹理几何候选。三组均能生成有效 3D 体积；单视图结果的有机史莱姆轮廓最好，并成功生成 9,000 面自动减面预览。但混元几何阶段不能可靠表达腹部纯黑事件视界，轨道和液滴也产生碎片，因此没有替换现有 Unity 模型。

本轮没有安装依赖、下载模型权重、使用账户/API Key、产生费用或修改 Unity 运行资产。全部外部输入副本、请求、GLB、渲染和低模预览都在 `_ProjectTools/Hunyuan3D/`，被 Git 忽略。

## 2. 官方路线核对

| 路线 | 2026-07-15 结论 | 项目决定 |
|---|---|---|
| [Hunyuan3D-2.1 开源仓库](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1) | 官方说明约需 10GB 显存生成形状、21GB 生成纹理、29GB 完整流程；纹理管线还需编译扩展 | 当前 RTX 4060 8GB、16GB RAM 和有限磁盘空间不适合完整本地部署，不安装 |
| [Hunyuan3D-2 官方 Space](https://huggingface.co/spaces/tencent/Hunyuan3D-2) | 可在无账号界面生成几何 | 用于单视图内部候选 |
| [Hunyuan3D-2mv 官方 Space](https://huggingface.co/spaces/tencent/Hunyuan3D-2mv) | 支持多视图几何候选 | 用于前视图模型与 front/left/back 三视图比较 |
| [腾讯云 HY-3D 3.1 国际站](https://intl.cloud.tencent.com/document/product/1284/75540) | 支持参考图、PBR、自定义面数与后续智能拓扑；需要账户、条款、凭据和 credits | 作为下一条正式候选路线，等待用户授权 |

## 3. 输入登记

权威正面输入：

```text
Assets/_Game/Art/Source/Characters/UR_CosmicSlime/BlackHoleSlime_TripoInput_Front.png
1024 x 1024
SHA-256 FC50F8D8E40F53925C7715F9B70E6452E4900420D9D4AF0BBDA7CE5736333437
```

三视图作业额外使用原始登记图的确定性裁剪：

| 视角 | SHA-256 | 隔离路径 |
|---|---|---|
| left | `ED61BDA049681769808E8424945FE0058B4444EAE6B08CDB381414E3BD714223` | `_ProjectTools/Hunyuan3D/inputs/CatherineMultiView/left.png` |
| back | `F9401563DD374664356C31AB096ADDA2B706ADAA8B56483E76642F3CB627FABD` | `_ProjectTools/Hunyuan3D/inputs/CatherineMultiView/back.png` |

裁剪来源为 `BlackHoleSlime_Reference.png`，源图 SHA-256 `BB6CAF3698C4DFADFA20B21EF470ECBB21D385519D0CE226C422CB034736AF6E`。裁剪坐标和哈希保存在忽略的 `crop_manifest.json`。

## 4. 生成作业

### 4.1 Hunyuan3D-2 单视图

```text
Space       tencent/Hunyuan3D-2
Model       tencent/Hunyuan3D-2/hunyuan3d-dit-v2-0
Event       5c9e08b0fc7948aab90fa05c1ffd937e
Parameters  30 steps, guidance 5, seed 929, octree 256, chunks 8000
Mode        remove background, geometry only, no random seed
Output      11,503,884 bytes
SHA-256     58BCB18098171A2BD5C9A24F37C1E5968F72F1179062EC9F061B82F1B68400FA
```

服务端显示 737,060 faces；Blender 审计为 442,980 triangles、221,492 vertices，归一化约 `3.000 × 2.950 × 2.177 m`。两者统计口径不同，因此以 Blender 导入后的三角数作为游戏预算依据。

### 4.2 Hunyuan3D-2mv 前视图模型

```text
Space       tencent/Hunyuan3D-2mv
Event       a7fefbe720d949339e1eebe25e68143e
Parameters  5 steps, guidance 5, seed 929, octree 256, chunks 8000
Input       front only
Output      11,911,364 bytes
SHA-256     A5C8C1E40205DB51DF489C6A27B9E0FCF57EBDB5585E6CBB62694F58D7A3E6AE
```

Blender 审计：474,768 triangles、237,506 vertices；归一化约 `3.000 × 2.994 × 2.128 m`。

### 4.3 Hunyuan3D-2mv 三视图

```text
Space       tencent/Hunyuan3D-2mv
Event       c9ef90ebd70748d6afec5d0501135f5d
Parameters  5 steps, guidance 5, seed 929, octree 256, chunks 8000
Input       front + left + back
Output      12,540,100 bytes
SHA-256     E21805D34B1C7CA5C1C1FB2F24E03D8B985D1B049712928B626FE1905FC3B536
```

Blender 审计：489,312 triangles、244,779 vertices；归一化约 `3.000 × 2.860 × 2.212 m`。

## 5. Blender 低模预览

单视图候选以 `-45°` 方向修正后生成非破坏性的自动减面比较预览：

```text
Raw triangles      442,980
Preview triangles    9,000
Reduction             97.97%
Dimensions          3.001 x 2.949 x 2.178 m
Output SHA-256      F305B7A77CE04C1B58DDA91CE772D25B6CC9E02EBAA8736195F60B60629594BA
Checks              within budget, grounded, nonzero volume
```

这只是形体预览，不是生产重拓扑。它没有游戏所需的材质分区、黑洞层级、Socket、Blend Shapes、碰撞和 UV 契约，不能直接进入 Unity。

## 6. 质量结论

### 可保留方向

- 更自然的穹顶主体与轻微不对称。
- 底部压扁、外摊的凝胶裙边。
- 角与软体轮廓的连续过渡。
- 作为 Blender `SlimeBody` 重建参考，优于继续从球体局部修形。

### 必须重建

- 腹部事件视界：几何模型把纯黑区域理解为凹陷或表面纹理，必须继续使用项目自有 `SingularityCore`、吸积盘和光子透镜结构。
- 轨道：生成结果断裂、粘连或悬浮，必须使用现有独立轨道网格。
- 液滴/碎片：需要删除或重新布置，避免高面数小连通件。
- 游戏契约：需要重拓扑、UV/材质分区、四个 Blend Shapes、Pivot、Socket、碰撞、Prefab 和性能验证。

## 7. 许可证与分发决定

- 实际候选适用 [Tencent Hunyuan 3D 2.0 Community License](https://github.com/Tencent-Hunyuan/Hunyuan3D-2/blob/main/LICENSE)；只用于研究本地部署门槛的 Hunyuan3D-2.1 适用独立的 [2.1 Community License](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1/blob/main/LICENSE)。两版许可证均说明腾讯不主张用户生成 Output 的权利，但仍将 Output/结果纳入使用限制，授权地域排除欧盟、英国和韩国；产品超过 100 万月活需另行申请，托管或分发还有协议、NOTICE 和供应商披露要求。
- BubbleMind 远程仓库是可分发代码/资产载体，因此本轮不提交原始或低模 GLB、渲染图、输入副本或请求响应。
- 当前候选不得标记为可全球商用、可发布或正式游戏资产。
- 腾讯云 HY-3D 3.1 的账户服务条款必须以用户账户中实际接受的版本为准；不能根据开源许可证推断云端输出权，也不能用中国区条款替代国际站条款。

## 8. 隔离与未改动项

- `_ProjectTools/Hunyuan3D/` 命中项目 `.gitignore`。
- 没有新增 Unity Package、SDK、Python 包、模型权重或系统级工具。
- 没有修改 `UR_CosmicSlime.blend`、`UR_CosmicSlime.fbx`、`PF_UR_CosmicSlime.prefab`、Shader、VFX、场景、C# 或 Build。
- 没有读取、运行、解包或提交工作区 XAPK/APK；`analysis/` 未修改。
- 参考游戏保持开启且只读，没有关闭、重启或执行账号状态变化操作。

## 9. 正式接入门槛

1. 用户开通腾讯云国际站 Tencent HY，并接受账户内实际条款。
2. 使用最小权限 SecretId/SecretKey 或 TokenHub Key，通过安全环境变量注入；不写项目、不发聊天、不入日志。
3. 用户确认参考图使用权与计划发行地域。
4. 用户授权首轮最多约 85–95 credits。
5. 先生成 HY-3D 3.1 + PBR 候选，再调用智能拓扑；结果在 24 小时下载期内归档到 `_ProjectTools/Hunyuan3D/`。
6. Blender 只取合格主体，重建黑洞、轨道和软体形变；通过现有几何审计、Unity Generate、PlayMode、Windows Build 与真实窗口检查后，才允许替换权威 FBX。

## 10. 完成确认

- [x] 腾讯官方单视图与多视图候选实际生成
- [x] 输入、参数、事件 ID 与输出哈希可追溯
- [x] Blender 四视图和网格审计完成
- [x] 9,000 面低模预览完成
- [x] 许可证与分发边界写入计划、角色规格、第三方清单、资产台账和验证报告
- [x] 候选保持 Git / Unity / Build 隔离
- [ ] 腾讯云 HY-3D 3.1 正式候选授权
- [ ] 正式 Blender 重拓扑与 Unity 接入
