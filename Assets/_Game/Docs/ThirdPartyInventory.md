# Third-Party Inventory

## 2026-07-13 P0 基线

P0 没有下载、安装或引入任何新的第三方工具、插件、模型、贴图、音频、字体或代码。

使用的运行环境仅为项目原本已有的 Unity `6000.5.3f1` 及其已安装包，包括 URP、uGUI、Input System 和 Unity Test Framework。程序化角色、UI、数据、逻辑与文档均在本项目内原创实现。

## 2026-07-14 长期制作工具

| 组件 | 版本/固定值 | 官方来源与许可证 | 用途与项目内路径 | 校验 |
|---|---|---|---|---|
| CoplayDev/unity-mcp | 上游 v10.1.0 / commit `c14de1e6dc01ab42d2bb358730cff954bce0ce6b`；项目补丁版 `10.1.0-project.1` | GitHub；MIT；原许可证保存在嵌入包 `LICENSE` | Unity Editor 集成；源码嵌入 `Packages/com.coplaydev.unity-mcp/`，隔离差异记录于 `UPSTREAM.md` | `manifest.json` 保留精确上游提交；`packages-lock.json` 解析为 `source: embedded` / `file:com.coplaydev.unity-mcp`，嵌入源码优先 |
| mcpforunityserver | `10.1.0` | PyPI / CoplayDev GitHub；MIT | Codex 与 Unity 的 Python MCP Server；由 uvx 运行，缓存位于 `_ProjectTools/uv/cache/` | uvx 使用精确版本约束 `mcpforunityserver==10.1.0` |
| uv | `0.11.28` Windows x64 | Astral GitHub 官方发布；MIT 或 Apache-2.0 | 便携 Python/工具运行器；`_ProjectTools/uv/0.11.28/` | 下载 ZIP SHA-256：`0A23463216D09C6A72FF80EF5DC5A795F07DC1575CB84D24596C2F124A441B7B` |
| CPython | `3.12.13` Windows x64 | uv 官方托管的 Python 发行物；Python Software Foundation License | MCP Server 运行时；`_ProjectTools/uv/python/cpython-3.12.13-windows-x86_64-none/` | 由 uv 版本解析并以 `only-managed` 模式使用 |

## 2026-07-14 首个 UR 角色样板

| 组件/服务 | 版本/接口 | 官方来源与许可/条款 | 用途与路径 | 限制与校验 |
|---|---|---|---|---|
| Blender | `5.1` | https://www.blender.org/；GPL-3.0-or-later | 用户已安装于 `C:\Program Files\Blender Foundation\Blender 5.1\`；用于清理 Tripo 网格、建立独立轨道环并导出 FBX | Codex 未下载或安装；已核对 `blender.exe` 与自带 Python 存在；运行配置/缓存/临时文件必须重定向到 `_ProjectTools/`；不随游戏分发 |
| Tripo OpenAPI | API v2 | https://platform.tripo3d.ai/docs/；受 Tripo Terms 约束 | 仅调用余额接口评估 `ART-CHAR-UR-COSMIC-SLIME-001` 候选路线 | available 0 / frozen 0；没有上传参考图、创建任务、下载产物或消费额度；不安装 SDK、不使用公共图床；未来如使用仍只作内部非商用原型 |

## 2026-07-15 角色卡面与 UI 字体

| 组件/服务 | 版本/固定值 | 官方来源与许可证 | 用途与项目内路径 | 校验 |
|---|---|---|---|---|
| OpenAI GPT Image | 2026-07-15 内置图像生成接口 | OpenAI 内置生成能力；按当前 OpenAI 服务条款使用 | 生成 7 张 BubbleMind 原创史莱姆角色卡面；`Assets/_Game/Art/Generated/UI/Portraits/` | 没有安装 SDK、Unity 包或运行时依赖；逐张 SHA-256 见下表 |
| Noto Sans CJK SC | Sans 2.004 / Regular OTF | `notofonts/noto-cjk` 官方 GitHub；SIL Open Font License 1.1 | 简体中文与拉丁 UI 字体；`Assets/_Game/Resources/Fonts/NotoSansCJKsc-Regular.otf`；许可证保存在 `Assets/_Game/ThirdParty/Fonts/NotoSansCJKsc/OFL.txt` | 字体 SHA-256 `2C76254F6FC379FDDFCE0A7E84FB5385BB135D3E399294F6EEB6680D0365B74B`；OFL SHA-256 `6A73F9541C2DE74158C0E7CF6B0A58EF774F5A780BF191F2D7EC9CC53EFE2BF2` |

