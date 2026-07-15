# BubbleMind First Demo — 试玩入口

首个 `BubbleMind` 垂直切片已经可以在 Unity 或 Windows 独立包中试玩。项目使用 Unity `6000.5.3f1`、URP 和 uGUI，不需要额外下载。

## 最快试玩方法

1. 在 Unity 打开本项目。
2. 按 `F8`，或双击 `Assets/_Game/Scenes/GachaRPGDemo.unity`。
3. 点击 Unity 顶部的 **Play**。
4. 从主页依次体验抽卡、角色收藏、五槽编队和当前 3v5 职业测试战斗。

完整流程：

```text
Home → 单抽 → Collection → Formation → 3v5 Battle → Result → Home / Restart
```

新存档拥有 3,000 Crystals 和五名初始角色。每次单抽消耗 100；`RESET DEMO DATA` 会恢复默认试玩状态。

## Unity 工具菜单

```text
F7  Tools > Generic Gacha RPG > Generate or Repair Demo
F8  Tools > Generic Gacha RPG > Open Demo Scene
    Tools > Generic Gacha RPG > Verify P0 Demo
    Tools > Generic Gacha RPG > Run Automated Play Smoke
F6  Tools > Generic Gacha RPG > Build Windows Demo
```

生成器可以重复运行，会校正七名角色、七份完整内容档案、五套元素基础史莱姆 Prefab、星渊吞噬体 Prefab、柔和漫画星渊观测台、十项技能、抽卡池和 Build Settings，并验证演示场景。验证器使用内存存档，不会污染玩家的 PlayerPrefs。

Windows 独立版已经生成在：

```text
Builds/Windows/BubbleMind.exe
```

请保留 `Builds/Windows` 内的配套文件和文件夹，不要只单独移动 `.exe`。

最终验证记录见 `Assets/_Game/Docs/VerificationReport.md`。

独立包窗口名为 `BubbleMind First Demo`，使用 Direct3D 11，并以 60 FPS 为目标。

## 当前内容

- 七名角色：六名标准角色使用水、火、土、风、雷五套 Blender 基础史莱姆，首位限定 UR 为“星渊吞噬体”。
- 原创柔和漫画风 16:9 战斗地图“星渊观测台”，同时作为首页与实战视觉基准。
- 全局稀有度顺序为 R / SR / SSR / SP / UR；标准池当前包含 R / SR / SSR，首位限定角色为 UR 且不进入标准池。
- 角色主从档案页、`Combat / Archive / Growth` 三种详情模式、严格五人编队，以及角色卡上的攻击距离和移动速度。
- 固定 Tick、Seed 可复现的 3v5 自动战斗；我方固定为 Catherine 坦克、Gold Ranger 射手和 Ember Striker 刺客，敌方为五名测试单位。
- 战场长度统一为 20 格；Tank / Assassin 射程 2 格，其余职业射程 10 格。Ember 的技能 2 在第 5 秒瞬移到敌方后排，第 15 秒再次施放时仍攻击同一存活目标。
- 普攻、怒气、三技能错峰、单体伤害、群体伤害、治疗、死亡、超时和结果页。
- JSON 本地存档、损坏恢复与一键 Reset。

## 内容编辑位置

```text
角色战斗数据    Assets/_Game/Data/Characters/
完整角色档案    Assets/_Game/Data/CharacterProfiles/
技能运行时数据  Assets/_Game/Data/Skills/
抽卡 Banner     Assets/_Game/Data/Gacha/
卡面             Assets/_Game/Art/Generated/UI/Portraits/
```

新增角色和能力的字段、接入顺序与校验规则见：

```text
Assets/_Game/Docs/ContentTemplateSpecification.md
Assets/_Game/Docs/OriginalCharacterCatalog.md
```

## 定位

这是离线、原创、clean-room 的玩法原型。抽卡、货币和奖励均为本地 Demo 数据；没有登录、联网、IAP、真实货币购买或线上服务。
