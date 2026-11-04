#!/bin/bash
# count rarities of relics in a vanilla relic pool file (arg 1 = pool class file)
RELICS=~/sts2api/sts2/MegaCrit/Sts2/Core/Models/Relics
POOL="$1"
grep -oE 'Relic<[A-Za-z0-9]+>' "$POOL" | sed 's/Relic<//;s/>//' | while read -r name; do
  f="$RELICS/$name.cs"
  if [ -f "$f" ]; then
    grep -oE 'RelicRarity\.[A-Za-z]+' "$f" | head -1
  else
    echo "RelicRarity.MISSING:$name"
  fi
done | sort | uniq -c
