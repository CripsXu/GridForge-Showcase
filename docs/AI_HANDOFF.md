# Showcase 交接与验收

## 2026-11-20 · Gameplay 展示版与吉伊接入

- **执行 Agent**　Codex GPT
- **模块**　独立 Unity 工程、局内阶段显示、实体头顶名与吉伊动画。
- **原工程状态**　Unity 2022.3.55f1c1，两个构建场景，业务代码属于默认程序集。原来已有商店购买、布阵、自动战斗与场景清理；开战主要依赖 B，结果仅在日志显示，两秒后自动返回。五份业务文件和两个 Package 文件已有本地修改。提取使用当时工作区的实际文件。
- **隔离验证**　同级新目录中创建普通文件，未使用链接或 Git worktree。复制前、初次运行后、吉伊恢复后，原工程可读的 12,843 个文件及 Git HEAD、可读工作区状态的合并 SHA-256 均为 `3861f5edab11bc869f43fdf9f336c71064602191071897176fd1cf5f2ff87a97`。范围排除 Library、Temp、Obj、Logs、.git 内部文件，以及系统拒绝访问的临时目录。全部 Unity 运行明确指定副本路径。原工程交接保持原样。
- **提取依据**　构建场景、入口运行时代码、配置和 Addressables 地址作为根，沿资源 GUID 复制 Prefab、卡面、模型、动画、材质及 .meta，并保留 TMP 运行资源。原工程可解析的本地序列化资源引用在副本的复核中缺失数为 0。正式场景直接沿用。未复制原 .git、历史文档、源美术工程、旧角色包、剧情编辑器、Spine／Live2D、第三方 DLL 或原导入缓存。
- **运行时地址**　Default Local Group 保留 ui/main、ui/shop、ui/bench、config/units、character/chiikawa。子卡片与槽位由父界面引用。工程不需要 Unity MCP 或网络服务来推进 Gameplay。
- **文件与变更**　下表列主要手动改动，目录 `Assets/Scrips` 的既有拼写保留。
- **结果**　Unity 完成导入与编译，实际从 AppScene 打开局内场景。Play 自动化通过吉伊购买、重复购买拦截、屏幕空间拾取与落子、两场战斗和返回、出售。逐字检查六种角色的可见中文网格，转动镜头复核朝向，核对上阵前后为同一名称对象。每次战后检查原实例仍在、金币正确、临时单位与名字清空，逐项比较资源引用计数及事件名单。最后出售后实体及名字均为零。
- **动作证据**　同一真实对局中采样到吉伊 Idle、Move、Attack、Hit、Death，两场均有记录。Skill 资源存在，局内无主动施放入口。提供对局相机截图；它不含屏幕叠加 UI。
- **验证记录**　回归入口 `Assets/Editor/ShowcaseSmoke.cs`，最终本地日志 `Logs/acceptance.log`（日志目录不随 Git 分发）。编译／运行分开检查。Unity 授权服务启动阶段提示账户 access token 不可用，已取得本机授权并执行至 PASS；最终玩法检查未收到 Unity Error、Exception 或 Assert。
- **尚未验证**　独立 Player 打包、物理鼠标端到端、不同屏幕比例的视觉验收、长期性能与素材公开授权。没有推送或创建远端仓库。
- **影响范围**　仅此副本。推荐验收者按 README 完成一次购买吉伊、上阵与返场，重点查看脸部、武器、名字和密集站位可读性。

| 修改位置 | 原问题与改动 |
|---|---|
| Game/Main/MainView.cs、MainController.cs、MainModule.cs；Assets/Prefabs/Main/MainView.prefab | 主菜单带拖拽实验入口，重新打开还会重复加入该入口监听。展示版隐藏旧控件，移除对应打开／关闭调用，保留进入对局的原按钮。 |
| Game/Scene/Battle/BattleSceneRunner.cs、BattlePhaseView.cs；Game/Scene/Match/MatchSceneRoot.cs | 复用局内 UI 根和既有阶段机，增加开战、持久结果、手动返回。准备条件等待场景组件激活。移除编辑器技能试播输入。 |
| Game/Scene/Unit/UnitActor.cs、UnitActorManager.cs、UnitVisual.cs；Assets/Prefabs/Unit/UnitPlaceholder.prefab | 同一 Actor 创建路径按配置生成跟随名字并绑定中文字体；准备阶段转动可视模型展示正脸。 |
| Game/Scene/Battle/BattlePresenter.cs、Game/Scene/Unit/UnitAnimationPlayer.cs | 胶囊着色跳过文字 Renderer。旧表现按每帧位移超过 0.01 单位判定行走，高帧率实测漏播 Move；现在用实际水平位移判定。修复短动作结束检查中的存活条件，使 Hit／Attack 能正确回到移动或待机。战斗伤害、寻敌与胜负算法沿用原实现。 |
| Game/Shop/Controller/ShopController.cs；Assets/Configs/units.json、Assets/Prefabs/Shop/ShopCard.prefab、Assets/Art/Characters/Chiikawa/AnimationSet_V01/ | 完整恢复原 91006 配置、卡面及正式模型依赖。首屏优先展示有模型地址的配置，之后免费刷新维持原抽样规则。 |
| Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset；Packages/manifest.json、packages-lock.json；ProjectSettings/ProjectSettings.asset | 保留五个运行时地址；工程名独立。移除工具依赖和无用命名空间。首次精简包后 Addressables 示例程序集缺少 NUnit，显式加入原工程所用 Test Framework 1.1.33 后编译通过。 |
| .gitignore、README.md、docs/、Assets/Editor/ShowcaseSmoke.cs | 排除本地生成物，新增实际运行说明、架构、面试提纲、角色资源清单及可复跑测试。 |

