# 当前里程碑：参考系统图谱与角色内容模板

> 状态：已完成
> 开始日期：2026-07-15
> 长期范围权威：`../PROJECT_PLAN.md`

## 目标

- 在不关闭或修改参考游戏账号状态的前提下，系统整理可观察页面、角色/技能结构、系统边界和数据连接。
- 将 `analysis/architecture/` 的只读资料与实机/用户截图观察分层记录，明确事实、推断、未知和置信度。
- 建立不复制外部专有内容的 BubbleMind 原创角色模板，并接入角色页、生成器、验证器和战斗数据链。

## 已完成实现

- 新增 `CharacterContentProfile`，支持元素、阵营、称号、关键词、Basic/Active/Passive/Domain/Awakening、技能等级参数、解锁、养成阶段、获取来源、关系标签和来源证明。
- 七名现有角色全部生成独立且已批准的 Profile；`ownerCharacterId` 阻止档案误挂，运行时仍以 `CharacterDefinition.Id` 为唯一身份键。
- Catherine 档案完整记录九级大招、五级 Break、四级 Dance、九级 Star Rage、两段觉醒及 11/41/61 级解锁语义；当前战斗仍按满级测试夹具执行。
- 角色页增加 `Combat / Archive / Growth` 三段控件，分别展示运行时三技能、完整能力档案、养成与获取信息；Archive 与 Growth 均支持分页浏览。
- `SkillDefinition` 增加技能标签和可选图标挂点。
- 修复角色级 `AttackRange / MaxRage / RagePerAttack / RageWhenHit` 在配置和战斗快照中被全局常量覆盖的问题；Demo 默认值保持不变。
- 验证器新增 Profile 唯一性/归属、运行时槽位、连续逐级参数、来源证明、Catherine 领域/觉醒和自定义战斗参数消费测试。
- PlayMode 冒烟新增三个角色页模式的实际点击与内容断言。
- 建立三层清洁室知识库和角色模板规范，参考观察不进入运行时资产。
- 生成器按稳定 ID 合并当前必需内容，保留数据库中的额外角色、技能和卡池，也不覆盖已有人工作者档案。

## 参考游戏只读状态

- 参考游戏窗口保持开启，没有退出、重启、登录、购买、抽卡、养成、改队、保存设置或进入战斗。
- 此前已只读观察首页、角色库、角色详情、技能说明、招募和概率页。
- 2026-07-15 再次连接时游戏处于自身省电模式；普通点击、双击、轻微拖动、回车和返回均未恢复画面。为避免登录风险，没有关闭或重启。
- 未实机复核的页面在知识库中继续标记为 `analysis_only` 或 `unknown`，不以推测冒充事实。

## 清洁室边界

- `analysis/` 保持只读；未读取、运行、解包或提交 XAPK/APK。
- 仓库不保存外部截图、角色立绘、模型、图标、音频、字体、逐字技能文案、外部概率/倍率、布局坐标、代码或协议。
- 参考层只保存匿名化结构事实；通用规律经过原创性检查后，才能映射为 BubbleMind 原创数据与 UI。
- 本轮没有下载、安装或购买任何新依赖。

## 当前验证证据

| 门槛 | 状态 | 证据 |
|---|---|---|
| 清洁室知识库 | 通过 | 21 条证据、36 个系统、25 个页面、27 个实体、89 条关系；所有系统/页面/实体至少有一条关系，坏外键与重复边均为 0 |
| Generate / C# / 核心验证 | 通过 | 终审后 `Artifacts/ContentTemplate/GenerateTriggerCompile.log` 再次编译并出现 `[GenericGachaRPG][P0_VERIFY_PASS_20260713]` |
| 生成器二次幂等 | 通过 | 连续生成前后 `GameDatabase`、Catherine Profile、Dance Skill 和 Demo Scene 的四个 SHA-256 完全一致 |
| PlayMode 完整流程 | 通过 | `Artifacts/ContentTemplate/PlaySmokeOwnershipFinal.log` 出现 `[P0_PLAY_SMOKE_PASS_20260713]`；角色页首尾回绕、普通角色切换、抽卡、五槽和 3v5 全流程通过 |
| Windows x64 / D3D11 | 通过 | `Artifacts/ContentTemplate/WindowsBuildOwnershipFinal.log` 出现 `[GenericGachaRPG][WINDOWS_BUILD_PASS_20260713]`；BuildReport `142,747,278` bytes / `76.6s` |
| Windows 构建产物 | 通过 | BubbleMind 分发集合 190 个文件、`142,956,016` bytes；`BubbleMind.exe` 为 `667,648` bytes，SHA-256 `5AE0C84993C6BFF7D0F03166CAFD148CDEC61F67EC85B5D97600E04B2ABB26E4` |
| Windows 真实窗口视觉 | 通过 | 1600x900 独立窗口检查首页、Combat、Archive、Growth、Recruitment；Star Rage 5/6、Awakening 6/6、分页箭头、长文本和七张 2D 卡面均正常 |
| Git 提交与远程推送 | 由版本控制记录 | 本里程碑与实现同批提交；提交号和远程状态以仓库 `main` / `origin/main` 为权威，避免文档自引用未来哈希 |

## 完成确认

1. 知识库、角色内容契约、角色页分页、生成器保留策略与验证器均已落地。
2. 最终 Generate、连续生成幂等、PlayMode、Windows Build 与真实独立窗口检查均已通过。
3. 最终 BubbleMind 试玩窗口已用最新构建重新打开在首页；旧基线和参考游戏进程保持开启，参考游戏未被关闭、重启或修改。
4. 参考游戏省电模式造成的未观察内容继续保留为 `unknown`，没有以推断补齐。
5. 本次 Git 提交号与远端状态由提交操作完成后回填，不属于运行时实现缺口。
