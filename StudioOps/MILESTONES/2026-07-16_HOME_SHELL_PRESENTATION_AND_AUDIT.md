# 2026-07-16 主页表现升级与参考审计收尾

> 状态：已完成
> Windows 入口：`Builds/FullSystemWindows/BubbleMind.exe`
> 参考审计：`Artifacts/ReferenceAudit/2026-07-16_session-02/`

## 范围

- 完成参考游戏第二轮只读巡检，保持登录与窗口，不触发消费、领取、购买、保存、战斗或确认类操作。
- 把新增观察转为 clean-room 中性知识，不把专有截图、长文案、角色数值或布局写入 BubbleMind 运行时。
- 保留现有完整离线系统和所有路由/服务契约，升级共享 UI、Home Hub 和 App Shell。

## 实现

- `DemoUiFactory` 新增 Pearl、Ink、Cyan、Coral、Gold、Leaf 等共享视觉令牌，以及可复用的玻璃命令入口。
- Home 改为全幅星渊观测台场景，使用 Recruit、Characters、Formation 三个语义热点与独立 Challenge CTA。
- 卡池状态、收藏进度和五槽状态由 `PlayerState` / `GameDatabase` 动态刷新；抽卡返回主页后不会显示陈旧信息。
- App Shell 改为明亮资源栏和六项底部主导航；Formation、Mail、Menu 在窄横屏保留稳定目标宽度。
- 未修改 `DemoUiRouter`、页面对象名、回调、存档 schema、抽卡服务、战斗模拟、角色数据或关卡结算。

## 参考审计与知识库

- Session 02 证据编号为 0001–0067，Manifest、Navigation 与截图引用完全连续。
- 巡检覆盖角色详情/技能/装备、高专课程资源页、故事玩法目录、Boss/讨伐/选拔规则、无限层级、竞技与防守编队、商店、分支站台和委托。
- 版本化知识库更新为 23 条证据、36 个模块和 92 条关系；BubbleMind 映射继续使用原创名称、信息架构与数值。
- 清洁室会话详情见 `2026-07-16_REFERENCE_GAME_AUDIT_SESSION_02.md`。

## 验证

| 门槛 | 结果 | 证据 |
|---|---|---|
| 编译与核心验证 | 通过 | `Artifacts/HomeShellPresentation/P2Generate.log`：三组 VERIFY PASS |
| PlayMode 全流程 | 通过 | `Artifacts/HomeShellPresentation/P2PlaySmoke.log`：三组 PLAY SMOKE PASS |
| Windows x64 / D3D11 | 通过 | `Artifacts/HomeShellPresentation/WindowsBuildFinal.log`：`143,680,856` bytes / `64.4s` |
| 1920×1080 Home | 通过 | 全幅背景、资源栏、热点、锁定入口、Challenge 与底部导航无重叠或裁切 |
| Character | 通过 | Catherine 卡面、UR/Tank/Limited 标签、属性、Combat/Archive/Growth 与三技能卡均可读 |
| Gacha 2D 结果 | 通过 | 单抽后显示完整 2D 角色卡、稀有度、名称和 NEW 状态；Crystal 精确扣除 100 |
| World | 通过 | 三节点、锁定状态、当前目标、消耗、战力、奖励与 Formation 入口均可读 |
| 动态刷新 | 通过 | 返回 Home 后 Crystal 2,900、Owned 6/7 与任务徽标同步更新 |

验证截图位于 `Artifacts/HomeShellPresentation/screenshots/`：

```text
home_3440x1440.jpg
home_1920x1080.jpg
home_dynamic_after_draw_1920x1080.jpg
characters_catherine_1920x1080.jpg
gacha_2d_card_result_1920x1080.jpg
world_map_1920x1080.jpg
```

## 交接

- BubbleMind 最终试玩版已保持打开并停在 Home，方便用户直接开始。
- 本次视觉 QA 执行了一次本地单抽；演示存档当前为 2,900 Crystal、收藏 6/7、五槽完整。
- 参考游戏窗口仍保持开启且未被改动。
- 下一步继续把 Character、Recruit、Formation、World 与战斗 HUD 迁移到共享主题；正式模型继续放在最后。
