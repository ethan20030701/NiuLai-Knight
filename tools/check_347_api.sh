for t in CustomEnergyCounter CustomTrailPath CustomRestSiteAnimPath CustomMerchantAnimPath CustomCharacterSelectBg SpireField ConstructedCardModel CustomCardPoolModel CustomRelicModel CustomPowerModel PoolAttribute CustomContentDictionary CanonicalCost SetCanonicalCost WasSpent ExemptFromSpellCostReduction IsDefaultOptional ICustomResourceVisualsHandler ICustomCostVisualsHandler; do
  HITS=$(grep -rl "$t" ~/sts2api/baselib-347/ --include='*.cs' 2>/dev/null | head -1)
  echo "$t => ${HITS:-GONE}"
done
