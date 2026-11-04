# v0.6.7 — 卡池收束、虫群重做与战斗反馈

2026-11-04已发布到工坊，ManifestID `8497751261094219862`，服务器回下载四文件哈希一致。内容汇总此前的虫群、构筑衔接、去重及卡池整理候选。

## 简体中文

- 卡池集中到**骨钉、灵魂、虫群召唤**三条主线，防御、消耗和低血作为配合工具。相对已发布的 v0.6.6，由 104 种卡牌／101 张奖励牌缩减为 **83 种卡牌／80 张奖励牌**：22 普通、37 罕见、21 稀有，另有 2 种基础牌和 1 种衍生牌；保留 25 件专属遗物。本轮结构整理退役 18 张，加上此前去重 3 张，共退役 21 张。中毒体系、永久囤积格挡转伤害支线及部分重复、泛用成长牌退出奖励池。
- 重做 17 张虫群关联牌的构筑配合：普通池提供召唤与指令入口，织丝连袭负责过牌，虫群追击奖励指令顺序，伙伴生命可以转化为攻击或防御。灵魂反哺、破茧续生提供牺牲与重建；魂丝哺育连接织者指令、实际灵魂支付及伙伴成长。
- 格林之子能力现在召唤并强化实体伙伴，织者之歌也立即召唤织者。格林成熟只强化回合末火焰，不增强指令。发光子宫消耗 8 灵魂，格林仪式消耗 22 灵魂；同种伙伴最多一只，生命上限仍为 24。
- 新开局为 4 旧骨钉、3 甲壳、织者之巢、聚神、复仇之魂，共 10 张。蓄能骨钉改为 2 能量、三段 5／6 伤害；暗影祠提供 6／9 格挡及 11 灵魂；蛾翼疾闪在打出时持有至少 66 灵魂则多抽一张。骨钉每次有效命中通过初始遗物获得 11 灵魂，多数法术基础消耗 33 灵魂，灵魂基础上限 99。
- 新增七种原创合成短音效，覆盖骨钉、灵魂、格挡、聚神、受伤、虚空和能力；补充随实际命中触发的骨钉、灵魂、俯冲、虚空特效，以及聚神和防御反馈。音效跟随游戏音量设置。构筑衔接和反馈借鉴对大黄蜂模组的研究，未打包其代码或媒体。
- 补齐旧卡 ID 迁移、虫群公共提示及灵魂阈值文字，强化可选音画功能失败时的隔离处理。

**旧存档：**21 个旧卡 ID 会映射到现役替代卡，但采用替代卡的费用和效果。例如魂能激涌 → 魂能流转、虚空吞噬 → 灵魂收割均引入基础 33 灵魂付款；中毒和壁垒牌组的玩法会改变。建议开始新一局体验重做后的构筑。完整替代表见 [卡池调整说明](../docs/CARD_POOL_2026-10-28.md)。

需要 **BaseLib v3.4.7 或更新版本**。支持简体中文和英文。完整爬塔、胜率及所有旧战斗存档恢复尚未穷尽验证，本说明不宣称全面平衡或所有游戏版本兼容。

## English

- Focuses the card pool on **Nail, SOUL and Swarm** builds, with defense, Exhaust and low-HP synergies connecting them. Compared with published v0.6.6, content goes from 104 card types / 101 reward cards to **83 card types / 80 reward cards**: 22 Common, 37 Uncommon and 21 Rare, plus 2 basic types and 1 token. All 25 exclusive relics remain. This combines 18 newly retired cards with 3 earlier duplicate removals, for 21 retirements. The Poison package, permanent Block-to-damage branch and selected redundant or generic scaling cards leave the pool.
- Reworks the synergies of 17 companion-related cards: accessible summons, commands that draw cards, command-sequence payoffs, HP-based attacks and defense, sacrifice and recovery. Soul Nourishment links Weaverling commands and actual SOUL spending to companion growth.
- Grimmchild and Weaversong now summon real companions. Grimmchild's maturity strengthens only its end-of-turn flame, not commands. Glowing Womb costs 8 SOUL and Grimm Rite costs 22. Each species has one living companion, with a 24-HP cap.
- The new 10-card starter deck contains 4 Old Nails, 3 Shells, Weaver Nest, Focus and Vengeful Spirit. Channelled Nail now costs 2 Energy for three hits of 5/6; Dark Shrine grants 6/9 Block and 11 SOUL; Mothwing Dash draws an extra card when played with at least 66 SOUL. The starter relic grants 11 SOUL per valid Nail hit, most spells cost 33 SOUL, and the base cap remains 99.
- Adds seven original synthesized sound effects and hit-timed Nail, SOUL, dive and Void visuals, plus Focus and guard feedback. Audio follows the game's volume settings. Build structure and feedback were informed by studying the Hornet mod; its code and media are not included.
- Includes retired-card ID migration, companion tooltip and SOUL-threshold text fixes, and improved isolation of optional audiovisual failures.

**Existing saves:** 21 retired card IDs map to current replacements, which use their own costs and effects. Soul Rush → Soul Flow and Void Feast → Soul Harvest both introduce a base 33-SOUL payment. Old Poison and Barricade decks will play differently. A new run is recommended for the redesigned builds.

Requires **BaseLib v3.4.7 or newer**. English and Simplified Chinese are available. Full-run balance, win rates and every old combat-save state have not been exhaustively tested; this note does not claim universal game-version compatibility.
