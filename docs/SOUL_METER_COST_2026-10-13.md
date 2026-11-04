# 原作主灵魂容器费用图标 — v0.6.6-test.5

用户要求用原作 HUD 中最大的主灵魂容器替代水滴。

- 直接采用原始 `Soul_Meter.png`，未重绘。来源：https://hollowknight.wiki/w/Soul；原图：https://cdn.wikimg.net/en/hkwiki/images/6/63/Soul_Meter.png。
- 素材无改动复制到 `HollowKnight/images/charui/soul_meter.png`。源文件与成品 SHA256 为 `21f22eef6ffa51d7c03bf1203b5d90652e99b4199819d5a699f8da7b10158fce`。
- 保留储君星辉式的左侧次级费用位置，42×44 容器内显示 40×40 原图。费用数字位于上半部，避开下方眼睛。
- 正常数字为深蓝灰色，灵魂不足时为红色，减费仍遵循游戏的绿色提示。图标本身不变红。
- 此前预览将魂量设为 0 用来检查不足颜色，所以显示了红色 24。新版保存正常状态和不足状态两张预览，默认展示魂量充足的状态。

卡牌数值、支付和战斗动作沿用 test.3 战斗基线。当前修改只影响费用图标和对比度。
