# ART-UI-CHARACTER-PORTRAITS-001

## 目标

为 BubbleMind 当前七名角色提供可在角色档案、抽卡结果和五槽编队中复用的原创竖版 2D 卡面。卡面必须让玩家先通过轮廓、主色和道具识别角色，再读取名称与职业。

## 统一美术约束

- 原创可爱动漫史莱姆，不使用现有版权角色、服装、Logo 或文字。
- 柔和漫画渲染、清晰轮廓、果冻高光、轻半透明厚度感。
- 竖版全身/近全身角色卡构图，角色居中，边缘保留 UI 裁切安全区。
- 背景服务于元素与职业识别，不使用外部游戏截图作图生图输入。
- 不在图内生成名称、稀有度、按钮或 UI 边框；这些信息由 Unity 统一渲染。

## 角色方向

| characterId | 视觉方向 | 文件 |
|---|---|---|
| `ur_cosmic_slime` | 近黑星云凝胶坦克，腹部纯黑事件视界，青金轨道环，庄重限定感 | `Portrait_ur_cosmic_slime.png` |
| `azure_vanguard` | 浅蓝水系史莱姆，水弓/水刃、气泡与灵活远程姿态 | `Portrait_azure_vanguard.png` |
| `ember_striker` | 珊瑚火系刺客，跃动火焰、前冲轮廓与高对比暖色 | `Portrait_ember_striker.png` |
| `verdant_medic` | 薄荷风系治疗，叶片、花朵、柔光与友善稳定轮廓 | `Portrait_verdant_medic.png` |
| `violet_arcanist` | 紫色雷系法师，金色电弧、奥术能量与高位施法姿态 | `Portrait_violet_arcanist.png` |
| `gold_ranger` | 琥珀土系射手，晶体弓、矿物切面与清晰拉弓姿态 | `Portrait_gold_ranger.png` |
| `cyan_warden` | 青色水系坦克，盾形冠、水鳍与厚重守护轮廓 | `Portrait_cyan_warden.png` |

## Unity 契约

- 根目录：`Assets/_Game/Art/Generated/UI/Portraits/`
- 文件名：`Portrait_<characterId>.png`
- 导入：Sprite / Single、sRGB、Clamp、Bilinear、无 mipmap、Max Size 1024。
- 绑定：`DemoSceneGenerator` 以 `characterId` 加载并写入 `CharacterDefinition.Portrait`。
- 验证：`DemoProjectVerifier` 阻止缺图、非 Sprite、开启 mipmap 或项目外卡面进入构建。
- 使用：角色档案、抽卡揭示和编队必须从同一个 `CharacterDefinition.Portrait` 读取。

## 校验

| 文件 | 原始尺寸 | SHA-256 |
|---|---:|---|
| `Portrait_ur_cosmic_slime.png` | 1024×1536 | `B6B24D3F9DEF9CF7743B726671B1B652F24ACE9F4C0CFA8CE7D41DB13EE1D5FC` |
| `Portrait_azure_vanguard.png` | 1003×1568 | `0021F25AFB42E85CAC3A585DF7FADD9EF1D7B552F5312061ADFBC0EAFC90CE37` |
| `Portrait_ember_striker.png` | 972×1619 | `F5878967D12B60C39AA7C695B79BCCD339C35FCF210CC5BD3D5634024937D26A` |
| `Portrait_verdant_medic.png` | 972×1619 | `D836D01BBF38230B3A6FA908826328B3E6CF3C145525C81EF891949D76114BE6` |
| `Portrait_violet_arcanist.png` | 982×1601 | `21E319BC6774DA48457BB376AE9839B1599E51DBE4262A8389F584B4EB03C5B0` |
| `Portrait_gold_ranger.png` | 971×1619 | `F37D97320D5D99193CD1DA6D4D2C9C3361CCD0C230F8EC3CBF4366A5A8C08266` |
| `Portrait_cyan_warden.png` | 1023×1537 | `511B1B085F105F9161CEA01B0946226187F7057DC3EF6CA28F23EEA0216AB93A` |

## 验收

- Generate / verifier：`Artifacts/CharacterPage/GenerateFinal.log`
- PlayMode 页面流：`Artifacts/CharacterPage/PlaySmokeFinal.log`
- Windows 构建：`Artifacts/CharacterPage/WindowsBuildFinal.log`
- 实机：1922×1112 最终窗口确认角色大卡面、列表卡面和真实抽卡结果可读，无文本/图像重叠。
