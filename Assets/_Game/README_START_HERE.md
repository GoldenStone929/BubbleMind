# Generic Gacha RPG Demo — 试玩入口

P0 垂直切片已经可以在 Unity 中试玩。项目使用 Unity `6000.5.3f1`、URP 和 uGUI，不需要额外下载。

## 最快试玩方法

1. 在 Unity 打开本项目。
2. 按 `F8`，或双击 `Assets/_Game/Scenes/GachaRPGDemo.unity`。
3. 点击 Unity 顶部的 **Play**。
4. 从主页依次体验抽卡、角色收藏、三人编队和 3v3 自动战斗。

完整流程：

```text
Home → 单抽 → Collection → Formation → 3v3 Battle → Result → Home / Restart
```

新存档拥有 3,000 Crystals 和三名初始角色。每次单抽消耗 100；`RESET DEMO DATA` 会恢复默认试玩状态。

## Unity 工具菜单

```text
F7  Tools > Generic Gacha RPG > Generate or Repair Demo
F8  Tools > Generic Gacha RPG > Open Demo Scene
    Tools > Generic Gacha RPG > Verify P0 Demo
    Tools > Generic Gacha RPG > Run Automated Play Smoke
F6  Tools > Generic Gacha RPG > Build Windows Demo
```

生成器可以重复运行，会校正六名角色、三个技能、抽卡池和 Build Settings，并验证演示场景。验证器使用内存存档，不会污染玩家的 PlayerPrefs。

Windows 独立版已经生成在：

```text
Builds/Windows/GenericGachaRPGDemo.exe
```

请保留 `Builds/Windows` 内的配套文件和文件夹，不要只单独移动 `.exe`。

最终验证记录见 `Assets/_Game/Docs/VerificationReport.md`。

## 当前内容

- 六名原创程序化角色，包含 Guardian、Striker 和 Support。
- R / SR / SSR 单抽展示、概率明细、余额扣除、解锁和重复记录。
- 收藏页和严格三人编队。
- 固定 Tick、Seed 可复现的 3v3 自动战斗。
- 普攻、能量、单体伤害、群体伤害、治疗、死亡、超时和结果页。
- JSON 本地存档、损坏恢复与一键 Reset。

## 定位

这是离线、原创、clean-room 的玩法原型。抽卡、货币和奖励均为本地 Demo 数据；没有登录、联网、IAP、真实货币购买或线上服务。
