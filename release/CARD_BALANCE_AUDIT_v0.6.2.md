# v0.6.2 小骑士全卡数值审查（代码级）

- 范围：86 张奖励池卡、2 张基础卡、1 张衍生 Token，共 89 张具体卡类。
- 基线：旧骨钉 1 能量造成 6 伤、命中得 11 魂；多数法术另付 33 魂；魂上限 99。
- 方法：逐张核对费用、目标、基础与升级收益、产魂事件、原版同职能卡和卡组内替代关系。
- 限制：此表是静态审查；条件效果、回合节奏和动画仍需要 Steam 正常启动的实机战斗验证。
- 能量 / 魂列显示当前代码费用；手动支付的魂已单独标注。

## 33 魂门槛复核

下表只计算该来源独立产魂，假设相关攻击全部命中、条件持续满足。

| 来源 | v0.6.1 凑足 33 魂 | v0.6.2 凑足 33 魂 |
|---|---:|---:|
| 蓄魂之池（基础 / 升级） | 17 / 11 回合 | 3 / 2 回合 |
| 灵魂虹吸（基础 / 升级） | 6 / 6 次 | 2 / 1 次 |
| 余魂（基础 / 升级） | 9 / 6 张 | 3 / 2 张 |
| 受难之歌（基础 / 升级） | 11 / 7 次掉血 | 3 / 2 次掉血 |
| 快速劈砍（每次所有段均命中） | 1 张 0 费卡 | 2 张 0 费卡（每张两段） |
| 萨鲁布拉的祝福（持续满血） | 11 回合 | 2 回合 |

## 全卡清单

