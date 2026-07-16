# Asset Spec：`ART-CHAR-PIXEL-PARTY-001 2D 像素战斗阵容`

> 状态：已生成并通过 Unity / Windows 内部原型验证
> 用途：五槽编队、5v5 自动战斗与 2.5D 像素表现
> 最后更新：2026-07-15

## 阵容语义

- 主角：`ur_cosmic_slime` / Catherine Yuki，默认首槽、UR Limited、黑洞史莱姆。
- 默认四伙伴：`gold_ranger`、`ember_striker`、`verdant_medic`、`violet_arcanist`。
- 水系 `azure_vanguard` 与 `cyan_warden` 继续进入敌方/替换编队；两者沿用同一水系基础轮廓，与原 3D Prefab 共享策略一致。
- 保留全部稳定 Character ID、数值、职业、技能、卡池、Profile 与存档语义。

## 美术方向

- 原创圆润果冻史莱姆、硬 Alpha、Point Filter、有限调色板和清晰像素轮廓。
- Catherine 保持近黑深靛紫透明凝胶、内部星云、白紫眼睛、中央纯黑事件视界与紫白吸积盘；黑洞是身体内部核心，不把角色改成人形、潜水员或普通圆球。
- 四伙伴分别保留土岩叶、火焰、薄荷风卷和金黄雷电轮廓。
- 只借鉴“2D 像素角色由现代 3D 引擎承载”的广义技术；不复制《潜水员戴夫》或任何其他作品的角色、字体、UI、地图、构图、玩法与资产。

## 来源与制作

- Catherine 运行时源：`Assets/_Game/Art/Source/Characters/Pixel2D/ur_cosmic_slime/Pixel_ur_cosmic_slime_Source.png`。
- Catherine 源图由本任务先前通过 Codex 内置图像生成工具制作；规范化提示意图：

```text
Use case: stylized-concept
Asset type: original pixel-art battle sprite
Primary request: create the user-designed black-hole slime as a readable full-body pixel sprite
Subject: round translucent jelly slime; near-black indigo-purple shell; internal stars and nebula; pure-black event-horizon core with a white-violet accretion disk; sharp white-violet eyes; asymmetric horns; broken purple-and-gold orbit rings
Composition: centered single character, generous padding, front three-quarter readability
Style: crisp true pixel art, limited palette, game-ready silhouette
Constraints: original BubbleMind design; no human/diver; no text, logo, UI, watermark, cast shadow, or copied IP content
```

- 元素史莱姆来自已登记项目设定表 `BasicElementSlimes_ConceptSheet.png` 的前视图；项目内 Pillow/NumPy/SciPy 工具只执行裁切、背景分离、58 像素工作分辨率、最近邻 2×、48 色量化与硬 Alpha，不新增外部素材。
- 完整输出哈希与裁切坐标：`Assets/_Game/Art/Generated/Pixel2D/Pixel2D_AssetManifest.json`。

## Unity 契约

- 路径：`Assets/_Game/Art/Generated/Pixel2D/Characters/Resources/BattleSprites/Pixel_<characterId>.png`。
- 画布：128×128 RGBA；PPU 64；底部中心 Pivot。
- 导入：Sprite/Single、Point、无 Mipmap、Clamp、Uncompressed、Alpha Is Transparency。
- 运行时：`PixelCharacterBuilder` 创建 `CharacterView`、标准 Sockets、`SpriteRenderer`、像素阴影与 `PixelCharacterVisual`；战斗模拟不读取表现组件。
- Catherine 继续挂接 `CatherineSkillVfxController`，以现代 Shader/VFX 表达黑洞阶段；不依赖旧 3D Blend Shape。
- 旧 FBX/Prefab 不删除；像素 Sprite 缺失时才回退到旧 authored 3D，再缺失才使用程序化防崩角色。

## 验收

- [x] 七个稳定角色 ID 均有 128×128 战斗 Sprite
- [x] QA 接触表确认透明边缘、元素轮廓与 Catherine 黑洞可读
- [x] Unity 导入设置与 Resources 加载通过
- [x] 正常 5v5 恰好生成十个 PixelCharacterVisual，旧 3D 控制器数量为零
- [x] 普攻、技能、受击、死亡、黑洞 VFX、世界条与重开流程通过

验证证据：`Artifacts/PixelPvp2D/Generate1.log`、`PlaySmoke1.log`、`WindowsBuild1.log`。
