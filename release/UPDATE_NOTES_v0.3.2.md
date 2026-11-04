# v0.3.2 更新公告 / Update Notes

## 中文

**《空洞骑士》角色 Mod v0.3.2 — 灵魂计数器修复 + 界面美化**

### 修复
- **修复战斗中灵魂（SOUL）计数器不显示的问题**：受 BaseLib 3.4.7 的显示判断逻辑影响，能量球旁的 "0/99" 计数器此前整个战斗全程不可见。现在它会稳定环绕在能量球旁，空魂时显示红色 0/99，骨钉命中后实时跳动并迸发魂魄粒子。

### 界面美化
- 灵魂计数器换上**专属魂魄容器图标**：暗色虚空球身、淡蓝描边、下半部发光的灵魂液体与旋绕魂丝，贴合原作的容器意象
- 计数器数字与卡牌灵魂费用描边统一为**淡魂蓝色**
- 悬停灵魂计数器时会显示**中英双语提示**（灵魂：通过骨钉命中与损失生命聚集；法术与聚神消耗灵魂）
- 修复能量小图标带半透明底色块的问题

> 注：本次为界面修复版本，卡牌与遗物内容与 v0.3.1 一致（35 张卡 + 1 衍生牌 / 13 能力 / 11 遗物）。**需要前置 BaseLib (v3.4.7+)**。

---

## English

**The Knight — Hollow Knight Character Mod v0.3.2 — SOUL Counter Fix + UI Polish**

### Fixed
- **The SOUL counter was invisible in combat**: due to a BaseLib 3.4.7 display quirk, the "n/99" counter never appeared around the energy orb. It now shows reliably — red "0/99" when empty, ticking up with soul particles on every nail hit.

### UI polish
- The counter gets a **dedicated soul vessel icon**: a dark void orb with a pale-blue rim, glowing soul pooled inside and a drifting wisp — true to the vessel imagery
- Counter digits and card SOUL-cost outlines now use a matching **pale soul blue**
- Hovering the counter shows a **bilingual tooltip** (SOUL: gathered by nail strikes and HP loss; spent by spells and Focus)
- Fixed a stray translucent background block on the small energy icons

> Note: UI-fix release only — cards and relics are unchanged from v0.3.1 (35 cards + 1 token / 13 powers / 11 relics). **Requires BaseLib (v3.4.7+)**.
