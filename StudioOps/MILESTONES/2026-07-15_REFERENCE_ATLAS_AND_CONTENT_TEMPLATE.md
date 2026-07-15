# 2026-07-15 参考系统图谱与角色内容模板

> 状态：已完成
> Unity：6000.5.3f1 / URP 17.5 / uGUI
> 范围：清洁室知识库、原创角色内容契约、角色档案扩展与验证工具

## 用户目标

- 在不关闭参考游戏、不改变账号或游戏状态的前提下，尽可能整理页面、角色、技能、养成、抽取、战斗和系统连接。
- 把只读 `analysis/architecture/`、用户截图、此前实时观察和 BubbleMind 当前实现分层记录，形成可长期维护的模板。
- 在信息足够且原创性边界清晰后，把角色内容模板接入 Unity，而不是只留下研究笔记。
- 所有成功更新都要有文档、验证证据和 Git 历史；未知内容不得用推测补齐。

## 操作边界

- 参考游戏窗口始终保持开启；没有退出、重启、重新登录、购买、抽卡、领取、养成、改队、保存设置或进入战斗。
- 此前已只读观察首页、角色列表、角色详情、技能说明、招募页和概率页。2026-07-15 再次连接时，参考游戏处于自身省电模式；普通唤醒输入无效，因此没有冒险关闭或重启。
- `analysis/` 仅按需只读；未修改，也未读取、运行、解包或提交 XAPK/APK。
- 仓库不保存外部截图、角色资产、逐字技能文案、精确概率/倍率、布局坐标、代码、协议或凭据。
- 本轮没有下载、安装、购买或引入新的软件、Unity 包、SDK、Shader、插件或运行时依赖。

## 清洁室知识库

知识库位于 `StudioOps/KnowledgeBase/`，采用“证据与覆盖 → 通用系统规律 → BubbleMind 原创映射”三层结构：

- `README.md`：声明标签、置信度、更新流程和强制边界。
- `EVIDENCE_LEDGER.csv`：21 条证据来源及保留策略。
- `COVERAGE_MATRIX.csv`：36 个中性系统模块的 analysis、实时窗口和截图覆盖，显式保留未知项。
- `REFERENCE_SYSTEM_ATLAS.md`：页面族、系统族、数据实体、全流程、状态权威和边界总图。
- `RELATIONSHIPS.csv`：89 条页面、系统与实体关系。
- `GENERALIZED_PATTERNS.md`：从多源证据归纳的通用产品和架构规律。
- `BUBBLEMIND_MAPPING.md`：36 个模块到当前原创 Unity 工程的状态与目标接口。
- `ORIGINALITY_CHECKLIST.md`：研究、设计、内容、代码和发布前的原创性门槛。

知识库只用于设计和审计，运行时代码不得依赖参考证据。新结论必须先登记证据和覆盖，再经过通用归纳与原创性检查，最后才能成为 BubbleMind 的需求或实现。最终审计覆盖 36 个系统、25 个页面和 27 个实体；每一项至少拥有一条关系，89 条关系中坏外键和重复边均为 0。

## Unity 内容模板

### 数据契约

- 新增 `CharacterContentProfile`，补充称号、元素、阵营、关键词、内容状态、完整能力档案、逐级参数、解锁阶段、获取来源、关系标签和来源证明。
- `CharacterDefinition.Id` 仍是角色档案、抽取、编成、存档和战斗的唯一身份键；Profile 的 `ownerCharacterId` 只是防误挂外键，不复制或取代玩家实例状态与战斗权威数据。
- `SkillDefinition` 增加结构化技能标签和可选图标挂点。
- 七名现有原创角色拥有独立 Profile；Catherine 档案可表达 Basic、三项主动能力、Star Rage 领域、觉醒和 11/41/61 级解锁语义。
- 当前战斗继续使用满级测试夹具；档案中的养成阶段是内容模板，不等于已经实现升级消费系统。

### 页面与数据连接

```text
抽取 rewardId
  -> GameDatabase.GetCharacter(characterId)
  -> CharacterDefinition
       -> 卡面 / 名称 / 稀有度 / 职业
       -> 三个运行时技能槽 / 战斗参数
       -> CharacterContentProfile
            -> Archive / Growth / Acquisition / 扩展能力
  -> 角色页 / 抽取揭示 / 编成 / 战斗请求
```

- 角色页增加 `Combat / Archive / Growth` 三段控件，分别显示运行时战斗槽、完整能力类型、养成与获取信息；Archive 与 Growth 均采用分页，避免长档案挤压布局。
- 验证器新增 Profile 唯一性/归属、运行时槽位、从 Lv.1 到 MaxLevel 的连续等级参数、来源、Catherine 领域/觉醒和角色级战斗参数消费检查。
- 修正角色自定义攻击范围、怒气上限、攻击回怒和受击回怒在配置/战斗快照中被全局默认值覆盖的问题；现有 Demo 默认数值保持不变。
- PlayMode 冒烟已实际点击三个角色页模式，并继续覆盖抽取、五槽编成和 3v5 战斗流程。
- 生成器按稳定 ID 合并必需资产，保留数据库额外角色、技能与卡池；已有 Profile 的人工内容也不会被默认模板覆盖。