| 档位 | 卡牌 | 类 | 能量 / 魂 | 类型 | 代码数值 | 效果文本 | 本轮决定 |
|---|---|---|---|---|---|---|---|
| 基础 | 旧骨钉 | `OldNail` | 1 / — | Attack | Damage=6m; 升级 +3m | 造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 基础 | 甲壳 | `Shell` | 1 / — | Skill | Block=5m; 升级 +3m | 获得 {Block:diff()} 点格挡。 | 保留；待实机数据复核 |
| 白 | 蓄能骨钉 | `ChannelledNail` | 1 / — | Attack | Damage=4m, Repeat=3; 升级 +1m | 造成 {Repeat:diff()} 次 {Damage:diff()} 点伤害。 | 升级取消第 4 击；保留 3 击产 33 魂，升级只加每段伤害 |
| 白 | 暗影祠 | `DarkShrine` | 1 / — | Skill | Block=10m, Repeat=5; 升级 +3m/+2m | 获得 {Block:diff()} 点格挡；本回合你打出的下一张法术牌伤害 +{Blessing:diff()}。 | 保留；待实机数据复核 |
| 白 | 梦之钉 | `DreamNail` | 1 / — | Skill | Damage=6m, Cards=2; 升级 +2m/+1m | 造成 {Damage:diff()} 点伤害，抽 1 张牌。对精英或 Boss，改为抽 {Dreams:diff()} 张牌。 | 保留；待实机数据复核 |
| 白 | 跳蚤之巢 | `Flukenest` | 1 / 33 | Attack | Damage=5m; 升级 +1m | 消耗 33 魂。对随机敌人造成 3 次 {Damage:diff()} 点伤害。 | 随机三段伤害 4/5→5/6；与魂涌拉开取舍 |
| 白 | 聚神 | `Focus` | 1 / 33 | Skill | Heal=10m; 升级 +5m | 消耗 33 魂。回复 {Heal:diff()} 点生命值。 | 保留；待实机数据复核 |
| 白 | 大斩 | `GreatSlash` | 2 / — | Attack | Damage=18m; 升级 +6m | 造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 白 | 幼虫优雅 | `GrubberflyElegance` | 1 / — | Power | Power=3m; 升级 +2m | 当你的生命值为满时，骨钉攻击额外造成 {GrubberflyElegance:diff()} 点伤害。 | 保留；待实机数据复核 |
| 白 | 重击 | `HeavyBlow` | 1 / — | Attack | Damage=8m; 升级 +3m | 造成 {Damage:diff()} 点伤害，施加 1 层虚弱。 | 保留；待实机数据复核 |
| 白 | 蛾翼疾闪 | `MothwingDash` | 1 / — | Skill | Block=6m; 升级 +3m | 获得 {Block:diff()} 点格挡，抽 1 张牌。 | 保留；待实机数据复核 |
| 白 | 快速劈砍 | `QuickSlash` | 0 / — | Attack | Damage=3m; 升级 +1m | 造成 2 次 {Damage:diff()} 点伤害。 | 0 费三击→双击，产魂上限由 33 降至 22 |
| 白 | 怒斩 | `RagingNail` | 0 / — | Attack | Damage=6m; 升级 +3m | 造成 {Damage:diff()} 点伤害；将一张此牌的复制加入你的弃牌堆。 | 保留；待实机数据复核 |
| 白 | 余魂 | `ResidualSoul` | 0 / — | Skill | Dynamic=11m; 升级 +11m | 获得 {Soul:diff()} 点灵魂。抽 1 张牌。 | 0 费消耗过牌产魂 4/6→11/22 |
| 白 | 锋锐突袭 | `SharpAssault` | 1 / — | Attack | Damage=BaseDamage; 升级 +2m | 造成 {Damage:diff()} 点伤害，抽 1 张牌；若本回合你已命中敌人 4 次或更多，额外造成 5 点伤害。 | 保留；待实机数据复核 |
| 白 | 壳击 | `ShellSlam` | 1 / — | Attack | CalculationBase=0m, ExtraDamage=1m, CalculatedDamage=Move | 造成等同于你当前格挡值的伤害。 | 保留；待实机数据复核 |
| 白 | 魂弹 | `SoulBolt` | 1 / 33 | Attack | Damage=16m; 升级 +5m | 消耗 33 魂。造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 白 | 魂涌 | `SoulSurge` | 1 / 33 | Attack | Damage=12m; 升级 +3m | 消耗 33 魂。造成 {Damage:diff()} 点伤害，抽 1 张牌。 | 保留；待实机数据复核 |
| 白 | 孢子吐息 | `SporeBreath` | 1 / — | Skill | Block=4m, Dynamic=2m; 升级 +2m/+1m | 获得 {Block:diff()} 点格挡，对目标施加 {Poison:diff()} 层中毒。 | 保留；待实机数据复核 |
| 白 | 稳固之壳 | `StalwartShell` | 1 / — | Skill | Block=6m; 升级 +3m | 获得 {Block:diff()} 点格挡。若你的生命值不高于 50%，再获得一次。 | 保留；待实机数据复核 |
| 白 | 稳固之躯 | `SteadyBody` | 1 / — | Power | Power=4m; 升级 +2m | 回合结束时，获得 {SteadyBody:diff()} 点格挡。 | 保留；待实机数据复核 |
| 白 | 双钉连斩 | `TwinSlash` | 1 / — | Attack | Damage=5m; 升级 +2m | 造成 2 次 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 白 | 磨刀石 | `Whetstone` | 1 / — | Skill | Power=3m; 升级 +1m | 获得 {Whetstone:diff()} 层磨刀石。本回合你的骨钉攻击额外造成伤害。 | 保留；待实机数据复核 |
| 蓝 | 巴德尔之壳 | `BaldurShell` | 1 / — | Skill | Block=8m, Power=4m; 升级 +3m/+2m | 获得 {Block:diff()} 点格挡。本回合内，每当敌人攻击你，向其反弹 {BaldurShell:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 断骨重斩 | `BoneBreak` | 2 / — | Attack | Damage=14m; 升级 +4m | 造成 {Damage:diff()} 点伤害，施加 2 层虚弱。 | 保留；待实机数据复核 |
| 蓝 | 甲壳回响 | `BrokenShell` | 1 / — | Power | Power=4m; 升级 +2m | 每当你的一张牌被消耗，获得 {BrokenShell:diff()} 点格挡。 | 保留；待实机数据复核 |
| 蓝 | 崩壳痛击 | `BrokenVessel` | 1 / — | Attack | HpLoss=SelfDamage, Damage=13m; 升级 +5m | 失去 {HpLoss:diff()} 点生命值，造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 催化毒液 | `Catalyst` | 1 / — | Skill | Dynamic=2m; 升级 +1m | 使目标的中毒层数变为 {Catalyst:diff()} 倍。 | 保留；待实机数据复核 |
| 蓝 | 蓄势待斩 | `ChargedArts` | 1 / — | Power | Power=3m; 升级 +2m | 本回合每打出一张技能牌，你的下一张剑技牌伤害 +{ChargedArts:diff()}（可叠加，打出剑技后清空）。 | 保留；待实机数据复核 |
| 蓝 | 盘绕骨钉 | `CoiledNail` | 2 / — | Power | Power=1m | 你的骨钉攻击额外打击 {CoiledNail:diff()} 次。 | 升级由额外 +2 击改为费用 2→1；避免伤害和产魂双重翻倍 |
| 蓝 | 旋风斩 | `CycloneSlash` | 1 / — | Attack | Damage=8m; 升级 +3m | 对所有敌人造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 冲刺 | `Dash` | 1 / — | Attack | Damage=8m, Block=8m; 升级 +3m/+3m | 造成 {Damage:diff()} 点伤害，获得 {Block:diff()} 点格挡。 | 保留；待实机数据复核 |
| 蓝 | 深凝 | `DeepFocus` | 2 / 33 | Skill | Heal=22m; 升级 +6m | 消耗 33 魂。回复 {Heal:diff()} 点生命值，抽 1 张牌。 | 保留；待实机数据复核 |
| 蓝 | 守护者纹章 | `DefenderCrest` | 1 / — | Power | Power=2m; 升级 +1m | 回合结束时，对所有敌人施加 {DefenderCrest:diff()} 层中毒。 | 保留；待实机数据复核 |
| 蓝 | 荒芜俯冲 | `DesolateDive` | 1 / 33 | Attack | Damage=16m, Block=8m; 升级 +5m/+4m | 消耗 33 魂。造成 {Damage:diff()} 点伤害，获得 {Block:diff()} 点格挡。 | 保留；待实机数据复核 |
| 蓝 | 脆弱之力 | `FragileStrength` | 1 / — | Power | Power=5m; 升级 +2m | 骨钉攻击额外造成 {FragileStrength:diff()} 点伤害。脆弱：每当你失去生命值，失去 1 层力量。 | 保留；待实机数据复核 |
| 蓝 | 堕落之怒 | `FuryOfTheFallen` | 1 / — | Power | Power=4m; 升级 +2m | 获得 {Fury:diff()} 层堕落之怒。 | 保留；待实机数据复核 |
| 蓝 | 格林之子 | `Grimmchild` | 1 / — | Power | Power=4m; 升级 +2m | 回合结束时，对随机敌人造成 {Grimmchild:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 受难之歌 | `GrubsongsMelody` | 1 / — | Power | Power=11m; 升级 +11m | 每当你实际失去生命值，获得 {GrubsongsMelody:diff()} 点灵魂。 | 每次掉血产魂 3/5→11/22，修复升级文案 |
| 蓝 | 嚎叫幽灵 | `HowlingWraiths` | 1 / 33 | Attack | Damage=6m; 升级 +2m | 消耗 33 魂。对所有敌人造成 3 次 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 铿鸣 | `Hum` | 0 / — | Attack | Damage=7m; 升级 +3m | 造成 {Damage:diff()} 点伤害。 | 0 费单击 5/7→7/10，与双击快速劈砍区分 |
| 蓝 | 血胆之茧 | `LifebloodCocoon` | 1 / — | Skill | Block=9m, Power=11m; 升级 +3m/+7m | 获得 {Block:diff()} 点格挡。本回合内，每当格挡吸收伤害，获得 {SoulPerAbsorb:diff()} 魂。 | 格挡吸伤产魂 3/4→11/18 |
| 蓝 | 钉气冲击 | `NailShockwave` | 1 / — | Attack | Damage=12m; 升级 +4m | 对所有敌人造成 {Damage:diff()} 点伤害。此攻击不产生灵魂。 | 8/11→12/16 全体伤害；明确不产魂，与旋风斩分工 |
| 蓝 | 痛楚烙印 | `PainBrand` | 1 / — | Power | Power=1m; 升级 +1m | 每当你受到未被格挡的伤害，获得 {PainBrand:diff()} 层力量。 | 保留；待实机数据复核 |
| 蓝 | 回旋钉 | `RicochetNail` | 1 / — | Attack | Damage=8m; 升级 +3m | 对随机敌人造成 2 次 {Damage:diff()} 点伤害。 | 随机双击每段 5/7→8/11，与定向双钉连斩区分 |
| 蓝 | 吐纳 | `SecondWind` | 1 / — | Skill | Block=5m; 升级 +2m | 消耗你手牌中所有非攻击牌，每张获得 {Block:diff()} 点格挡。 | 保留；待实机数据复核 |
| 蓝 | 影披风 | `ShadeCloak` | 1 / — | Skill | Block=8m, Power=1m; 升级 +4m | 获得 {Block:diff()} 点格挡。本回合内，下一次敌方攻击对你造成 0 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 萨满石 | `ShamanStone` | 1 / — | Power | Power=4m; 升级 +2m | 你的法术额外造成 {ShamanStone:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 蛞蝓之形 | `ShapeOfUnn` | 1 / 33 | Skill | Block=18m, Cards=1; 升级 +6m/+1m | 消耗 33 魂。获得 {Block:diff()} 点格挡，抽 {Draw:diff()} 张牌。 | 33 魂防御过牌：格挡 12/16→18/24 |
| 蓝 | 蓄魂之池 | `SoulBattery` | 1 / — | Power | Power=11m; 升级 +11m | 在你的每个回合开始时，获得 {SoulBattery:diff()} 点灵魂。 | 每回合产魂 2/3→11/22；基础 3 回合、升级 2 回合满足 33 魂门槛 |
| 蓝 | 魂能流转 | `SoulFlow` | 0 / 33 | Skill | Energy=1, Cards=1; 升级 +1m | 消耗 33 魂。获得 1 点能量，抽 {Draw:diff()} 张牌。 | 保留；待实机数据复核 |
| 蓝 | 灵魂虹吸 | `SoulSiphon` | 1 / — | Skill | Damage=4m, Dynamic=22m; 升级 +2m/+11m | 造成 {Damage:diff()} 点伤害，获得 {Soul:diff()} 点灵魂。 | 1 费 4/6 伤害，产魂 6→22/33；升级现在改善产魂 |
| 蓝 | 灵魂图腾 | `SoulTotem` | 1 / 33（手动） | Skill | Energy=3, Cards=1; 升级 +1m | 消耗 33 魂。获得 {Energy:diff()} 点能量，抽 {Draw:diff()} 张牌。 | 手动支付 33 魂记录实际支出，供魂之回响/梦之图腾判定 |
| 蓝 | 魂铃 | `Soulbell` | 1 / — | Power | Power=1m; 升级 +1m | 你每回合打出的第一张法术牌，额外抽 {Soulbell:diff()} 张牌。 | 保留；待实机数据复核 |
| 蓝 | 咒文蓄势 | `Spellcharge` | 1 / — | Power | Power=3m; 升级 +2m | 本回合每打出一张技能牌，你的下一张法术牌伤害 +{Spellcharge:diff()}（可叠加，打出法术后清空）。 | 保留；待实机数据复核 |
| 蓝 | 苦痛荆棘 | `ThornsOfAgony` | 1 / — | Power | Power=3m; 升级 +2m | 当你受到攻击时，向攻击者反弹 {ThornsOfAgony:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 不碎之力 | `UnbreakableStrength` | 1 / — | Power | Power=3m; 升级 +2m | 获得 {UnbreakableStrength:diff()} 层不碎之力。 | 保留；待实机数据复核 |
| 蓝 | 复仇之魂 | `VengefulSpirit` | 1 / 33 | Attack | Damage=20m; 升级 +6m | 消耗 33 魂。造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 蓝 | 剧毒之刃 | `VenomBlade` | 1 / — | Skill | Dynamic=6m; 升级 +3m | 对目标施加 {Poison:diff()} 层中毒。 | 保留；待实机数据复核 |
| 蓝 | 织网 | `WebSpin` | 1 / — | Skill | Dynamic=2m; 升级 +1m | 对所有敌人施加 {Weak:diff()} 层虚弱。 | 保留；待实机数据复核 |
| 蓝 | 残像 | `WispAfterimage` | 1 / — | Power | Power=1m; 升级 +1m | 每当你打出一张牌，获得 {WispAfterimage:diff()} 点格挡。 | 保留；待实机数据复核 |
| 金 | 深渊尖啸 | `AbyssShriek` | 2 / 33 | Attack | Damage=20m; 升级 +4m | 消耗 33 魂。对所有敌人造成 {Damage:diff()} 点伤害；战场上每有一个其他存活敌人，额外 +5 点。 | 保留；待实机数据复核 |
| 金 | 无忧旋律 | `CarefreeMelody` | 1 / — | Power | Power=1m; 升级 +1m | 打出时获得安眠。当你受到致命伤害时，消耗 1 层安眠：保留 1 点生命值，并获得 22 点格挡。 | 保留；待实机数据复核 |
| 金 | 冲刺斩 | `DashSlash` | 1 / — | Attack | Damage=9m; 升级 +3m | 造成两次 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 金 | 黑暗降临 | `DescendingDark` | 2 / 33 | Attack | Damage=9m, Block=12m; 升级 +4m/+4m | 消耗 33 魂。对所有敌人造成 {Damage:diff()} 点伤害和 6 点无视格挡的伤害。在你的下回合开始时获得 {Block:diff()} 点格挡。 | 保留；待实机数据复核 |
| 金 | 梦之盾 | `Dreamshield` | 1 / — | Skill | Power=10m; 升级 +3m | 在接下来的 3 回合内，每回合开始时获得 {Dreamshield:diff()} 点格挡。 | 延迟防御 6/8→10/13，每场三次 |
| 金 | 盈满之魂 | `FullSoul` | 1 / — | Power | Power=7m; 升级 +3m | 打出法术前若持有至少 66 灵魂，该法术额外造成 {FullSoul:diff()} 点伤害。 | 按施法前魂量判定 66 门槛，计入本次实际支付 |
| 金 | 圣巢回响 | `HallownestsEcho` | 2 / 33 | Attack | Damage=26m; 升级 +4m | 消耗 33 魂。对所有敌人造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |
| 金 | 固若金汤 | `Impregnable` | 3 / — | Power | — | 获得壁垒：你的格挡不再于回合结束时消失。 | 费用 2/1→3/2，对齐原版壁垒 |
| 金 | 国魂 | `Kingsoul` | 1 / — | Power | Power=2m; 升级 +1m | 你每持有 33 灵魂，骨钉攻击便额外造成 {Kingsoul:diff()} 点伤害。 | 保留；待实机数据复核 |
| 金 | 淬毒之锋 | `NailEnvenom` | 1 / — | Power | Power=1m; 升级 +1m | 你的骨钉攻击命中时，对目标施加 {NailEnvenom:diff()} 层中毒。 | 保留；待实机数据复核 |
| 金 | 钉乱舞 | `NailFlurry` | X / — | Attack | Damage=6m; 升级 +2m | 造成 {Damage:diff()} 点伤害 X 次（X 为打出的能量）。 | 保留；待实机数据复核 |
| 金 | 骨钉风暴 | `NailStorm` | X / — | Attack | Damage=5m; 升级 +2m | 对所有敌人造成 {Damage:diff()} 点伤害 X 次（X 为打出的能量）。 | 保留；待实机数据复核 |
| 金 | 傲慢印记 | `PridesMark` | 1 / — | Power | Power=1m; 升级 +1m | 「技能蓄力」的收益翻倍（每张技能 +6 伤而非 +3）。每当你打出剑技牌，抽 1 张牌。 | 保留；待实机数据复核 |
| 金 | 百炼升华 | `Refinement` | 3 / — | Skill | — | 升级你牌组中的所有牌。 | 费用 2/1→3/2；稀有池可获得的全牌升级比原版远古卡贵 1 能量 |
| 金 | 暗影之魂 | `ShadeSoul` | 2 / 33 | Attack | Damage=32m; 升级 +8m | 消耗 33 魂。造成 {Damage:diff()} 点伤害并施加 2 层易伤，对所有其他敌人造成 12 点伤害。 | 保留；待实机数据复核 |
| 金 | 魂壳涌现 | `SoulAegis` | 1 / 全部（手动） | Skill | — | 获得等同于你当前灵魂数量的格挡，随后清空灵魂。 | 手动清魂记录实际支出，供魂之回响/梦之图腾判定 |
| 金 | 魂之回响 | `SoulEcho` | 2 / — | Power | Power=1m; 升级 +1m | 每当你打出牌并实际消耗灵魂后，抽 {SoulEcho:diff()} 张牌。 | 仅实际支付魂的卡抽牌；修复 BaseLib WasSpent 对零支出返回 true |
| 金 | 灵魂收割 | `SoulHarvest` | 2 / 33 | Attack | Damage=16m; 升级 +5m | 消耗 33 魂。对所有敌人造成 {Damage:diff()} 点伤害；每击杀一名敌人，回复 5 点生命值。 | 保留；待实机数据复核 |
| 金 | 焚魂契约 | `SoulPact` | 1 / — | Skill | Cards=2; 升级 +1m | 消耗 1 张手牌，抽 {Draw:diff()} 张牌。 | 保留；待实机数据复核 |
| 金 | 魂能激涌 | `SoulRush` | 0 / — | Skill | Energy=1, Cards=2; 升级 +1m | 获得 {Energy:diff()} 点能量，抽 {Draw:diff()} 张牌。 | 保留；待实机数据复核 |
| 金 | 魂之奔流 | `SoulTorrent` | X / 33 | Attack | Damage=10m; 升级 +2m | 消耗 33 魂。对所有敌人造成 {Damage:diff()} 点伤害 X 次（X 为打出的能量）。 | 保留；待实机数据复核 |
| 金 | 灵魂暴君 | `SoulTyrant` | 2 / — | Power | Power=1m | 每当你的法术击杀敌人，获得 1 点能量并抽 1 张牌（每回合最多 2 次）。 | 增加升级费用 2→1；击杀收益只认造成击杀的法术 |
| 金 | 溢魂斩 | `SoulUnbound` | 1 / ≥33，清空 | Attack | Damage=BaseDamage; 升级 +5m | 需至少 33 点灵魂。造成 {Damage:diff()} 点伤害，每有 33 点灵魂额外造成 15 点伤害，随后清空灵魂。 | 清魂记录实际支出；排除骨钉加段与产魂，防止一张牌被重复放大 |
| 金 | 虚空吞噬 | `VoidFeast` | 1 / — | Attack | Damage=12m, MaxHp=3m; 升级 +3m/+1m | 造成 {Damage:diff()} 点伤害；若击杀敌人，最大生命值永久 +{MaxHp:diff()}。 | 保留；待实机数据复核 |
| 金 | 虚空涌动 | `VoidSurge` | 3 / — | Power | Power=2m; 升级 +1m | 在你的每个回合开始时，获得 {VoidSurge:diff()} 层力量。 | 保留；待实机数据复核 |
| 金 | 织者之歌 | `Weaversong` | 1 / — | Power | Power=4m; 升级 +2m | 每回合开始时，将一张织者之咬加入你的手牌。 | 保留；待实机数据复核 |
| 金 | 虚影形态 | `WraithForm` | 3 / — | Power | Dynamic=2m; 升级 +1m | 获得 {Wraith:diff()} 层虚影：你受到的所有伤害降至 1 点。每回合结束时失去 1 层虚影；每回合开始时失去 1 点敏捷。 | 费用 2→3，加入原版每回合 -1 敏捷代价 |
| 衍生 | 织者之咬 | `WeaverBite` | 0 / — | Attack | Damage=0m | 造成 {Damage:diff()} 点伤害。 | 保留；待实机数据复核 |

## 同时核对的遗物与结算

- 萨鲁布拉的祝福：满血回合开始 3→22 魂；噬魂者：每次骨钉额外 4→6 魂，和基础 11 合计 17，两次命中可施法。
- 幼虫之歌 6→11 魂/掉血事件；受难蚀魂 3→6 魂/消耗；生命血之心 3→6 魂/掉血事件。
- 梦之图腾现在检查同一位玩家的实际魂支出，不再把普通牌的 0 支出当作支付。
- 原版对照：壁垒 3/2 能量，虚影形态 3 能量且每回合 -1 敏捷；原版全牌升级为远古卡 2/1 能量。
