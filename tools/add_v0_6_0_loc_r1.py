# -*- coding: utf-8 -*-
"""Append v0.6.0 round-1 localization keys (8 gold cards / 1 power, zhs+eng)."""
import json, io, os

ROOT = r"C:\Users\asus\HollowKnight\HollowKnight\localization"
IMG = "res://HollowKnight/images/charui/soul_icon_text.png"

def soul(t):
    return f"消耗 33 [img]{IMG}[/img]。{t}"

def soul_en(t):
    return f"Spend 33 [img]{IMG}[/img]. {t}"

data = {
"zhs": {
"cards.json": {
"HOLLOWKNIGHT-NAIL_STORM.title": "骨钉风暴",
"HOLLOWKNIGHT-NAIL_STORM.description": "对所有敌人造成 {Damage:diff()} 点伤害 X 次（X 为打出的能量）。",
"HOLLOWKNIGHT-NAIL_STORM.flavor": "钉影如暴风，席卷圣巢。",
"HOLLOWKNIGHT-IMPREGNABLE.title": "固若金汤",
"HOLLOWKNIGHT-IMPREGNABLE.description": "获得壁垒：你的格挡不再于回合结束时消失。",
"HOLLOWKNIGHT-IMPREGNABLE.flavor": "壳已不破，心亦不摇。",
"HOLLOWKNIGHT-SOUL_AEGIS.title": "魂壳涌现",
"HOLLOWKNIGHT-SOUL_AEGIS.description": "获得等同于你当前灵魂数量的格挡，随后清空灵魂。",
"HOLLOWKNIGHT-SOUL_AEGIS.flavor": "满溢之魂，化作最后之壁。",
"HOLLOWKNIGHT-HALLOWNESTS_ECHO.title": "圣巢回响",
"HOLLOWKNIGHT-HALLOWNESTS_ECHO.description": soul("对所有敌人造成 {Damage:diff()} 点伤害。"),
"HOLLOWKNIGHT-HALLOWNESTS_ECHO.flavor": "古老的国度，仍记得它的守护者。",
"HOLLOWKNIGHT-VOID_FEAST.title": "虚空吞噬",
"HOLLOWKNIGHT-VOID_FEAST.description": "造成 {Damage:diff()} 点伤害；若击杀敌人，最大生命值永久 +{MaxHp:diff()}。",
"HOLLOWKNIGHT-VOID_FEAST.flavor": "深渊从不安眠，也从不饱足。",
"HOLLOWKNIGHT-SOUL_PACT.title": "焚魂契约",
"HOLLOWKNIGHT-SOUL_PACT.description": "消耗 1 张手牌，抽 {Draw:diff()} 张牌。",
"HOLLOWKNIGHT-SOUL_PACT.flavor": "舍一得一，契约公平。",
"HOLLOWKNIGHT-SOUL_ECHO.title": "魂之回响",
"HOLLOWKNIGHT-SOUL_ECHO.description": "每当你消耗灵魂后，抽 {SoulEcho:diff()} 张牌。",
"HOLLOWKNIGHT-SOUL_ECHO.flavor": "付出多少，回响多少。",
"HOLLOWKNIGHT-REFINEMENT.title": "百炼升华",
"HOLLOWKNIGHT-REFINEMENT.description": "升级你牌组中的所有牌。",
"HOLLOWKNIGHT-REFINEMENT.flavor": "百炼之后，无一凡铁。",
},
"powers.json": {
"HOLLOWKNIGHT-SOUL_ECHO_POWER.title": "魂之回响",
"HOLLOWKNIGHT-SOUL_ECHO_POWER.description": "每当你消耗灵魂后，抽 {Amount} 张牌。",
"HOLLOWKNIGHT-SOUL_ECHO_POWER.smartDescription": "获得 {Amount} 层魂之回响。",
},
},
"eng": {
"cards.json": {
"HOLLOWKNIGHT-NAIL_STORM.title": "Nail Storm",
"HOLLOWKNIGHT-NAIL_STORM.description": "Deal {Damage:diff()} damage to ALL enemies X times (X = energy spent).",
"HOLLOWKNIGHT-NAIL_STORM.flavor": "Nail-shadows sweep Hallownest like a storm.",
"HOLLOWKNIGHT-IMPREGNABLE.title": "Impregnable",
"HOLLOWKNIGHT-IMPREGNABLE.description": "Gain Barricade: your Block no longer expires at the end of your turn.",
"HOLLOWKNIGHT-IMPREGNABLE.flavor": "The shell holds; so does the will.",
"HOLLOWKNIGHT-SOUL_AEGIS.title": "Soul Aegis",
"HOLLOWKNIGHT-SOUL_AEGIS.description": "Gain Block equal to your current SOUL, then lose all SOUL.",
"HOLLOWKNIGHT-SOUL_AEGIS.flavor": "A brimming vessel makes the final wall.",
"HOLLOWKNIGHT-HALLOWNESTS_ECHO.title": "Hallownest's Echo",
"HOLLOWKNIGHT-HALLOWNESTS_ECHO.description": soul_en("Deal {Damage:diff()} damage to ALL enemies."),
"HOLLOWKNIGHT-HALLOWNESTS_ECHO.flavor": "The old kingdom still remembers its guardian.",
"HOLLOWKNIGHT-VOID_FEAST.title": "Void Feast",
"HOLLOWKNIGHT-VOID_FEAST.description": "Deal {Damage:diff()} damage. If this kills, permanently gain {MaxHp:diff()} Max HP.",
"HOLLOWKNIGHT-VOID_FEAST.flavor": "The abyss never sleeps, and is never full.",
"HOLLOWKNIGHT-SOUL_PACT.title": "Soul Pact",
"HOLLOWKNIGHT-SOUL_PACT.description": "Exhaust 1 card from your hand. Draw {Draw:diff()} card(s).",
"HOLLOWKNIGHT-SOUL_PACT.flavor": "Give one, gain two — a fair contract.",
"HOLLOWKNIGHT-SOUL_ECHO.title": "Soul Echo",
"HOLLOWKNIGHT-SOUL_ECHO.description": "Whenever you spend SOUL, draw {SoulEcho:diff()} card(s).",
"HOLLOWKNIGHT-SOUL_ECHO.flavor": "What you give comes echoing back.",
"HOLLOWKNIGHT-REFINEMENT.title": "Refinement",
"HOLLOWKNIGHT-REFINEMENT.description": "Upgrade every card in your deck.",
"HOLLOWKNIGHT-REFINEMENT.flavor": "After a hundred forgings, no iron remains common.",
},
"powers.json": {
"HOLLOWKNIGHT-SOUL_ECHO_POWER.title": "Soul Echo",
"HOLLOWKNIGHT-SOUL_ECHO_POWER.description": "Whenever you spend SOUL, draw {Amount} card(s).",
"HOLLOWKNIGHT-SOUL_ECHO_POWER.smartDescription": "Gain {Amount} Soul Echo.",
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
