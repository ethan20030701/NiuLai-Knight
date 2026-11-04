"""Audit current Knight card values and localization without writing files.

Counts and constructors come from the shared source inventory. Retired source
is reported separately and is never treated as a compiled legacy card model.
"""
from collections import Counter
import json
import re
import sys

from card_inventory import ROOT, card_definitions, retired_cards, retirement_manifest, REWARD_RARITIES
from export_catalog import card_rows, relic_rows, RARITIES


def audit():
    active = card_definitions()
    retired = retired_cards()
    manifest = retirement_manifest()
    aliases = dict(re.findall(r'"([^"]+)"\s*=>\s*ModelDb.GetId<(\w+)>\(\)',
                             (ROOT / 'HollowKnightCode/Compatibility/RetiredCards.cs').read_text(encoding='utf-8-sig')))
    assert aliases == {entry['old_entry']: entry['replacement_type'] for entry in manifest.values()}, 'Save aliases and archives disagree'
    for language in ('zhs', 'eng'):
        localization = json.loads((ROOT / f'HollowKnight/localization/{language}/cards.json').read_text(encoding='utf-8-sig'))
        for model in active:
            for suffix in ('title', 'description'):
                key = model.localization_key + '.' + suffix
                assert key in localization, f'Missing {language} localization: {key}'
        for model in retired:
            assert not any(key.startswith(model.localization_key + '.') for key in localization), f'Retired {model.name} remains in shipped {language} localization'
    rows = card_rows()
    assert {row[7] for row in rows} == {model.name for model in active}, 'Active export and source inventory disagree'
    counts = Counter(model.rarity for model in active)
    assert sum(counts.values()) == len(active)
    rewards = sum(counts[rarity] for rarity in REWARD_RARITIES)
    return active, retired, rows, counts, rewards


def main():
    # Windows may choose GBK for a redirected stdout; card formulas include
    # floor brackets and other Unicode symbols outside that code page.
    sys.stdout.reconfigure(encoding='utf-8')
    active, retired, rows, counts, rewards = audit()
    version = json.loads((ROOT / 'HollowKnight.json').read_text(encoding='utf-8-sig'))['version']
    relics = relic_rows()
    print(f'# 小骑士当前卡牌源码审查 — {version}\n')
    print(f'- 现役 {len(active)} 种：{rewards} 种奖励卡、{counts["Basic"]} 种基础卡、{counts["Token"]} 种衍生卡。')
    print(f'- 另有 {len(retired)} 种退役卡源码留档；没有对应旧 CardModel 编译进当前版本。')
    print(f'- 遗物 {len(relics)} 件；现役稀有度分布：{dict(sorted(counts.items()))}。')
    print('- 卡数、类型和稀有度从当前 C# 源码读取；支持 HollowKnightCard、SwarmCard 与 SwarmCommandCard。')
    print('- 已核对中英文标题/效果键，并解析真实基础/升级变量、灵魂费用和已审阅的动态伤害公式。')
    print('- 此表是静态源码审查，不能替代原生回归、完整对局与胜率测量。\n')
    print('| 卡牌 | 类 | 稀有度 | 类型 | 基础费用 | 基础效果 | 升级费用 | 升级效果 |')
    print('|---|---|---|---|---|---|---|---|')
    for rarity, title, kind, cost, effect, upgraded_cost, upgraded_effect, name in rows:
        print(f'| {title} | {name} | {RARITIES[rarity]} | {kind} | {cost} | {effect} | {upgraded_cost} | {upgraded_effect} |')
    if retired:
        print('\n退役源码记录：' + '、'.join(model.name for model in retired) + '；旧存档使用 ID 迁移。')


if __name__ == '__main__':
    main()
