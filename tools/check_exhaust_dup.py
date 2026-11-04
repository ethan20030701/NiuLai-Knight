# -*- coding: utf-8 -*-
"""Regression guard: no card description may hand-write Exhaust (消耗。/Exhaust.)
when the card declares CardKeyword.Exhaust in code — the keyword banner
renders it automatically. Run: python tools/check_exhaust_dup.py"""
import json
import os
import sys
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CODE = os.path.join(REPO, "HollowKnightCode", "Cards")

exhaust = set()
for f in os.listdir(CODE):
    if f.endswith(".cs"):
        c = open(os.path.join(CODE, f), encoding="utf-8").read()
        if "CardKeyword.Exhaust" in c:
            exhaust.add(f[:-3].upper())

bad = 0
for lang in ("zhs", "eng"):
    path = os.path.join(REPO, "HollowKnight", "localization", lang, "cards.json")
    data = json.load(open(path, encoding="utf-8"))
    for k, v in data.items():
        if not k.endswith(".description"):
            continue
        cid = k.replace("HOLLOWKNIGHT-", "").replace(".description", "")
        if cid in exhaust and ("消耗。" in v or "Exhaust." in v):
            print(f"DUPLICATE [{lang}] {k}")
            bad += 1
print("FAIL" if bad else "OK: no duplicate Exhaust in any description")
sys.exit(1 if bad else 0)
