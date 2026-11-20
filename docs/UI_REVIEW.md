
## 2026-11-20 18:08（Asia/Shanghai） · 战斗结算界面

复用 MatchScene 的 UI 根、BattlePhaseView 与 Noto 中文字体。结算时拉伸为全屏遮罩，在中央色带显示胜利、失败或平局及按任意键返回提示。准备阶段及开战时使用原阶段按钮。

Unity Play Mode 实测了两场自然失败，以及通过测试设置单位生命值产生的胜利与同时阵亡平局。检查了实际标题、提示的中文字形、遮罩边界和静候输入状态。虚拟键盘的空格、B、Enter、左 Shift 均完成返回，四场恢复后检查站位、金币、实体、监听与资源引用。最终日志 Logs/result-acceptance.log 显示 PASS。未验收物理键盘、多分辨率画面及独立 Player。