## 公开前核对

- [ ] 确认吉伊名称、形象、模型、动画、头像及包含吉伊的截图或视频可按拟选范围公开。保留本地演示角色与授予再分发许可是两件事。
- [ ] 确认其余商店插画、棋盘 FBX、原创代码以及任何委托／协作素材的来源和作品集使用范围，再决定仓库许可证。
- [ ] Noto CJK 和 Liberation Sans 已附随源字体的 OFL 文件。TMP 默认 EmojiOne 图集保留原署名说明；需核对图集及 TMP 导入资源的适用分发条款。Unity 包经 manifest/lock 从注册表安装，未打包 Library/PackageCache。
- [ ] 静态检查未发现常见密钥模式、私钥文件、签名文件或项目内 DLL；这不能判定代码的法律归属。公开前检查实际暂存清单，排除所有本机日志、缓存与 UI 测试截图以外的个人文件。大型动画中 Skill 约 98.6 MB，可在决定公开后规划大文件存储。

## 变更历史

| 日期（Asia/Shanghai） | Agent | 交付 |
|---|---|---|
| 2026-11-20 | Codex GPT | 独立工程、商店到重复战斗回归、头顶名称、吉伊模型与动画、作品集文档和公开前核对 |


2026-11-20 17:31 (Asia/Shanghai) Codex GPT 完成交付复核。清除无引用的 TMP 示例后，重新启动编辑器，最终 Logs/final-acceptance.log 无 C# 编译错误，完整回归 PASS。另见 docs/bench.png 中的吉伊正脸与中文名。

## 2026-11-20 17:56（Asia/Shanghai）· 棋子初始朝向

- 执行 Agent：Codex GPT。模块：Showcase 实体表现。移除己方准备模型的 180° 镜头展示旋转，按阵营设置战斗出生朝向。
- 修改文件：Assets/Scrips/Game/Scene/Unit/UnitVisual.cs、UnitActorManager.cs（同目录）、Assets/Scrips/Game/Scene/Battle/BattlePresenter.cs、Assets/Editor/ShowcaseSmoke.cs、docs/bench.png、docs/CharacterResources/Chiikawa_RESOURCE_MANIFEST.md、docs/AI_HANDOFF.md。
- 验证：原编辑器有工程锁，使用相同源码和资源的临时副本编译并运行 Play Mode。断言以己方与敌方格子实测世界方向为准，验证备战、拖拽上阵、双方出生、连续两场返回后的方向和头顶名字。两场通过；实拍备战席已更新。日志 Logs/facing-check.log。
- 尚未验证：独立 Player。变更范围限 Showcase；可从 AppScene 购买吉伊查看。

| 日期 | Agent | 变更 |
|---|---|---|
| 2026-11-20 17:56 | Codex GPT | 修正初始朝向，两场 Play Mode 回归通过 |

## 2026-11-20 18:08（Asia/Shanghai） · 胜负与按键返场

- 执行 Agent：Codex GPT。模块：Showcase 战斗结算 UI。
- 修改：Assets/Scrips/Game/Scene/Battle/BattlePhaseView.cs、BattleSceneRunner.cs（同目录），Assets/Scenes/MatchScene.unity、Assets/Editor/ShowcaseSmoke.cs、README.md、docs/architecture.md、docs/UI_REVIEW.md、docs/AI_HANDOFF.md。
- 文件已保存。复用局内 Canvas，在结算时呈现全屏中文反馈。结果出现后等待新的键盘按下；B 可返回且不会用同一次按下再次开战。
- 验证：隔离副本以 Unity 2022.3.55f1c1 编译和运行四场，两个自然失败及测试构造的胜利、平局均通过，四种按键通过。自动化测试使用批处理专用输入设置，保证虚拟键盘事件进入 Player Loop。日志 Logs/result-acceptance.log。
- 未验证：独立 Player、多分辨率视觉及物理键盘。仅影响 Showcase。下一步可用实际游戏窗口验收字号和遮罩。

| 日期 | Agent | 变更 |
|---|---|---|
| 2026-11-20 18:08 | Codex GPT | 三种结算提示和按任意键返回，四场 Play Mode 通过 |
