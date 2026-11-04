# 魂量数字采用能量费用字体样式 — v0.6.6-test.9

用户指出深色33与魂量图标的眼洞重叠，明确要求参考能量1的浅色字与深色厚描边。

- 直接复用当前原生卡牌 `%EnergyLabel` 的字体，取代系统无衬线字体。
- 正常费用使用游戏原生暖白色数字，描边采用当前卡池的能量费用描边颜色。
- 字号22，描边8；减费、费用不足与加费保留游戏原生绿/红/蓝颜色及对应深色描边。
- 原作 Soul Meter 底图维持95%不透明，居中位置、原生次级费用槽位和 test.8 层级修复保留。

正式版0.107.1和本地beta0.111.0各95项实际卡牌界面检查，覆盖21张扣魂卡及放大查看、切牌、升级、隐藏、减费和资源不足。核对实际渲染，数字浅色填充在深色眼洞上可辨。

预览：`art_review/soul_cost_energy_style_closeup.png`、`soul_cost_energy_style_inspect_beta.png`。
安装校验：`release/LOCAL_SOUL_ENERGY_STYLE_VERIFICATION_2026-10-13.json`。
