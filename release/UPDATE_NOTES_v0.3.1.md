# v0.3.1 更新公告 / Update Notes

## 中文

**《空洞骑士》角色 Mod v0.3.1 — 内容大扩展**

### 修复
- **修复工坊版本无法加载的问题**（`ReflectionTypeLoadException`）：此前版本是针对旧版 BaseLib 3.4.5 编译的，与工坊当前的 BaseLib 3.4.7 不兼容。现在改为直接对游戏加载的 BaseLib 版本编译，今后不会再出现此类漂移。

### 全新内容
- **9 张新卡**：梦之钉（对精英/Boss 抽更多牌）、深凝（消耗型大治疗）、堕落之怒（低血骨钉强化）、灵魂图腾（耗魂换能量）、织者之歌（每回合生成衍生牌「织者之咬」）、血胆之茧（格挡吸收伤害得灵魂）、磨刀石（临时骨钉强化）、稳固之壳（低血双倍格挡）、钉气冲击（全体骨钉气浪）
- **2 个新遗物**：虚空之心（存满 66 灵魂每回合转化 1 能量）、苏醒之梦（强化聚神）
- 内容规模达到 **35 张卡 + 1 张衍生牌 / 13 个能力 / 11 个遗物**

### 平衡调整
- 大斩 14→16（升级 +6）；深渊尖啸 16→20（升级 +7）
- 衍生攻击（织者之咬）不再触发捕魂者产魂——灵魂依然只来自真正的骨钉
- 修复原版事件「满屋芝士」「药水的未来」对小骑士不可用的问题（卡池覆盖补全）

### 视觉全面翻新
- **自绘小骑士形象全部替换为官方立绘**：战斗形象、选人界面、休息点、商店
- 新增原作死亡动画、魂魄能量球、出牌魂魄轨迹、联机猜拳手势
- 全部 36 张卡面与图标重绘，部分卡面采用游戏内贴图（复仇之魂、织者、灵魂图腾、聚神、梦之钉）

### 其他
- 新增简体中文本地化（自动跟随游戏语言）
- 修复死亡动画与角色重叠、能量数字看不清等问题
- **需要前置 BaseLib (v3.4.7+)**——请先订阅并启用 BaseLib！

---

## English

**The Knight — Hollow Knight Character Mod v0.3.1 — Major Content Update**

### Fixed
- **Fixed the workshop build failing to load** (`ReflectionTypeLoadException`): previous builds targeted the outdated BaseLib 3.4.5 while players run 3.4.7. Builds now compile against the exact BaseLib the game loads.

### New content
- **9 new cards**: Dream Nail (extra card draw vs Elites/Bosses), Deep Focus (one-shot burst heal), Fury of the Fallen (low-HP nail damage), Soul Totem (convert SOUL to Energy), Weaversong (spins Weaver's Bite tokens into your hand each turn), Lifeblood Cocoon (block absorbs damage into SOUL), Whetstone (temporary nail strength), Stalwart Shell (double block when wounded), Nail Shockwave (AOE nail wave)
- **2 new relics**: Void Heart (convert 66 SOUL into Energy each turn), Awakened Dream (supercharges Focus)
- Now **35 cards + 1 token / 13 powers / 11 relics**

### Balance
- Great Slash 14→16 (upgraded +6); Abyss Shriek 16→20 (upgraded +7)
- Shiv tokens (Weaver's Bite) no longer trigger Soul Catcher — SOUL still comes only from real nail strikes
- Fixed vanilla events (Room Full of Cheese, The Future of Potions) not working for The Knight

### Visual overhaul
- **All self-drawn Knight art replaced with official renders**: combat figure, character select, rest site, shop
- Added the original death animation, soul energy orb, card-play soul trail, and multiplayer hand icons
- All 36 card faces and icons redrawn; several now use in-game sprites (Vengeful Spirit, Weaver, Soul Totem, Focus, Dream Nail)

### Other
- Simplified Chinese localization (follows the game's language automatically)
- Fixed death animation overlap and low-contrast energy figures
- **Requires BaseLib (v3.4.7+)** — please subscribe to and enable BaseLib first!
