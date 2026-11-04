#!/usr/bin/env python3
"""Export readable current card/relic effects from C# values and Chinese text.

This is a source inventory. It reports base/upgrade values before combat
modifiers, and fails on unresolved values instead of silently guessing.
"""
import json
import re
from collections import Counter
from pathlib import Path
from card_inventory import card_definitions, retired_cards

ROOT = Path(__file__).resolve().parents[1]
LOC = ROOT / 'HollowKnight/localization/zhs'
RARITIES = {'Basic': '基础', 'Common': '普通', 'Uncommon': '罕见', 'Rare': '稀有', 'Token': '衍生', 'Starter': '初始'}
TYPES = {'Attack': '攻击', 'Skill': '技能', 'Power': '能力'}
KEYWORDS = {'Exhaust': '消耗', 'Retain': '保留', 'Innate': '固有', 'Ethereal': '虚无'}
ORDER = {'Basic': 0, 'Common': 1, 'Uncommon': 2, 'Rare': 3, 'Token': 4, 'Starter': -1}


def load(name):
    return json.loads((LOC / f'{name}.json').read_text(encoding='utf-8-sig'))


def entry(name):
    return 'HOLLOWKNIGHT-' + re.sub(r'(?<!^)(?=[A-Z])', '_', name).upper()


def atom(text, constants):
    text = text.strip()
    if text.startswith('"') and text.endswith('"'):
        return text[1:-1]
    if text in constants:
        return constants[text]
    if re.fullmatch(r'-?\d+(?:\.\d+)?[mMfF]?', text):
        return float(text.rstrip('mMfF'))
    raise ValueError('Unresolved C# value: ' + text)


def constants_in(source):
    values = {}
    for name, value in re.findall(r'const\s+(?:string|int|decimal)\s+(\w+)\s*=\s*([^;]+);', source):
        values[name] = atom(value, values)
    return values


def number(value):
    return str(int(value)) if isinstance(value, (int, float)) and value == int(value) else str(value)


def soul_payment(expression, constants):
    if expression.strip() == 'Soul.SpellCost':
        source = (ROOT / 'HollowKnightCode/Resources/Soul.cs').read_text(encoding='utf-8-sig')
        return number(constants_in(source)['SpellCost'])
    return number(atom(expression, constants))


def calculated_damage(model_name, source, values, constants):
    """Describe only formulas whose C# multiplier has been explicitly reviewed."""
    base, extra = values['CalculationBase'], values['ExtraDamage']
    if model_name == 'SharpAssault':
        if not re.search(r'HitCounterPower\.HitsThisTurnFor\(card\.Owner\) >= HitsThreshold \? 1m : 0m', source):
            raise ValueError('SharpAssault multiplier changed; review the catalog formula')
        threshold = number(constants['HitsThreshold'])
        return f'{number(base)}（本回合已命中至少 {threshold} 次时为 {number(base + extra)}）'
    if model_name == 'SoulUnbound':
        if ('WithMultiplier(static (card, _) => FullCharges(card.Owner))' not in source
                or not re.search(r'soul\.Amount / Soul\.SpellCost', source)):
            raise ValueError('SoulUnbound multiplier changed; review the catalog formula')
        spell_cost = soul_payment('Soul.SpellCost', constants)
        return f'{number(base)} + {number(extra)} × ⌊当前灵魂 ÷ {spell_cost}⌋'
    raise ValueError('Unreviewed CalculatedDamageVar formula for ' + model_name)


