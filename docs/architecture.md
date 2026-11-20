# 架构与生命周期

以下路径相对于 `Assets/Scrips/`。源工程中的目录拼写沿用至展示版。

| 源码位置 | 持有的状态与调用关系 |
|---|---|
| BootStrap/GameBootStrap.cs、GameContext.cs | 启动 Addressables，注册并等待配置表加载，建立应用层 UI 与导航，创建 MainModule 并打开主菜单。 |
| BootStrap/GameSceneManager.cs | 用异步门锁串行化导航，Additive 加载 MatchScene，等待 MatchSceneRoot 初始化，切换相机与应用界面。退出局时清理场景并释放 MatchResources。 |
| BootStrap/ModuleRegistry.cs、Framework/Module/ModuleManager.cs | 分开注册应用和局内模块工厂，按需初始化；记录创建顺序，倒序 Dispose。Shop 依赖 Player、Bench 和 Board 模型。 |
| Framework/Resource/ResourceManager.cs、Framework/UI/UIManager.cs | 按地址复用加载任务和引用计数。UIManager 区分关闭与销毁，同一局内重新打开商店可以复用已创建的界面。 |
| Game/Scene/Match/MatchSceneRoot.cs、MatchResources.cs | 每局拥有独立模块容器、事件总线、UI 与 Actor 管理器。模型地址可选，五种基础棋子走胶囊分支，吉伊从 character/chiikawa 预载有骨骼、武器及 Animator 的 Prefab。 |
| Game/Shop/Controller/ShopController.cs | 检查商品是否已售、余额和备战空位。生成实例后扣款、放入备战席并标记已售，再发布购买及备战变更。购买界面直接读回余额。 |
| Game/Scene/Input/PlacementInput.cs、Game/Board/Controller/BoardController.cs | 输入层做相机射线、UI 阻挡、拖动与落点判断。控制器校验己方格与占用，在两个模型间移动相同 UnitInstance，并通知界面与场景。 |
| Framework/Event/EventBus.cs、Game/GameEvents.cs | 用类型化键同步分发。订阅去重，遍历监听快照，聚合异常。购买等操作的数据变更先于广播，因此监听报错时不应重新扣款。 |
| Game/Scene/Battle/BattleSceneRunner.cs、BattlePhaseView.cs | 管理准备、战斗、结果、返回四个阶段；禁用拖拽、隐藏准备 Actor，快照己方站位并追加固定敌人。面板把一次点击交给 Runner 判断。 |
| Game/Battle/BattleUnitFactory.cs、Game/Battle/Controller/BattleController.cs | 从配置建立本场数据。Tick 优先沿用存活目标，否则按距离选择敌人，等距按 BattleId。推进冷却、应用移动与伤害后检查存活数，60 秒超时平局。 |
| Game/Scene/Battle/BattlePresenter.cs、Game/Scene/Unit/UnitActorManager.cs | 战斗使用独立 Actor 字典。Presenter 读取位置、阵营、死亡和攻击序号，更新实体。返回时先解除表现对模型的引用、销毁临时 Actor，再清空战斗并恢复准备实体。 |
| Game/Scene/Unit/UnitActor.cs | Actor 创建后从配置获得中文名，用 Prefab 所绑 Noto SDF 在可见 Renderer 上方创建一个世界空间 TMP 子对象。LateUpdate 对齐活动主相机；移动复用、隐藏与销毁跟随父物体。 |

## 一次购买的三个标识

`ConfigId` 决定价格、名称、生命和攻击参数。购买生成的 `InstanceId` 区分同配置的多个己方棋子，贯穿备战席和棋盘。开战后工厂为每个战斗单位分配 `BattleId`，己方单位的 `SourceInstanceId` 可回溯原棋子。战斗伤害只落在 BattleUnit 上，返回准备阶段无需恢复上一场的血量。

## 开战与返回的顺序

1. Runner 只接受准备阶段的开战请求。无己方棋子时显示提示；有单位时禁用放置输入，通过 BoardPresenter 将格坐标映射为战斗位置。
2. 每帧 Controller 计算，Presenter 随后读取同一模型。UI 不参与选择目标或结算。控制器使用 Unity 的 Vector3 和 Mathf，因此脱离场景对象仍依赖 Unity 数学类型。
3. 战斗结束后显示全屏中文结果，收到新的键盘按键或点击结果界面后清理。Returning 阶段覆盖异步重开商店的等待时间；准备 Actor、放置输入和按钮恢复后才能再次开战。
4. 退出整局与返回准备是不同生命周期。前者会销毁局内 UI 和模块，后者保留购买、金币和站位。两场回归测试比较资源地址对应的引用计数与每个事件的实际监听委托，并检查临时 Actor 与标签的数量。
