"""Read the Knight's concrete card definitions without loading game assemblies.

Keep card counts, portrait names and constructor parsing shared by source,
art and package audits. Unknown constructor shapes fail with the model name.
"""
from dataclasses import dataclass
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CARD_DIR = ROOT / 'HollowKnightCode/Cards'
RETIRED_CARD_ROOT = ROOT / 'archive'
REWARD_RARITIES = {'Common', 'Uncommon', 'Rare'}


@dataclass(frozen=True)
class CardDefinition:
    name: str
    path: Path
    source: str
    base_type: str
    constructor: str
    card_type: str
    rarity: str

    @property
    def slug(self):
        return re.sub(r'(?<!^)(?=[A-Z])', '_', self.name).lower()

    @property
    def localization_key(self):
        return 'HOLLOWKNIGHT-' + self.slug.upper()

    @property
    def is_reward(self):
        return self.rarity in REWARD_RARITIES


def card_definitions(directory=CARD_DIR):
    definitions = []
    for path in sorted(directory.glob('*.cs')):
        source = path.read_text(encoding='utf-8-sig')
        declaration = re.search(r'public\s+(?:(?:sealed|partial)\s+)*class\s+(\w+)(?:\s*\([^)]*\))?\s*:\s*(\w+)', source)
        if not declaration:
            continue
        name, base_type = declaration.groups()
        constructor = re.search(rf'public\s+{name}\s*\([^)]*\)\s*:\s*base\(([^)]*)\)', source)
        if constructor is None:
            constructor = re.search(rf'class\s+{name}\s*\([^)]*\)\s*:\s*{base_type}\(([^)]*)\)', source)
        if constructor is None:
            raise ValueError('Missing card constructor: ' + name)
        arguments = constructor[1]
        kind = re.search(r'CardType\.(\w+)', arguments)
        if kind is None and base_type != 'SwarmCommandCard':
            raise ValueError('Missing CardType in constructor: ' + name)
        rarity = re.search(r'CardRarity\.(\w+)', arguments)
        if rarity is None:
            raise ValueError('Missing CardRarity in constructor: ' + name)
        definitions.append(CardDefinition(name, path, source, base_type, arguments,
                                          kind[1] if kind else 'Attack', rarity[1]))
    if not definitions and directory == CARD_DIR:
        raise ValueError('No concrete Knight cards found in ' + str(directory))
    names = [card.name for card in definitions]
    if len(set(names)) != len(names):
        raise ValueError('Duplicate concrete card model names')
    return definitions


def active_cards():
    return card_definitions()


def retired_cards():
    """Archived source is an audit record, not a compiled legacy CardModel."""
    retired = [card for directory in sorted(RETIRED_CARD_ROOT.glob('retired_cards_*'))
               if directory.is_dir() for card in card_definitions(directory)]
    names = [card.name for card in retired]
    if len(set(names)) != len(names):
        raise ValueError('Duplicate retired card model names across archives')
    overlap = {card.name for card in retired} & {card.name for card in active_cards()}
    if overlap:
        raise ValueError('Retired models are still active: ' + ', '.join(sorted(overlap)))
    return retired


def retirement_manifest():
    """Every archived card must have one direct, current save replacement."""
    import json
    manifest = {}
    for path in sorted(RETIRED_CARD_ROOT.glob('retired_cards_*/retired_cards.json')):
        entries = json.loads(path.read_text(encoding='utf-8-sig'))
        overlap = manifest.keys() & entries.keys()
        if overlap:
            raise ValueError('Duplicate retirement aliases: ' + ', '.join(sorted(overlap)))
        manifest.update(entries)
    archived = {card.name: card for card in retired_cards()}
    active = {card.name: card for card in active_cards()}
    if manifest.keys() != archived.keys():
        raise ValueError('Archived sources and retirement manifests disagree')
    for name, entry in manifest.items():
        replacement = active.get(entry['replacement_type'])
        if replacement is None:
            raise ValueError('Retired card must map directly to an active card: ' + name)
        if (entry['old_entry'] != archived[name].localization_key
                or entry['replacement_entry'] != replacement.localization_key):
            raise ValueError('Retirement ID mismatch: ' + name)
    return manifest


def portrait_names():
    """Only current models are included in the active art review."""
    return {card.slug for card in card_definitions()}


def retired_portrait_names():
    return {card.slug for card in retired_cards()}