### 原创角色卡面登记

| 文件 | 角色方向 | 尺寸 | SHA-256 |
|---|---|---:|---|
| `Portrait_ur_cosmic_slime.png` | Catherine Yuki；近黑星云凝胶坦克、腹部纯黑事件视界、青金轨道环 | 1024×1536 | `B6B24D3F9DEF9CF7743B726671B1B652F24ACE9F4C0CFA8CE7D41DB13EE1D5FC` |
| `Portrait_azure_vanguard.png` | 蓝色水系远程史莱姆、水弓与气泡 | 1003×1568 | `0021F25AFB42E85CAC3A585DF7FADD9EF1D7B552F5312061ADFBC0EAFC90CE37` |
| `Portrait_ember_striker.png` | 珊瑚火系刺客史莱姆、跃动火焰与突袭姿态 | 972×1619 | `F5878967D12B60C39AA7C695B79BCCD339C35FCF210CC5BD3D5634024937D26A` |
| `Portrait_verdant_medic.png` | 薄荷风系治疗史莱姆、叶片与花朵 | 972×1619 | `D836D01BBF38230B3A6FA908826328B3E6CF3C145525C81EF891949D76114BE6` |
| `Portrait_violet_arcanist.png` | 紫色雷系法师史莱姆、金色电弧 | 982×1601 | `21E319BC6774DA48457BB376AE9839B1599E51DBE4262A8389F584B4EB03C5B0` |
| `Portrait_gold_ranger.png` | 琥珀土系射手史莱姆、晶体弓 | 971×1619 | `F37D97320D5D99193CD1DA6D4D2C9C3361CCD0C230F8EC3CBF4366A5A8C08266` |
| `Portrait_cyan_warden.png` | 青色水系坦克史莱姆、盾形冠与水鳍 | 1023×1537 | `511B1B085F105F9161CEA01B0946226187F7057DC3EF6CA28F23EEA0216AB93A` |

所有卡面提示词均限定为 BubbleMind 原创可爱动漫史莱姆、竖版角色卡构图、无文字、无商标、无现有版权角色；未把外部参考游戏截图作为图生图输入，也未复制其角色、服装、图标或 UI 素材。

## 2026-07-15 腾讯混元 3D 内部候选评估

