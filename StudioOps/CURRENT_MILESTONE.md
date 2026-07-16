# 当前里程碑：完整系统壳与离线主线闭环

> 状态：已完成（Windows 完整系统试玩包已构建）
> 开始日期：2026-07-15
> 长期范围权威：`../PROJECT_PLAN.md`

## 目标

- 暂缓正式模型制作，把现有抽卡、角色档案、五槽编队与 5v5 像素战斗接入可导航的完整游戏系统。
- 提供 Home、World、Characters、Recruit、Formation、Battle、Result、Inventory、Missions 与 Settings 页面。
- 完成 World → Stage → Formation → Battle → Result → Rewards → World（下一关解锁）/ Home / Restart 的离线主线闭环。
- 为 Arena、Events、Shop、Mail、Guild 保留明确锁定入口，不伪造联网、IAP 或服务器奖励。

## 实施边界

- UI 通过 `DemoUiRouter` 与 `AppShellView` 导航；货币、体力、关卡奖励、任务和设置仍由服务/存档层权威处理。
- 三个 `StageDefinition` 使用稳定 ID、顺序前置、体力消耗、推荐战力和奖励数据；战斗继续复用现有确定性 5v5 模拟。
- 存档 schema 升至 v4，并显式迁移 v3；保留旧水晶、角色和五槽阵容，同时补全金币、体力、进度、背包、任务与设置。
- `analysis/` 只读且不是运行时依赖；根目录 XAPK/APK 不读取、不运行、不解包。

## 验收门槛

- [x] Home 与全局资源栏、底部导航、返回栈和手柄/键盘取消路径可用。
- [x] World 显示三关、锁定/开放/通关状态、体力、推荐战力与奖励；1-1 → 1-2 → 1-3 顺序解锁。
- [x] Characters、Recruit、Formation、Inventory、Missions、Settings 进入与返回正常。
- [x] 1-1 Formation → Battle → Result 连续结算体力、首通水晶、金币和 Echo Gel；Restart 再扣体力并只发常规奖励，同局重复结算被守卫拦截。
- [x] 返回 Home 后 Continue 刷新为 1-2，World 的 1-2 节点同步解锁；Boss Void Fragment 已进入结算摘要。
- [x] schema v3 → v4 迁移与关卡前置通过核心验证；任务领取、四项 Settings 持久化和重置确认/取消通过 PlayMode 冒烟。
- [x] PlayMode 完整系统冒烟与 Windows x64 Build 通过，`1600×900` 主页无重叠或裁切。

## 当前证据目录

`Artifacts/FullSystem/`

- `Generate.log`：`[GenericGachaRPG][FULL_SYSTEM_VERIFY_PASS_20260715]`
- `PlaySmokeEconomyFinal.log`：`[FULL_SYSTEM_PLAY_SMOKE_PASS_20260715]`
- `WindowsBuildFinal2.log`：`[GenericGachaRPG][FULL_SYSTEM_VERIFY_PASS_20260715]`、`[GenericGachaRPG][FULL_SYSTEM_WINDOWS_BUILD_PASS_20260715]`；`143,676,760` bytes / `65.4s`
- 完整记录：`MILESTONES/2026-07-15_FULL_SYSTEM_SHELL.md`
- Windows 试玩入口：`Builds/FullSystemWindows/BubbleMind.exe`