## 最终验证证据

| 门槛 | 结果 | 证据 |
|---|---|---|
| 清洁室知识库 | 通过 | 21 条证据、36 个系统、25 个页面、27 个实体、89 条关系；所有节点至少有一条关系，坏外键和重复边均为 0 |
| Generate / C# / 核心验证 | 通过 | 终审后 `Artifacts/ContentTemplate/GenerateTriggerCompile.log` 再次编译并出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| 生成器二次幂等 | 通过 | 连续生成前后四个权威文件 SHA-256 完全一致，且额外数据库内容与已有人工作者档案保留 |
| PlayMode 完整流程 | 通过 | `Artifacts/ContentTemplate/PlaySmokeOwnershipFinal.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；三个角色页模式、首尾回绕、普通角色切换、抽取、五槽和 3v5 流程通过 |
| Windows x64 / D3D11 | 通过 | `Artifacts/ContentTemplate/WindowsBuildOwnershipFinal.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildReport `142,747,278` bytes / `76.6s` |
| Windows 构建产物 | 通过 | BubbleMind 分发集合 190 个文件、`142,956,016` bytes；`BubbleMind.exe` 为 `667,648` bytes，SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| 1600x900 独立窗口 | 通过 | 首页、Combat、Archive、Growth、Recruitment 均已打开检查；Star Rage 5/6、Awakening 6/6、分页箭头、长文本边界与七张 2D 卡面正常 |
| 新 Profile 资产 | 通过 | `Assets/_Game/Data/CharacterProfiles/` 中七个原创角色 Profile 均通过唯一性、槽位、等级、来源与运行时交叉校验 |

幂等校验使用以下四个权威文件测试时序列化哈希：

- `GameDatabase`：`2B21B6690C0B9F053D42B44BCA68E989E3C624B6FCC40D9E3162EF92CAE296A3`
- Catherine Profile：`4D107C0654CEFFC1714AB04736B32C66F259B42AC3872BFB4D5980E132B063F5`
- Dance Skill：`5CE92ECA60BCE06890A1EF3588D9836EB7C9C48854DEA489F515BB7ABB339241`
- Demo Scene：`C64F5B293EEBBFFD34DAA6EFA45DEA8A759E13BA653938C6F183BB0D08322174`

两次 Unity Generate 的前后值完全一致。提交前只规范化了 Unity YAML 的行尾空格，因此当前仓库中的 Demo Scene 文本哈希为 `1A3A6C78E3949BE53A295081342DB9F1BADD2388B4D7133AA652197D7B65C7C0`；其忽略行尾空格的语义差异为空，其余三个文件哈希不变。

## 门槛结论

| 门槛 | 当前状态 | 结论 |
|---|---|---|
| 代码与数据审查 | 已完成 | 生成器扩展性、运行时数值同源、怒气精确消费和长档案分页均已处理 |
| 生成器二次幂等 | 已完成 | 四个权威文件哈希连续生成前后一致；额外内容和人工档案不被覆盖 |
| 最终 PlayMode 回归 | 已完成 | 覆盖角色页三模式、档案翻页、抽取、编成、3v5、结算和返回首页 |
| Windows x64 / D3D11 Build | 已完成 | 最终模板进入 `BubbleMind.exe`，日志具有 Build PASS 标记 |
| Windows 真实窗口视觉 | 已完成 | 1600x900 检查长文本、箭头、2D 卡面与页面边界；箭头修复后已重建复核 |
| Git 差异与远程 | 由版本控制记录 | 本记录与实现同批提交；提交号和远程状态以仓库 `main` / `origin/main` 为权威，避免文档自引用未来哈希 |

`Builds/Windows` 同时保留上一轮 `GenericGachaRPGDemo` 基线，因为对应进程仍在运行，未强行关闭或删除。BubbleMind 本轮分发集合为 190 个文件、`142,956,016` bytes；整个共存目录为 355 个文件、`231,448,456` bytes。最终试玩窗口已用最新构建重新打开在首页。

## 已知限制

- 省电模式阻止了本轮继续实时观察；未观察页面继续标记为 `UNKNOWN`、`analysis_only` 或较低置信度。
- 角色养成、装备、十连、保底、碎片、正式奖励、账号、支付、活动、竞技和排行仍未实现；知识库只定义边界，不把规格说明伪装为可玩功能。
- 角色内容模板目前服务于七名原创 Demo 角色；正式大规模内容生产还需要本地化键、资产预算、迁移策略、编辑器表单和更完整的构建阻断规则。
- 正式本地化、完整养成和在线服务仍是后续里程碑；这些边界不影响本轮清洁室图谱与内容模板已经完成。
