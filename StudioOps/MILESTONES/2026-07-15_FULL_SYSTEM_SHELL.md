# 2026-07-15 完整系统壳与离线主线闭环

> 状态：已完成
> 日期：2026-07-15
> Windows 入口：`Builds/FullSystemWindows/BubbleMind.exe`

## 目标

- 暂缓正式模型制作，把已验证的 2D 像素 5v5 战斗接入一个可持续扩展的完整游戏系统壳。
- 让玩家从主页进入世界、角色、招募、编队、背包、任务和设置，并完成关卡选择、战斗、结算、奖励、解锁下一关与返回主界面的离线闭环。
- 在线系统不伪造本地实现；竞技场、活动、商店、邮件和公会保留可见入口，并以明确的锁定说明返回现有离线系统。

## 已实现页面与连接

```text
Home
├─ World → Stage Detail → Formation → Battle → Result → World / Home / Restart
├─ Characters / Hero Archive
├─ Recruit
├─ Formation
├─ Inventory
├─ Missions → Claim / Go
├─ Settings
└─ Arena / Events / Shop / Mail / Guild → Feature Locked
```

- `DemoUiRouter` 统一管理页面注册、前进、替换、返回与重置；`AppShellView` 提供玩家档案、水晶、金币、体力、编队、邮件、设置和底部主导航。
- Home 显示继续主线、世界入口、招募、角色档案、编队和锁定功能区；World 显示章节路线、关卡状态、消耗、推荐战力与奖励。
- Characters、Recruit、Formation 与既有角色档案、卡面、抽卡权威服务和五槽阵容保持同一数据源。
- Battle Result 展示首通水晶、Gold、Echo Gel 与 Boss 首通 Void Fragment 摘要，并提供返回 World、返回 Home 和重开本关。
- Inventory 展示 Signal Ticket、Echo Gel、Void Fragment 与 Universal Shard；重复抽卡会转换 10 个 Universal Shard。
- Missions 提供招募、拥有角色、战斗胜利和通关 1-3 四项离线任务，完成后可领取水晶/金币；未完成任务可跳转到相关页面。
- Settings 保存音乐音量、效果音量、全屏和 60 FPS 模式，并提供二次确认的本地数据重置。

## 主线与进度

| 关卡 | 前置 | 体力 | 推荐战力 | 首通水晶 | 常规奖励 | 说明 |
|---|---|---:|---:|---:|---|---|
| 1-1 Fracture Gate | 无 | 6 | 1,000 | 100 | 250 Gold + 2 Echo Gel | 初始开放 |
| 1-2 Resonance Gallery | 通关 1-1 | 8 | 1,800 | 120 | 350 Gold + 3 Echo Gel | 顺序解锁 |
| 1-3 Event Horizon | 通关 1-2 | 10 | 2,600 | 200 | 500 Gold + 5 Echo Gel | Boss；首通另得 1 Void Fragment |

- 进入关卡时扣除体力；胜利后记录通关、战斗胜场并发放常规奖励，首胜额外发放水晶。
- 玩家保存的合法五槽阵容进入现有确定性 5v5 像素战斗；奖励和进度由 `GameStateService` 结算，不由 UI 或表现层直接修改。
- 存档升级为 schema v4，新增玩家档案、金币、体力、抽卡次数、胜场、背包、已通关关卡、已领取任务和设置；验证器覆盖 v3 → v4 显式迁移。

## 最终修复

- 每次战斗只允许提交一次结果；同局重复完成回调由 `battleResultCommitted` 拦截，Restart 建立新一局后才重新开放结算。
- Restart 会再次扣除本关体力并发放 Gold/Echo Gel/胜场，但不会重复发放首通水晶；增强冒烟对两次结算的精确差值逐项断言。
- Home 的 Continue 每次从存档解析当前关卡；通关 1-1 后立即显示并打开 1-2，World 的 1-2 节点同步解锁，不再保留陈旧选关。
- Boss 首通的 `Void Fragment` 进入 `StageRewardGrant.RareMaterials` 并显示在 Result 摘要。
- Settings 的全屏重置确认层改为覆盖整个安全区域，保持真正的模态输入层；四项设置均由增强冒烟修改并验证保存。
- Recruit 的概率说明重新分配垂直空间，避免概率文案与按钮发生裁切或重叠。

## 验证

证据目录：`Artifacts/FullSystem/`

| 门槛 | 结果 | 证据 |
|---|---|---|
| Generate / 编译 / 核心验证 | 通过 | `Generate.log`：`[GenericGachaRPG][FULL_SYSTEM_VERIFY_PASS_20260715]` |
| PlayMode 完整系统冒烟 | 通过 | `PlaySmokeEconomyFinal.log`：`[FULL_SYSTEM_PLAY_SMOKE_PASS_20260715]`；连续覆盖 1-1 编队→战斗→首通结算→Restart 重复结算→Home Continue/1-2 解锁，并修改/验证四项 Settings；同时覆盖 Inventory、Mission Claim 与锁定 Arena |
| Windows x64 / D3D11 | 通过 | `WindowsBuildFinal2.log`：`[GenericGachaRPG][FULL_SYSTEM_VERIFY_PASS_20260715]`、`[GenericGachaRPG][FULL_SYSTEM_WINDOWS_BUILD_PASS_20260715]`；BuildReport `143,676,760` bytes / `65.4s`，包含上述最终修复 |
| 独立窗口视觉检查 | 通过 | `1600×900` 窗口确认主页品牌、资源栏、热点、锁定入口和六项底部导航无重叠或裁切 |

## 边界

- 本轮没有下载或增加第三方运行依赖，正式模型按用户决定延期。
- `analysis/` 仅作为既有只读架构依据，未修改；没有读取、运行、解包或提交工作区根目录的 XAPK/APK。
- 竞技场、活动、商店、邮件、公会、登录、IAP、联网 PvP 和后端仍不在离线 Demo 范围；锁定页面不会伪造服务器奖励或真实货币行为。
