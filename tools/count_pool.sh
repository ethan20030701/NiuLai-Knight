#!/bin/bash
# count rarities of cards in a vanilla pool file (arg 1 = pool class file)
usage() {
  echo "Usage: bash tools/count_pool.sh <vanilla-pool-class.cs>" >&2
  echo "Counts vanilla card rarities using decompiled cards under ~/sts2api/sts2/." >&2
  echo "For the Knight's active-card and retirement audit, use python tools/audit_card_values.py." >&2
}
if [ "$#" -ne 1 ]; then
  usage
  exit 2
fi
if [ "$1" = "-h" ] || [ "$1" = "--help" ]; then
  usage
  exit 0
fi
CARDS=~/sts2api/sts2/MegaCrit/Sts2/Core/Models/Cards
POOL="$1"
if [ ! -f "$POOL" ] || [ ! -r "$POOL" ]; then
  echo "Pool source is not a readable file: $POOL" >&2
  usage
  exit 2
fi
grep -oE 'Card<[A-Za-z0-9]+>' "$POOL" | sed 's/Card<//;s/>//' | while read -r name; do
  f="$CARDS/$name.cs"
  if [ -f "$f" ]; then
    grep -oE 'CardRarity\.[A-Za-z]+' "$f" | head -1
  else
    echo "CardRarity.MISSING:$name"
  fi
done | sort | uniq -c
