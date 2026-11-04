#!/bin/bash
CARDS=~/sts2api/sts2/MegaCrit/Sts2/Core/Models/Cards
POOL=~/sts2api/sts2/MegaCrit/Sts2/Core/Models/CardPools/IroncladCardPool.cs
grep -oE 'Card<[A-Za-z0-9]+>' "$POOL" | sed 's/Card<//;s/>//' | while read -r name; do
  f="$CARDS/$name.cs"
  [ -f "$f" ] || continue
  if grep -q 'CardRarity.Basic' "$f"; then echo "BASIC:$name"; fi
  if grep -q 'CardRarity.Ancient' "$f"; then echo "ANCIENT:$name"; fi
done
echo "===SHARED-RELICS==="
bash /tmp/r.sh ~/sts2api/sts2/MegaCrit/Sts2/Core/Models/RelicPools/SharedRelicPool.cs 2>/dev/null || {
  RELICS=~/sts2api/sts2/MegaCrit/Sts2/Core/Models/Relics
  grep -oE 'Relic<[A-Za-z0-9]+>' ~/sts2api/sts2/MegaCrit/Sts2/Core/Models/RelicPools/SharedRelicPool.cs | sed 's/Relic<//;s/>//' | while read -r name; do
    f="$RELICS/$name.cs"
    if [ -f "$f" ]; then grep -oE 'RelicRarity\.[A-Za-z]+' "$f" | head -1; fi
  done | sort | uniq -c
}