def variables_in(source, constants, model_name):
    values, powers = {}, []
    has_calculated_damage = False
    for kind, generic, args in re.findall(r'new\s+(\w+Var)(?:<([^>]+)>)?\s*\(([^)]*)\)', source):
        parts = [p.strip() for p in args.split(',')]
        if kind == 'CalculatedDamageVar':
            has_calculated_damage = True
            continue
        if kind in {'DynamicVar', 'PowerVar'} or (len(parts) > 1 and kind in {'RepeatVar', 'CardsVar'}):
            name, value = atom(parts[0], constants), atom(parts[1], constants)
        else:
            name, value = kind[:-3], atom(parts[0], constants)
        values[name] = value
        if kind == 'PowerVar':
            powers.append((generic, name))
    upgrades = dict(values)
    for direct, indexed, delta in re.findall(r'DynamicVars(?:\.(\w+)|\[([^]]+)\])\.UpgradeValueBy\(([^)]+)\)', source):
        name = direct or atom(indexed, constants)
        upgrades[name] += atom(delta, constants)
    if has_calculated_damage:
        values['CalculatedDamage'] = calculated_damage(model_name, source, values, constants)
        upgrades['CalculatedDamage'] = calculated_damage(model_name, source, upgrades, constants)
    return values, upgrades, powers


def clean(text):
    text = re.sub(r'\[img\].*?\[/img\]', '灵魂', text)
    text = re.sub(r'\[/?[a-zA-Z][^]]*\]', '', text)
    return text.replace('\n', ' ').replace('|', '\\|')


def render(text, values):
    def replace(match):
        name = match[1]
        if name not in values:
            raise ValueError('Unresolved localization variable: ' + name + ' in ' + text)
        value = values[name]
        return number(value)
    return clean(re.sub(r'\{(\w+)(?::diff\(\))?\}', replace, text))


def card_rows():
    localization, power_loc = load('cards'), load('powers')
    rows = []
    for model in card_definitions():
        source = re.sub(r'//[^\n]*', '', model.source)
        name, args, kind, rarity = model.name, model.constructor, model.card_type, model.rarity
        constants = constants_in(source)
        values, upgraded, powers = variables_in(source, constants, name)
        cost = atom(args.split(',')[0], constants)
        upgraded_cost = cost
        for delta in re.findall(r'EnergyCost\.UpgradeBy\(([^)]+)\)', source):
            upgraded_cost += atom(delta, constants)
        cost, upgraded_cost = str(int(cost)), str(int(upgraded_cost))
        if 'HasEnergyCostX => true' in source:
            cost = upgraded_cost = 'X'
        soul = re.search(r'SetCanonicalCost\(this,\s*([^)]*)\)', source)
        if soul:
            amount = soul_payment(soul[1], constants)
            cost += ' + ' + amount + '魂'
            upgraded_cost += ' + ' + amount + '魂'
        elif 'Soul.TrySpendManual(' in source:
            manual = re.search(r'Soul\.TrySpendManual\([^,]+,\s*[^,]+,\s*([^)]*)\)', source)
            if not manual:
                raise ValueError('Unresolved manual SOUL payment: ' + name)
            amount = soul_payment(manual[1], constants)
            cost += ' + ' + amount + '魂（手动）'
            upgraded_cost += ' + ' + amount + '魂（手动）'
        elif 'Soul.SpendAll(' in source:
            cost += ' + 全部灵魂'
            upgraded_cost += ' + 全部灵魂'
        key = entry(name)
        descriptions = [render(localization[key + '.description'], v) for v in (values, upgraded)]
        if name == 'WeaverBite':
            descriptions = ['造成织者之歌层数对应的伤害（单张基础织者之歌为 4 点，升级版为 6 点）。', '无独立升级效果。']
        for power, variable in powers:
            power_text = power_loc.get(entry(power) + '.description')
            if not power_text:
                continue
            for i, v in enumerate((values, upgraded)):
                explanation = render(power_text, {'Amount': v[variable]})
                if explanation.startswith(descriptions[i]):
                    descriptions[i] = explanation
                elif explanation.rstrip('。') not in descriptions[i]:
                    descriptions[i] += ' 能力效果：' + explanation
        keywords = re.search(r'CanonicalKeywords\s*=>\s*([^;]+);', source)
        keyword_list = re.findall(r'CardKeyword\.(\w+)', keywords[1]) if keywords else []
        label = TYPES[kind] + ('；' + '、'.join(KEYWORDS[k] for k in keyword_list) if keyword_list else '')
        rows.append((rarity, localization[key + '.title'], label, cost, descriptions[0], upgraded_cost, descriptions[1], name))
    rows.sort(key=lambda r: (ORDER[r[0]], r[7]))
    expected = card_definitions()
    assert {row[7] for row in rows} == {model.name for model in expected}, 'Card export omitted a source model'
    return rows


