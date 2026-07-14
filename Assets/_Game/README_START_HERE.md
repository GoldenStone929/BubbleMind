# BubbleMind First Demo — 试玩入口

首个 `BubbleMind` 垂直切片已经可以在 Unity 或 Windows 独立包中试玩。项目使用 Unity `6000.5.3f1`、URP 和 uGUI，不需要额外下载。

## 最快试玩方法

1. 在 Unity 打开本项目。
2. 按 `F8`，或双击 `Assets/_Game/Scenes/GachaRPGDemo.unity`。
3. 点击 Unity 顶部的 **Play**。
4. 从主页依次体验抽卡、角色收藏、五人编队和 5v5 自动战斗。

完整流程：

```text
Home → 单抽 → Collection → Formation → 5v5 Battle → Result → Home / Restart
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

生成器可以重复运行，会校正七名角色、星渊吞噬体 Prefab、星渊观测台材质、三个技能、抽卡池和 Build Settings，并验证演示场景。验证器使用内存存档，不会污染玩家的 PlayerPrefs。

Windows 独立版已经生成在：

```text
Builds/Windows/GenericGachaRPGDemo.exe
```

请保留 `Builds/Windows` 内的配套文件和文件夹，不要只单独移动 `.exe`。

最终验证记录见 `Assets/_Game/Docs/VerificationReport.md`。

独立包窗口名为 `BubbleMind First Demo`，使用 Direct3D 11，并以 60 FPS 为目标。

## 当前内容

- 七名角色：六名原创程序化占位角色，以及首个 Blender/URP 正式样板“星渊吞噬体”。
- 原创 16:9 战斗地图“星渊观测台”，同时作为首页与实战视觉基准。
- 全局稀有度顺序为 R / SR / SSR / SP / UR；标准池当前包含 R / SR / SSR，首位限定角色为 UR 且不进入标准池。
- 收藏页、严格五人编队，以及角色卡上的攻击距离和移动速度。
- 固定 Tick、Seed 可复现的 5v5 自动战斗；单位持续锁定存活目标，进入自身射程后留在前线，目标死亡后才按当前位置重选。
- 普攻、能量、单体伤害、群体伤害、治疗、死亡、超时和结果页。
- JSON 本地存档、损坏恢复与一键 Reset。

## 定位

这是离线、原创、clean-room 的玩法原型。抽卡、货币和奖励均为本地 Demo 数据；没有登录、联网、IAP、真实货币购买或线上服务。
