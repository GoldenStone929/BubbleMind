# 2026-07-15：2D 像素 5v5 抽卡 PvP 纵切片

## 结果

BubbleMind 的玩法范围保持为抽卡、收藏、角色档案、五槽编队与确定性自动战斗；默认视觉从 3D 角色转换为原创 2D 像素 Sprite + Unity 现代 VFX 的 2.5D 表现。新存档由 Catherine Yuki 主角带 Gold、Ember、Verdant、Violet 四只伙伴史莱姆，玩家保存的合法五槽阵容直接进入本地 5v5。

《潜水员戴夫》只作为“2D 像素角色由现代 3D 引擎承载”的广义技术参考。项目没有复制其角色、字体、UI、地图、构图、玩法或资产。

## 运行时改动

- 七个稳定角色 ID 均有 128×128 RGBA、Point Filter、PPU 64 的战斗 Sprite。
- Catherine 保留近黑深靛紫透明凝胶、内部星空、纯黑事件视界、白紫吸积盘、不对称角与破碎紫金轨道；黑洞始终是体内核心。
- `PixelCharacterBuilder` 创建 `CharacterView`、统一 Sockets、阴影、发光与 `PixelCharacterVisual`；Catherine 继续接入黑洞技能 VFX。
- `DemoBattlePresenter` 优先加载 Pixel2D Sprite 和 480×270 像素星渊观测台，使用正交镜头、深度站位、世界 UI、伤害数字和现代 Shader/VFX。
- 旧 FBX/Prefab 不删除，只是素材缺失时的兼容回退；正常流程强制拒绝旧 3D 控制器与程序化角色。
- 玩家队伍从当前保存的五槽阵容解析；历史固定 Catherine / Gold / Ember 三人测试名单不再决定出战。

## 资产来源

- Catherine 像素源图：`Assets/_Game/Art/Source/Characters/Pixel2D/ur_cosmic_slime/Pixel_ur_cosmic_slime_Source.png`，由本任务早期 Codex 内置图像生成能力依据用户参考图制作。
- 六张元素运行时 Sprite：项目内工具从已登记的原创五元素概念表执行裁切、背景分离、有限色量化、硬 Alpha 与最近邻放大，没有引入外部素材。
- 地图：项目内工具从已登记的原创星渊观测台概念图生成 240×135 有限色底图，再以最近邻放大到 480×270。
- 哈希与裁切记录：`Assets/_Game/Art/Generated/Pixel2D/Pixel2D_AssetManifest.json`。

## 验证

| 门槛 | 结果 | 证据 |
|---|---|---|
| Generate / C# / 核心验证 | 通过 | `Artifacts/PixelPvp2D/Generate1.log`；`[GenericGachaRPG][PIXEL_PVP_VERIFY_PASS_20260715]` |
| 5v5 PlayMode | 通过 | `Artifacts/PixelPvp2D/PlaySmoke1.log`；`[GenericGachaRPG][PIXEL_PVP_PLAY_SMOKE_PASS_20260715]` |
| Restart 第二局 | 通过 | 同一 PlayMode 冒烟检查第二局确定性结果、10 个新单位、10 个怒气条与恰好一个 BattleWorld |
| Windows x64 Build | 通过 | `Artifacts/PixelPvp2D/WindowsBuild1.log`；`[GenericGachaRPG][PIXEL_PVP_WINDOWS_BUILD_PASS_20260715]`；`143,616,984` bytes / `69.7s` |
| Windows 独立窗口 | 通过 | `Builds/Windows/BubbleMind.exe` 已实际启动并显示最新 Pixel PvP 首页、像素星渊观测台与 19.3 秒 Victory 结算；Catherine / Ember 像素角色、世界条、Restart / Return Home 可见，随后保留给玩家试玩 |

## 边界与下一轮

- 当前是离线本地 PvP 风格模拟，不是联网 PvP；没有匹配、服务器裁决、账号或防作弊。
- 当前角色是单帧 Sprite + 程序化挤压/移动/闪白/VFX；逐帧行走、攻击、表情、倒地 Sprite Sheet 留待下一轮。
- 1280×720、移动端过绘、多人聚拢遮挡与触控手感应根据玩家本次实机反馈继续微调。