| 组件/服务 | 版本/接口 | 官方来源与许可/条款 | 用途与项目内路径 | 限制与校验 |
|---|---|---|---|---|
| Tencent Hunyuan3D official Space | `tencent/Hunyuan3D-2` / `shape_generation`；服务返回模型标识 `tencent/Hunyuan3D-2/hunyuan3d-dit-v2-0` | [腾讯官方 Space](https://huggingface.co/spaces/tencent/Hunyuan3D-2)、[官方仓库](https://github.com/Tencent-Hunyuan/Hunyuan3D-2)、[2.0 社区许可证](https://github.com/Tencent-Hunyuan/Hunyuan3D-2/blob/main/LICENSE) | 将登记的 Catherine 正面参考图生成无纹理几何候选；请求、原始 GLB、Blender 审计和渲染位于 `_ProjectTools/Hunyuan3D/jobs/job-20260715-catherine-seed929/` | 无账号、无 API Key、零费用；输入 SHA-256 `FC50F8D8E40F53925C7715F9B70E6452E4900420D9D4AF0BBDA7CE5736333437`；输出 SHA-256 `58BCB18098171A2BD5C9A24F37C1E5968F72F1179062EC9F061B82F1B68400FA`；服务端显示 737,060 faces，Blender 导入后为 442,980 triangles；仅内部评估 |
| Tencent Hunyuan3D official multiview Space | `tencent/Hunyuan3D-2mv` / `shape_generation` | [腾讯官方 Space](https://huggingface.co/spaces/tencent/Hunyuan3D-2mv)、[官方仓库](https://github.com/Tencent-Hunyuan/Hunyuan3D-2)、[2.0 社区许可证](https://huggingface.co/tencent/Hunyuan3D-2mv/blob/main/LICENSE) | 完成前视图模型作业与 front/left/back 三视图作业；位于 `_ProjectTools/Hunyuan3D/jobs/job-20260715-catherine-mv-seed929/` 和 `job-20260715-catherine-3view-seed929/` | 输出 SHA-256 分别为 `A5C8C1E40205DB51DF489C6A27B9E0FCF57EBDB5585E6CBB62694F58D7A3E6AE`、`E21805D34B1C7CA5C1C1FB2F24E03D8B985D1B049712928B626FE1905FC3B536`；仅内部评估 |
| Hunyuan3D-2.1 | 官方开源版本 `2.1`，仅研究部署门槛，未安装 | [官方仓库](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1)、[官方模型](https://huggingface.co/tencent/Hunyuan3D-2.1)、[2.1 社区许可证](https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1/blob/main/LICENSE) | 用于核对本地硬件需求和许可证；没有下载权重、Python 包或编译扩展 | 官方需求约为形状 10GB、纹理 21GB、完整 29GB 显存；当前 RTX 4060 8GB 不满足完整流程；腾讯不主张用户生成 Output 的权利，但许可证仍限制 Output/结果的使用且授权地域排除欧盟、英国和韩国；超过 100 万 MAU 还需单独许可 |
| Tencent Cloud HY-3D | 云端最新评估方向 `3.1`，未开通/未调用 | [国际站 API 文档](https://intl.cloud.tencent.com/document/product/1284/75540) | 计划用于正式候选的参考图生成、PBR 与智能拓扑 | 当前没有登录、接受账户条款、提供凭据或消费 credits；预计首轮约 85–95 credits，必须先获用户授权并审查实际服务条款 |

单视图原始网格在 Blender 中为 442,980 triangles / 221,492 vertices，归一化尺寸约 `3.000 × 2.950 × 2.177 m`；项目内工具生成了 9,000 triangles 的自动减面预览，SHA-256 `F305B7A77CE04C1B58DDA91CE772D25B6CC9E02EBAA8736195F60B60629594BA`。它不是生产重拓扑。前视图多视图模型为 474,768 triangles；三视图模型为 489,312 triangles。三组候选均含非空体积，但中央黑洞被误读为凹陷/表面结构，轨道与液滴产生断裂或漂浮碎片，因此没有替换 Unity 权威模型。

本次没有下载或安装 SDK、模型权重、Python 包或 Unity 运行时依赖。项目内 PowerShell 客户端仅调用腾讯官方账号发布、托管于 Hugging Face 的 Space；所有请求和产物都在 `_ProjectTools/Hunyuan3D/`，被 Git 忽略且不进入 Build。

## 隔离声明

- 预期运行路径中，所有下载、缓存、Python、临时文件、AppData 映射和状态文件均位于 `_ProjectTools/`；没有修改系统 PATH 或进行系统级安装。
- 隔离加固前的一次验证意外创建了 `C:\Users\yshaw\AppData\Local\UnityMCP`。该目录未被读取或删除，正等待用户明确授权清理；当前配置已把未来的 `LOCALAPPDATA`、HOME、XDG、TEMP 与 uv 写入全部重定向回项目内。
- 外部资产例外仅限用户明确批准的 `ART-CHAR-UR-COSMIC-SLIME-001` Tripo 评估，以及 2026-07-15 用户明确要求的腾讯官方 Hunyuan3D-2/2mv 内部几何候选评估；没有接入 Meshy、Sketchfab、FAL、公共图床或其他服务。
- Tripo Key 已由此前流程写入用户级环境变量/凭据存储；本项目不保存其值，Codex 不会输出或提交它。该项目外状态的轮换和删除记录在 `StudioOps/DEFERRED_WORK.md`，需要用户另行授权。
- 工具不属于游戏内容资产，因此不写入 `StudioOps/ASSET_LEDGER.csv`。
- `_ProjectTools/` 被 Git 忽略，不随游戏源代码提交，也不包含在 Windows 游戏 Build 中。