def relic_rows():
    localization = load('relics')
    rows = []
    for path in (ROOT / 'HollowKnightCode/Relics').glob('*.cs'):
        source = path.read_text(encoding='utf-8-sig')
        model = re.search(r'public\s+sealed\s+class\s+(\w+)', source)
        if not model:
            continue
        name = model[1]
        rarity = re.search(r'RelicRarity\.(\w+)', source)[1]
        key = entry(name)
        rows.append((rarity, localization[key + '.title'], clean(localization[key + '.description']), name))
    rows.sort(key=lambda r: (ORDER[r[0]], r[3]))
    assert rows, 'No concrete relic classes found'
    return rows


def main():
    cards, relics = card_rows(), relic_rows()
    retired = retired_cards()
    counts = Counter(r[0] for r in cards)
    version = json.loads((ROOT / 'HollowKnight.json').read_text(encoding='utf-8-sig'))['version']
    proof = ROOT / f'release/PUBLISH_VERIFICATION_{version}.json'
    published = proof.exists() and json.loads(proof.read_text(encoding='utf-8-sig')).get('published') is True
    publication = ('已发布到 [Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3796063046)。'
                   if published else '这是当前源码清单，尚未发布该版本。')
    lines = [
        f'# 小骑士全部卡牌与遗物效果 — {version}', '',
        f'共 **{len(cards)} 种现役卡牌**：{counts["Basic"]} 张基础卡、{counts["Common"] + counts["Uncommon"] + counts["Rare"]} 张奖励池卡、{counts["Token"]} 张衍生牌；**{len(relics)} 件遗物**。' + publication, '',
        '表中数值不计战斗中的力量、增伤、费用折扣或敌方状态。魂表示灵魂；X 表示消耗全部当前能量。每次有效骨钉命中，初始遗物捕魂者提供 11 灵魂；灵魂基础上限 99。', '',
        '新召唤流规则、伙伴特性和连携说明见 [虫群召唤流设计](SUMMONING_DESIGN.md)。', '',
        '## 卡牌', '',
        '| 卡牌 | 稀有度 | 类型 / 关键词 | 基础费用 | 基础效果 | 升级费用 | 升级效果 |',
        '|---|---|---|---|---|---|---|',
    ]
    for rarity, title, label, cost, effect, upgraded_cost, upgraded_effect, _ in cards:
        lines.append(f'| {title} | {RARITIES[rarity]} | {label} | {cost} | {effect} | {upgraded_cost} | {upgraded_effect} |')
    if retired:
        lines += ['', '## 退役卡与旧存档', '',
                  f'另有 {len(retired)} 种退役卡的源码留在归档目录，不再编译为卡牌模型；旧存档通过 ID 迁移读取现役替代卡。', '',
                  '退役模型：' + '、'.join(card.name for card in retired) + '。']
    lines += ['', '## 遗物', '', '| 遗物 | 稀有度 | 效果 |', '|---|---|---|']
    for rarity, title, effect, _ in relics:
        lines.append(f'| {title} | {RARITIES[rarity]} | {effect} |')
    path = ROOT / 'docs/CARD_RELIC_CATALOG.md'
    path.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(path)
    print(f'Exported {len(cards)} active cards and {len(relics)} relics; {len(retired)} archived retired models; active rarity counts: {dict(counts)}')


if __name__ == '__main__':
    main()
