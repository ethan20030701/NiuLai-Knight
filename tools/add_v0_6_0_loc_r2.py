# -*- coding: utf-8 -*-
"""Append v0.6.0 round-2 localization keys (5 blue + 2 white cards / 1 power / 3 relics, zhs+eng)."""
import json, io, os

ROOT = r"C:\Users\asus\HollowKnight\HollowKnight\localization"

data = {
"zhs": {
"cards.json": {
"HOLLOWKNIGHT-SOUL_BATTERY.title": "蓄魂之池",
"HOLLOWKNIGHT-SOUL_BATTERY.description": "在你的每个回合开始时，获得 {SoulBattery:diff()} 点灵魂。",
"HOLLOWKNIGHT-SOUL_BATTERY.flavor": "静置的池水，也会自己涨起来。",
"HOLLOWKNIGHT-VENOM_BLADE.title": "剧毒之刃",
"HOLLOWKNIGHT-VENOM_BLADE.description": "对目标施加 {Poison:diff()} 层中毒。",
"HOLLOWKNIGHT-VENOM_BLADE.flavor": "刃上的毒，比恨更久。",
"HOLLOWKNIGHT-CATALYST.title": "催化毒液",
"HOLLOWKNIGHT-CATALYST.description": "使目标的中毒层数变为 {Catalyst:diff()} 倍。",
"HOLLOWKNIGHT-CATALYST.flavor": "一滴入锅，满锅沸腾。",
"HOLLOWKNIGHT-BONE_BREAK.title": "断骨重斩",
"HOLLOWKNIGHT-BONE_BREAK.description": "造成 {Damage:diff()} 点伤害，施加 2 层虚弱。",
"HOLLOWKNIGHT-BONE_BREAK.flavor": "这一击不为杀敌，只为终结交换。",
"HOLLOWKNIGHT-SOUL_SIPHON.title": "灵魂虹吸",
"HOLLOWKNIGHT-SOUL_SIPHON.description": "造成 {Damage:diff()} 点伤害，获得 6 点灵魂。",
"HOLLOWKNIGHT-SOUL_SIPHON.flavor": "敌人的精魄，顺着钉刃倒流。",
"HOLLOWKNIGHT-TWIN_SLASH.title": "双钉连斩",
"HOLLOWKNIGHT-TWIN_SLASH.description": "造成 2 次 {Damage:diff()} 点伤害。",
"HOLLOWKNIGHT-TWIN_SLASH.flavor": "一息两斩，步步生魂。",
"HOLLOWKNIGHT-RESIDUAL_SOUL.title": "余魂",
"HOLLOWKNIGHT-RESIDUAL_SOUL.description": "获得 {Soul:diff()} 点灵魂。",
"HOLLOWKNIGHT-RESIDUAL_SOUL.flavor": "漏网的微光，也是光。",
},
"powers.json": {
"HOLLOWKNIGHT-SOUL_BATTERY_POWER.title": "蓄魂之池",
"HOLLOWKNIGHT-SOUL_BATTERY_POWER.description": "在你的每个回合开始时，获得 {Amount} 点灵魂。",
"HOLLOWKNIGHT-SOUL_BATTERY_POWER.smartDescription": "获得 {Amount} 层蓄魂之池。",
},
"relics.json": {
"HOLLOWKNIGHT-SOUL_CANDLE.title": "引魂烛",
"HOLLOWKNIGHT-SOUL_CANDLE.description": "每场战斗开始时，获得 11 点灵魂。",
"HOLLOWKNIGHT-SOUL_CANDLE.flavor": "烛火摇曳，迷途的魂循光而来。",
"HOLLOWKNIGHT-VOID_BRAND.title": "虚空印记",
"HOLLOWKNIGHT-VOID_BRAND.description": "每场战斗开始时，获得 2 层力量。",
"HOLLOWKNIGHT-VOID_BRAND.flavor": "深渊的烙印，是无声的许诺。",
"HOLLOWKNIGHT-HALLOWNEST_SEAL.title": "圣巢徽章",
"HOLLOWKNIGHT-HALLOWNEST_SEAL.description": "你的骨钉攻击额外造成 3 点伤害。",
"HOLLOWKNIGHT-HALLOWNEST_SEAL.flavor": "佩戴它，便是宣告：古老的荣光未死。",
},
},
"eng": {
"cards.json": {
"HOLLOWKNIGHT-SOUL_BATTERY.title": "Soul Battery",
"HOLLOWKNIGHT-SOUL_BATTERY.description": "At the start of each of your turns, gain {SoulBattery:diff()} SOUL.",
"HOLLOWKNIGHT-SOUL_BATTERY.flavor": "Still water rises on its own.",
"HOLLOWKNIGHT-VENOM_BLADE.title": "Venom Blade",
"HOLLOWKNIGHT-VENOM_BLADE.description": "Apply {Poison:diff()} Poison to the target.",
"HOLLOWKNIGHT-VENOM_BLADE.flavor": "The venom on the edge outlasts the hate.",
"HOLLOWKNIGHT-CATALYST.title": "Catalyst",
"HOLLOWKNIGHT-CATALYST.description": "Multiply the target's Poison by {Catalyst:diff()}.",
"HOLLOWKNIGHT-CATALYST.flavor": "One drop in the pot, and the whole pot boils.",
"HOLLOWKNIGHT-BONE_BREAK.title": "Bone Break",
"HOLLOWKNIGHT-BONE_BREAK.description": "Deal {Damage:diff()} damage. Apply 2 Weak.",
"HOLLOWKNIGHT-BONE_BREAK.flavor": "This swing ends the exchange, not the fight.",
"HOLLOWKNIGHT-SOUL_SIPHON.title": "Soul Siphon",
"HOLLOWKNIGHT-SOUL_SIPHON.description": "Deal {Damage:diff()} damage. Gain 6 SOUL.",
"HOLLOWKNIGHT-SOUL_SIPHON.flavor": "Their essence flows back along the nail.",
"HOLLOWKNIGHT-TWIN_SLASH.title": "Twin Slash",
"HOLLOWKNIGHT-TWIN_SLASH.description": "Deal {Damage:diff()} damage twice.",
"HOLLOWKNIGHT-TWIN_SLASH.flavor": "Two cuts in one breath, SOUL in every step.",
"HOLLOWKNIGHT-RESIDUAL_SOUL.title": "Residual Soul",
"HOLLOWKNIGHT-RESIDUAL_SOUL.description": "Gain {Soul:diff()} SOUL.",
"HOLLOWKNIGHT-RESIDUAL_SOUL.flavor": "Even stray light is light.",
},
"powers.json": {
"HOLLOWKNIGHT-SOUL_BATTERY_POWER.title": "Soul Battery",
"HOLLOWKNIGHT-SOUL_BATTERY_POWER.description": "At the start of each of your turns, gain {Amount} SOUL.",
"HOLLOWKNIGHT-SOUL_BATTERY_POWER.smartDescription": "Gain {Amount} Soul Battery.",
},
"relics.json": {
"HOLLOWKNIGHT-SOUL_CANDLE.title": "Soul Candle",
"HOLLOWKNIGHT-SOUL_CANDLE.description": "At the start of each combat, gain 11 SOUL.",
"HOLLOWKNIGHT-SOUL_CANDLE.flavor": "The flame flickers, and wandering souls follow the light.",
"HOLLOWKNIGHT-VOID_BRAND.title": "Void Brand",
"HOLLOWKNIGHT-VOID_BRAND.description": "At the start of each combat, gain 2 Strength.",
"HOLLOWKNIGHT-VOID_BRAND.flavor": "The abyss's mark is a silent promise.",
"HOLLOWKNIGHT-HALLOWNEST_SEAL.title": "Hallownest Seal",
"HOLLOWKNIGHT-HALLOWNEST_SEAL.description": "Your Nail attacks deal 3 additional damage.",
"HOLLOWKNIGHT-HALLOWNEST_SEAL.flavor": "To wear it is to declare: the old glory is not dead.",
},
},
}

for lang, files in data.items():
    for fname, new in files.items():
        path = os.path.join(ROOT, lang, fname)
        with io.open(path, "r", encoding="utf-8-sig") as f:
            d = json.load(f)
        before = len(d)
        dup = [k for k in new if k in d]
        if dup:
            raise SystemExit(f"DUP KEYS in {lang}/{fname}: {dup}")
        d.update(new)
        with io.open(path, "w", encoding="utf-8") as f:
            json.dump(d, f, ensure_ascii=False, indent=4)
            f.write("\n")
        print(f"{lang}/{fname}: {before} -> {len(d)} keys")
print("ALL OK")
