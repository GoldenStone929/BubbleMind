# Asset Spec：`ART-ENV-BATTLE-ABYSSAL-OBSERVATORY-001 星渊观测台`

> 状态：已批准用于内部原型
> 用途：首个 5v5 战斗地图与视觉基准
> 最后更新：2026-07-14

## 视觉目标

- 圆形古代观测场悬浮在星海和破碎环形遗迹之间。
- 中央战区使用中性石墨灰；冷青站位与苍金镶边提供结构，紫色主要留在远景奇点。
- 固定三分之四镜头下，十名角色、世界血条、伤害数字和顶部 HUD 必须优先可读。
- 不使用任何既有 IP 的符号、建筑、构图或命名。

## 生成基准

- 图像：`Assets/_Game/Art/Generated/Environments/AbyssalObservatory/Textures/Resources/AbyssalObservatory_Concept.png`
- 方式：用户明确要求后，通过 Codex 内置图像生成工具生成；未使用外部下载素材。
- SHA-256：`182F89A0A379F44A9B716EC6F9C480B6A10830F76A6D06982A1DFB9D8545A8A4`
- 权利状态：当前仅作为内部原型；商业发布前统一复核生成服务条款与项目权利链。

## Unity 实施

- 概念图作为固定战斗镜头的全画幅背板；前景使用项目内程序化站位标记和轻量遗迹体提供空间层次。
- 地图随 `BattleWorld` 创建和销毁，不改变固定 Tick 的直线移动、射程判定或目标选择。
- 背板使用构建前生成并由 `Resources` 保留的 URP Unlit 不透明材质；运行时不依赖可能被裁剪的 Shader 字符串查找。
- 缺失图片或材质时回退到纯程序环境，不能阻塞战斗。

## 预算与验收

- 地图 3D 几何不超过 25,000 triangles；地图 Draw Call 目标不超过 15。
- 背板纹理 Windows 最大 2048；移动端发布前目标覆盖为 1024，本轮尚未设置 Android override。
- 除十个出生标记外不新增大面积透明层；中央前线路径保持净空。
- [x] Unity 导入设置正确
- [x] 战斗地图运行时显示
- [x] Home → Battle → Result 全流程通过
- [x] 1920×1080 真实窗口无 UI/角色遮挡
- [ ] 1280×720 留待下一轮实机截图
- [x] Windows Build 回归通过
