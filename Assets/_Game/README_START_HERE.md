# BubbleMind 完整系统 Demo — 试玩入口

`BubbleMind` 的完整离线系统壳与首章主线闭环已经可以在 Unity 或 Windows 独立包中试玩。项目使用 Unity `6000.5.3f1`、URP 和 uGUI，不需要额外下载。

## 最快试玩方法

1. 在 Unity 打开本项目。
2. 按 `F8`，或双击 `Assets/_Game/Scenes/GachaRPGDemo.unity`。
3. 点击 Unity 顶部的 **Play**。
4. 从主页进入 World、Characters、Recruit、Formation、Inventory、Missions 或 Settings；在 World 选择关卡并完成五槽编队和 5v5 像素战斗。

完整流程：

```text
Home → World → Stage Detail → Formation → 5v5 Pixel Battle → Result
     → Rewards → World (next stage unlocked) / Home / Restart
```

新存档拥有 3,000 Crystals、2,500 Gold、120 Energy 和五名初始角色。每次单抽消耗 100 Crystals；`RESET LOCAL DEMO DATA` 会在二次确认后恢复默认试玩状态。

## Unity 工具菜单

```text
F7  Tools > Generic Gacha RPG > Generate or Repair Demo
F8  Tools > Generic Gacha RPG > Open Demo Scene
    Tools > Generic Gacha RPG > Verify P0 Demo
    Tools > Generic Gacha RPG > Run Automated Play Smoke
F6  Tools > Generic Gacha RPG > Build Windows Demo
```

生成器可以重复运行，会校正七名角色、七份完整内容档案、七张 Point Filter 像素战斗 Sprite、像素星渊观测台、兼容回退 Prefab、十项技能、抽卡池、三关主线和 Build Settings，并验证演示场景。验证器使用内存存档，不会污染玩家的 PlayerPrefs。

Windows 独立版已经生成在：

```text
Builds/FullSystemWindows/BubbleMind.exe
```

请保留 `Builds/FullSystemWindows` 内的配套文件和文件夹，不要只单独移动 `.exe`。

最终验证记录见 `Assets/_Game/Docs/VerificationReport.md`。

独立包窗口名为 `BubbleMind First Demo`，使用 Direct3D 11，并以 60 FPS 为目标。

## 当前内容

- 全局 App Shell：玩家档案、水晶、金币、体力、编队、邮件/设置入口，以及 Home、World、Heroes、Recruit、Inventory、Missions 六项主导航。
- Home、World/Stage Detail、Characters/Hero Archive、Recruit、Formation、Battle/Result、Inventory、Missions、Settings 页面均可进入并返回；Arena、Events、Shop、Mail、Guild 以明确锁定页面保留未来入口。
- Chapter 01 包含 Fracture Gate、Resonance Gallery、Event Horizon 三关；按 1-1 → 1-2 → 1-3 顺序解锁，并显示体力、推荐战力、首通水晶和常规掉落。
- 七名原创角色均有 128×128 Point Filter 像素战斗 Sprite；默认队伍为 Catherine Yuki 主角与 Gold、Ember、Verdant、Violet 四只伙伴史莱姆。
- 原创像素化 16:9 战斗地图“星渊观测台”，由 Unity 的正交镜头、世界 UI、技能 Shader/VFX 和深度站位组成 2.5D 画面。
- 全局稀有度顺序为 R / SR / SSR / SP / UR；标准池当前包含 R / SR / SSR，首位限定角色为 UR 且不进入标准池。
- 角色主从档案页、`Combat / Archive / Growth` 三种详情模式、严格五人编队，以及角色卡上的攻击距离和移动速度。
- 固定 Tick、Seed 可复现的本地 5v5 自动战斗；玩家保存的合法五槽阵容直接参战，敌方为五名测试单位。
- 战场长度统一为 20 格；Tank / Assassin 射程 2 格，其余职业射程 10 格。Ember 的技能 2 在第 5 秒瞬移到敌方后排，第 15 秒再次施放时仍攻击同一存活目标。
- 普攻、怒气、三技能错峰、单体伤害、群体伤害、治疗、死亡、超时和结果页。
- 胜利结算体力、首通水晶、Gold、Echo Gel 和 Boss Fragment；任务可领取水晶/金币，重复抽卡会转换 Universal Shard。
- 同一局只结算一次；Restart 作为新战斗再次消耗体力并发放常规奖励，不重复发首通水晶。返回 Home 后 Continue 会立即指向已解锁的下一关。
- Boss Void Fragment 会显示在 Result 奖励摘要；Recruit 概率说明和 Settings 全屏重置确认层已完成防裁切/防穿透修正。
- schema v4 JSON 本地存档、v3 显式迁移、损坏恢复、背包/主线/任务/设置持久化与确认式 Reset。

## 内容编辑位置

```text
角色战斗数据    Assets/_Game/Data/Characters/
完整角色档案    Assets/_Game/Data/CharacterProfiles/
技能运行时数据  Assets/_Game/Data/Skills/
抽卡 Banner     Assets/_Game/Data/Gacha/
主线关卡        Assets/_Game/Data/Stages/
卡面             Assets/_Game/Art/Generated/UI/Portraits/
```

新增角色和能力的字段、接入顺序与校验规则见：

```text
Assets/_Game/Docs/ContentTemplateSpecification.md
Assets/_Game/Docs/OriginalCharacterCatalog.md
```

## 定位

这是离线、原创、clean-room 的玩法原型。只借鉴“2D 像素角色由现代 3D 引擎承载”的广义技术，不复制任何参考作品的角色、字体、UI、地图、构图、玩法或资产。抽卡、货币和奖励均为本地 Demo 数据；没有登录、联网、IAP、真实货币购买或线上服务。
