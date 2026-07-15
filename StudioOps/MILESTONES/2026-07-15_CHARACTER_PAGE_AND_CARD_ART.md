# 2026-07-15 角色档案、2D 卡面与 UI 重制

> 状态：已完成
> Unity：6000.5.3f1 / URP 17.5 / uGUI
> 交付：Windows x64 Demo、项目知识库、原创卡面资产与验证日志

## 用户目标

- 增加真正的角色页面，而不是只有文字/色块收藏列表。
- 抽卡结果显示可识别的 2D 角色卡面，让玩家知道抽到了谁。
- 改善首页、字体、边框、层级和五槽编队的整体品质。
- 只读研究当前打开的参考游戏与 `analysis/`，把结论记录为项目知识库。

## 只读研究与边界

- 查看用户提供的六张截图，以及参考游戏的主界面、角色列表、角色详情、技能说明、招募页和概率页。
- 全程未购买、抽卡、养成、修改队伍、保存设置或关闭参考游戏；结束后恢复其主界面。
- `analysis/architecture/` 仅按需只读，重点使用 battle、gacha、character pipeline、data model、game flow 与 clean-room architecture 资料。
- 未读取、运行、解包或提交 XAPK/APK；没有复制外部角色、立绘、图标、字体、文案、布局坐标、数值、概率、代码或协议。
- 清洁室归纳结果写入 `Assets/_Game/Docs/UiReferenceKnowledgeBase.md`。

## 数据与架构

- `CharacterDefinition.Id` 继续作为角色档案、抽卡、编队、存档和战斗间的唯一身份键。
- `CharacterDefinition.Portrait` 成为 2D 卡面单一来源；生成器自动绑定，验证器拒绝缺图或错误导入设置。
- 抽卡服务仍只处理概率、扣费、结果和重复角色；UI 通过 `rewardId` 查数据库并显示卡面，不接管 RNG。
- 四个非战斗页面拆为独立 View：`ObservatoryHomeScreenView`、`SummonScreenView`、`CharacterPageScreenView`、`RosterFormationScreenView`。
- 抽卡结果使用 `DemoCardReveal` 做 0.38 秒淡入/缩放；只改变表现，不延迟或修改抽卡结果。
- 存档 schema 保持 v3；卡面与页面只增加静态展示能力，不借版本升级清空玩家水晶、拥有角色、重复计数或五槽编队。

## 原创资产

- 使用内置 GPT Image 生成 7 张原创动漫史莱姆竖版卡面：Catherine Yuki、Azure Vanguard、Ember Striker、Verdant Medic、Violet Arcanist、Gold Ranger、Cyan Warden。
- Catherine 卡面明确呈现近黑星云凝胶、腹部纯黑事件视界与青金轨道环；其余角色按水、火、风、雷、土与职业轮廓区分。
- 图片均无文字、商标和现有版权角色，未使用外部游戏截图作图生图参考。
- 引入 Noto Sans CJK SC 2.004 Regular，SIL OFL 1.1，用于可随包分发的中英文 UI 字形。
- 尺寸、SHA-256、来源与许可证完整登记在 `Assets/_Game/Docs/ThirdPartyInventory.md`。

## 页面交付

### 首页

- 保留星渊观测台全幅地图，减少遮挡。
- 资源、当前任务与四个主要入口形成稳定层级。
- 使用薄线框、深灰玻璃表面、薄荷信息色、珊瑚行动色和金色稀有强调。

### 角色档案

- 左侧为可滚动双列卡面库，可查看已拥有与未拥有角色。
- 右侧为大卡面、稀有度、职业、限定状态、描述、属性、拥有状态与三项技能。
- 未拥有角色可预览，但明确标记 Locked；Catherine 默认选中，突出 UR 限定坦克身份。

### 抽卡

- 池主题使用角色卡面作为主要视觉，概率始终可见。
- 单抽结果显示卡面、稀有度、角色名、NEW/DUPLICATE 和剩余水晶。
- 实机单抽已确认 Azure Vanguard 卡面与 NEW 状态正确显示。

### 编队

- 五个槽位使用稳定尺寸的角色卡面、职业与射程信息。
- 候选库区分已选、可选、未拥有状态；现有固定 3v5 测试部署说明保持准确。

## 验证证据

| 门槛 | 结果 | 证据 |
|---|---|---|
| Generate / 核心验证 | 通过 | `Artifacts/CharacterPage/GenerateFinal.log`；`[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| PlayMode UI 冒烟 | 通过 | `Artifacts/CharacterPage/PlaySmokeFinal.log`；`[P0_PLAY_SMOKE_PASS_20260713]` |
| Windows D3D11 Build | 通过 | `Artifacts/CharacterPage/WindowsBuildFinal.log`；`[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`，`142,698,350` bytes / `70.1s` |
| Windows 文件校验 | 通过 | 191 文件 / `143,115,832` bytes；EXE `667,648` bytes；SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| 真实窗口 | 通过 | 1282×720 首轮与 1922×1112 最终包均检查；首页、角色页、抽卡与五槽无重叠，最终字体版实抽卡面正确 |
| 初始状态 | 通过 | 实抽验证后通过 Demo Reset 恢复 3,000 水晶并停留首页；schema 保持 v3，既有试玩存档继续兼容 |

## 本轮新增依赖

- 游戏运行依赖：无新增 Unity 包、SDK、Shader 或插件。
- 内容服务：OpenAI 内置 GPT Image，仅用于离线生成原创 PNG，无运行时依赖。
- 字体：Noto Sans CJK SC 2.004 Regular，SIL OFL 1.1，许可证随项目保存。

## 后续打磨

- 增加十连抽、逐张揭示、保底历史与角色碎片结果表现。
- 为技能增加正式图标、元素标签与可滚动详情，而不改变现有三技能数据契约。
- 角色页后续可增加 3D 旋转预览；当前以高品质 2D 卡面保证识别度和加载稳定性。
