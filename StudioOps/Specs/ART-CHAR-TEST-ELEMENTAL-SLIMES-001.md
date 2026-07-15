# Asset Spec：`ART-CHAR-TEST-ELEMENTAL-SLIMES-001 五元素基础史莱姆`

> 状态：已通过内部原型验收
> 用途：标准池测试单位、五槽编队与固定 3v5 战斗基础美术基准
> 最后更新：2026-07-14

## 视觉目标

- 水、火、土、风、雷五系共享同一圆润、低重心的基础史莱姆比例，保持柔和漫画与轻量 3D 动画质感。
- 水使用蓝色水滴与气泡；火使用珊瑚橙火焰；土使用青绿胶体、岩块与叶片；风使用薄荷色卷曲气流；雷使用暖金主体与紫色电弧。
- 五系通过主体轮廓和元素装饰区分，不把色相作为唯一识别手段。
- 眼睛、瞳孔与高光在固定三分之四战斗镜头下仍须清楚；不使用写实折射、透明胶体或高金属材质。

## GPT Image 设定基准

- 图像：`Assets/_Game/Art/Source/Characters/BasicElementSlimes/BasicElementSlimes_ConceptSheet.png`
- 方式：用户明确要求后，通过 Codex 内置 GPT Image 生成；未使用外部下载素材。
- 构图：1672×941，无文字五列角色表；每列包含正面与侧面，并在顶部给出独立元素符号。
- 提示意图：统一比例的柔和漫画史莱姆模型表，水/火/土/风/雷五种清晰轮廓，正侧视、浅色干净背景、无文字、无 UI、无既有 IP 标志。
- SHA-256：`53D925A76F1FD2163066FADF0654FDA82C5CED25AC9E0AE1693057173D500FB9`
- 权利状态：项目定向生成的原创内部原型；商业发布前统一复核生成服务条款与项目权利链。

## Blender 生产契约

- 生成目录：`Assets/_Game/Art/Generated/BasicElementSlimes/`
- 保留一份可重复生成的 Blender 5.1 源场景和 Python 生成/审计脚本；五系分别导出独立 FBX。
- 每个 FBX 必须包含 `SlimeBody`、可读面部与元素装饰；装饰使用稳定的 `Element_*`、`Bubble_*`、`Flame_*`、`Rock_*`、`Leaf_*`、`Wind_*` 或 `Spark_*` 命名。
- 底部 pivot 接地，应用正缩放，法线朝外；Unity 中约占 `1.15 × 1.0 × 1.2 m`，单体不超过 2,500 triangles。
- 材质使用不透明、低金属、低光滑的色块表现；每系主体、强调色、脸部与高光材质槽命名稳定。

## Unity 映射

| 稳定角色 ID | 元素 | Prefab |
|---|---|---|
| `azure_vanguard` | 水 | `PF_BasicSlime_Water` |
| `cyan_warden` | 水 | `PF_BasicSlime_Water` |
| `ember_striker` | 火 | `PF_BasicSlime_Fire` |
| `gold_ranger` | 土 | `PF_BasicSlime_Earth` |
| `verdant_medic` | 风 | `PF_BasicSlime_Wind` |
| `violet_arcanist` | 雷 | `PF_BasicSlime_Lightning` |

- 保留原角色 ID、显示名、技能、数值、稀有度与卡池权重，避免存档迁移。
- 每个 Prefab 根节点有 `CharacterView` 与 `BasicSlimeVisualController`，模型位于独立 `ModelRoot`；正常战斗不调用程序化人形回退。
- 元素本轮只作为视觉分类，不修改伤害、治疗、射程、速度、目标选择或卡池逻辑。

## 验收

- [x] GPT Image 五元素正侧视设定表已生成并登记
- [x] Blender 五系模型、预览与几何审计全部通过；单体 1,064–1,324 triangles
- [x] 五个 FBX 与五个 Unity Prefab 通过生成器和验证器
- [x] 当前 3v5 为一名限定 UR 与七名基础元素史莱姆；五槽收藏/编队继续保留
- [x] PlayMode、Windows Build、1920×1080 与 1280×720 视觉核验通过
