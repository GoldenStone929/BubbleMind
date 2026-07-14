# BubbleMind 首个地图与试玩版

> 日期：2026-07-14
> 状态：已完成
> 角色：`ART-CHAR-UR-COSMIC-SLIME-001`
> 地图：`ART-ENV-BATTLE-ABYSSAL-OBSERVATORY-001`

## 成功更新

- 使用内置图像生成能力制作原创 16:9 星渊观测台概念图，并将其作为首页及 3v3 战斗的全画幅视觉基准。
- 将 Blender 5.1 本地生成的 4,632 三角面星渊吞噬体接入 Unity：Prefab、Sockets、`CharacterView`、URP 材质、黑洞核心与轨道动画均已生效。
- 数据库扩展到 7 名角色；`ur_cosmic_slime` 为 UR Striker，并进入默认三人编队，但不改变现有六角色标准抽卡池。
- UI 统一为 `BubbleMind` 品牌，加入 0.2 秒页面淡入/位移过渡、响应式网格、稳定安全区、战斗退出入口与更清晰的结算面板。
- 战斗地图使用相机对齐背板、六个轻量站位和遗迹体；背景自动按相机宽高比超扫，避免宽屏黑边。
- Windows 产品名改为 `BubbleMind First Demo`，目标 60 FPS，并固定 Direct3D 11 以提高首个试玩的兼容性与演示稳定性。

## 地图来源

```text
Assets/_Game/Art/Generated/Environments/AbyssalObservatory/Textures/Resources/AbyssalObservatory_Concept.png
SHA-256: 182F89A0A379F44A9B716EC6F9C480B6A10830F76A6D06982A1DFB9D8545A8A4
```

另在 `StudioOps/ART_BIBLE.md` 保留“翠空铸园”和“镜潮陨坑”两个后续地图方向，本轮没有为它们生成或下载资源。

## 关键实现

```text
Assets/_Game/Scripts/Core/DemoBattlePresenter.cs
Assets/_Game/Scripts/Editor/AbyssalObservatoryAssetBuilder.cs
Assets/_Game/Scripts/Editor/CosmicSlimeAssetBuilder.cs
Assets/_Game/Scripts/Characters/CosmicSlimeVisualController.cs
Assets/_Game/Scripts/UI/DemoScreenTransition.cs
Assets/_Game/Scripts/UI/ResponsiveGridLayout.cs
Assets/_Game/Prefabs/Characters/PF_UR_CosmicSlime.prefab
```

## 验证结果

| 门槛 | 结果 |
|---|---|
| 项目生成与内容验证 | `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| 自动 Play 全流程 | `[P0_PLAY_SMOKE_PASS_20260713]` |
| Windows x64 Build | `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]` |
| 真实窗口 | 首页、收藏、编队、战斗、重开、退出与结算均已实际操作检查 |
| 图形后端 | Direct3D 11.0 / NVIDIA GeForce RTX 4060 |

最终 Windows BuildPipeline 报告为 `107,946,724` bytes / `10.0s`。`Builds/Windows` 共 191 个文件、`108,364,206` bytes；主程序 SHA-256 为 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4`。

## 已解决问题

- Windows 独立包首次实机进入战斗时，运行时 `Shader.Find` 找不到已被裁剪的 Unlit Shader。现改为编辑器生成并通过 `Resources` 保留材质，缺失资源时仍能回退到程序环境。
- 工作区从旧路径迁移到 `BubbleMind` 后，Bee 缓存保留旧绝对路径。构建工具现使用 `BuildOptions.CleanBuildCache` 通过 Unity 官方流程重建缓存。
- 初版地图背板仅覆盖画面中央且程序圆台比例偏重。最终改为相机中心全画幅背板并移除遮挡地图主体的圆台。

## 留待 P1

- 1280×720 真实窗口截图与更小尺寸专项布局打磨。
- 收藏页角色 3D 预览、其他六名角色正式模型、正式 Animator 与更细致的战斗表现队列。
- 商业发布前复核用户参考图和生成地图的完整权利链。

本轮没有新增下载、软件、SDK、Unity 包或第三方依赖；所有实现文件均位于唯一可写工程 `GenericGachaRPG/`。
