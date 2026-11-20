# 吉伊 · Showcase 资源清单

运行配置为 `Assets/Configs/units.json` 中的 91006。中文名、费用和预载地址均来自该记录。商店首屏第一个报价保留给带模型地址的配置，免费刷新仍从六种棋子中随机抽取。

运行包根目录为 `Assets/Art/Characters/Chiikawa/AnimationSet_V01/`。以下相对路径均从这个目录起算。原项目后续曾在此目录原位更新动画，目录名沿用原名。

| 内容 | 实际路径与用途 |
|---|---|
| 入口 | `Chiikawa_Combat.prefab`，GUID `5acd1f645a93f4c41929acfbf3697379`，Default Local Group，地址 `character/chiikawa` |
| 模型 | `Models/Chiikawa_Model.fbx`，六动作共用一份模型、骨架和武器 |
| 动画组件 | Prefab 的 VisualRoot 上的 Animator 使用 `Animations/Chiikawa_Combat.controller` |
| 材质 | `Materials/Chiikawa_Body.mat`、`Materials/Chiikawa_Face.mat`、`Materials/Chiikawa_WeaponWhite.mat`、`Materials/Chiikawa_WeaponPink.mat` |
| 面部 | `Textures/FaceAtlas.png` 与 `Shaders/ChiikawaCombatFace.shader` |
| 卡面 | `Assets/Art/GameUI/Shop/portrait_91006.png`，ShopCard 的序列化映射引用 |
| 宿主 | `Assets/Prefabs/Unit/UnitPlaceholder.prefab`。显示模型时禁用胶囊 Renderer，保留 Collider 供拖动。名称由外层 Actor 管理。 |

| Clip 路径 | 用途 | 秒 | 循环 |
|---|---|---:|---|
| Animations/Chiikawa_Idle.anim | 待机 | 2.000 | 是 |
| Animations/Chiikawa_Move.anim | 移动 | 0.600 | 是 |
| Animations/Chiikawa_Attack.anim | 普攻 | 0.700 | 否 |
| Animations/Chiikawa_Hit.anim | 受击 | 0.233 | 否 |
| Animations/Chiikawa_Skill.anim | 技能表演资源 | 4.467 | 否 |
| Animations/Chiikawa_Death.anim | 阵亡 | 1.200 | 否 |

角色采用 Unity XZ 地面、+Y 向上。原资源交接记录脸朝 +Z，更新后的向前刺击以角色前方为基准；本次未重新标定武器姿态。备战和上阵时沿棋盘 +Z 面向敌方。战斗出生朝向按阵营设置，敌方沿棋盘 −Z。头顶名称独立朝向相机。武器链使用 WeaponSocket_R → WeaponMount → Weapon，并保留 GripPoint、SupportGrip、WeaponTip 系列与 EffectPoint。没有通用 HeadPoint，头顶名读取实体可见 Renderer 的包围盒。

原资源记录的视觉接触时间是 Attack 0.300 秒、Skill 2.400 秒。SkillImpact 事件在 2.500 秒，SkillFinished 在结尾。这些时间服务动画表现，当前基础战斗由控制器结算伤害。Skill Clip 随控制器保留，本演示没有主动技能入口。

验证从应用入口购买吉伊，检查模型替换胶囊、六种 Clip 引用、上阵和战斗时的头顶字形，核对返回与出售后的 Actor、标签、资源计数和事件监听。动作运行结果见交付记录。素材仅用于本地验收；公开展示形象、录屏以及分发角色模型和贴图的权利仍待权利人确认。


2026-11-20 17:31 (Asia/Shanghai) Unity Play 实测吉伊两场均采样到 Move、Hit、Attack、Idle、Death。六种配置名称均通过逐字网格检查。Skill 未在基础对战中触发。
