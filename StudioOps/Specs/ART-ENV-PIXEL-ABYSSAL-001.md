# Asset Spec：`ART-ENV-PIXEL-ABYSSAL-001 像素星渊观测台`

> 状态：已生成并通过 Unity / Windows 内部原型验证
> 用途：首个 2D 像素 / 2.5D 5v5 战斗地图
> 最后更新：2026-07-15

## 视觉与来源

- 保留 BubbleMind 原创星渊观测台的悬浮圆形战场、远方黑洞、靛紫星海、暗金镶边与中央净空。
- 权威源：`Assets/_Game/Art/Generated/Environments/AbyssalObservatory/Textures/Resources/AbyssalObservatory_Concept.png`。
- 运行时像素图：`Assets/_Game/Art/Generated/Pixel2D/Environments/AbyssalObservatory/Textures/Resources/AbyssalObservatory_Pixel.png`。
- 项目内工具将源图裁成 16:9，缩至 240×135、无抖动量化为 128 色，再用最近邻放大到 480×270；未使用外部地图或游戏截图。

## 2.5D 契约

- 固定正交斜视相机保留 X/Z 战斗位置与前后 lane 深度。
- 480×270 背板使用 Point Filter、无 Mipmap、Clamp、Uncompressed。
- Unity 继续生成低矮站位标记、世界 UI、伤害数字、粒子与黑洞 VFX，形成像素角色与现代渲染的层次。
- 地图不改变 20 格战场、射程、移动、目标选择或技能结果。

## 验收

- [x] 480×270 像素背板与 2× QA 图已生成
- [x] Unity Resources 加载并在正交相机中覆盖安全画幅
- [x] PlayMode 运行时生成十个角色、姓名、生命/怒气条与伤害数字并完成两局战斗
- [x] 1920 级宽屏独立窗口确认 Point Filter 地图覆盖完整；1280×720 留作后续玩家视觉回归

验证证据：`Artifacts/PixelPvp2D/Generate1.log`、`PlaySmoke1.log`、`WindowsBuild1.log`。
