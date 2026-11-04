using System.Reflection;
using System.Text.Json;
using Godot;
using HarmonyLib;
using BaseLib.Abstracts;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Compatibility;
using HollowKnight.HollowKnightCode.Powers;
using HollowKnight.HollowKnightCode.Resources;
using HollowKnight.HollowKnightCode.Summons;
using HollowKnight.HollowKnightCode.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using Knight = HollowKnight.HollowKnightCode.Character.HollowKnight;

[ModInitializer(nameof(Initialize))]
public static class HKAudit
{
    static readonly PlayerChoiceContext Context = new ThrowingPlayerChoiceContext();
    static readonly List<object> Results = [];
    static CombatState State = null!;
    static Player Player = null!;
    static Creature Enemy = null!;
    static int Failed;

    // Independently specified expectations: do not derive these from the
    // production alias table, or a missing migration would disappear here too.
    public static (string RetiredType, string Entry, CardModel Replacement)[] ExpectedRetiredCards() =>
    [
        ("SoulBolt", "HOLLOWKNIGHT-SOUL_BOLT", ModelDb.Card<VengefulSpirit>()),
        ("Hum", "HOLLOWKNIGHT-HUM", ModelDb.Card<QuickSlash>()),
        ("HallownestsEcho", "HOLLOWKNIGHT-HALLOWNESTS_ECHO", ModelDb.Card<AbyssShriek>()),
        ("BoneBreak", "HOLLOWKNIGHT-BONE_BREAK", ModelDb.Card<HeavyBlow>()),
        ("FragileStrength", "HOLLOWKNIGHT-FRAGILE_STRENGTH", ModelDb.Card<UnbreakableStrength>()),
        ("NailFlurry", "HOLLOWKNIGHT-NAIL_FLURRY", ModelDb.Card<NailStorm>()),
        ("SporeBreath", "HOLLOWKNIGHT-SPORE_BREATH", ModelDb.Card<DarkShrine>()),
        ("Catalyst", "HOLLOWKNIGHT-CATALYST", ModelDb.Card<Whetstone>()),
        ("DefenderCrest", "HOLLOWKNIGHT-DEFENDER_CREST", ModelDb.Card<ThornsOfAgony>()),
        ("VenomBlade", "HOLLOWKNIGHT-VENOM_BLADE", ModelDb.Card<WebSpin>()),
        ("NailEnvenom", "HOLLOWKNIGHT-NAIL_ENVENOM", ModelDb.Card<CoiledNail>()),
        ("ShellSlam", "HOLLOWKNIGHT-SHELL_SLAM", ModelDb.Card<Dash>()),
        ("Impregnable", "HOLLOWKNIGHT-IMPREGNABLE", ModelDb.Card<Dreamshield>()),
        ("WispAfterimage", "HOLLOWKNIGHT-WISP_AFTERIMAGE", ModelDb.Card<BrokenShell>()),
        ("WraithForm", "HOLLOWKNIGHT-WRAITH_FORM", ModelDb.Card<ShadeCloak>()),
        ("VoidSurge", "HOLLOWKNIGHT-VOID_SURGE", ModelDb.Card<Kingsoul>()),
        ("Refinement", "HOLLOWKNIGHT-REFINEMENT", ModelDb.Card<SoulPact>()),
        ("VoidFeast", "HOLLOWKNIGHT-VOID_FEAST", ModelDb.Card<SoulHarvest>()),
        ("RagingNail", "HOLLOWKNIGHT-RAGING_NAIL", ModelDb.Card<QuickSlash>()),
        ("SoulRush", "HOLLOWKNIGHT-SOUL_RUSH", ModelDb.Card<SoulFlow>()),
        ("NailShockwave", "HOLLOWKNIGHT-NAIL_SHOCKWAVE", ModelDb.Card<CycloneSlash>())
    ];

    public static async Task Initialize()
    {
        TestMode.IsOn = true;
        if (OS.GetCmdlineUserArgs().Contains("--knight-visual-preview"))
        {
            await KnightVisualPreview.Run();
            return;
        }
        try
        {
            SaveManager.Instance.InitSettingsDataForTest();
            LocManager.Initialize();
            var assemblyInfo = typeof(ModelDb).Assembly.GetTypes().FirstOrDefault(t => t.Name == "AssemblyInfo");
            assemblyInfo?.GetMethod("Init")?.Invoke(null, null);
            var init = typeof(ModelDb).GetMethod("Init")!;
            init.Invoke(null, init.GetParameters().Length == 0 ? null : new object?[] { null });
            ModelIdSerializationCache.Init(); ModelDb.InitIds();
            MessageTypes.Initialize(); ActionTypes.Initialize();
            if (OS.GetCmdlineUserArgs().Contains("--knight-combat-hud-preview"))
            {
                await CombatHudPreview.Run();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--knight-soul-cost-design-preview"))
            {
                await SoulCostDesignPreview.Run();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--knight-card-ui-preview"))
            {
                await CardUiPreview.Run();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--knight-swarm-preview"))
            {
                await SwarmPreview.Run();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--knight-hornet-study-preview"))
            {
                await HornetStudyPreview.Run();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--knight-cleanup-preview"))
            {
                await HornetStudyPreview.Run();
                return;
            }
            await Run();
        }
        catch (Exception e) { Failed++; Results.Add(new { name = "setup", passed = false, error = e.ToString() }); }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "..", "audit-results.json"),
            JsonSerializer.Serialize(new { failed = Failed, results = Results }, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print($"HK AUDIT COMPLETE: {Results.Count} checks, {Failed} failed");
        ((SceneTree)Engine.GetMainLoop()).Quit(Failed == 0 ? 0 : 1);
    }

    static async Task Check(string name, Func<Task> run)
    {
        try { await run(); Results.Add(new { name, passed = true }); }
        catch (Exception e) { Failed++; Results.Add(new { name, passed = false, error = e.ToString() }); }
    }
    static void Equal(decimal expected, decimal actual)
    {
        if (expected != actual) throw new Exception($"expected {expected}, actual {actual}");
    }
    static async Task Run()
    {
        Player = MegaCrit.Sts2.Core.Entities.Players.Player.CreateForNewRun<Knight>(UnlockState.all, 1);
        LocalContext.NetId = Player.NetId;
        var run = RunState.CreateForTest([Player], seed: "HKAUDIT");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService(), shouldSave: false);
        State = new CombatState(runState: run);
        State.AddPlayer(Player);
        Enemy = State.CreateCreature(ModelDb.Monster<Toadpole>().ToMutable(), CombatSide.Enemy, null);
        State.AddCreature(Enemy);
        Enemy.SetMaxHpInternal(10000); Enemy.SetCurrentHpInternal(10000);
        CombatManager.Instance.SetUpCombat(State);
        var progress = typeof(CombatManager).GetProperty("IsInProgress")!;
        if (progress.SetMethod != null) progress.SetValue(CombatManager.Instance, true);
        else
        {
            var turn = typeof(CombatManager).GetField("_turnState", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(CombatManager.Instance)!;
            turn.GetType().GetProperty("IsInProgress")!.SetValue(turn, true);
        }
        await Hook.BeforeCombatStart(run, State);
        await Check("OldNail: 6 damage and 11 SOUL", async () =>
        {
            var hp = Enemy.CurrentHp; Soul.Gain(Player, -99);
            var card = await Play<OldNail>(); Equal(6, hp - Enemy.CurrentHp); Equal(11, CustomResources<Soul>.Get(Player.PlayerCombatState!).Amount);
            if (card.Pile?.Type != PileType.Discard) throw new Exception("Old Nail did not finish in discard pile");
        });
        await Check("ChannelledNail: two energy, three hits, 15/18 damage and 33 SOUL", async () =>
        {
            foreach (var upgraded in new[] { false, true })
            {
                var hp = Enemy.CurrentHp; Soul.Gain(Player, -99);
                var card = await Play<ChannelledNail>(upgraded: upgraded);
                Equal(2, card.EnergyCost.Canonical); Equal(3, card.DynamicVars.Repeat.IntValue);
                Equal(upgraded ? 18 : 15, hp - Enemy.CurrentHp); Equal(33, SoulAmount());
            }
        });
        await Check("Mothwing Dash: 0/65/66/99 SOUL draw boundary, unchanged by upgrade and no payment", async () =>
        {
            foreach (var upgraded in new[] { false, true })
            foreach (var amount in new[] { 0, 65, 66, 99 })
            {
                foreach (var handCard in Player.PlayerCombatState!.Hand.Cards.ToArray())
                    await CombatApi.Exhaust(Context, handCard);
                await ClearBlock(); SeedDraw(); Soul.Gain(Player, -99); Soul.Gain(Player, amount);
                var card = await Play<MothwingDash>(upgraded: upgraded);
                Equal(1, card.EnergyCost.Canonical); Equal(66, card.DynamicVars["SoulThreshold"].IntValue);
                Equal(1, card.DynamicVars["BonusCards"].IntValue);
                Equal(amount >= 66 ? 2 : 1, Player.PlayerCombatState.Hand.Cards.Count);
                Equal(upgraded ? 9 : 6, Player.Creature.Block); Equal(amount, SoulAmount());
                if (CustomResources<Soul>.CanonicalCost(card) != -1)
                    throw new Exception("Mothwing Dash must not acquire a SOUL cost");
            }
            foreach (var handCard in Player.PlayerCombatState!.Hand.Cards.ToArray())
                await CombatApi.Exhaust(Context, handCard);
            await ClearBlock();
        });
        await Check("Dark Shrine: 22 to 33 SOUL unlocks Vengeful Spirit, blessing pays once and SOUL caps at 99", async () =>
        {
            foreach (var upgraded in new[] { false, true })
            {
                await ClearBlock(); Soul.Gain(Player, -99); Soul.Gain(Player, 22);
                var shrine = await Play<DarkShrine>(upgraded: upgraded);
                Equal(upgraded ? 9 : 6, Player.Creature.Block); Equal(33, SoulAmount());
                Equal(11, shrine.DynamicVars["SoulGain"].IntValue);
                Equal(upgraded ? 4 : 3, Player.Creature.GetPower<ShrineBlessingPower>()!.Amount);
                var hp = Enemy.CurrentHp; await Play<VengefulSpirit>(soulBefore: null);
                Equal(upgraded ? 24 : 23, hp - Enemy.CurrentHp); Equal(0, SoulAmount());
                if (Player.Creature.HasPower<ShrineBlessingPower>())
                    throw new Exception("The first attack spell did not consume Shrine Blessing");
                Soul.Gain(Player, 33); hp = Enemy.CurrentHp; await Play<VengefulSpirit>(soulBefore: null);
                Equal(20, hp - Enemy.CurrentHp); Equal(0, SoulAmount());
            }
            Soul.Gain(Player, 88);
            for (int i = 0; i < 3; i++) { await Play<DarkShrine>(); Equal(99, SoulAmount()); }
            await PowerCmd.Remove(Player.Creature.GetPower<ShrineBlessingPower>()!);
            await ClearBlock();
        });
        await Check("Soul Vessel: real 66 SOUL threshold agrees with Chinese and English descriptions", async () =>
        {
            var relic = (SoulVessel)ModelDb.Relic<SoulVessel>().ToMutable(); Player.AddRelicInternal(relic);
            try
            {
                Equal(Soul.SpellCost * 2, SoulVessel.Threshold);
                foreach (var amount in new[] { SoulVessel.Threshold - 1, SoulVessel.Threshold, Soul.MaxSoul })
                {
                    await ClearBlock(); Soul.Gain(Player, -99); Soul.Gain(Player, amount);
                    await relic.BeforeSideTurnEnd(Context, Player.Creature.Side, [Player.Creature]);
                    Equal(amount >= SoulVessel.Threshold ? SoulVessel.BlockOnHold : 0, Player.Creature.Block);
                    Equal(amount, SoulAmount());
                }
                foreach (var language in new[] { "zhs", "eng" })
                {
                    LocManager.Instance.SetLanguage(language);
                    var text = new LocString("relics", "HOLLOWKNIGHT-SOUL_VESSEL.description").GetFormattedText();
                    var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d+")
                        .Select(match => int.Parse(match.Value)).ToArray();
                    if (!numbers.SequenceEqual(new[] { SoulVessel.Threshold, SoulVessel.BlockOnHold }))
                        throw new Exception($"Soul Vessel text disagrees with its rules in {language}: {text}");
                }
            }
            finally { LocManager.Instance.SetLanguage("eng"); Player.RemoveRelicInternal(relic, silent: true); await ClearBlock(); }
        });
        await Check("Grimmchild and Weaversong retain the shared swarm rule and their own power tips", () =>
        {
            try
            {
                foreach (var language in new[] { "zhs", "eng" })
                {
                    LocManager.Instance.SetLanguage(language);
                    foreach (var canonical in new CardModel[] { ModelDb.Card<Grimmchild>(), ModelDb.Card<Weaversong>() })
                    foreach (var upgraded in new[] { false, true })
                    {
                        var card = State.CreateCard(canonical, Player); if (upgraded) card.UpgradeInternal();
                        var tips = ((IEnumerable<IHoverTip>)card.GetType()
                            .GetProperty("ExtraHoverTips", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(card)!).ToArray();
                        var power = card is Grimmchild ? ModelDb.Power<GrimmchildPower>() : (PowerModel)ModelDb.Power<WeaversongPower>();
                        if (!tips.Any(tip => tip.Id.Contains("HOLLOWKNIGHT-SWARM.description"))
                            || !tips.Any(tip => tip.Id == power.Id.ToString()))
                            throw new Exception($"Missing swarm or power tip on {card.GetType().Name}, {language}, upgraded={upgraded}");
                        if (tips.Any(tip => tip.Id.Contains("HOLLOWKNIGHT-SWARM_COMMAND.description")))
                            throw new Exception("Summon powers must not claim to be companion commands");
                    }
                }
                return Task.CompletedTask;
            }
            finally { LocManager.Instance.SetLanguage("eng"); }
        });
        await Check("Weaver growth and three-hit command", async () =>
        {
            await Play<WeaverNest>(); await Play<WeaverNest>();
            var pet = KnightSwarm.Find<WeaverlingPet>(Player)!; Equal(12, pet.CurrentHp); Equal(12, pet.MaxHp);
            var hp = Enemy.CurrentHp; await Play<WeaverVolley>(); Equal(9, hp - Enemy.CurrentHp);
            Equal(3, KnightSwarm.State(Player)!.CommandsThisTurn);
        });
        await Check("Hatchling death explosion and resurrection", async () =>
        {
            await Play<GlowingWomb>(); var hp = Enemy.CurrentHp;
            var pet = KnightSwarm.Find<HatchlingPet>(Player)!;
            await CreatureCmd.Kill(pet);
            Equal(9, hp - Enemy.CurrentHp);
            await Play<BroodReturn>(); Equal(6, KnightSwarm.Find<HatchlingPet>(Player)!.CurrentHp);
        });
        await Check("Grimm maturity caps at two", async () =>
        {
            for (int i = 0; i < 4; i++) await Play<GrimmRite>();
            Equal(2, KnightSwarm.Find<GrimmkinPet>(Player)!.GetPower<GrimmMaturityPower>()!.Amount);
        });
        await Check("Soul spell and card exhaust adapters", async () =>
        {
            await Play<SoulSurge>();
            var card = State.CreateCard<Shell>(Player);
            Player.PlayerCombatState!.Hand.AddInternal(card);
            await CombatApi.Exhaust(Context, card);
            if (card.Pile?.Type != PileType.Exhaust) throw new Exception("Card did not exhaust");
        });
        await Check("Nail charge: previews and all hits, no later payout", async () =>
        {
            await Play<ChargedArts>(); await Play<CoiledNail>(); await Play<Shell>();
            var power = Player.Creature.GetPower<ChargedArtsPower>()!;
            var art = State.CreateCard<CycloneSlash>(Player);
            for (int i = 0; i < 2; i++) Equal(3, power.KnightDamageAdditive(Enemy, 8, ValueProp.Move, Player.Creature, art, null));
            var hp = Enemy.CurrentHp; await Play<CycloneSlash>(); Equal(22, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<CycloneSlash>(); Equal(16, hp - Enemy.CurrentHp);
            await PowerCmd.Remove(power); await PowerCmd.Remove(Player.Creature.GetPower<CoiledNailPower>()!);
        });
        await Check("Spell charge: all three waves, then consumed", async () =>
        {
            await Play<Spellcharge>(); await Play<Shell>();
            var hp = Enemy.CurrentHp; await Play<HowlingWraiths>(); Equal(27, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<HowlingWraiths>(); Equal(18, hp - Enemy.CurrentHp);
            await PowerCmd.Remove(Player.Creature.GetPower<SpellchargePower>()!);
        });
        await Check("Nailmaster's Glory: preview never consumes first-art bonus", async () =>
        {
            var relic = (NailmastersGlory)ModelDb.Relic<NailmastersGlory>().ToMutable(); Player.AddRelicInternal(relic);
            var art = State.CreateCard<CycloneSlash>(Player);
            for (int i = 0; i < 2; i++) Equal(8, relic.KnightDamageAdditive(Enemy, 8, ValueProp.Move, Player.Creature, art, null));
            await Play<CoiledNail>();
            var hp = Enemy.CurrentHp; await Play<CycloneSlash>(); Equal(32, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<CycloneSlash>(); Equal(16, hp - Enemy.CurrentHp);
            Player.RemoveRelicInternal(relic, silent: true); await PowerCmd.Remove(Player.Creature.GetPower<CoiledNailPower>()!);
        });
        await Check("Abyss Shriek: single enemy gives no other-enemy bonus", async () =>
        {
            var hp = Enemy.CurrentHp; await Play<AbyssShriek>(); Equal(20, hp - Enemy.CurrentHp);
        });
        await Check("SOUL cost is paid and spells never gather nail SOUL", async () =>
        {
            await Play<VengefulSpirit>(); Equal(66, CustomResources<Soul>.Get(Player.PlayerCombatState!).Amount);
        });
        await Check("Full SOUL: previews and three waves use the pre-payment balance", async () =>
        {
            await PowerCmd.Apply<FullSoulPower>(Context, Player.Creature, 4, Player.Creature, null);
            Soul.Gain(Player, 99);
            var spell = State.CreateCard<HowlingWraiths>(Player);
            Equal(4, Player.Creature.GetPower<FullSoulPower>()!.KnightDamageAdditive(Enemy, 6, ValueProp.Move, Player.Creature, spell, null));
            var hp = Enemy.CurrentHp; await Play<HowlingWraiths>(66); Equal(30, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<HowlingWraiths>(65); Equal(18, hp - Enemy.CurrentHp);
            await PowerCmd.Remove(Player.Creature.GetPower<FullSoulPower>()!);
        });
        await Check("Swarm intercept: block first, overkill reaches owner", async () =>
        {
            await Play<GlowingWomb>(); var pet = KnightSwarm.Find<HatchlingPet>(Player)!;
            await ClearBlock();
            await CreatureCmd.GainBlock(Player.Creature, 5, ValueProp.Move, null);
            var hp = Player.Creature.CurrentHp; var petHp = pet.CurrentHp;
            await CombatApi.Damage(Context, Player.Creature, 9, ValueProp.Move, Enemy);
            Equal(hp, Player.Creature.CurrentHp); Equal(petHp - 4, pet.CurrentHp); Equal(0, Player.Creature.Block);
            await CombatApi.Damage(Context, Player.Creature, pet.CurrentHp + 3, ValueProp.Move, Enemy);
            Equal(hp - 3, Player.Creature.CurrentHp); Equal(0, pet.CurrentHp);
        });
        await Check("Shade Cloak: fully blocked hits preserve charge, one dodge and Sharp Shadow", async () =>
        {
            foreach (var pet in KnightSwarm.Living(Player)) await CreatureCmd.Kill(pet);
            if (KnightSwarm.State(Player) is { } guard) await PowerCmd.Remove(guard);
            var relic = ModelDb.Relic<SharpShadow>().ToMutable(); Player.AddRelicInternal(relic);
            await ClearBlock();
            await PowerCmd.Apply<ShadeCloakPower>(Context, Player.Creature, 1, Player.Creature, null);
            await CreatureCmd.GainBlock(Player.Creature, 5, ValueProp.Move, null);
            var hp = Player.Creature.CurrentHp;
            await CombatApi.Damage(Context, Player.Creature, 5, ValueProp.Move, Enemy);
            Equal(1, Player.Creature.GetPower<ShadeCloakPower>()!.Amount);
            var enemyHp = Enemy.CurrentHp;
            await CombatApi.Damage(Context, Player.Creature, 10, ValueProp.Move, Enemy);
            Equal(hp, Player.Creature.CurrentHp); Equal(9, enemyHp - Enemy.CurrentHp);
            if (Player.Creature.HasPower<ShadeCloakPower>()) throw new Exception("Dodge was not consumed");
            await CombatApi.Damage(Context, Player.Creature, 5, ValueProp.Move, Enemy);
            Equal(hp - 5, Player.Creature.CurrentHp); Player.RemoveRelicInternal(relic, silent: true);
        });
        await Check("Carefree Melody: fatal damage leaves 1 HP and 22 block", async () =>
        {
            await ClearBlock();
            await Play<CarefreeMelody>();
            await CombatApi.Damage(Context, Player.Creature, 999, ValueProp.Move, Enemy);
            Equal(1, Player.Creature.CurrentHp); Equal(22, Player.Creature.Block);
            if (Player.Creature.HasPower<CarefreeMelodyPower>()) throw new Exception("Melody was not consumed");
            await CreatureCmd.SetCurrentHp(Player.Creature, Player.Creature.MaxHp);
        });
        await Check("Remaining companion cards: full brood, commands, healing, sacrifice and burst", async () =>
        {
            await ClearBlock();
            var relic = ModelDb.Relic<BroodCharm>().ToMutable(); Player.AddRelicInternal(relic);
            var chorus = await Play<BroodChorus>();
            Equal(3, KnightSwarm.LivingCount(Player));
            foreach (var pet in KnightSwarm.Living(Player)) Equal(8, pet.CurrentHp);
            if (chorus.Pile?.Type != PileType.Exhaust) throw new Exception("Chorus did not exhaust");
            await Play<BroodInstinct>();
            var hp = Enemy.CurrentHp; await Play<SwarmRush>(); Equal(15, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<SwarmEcho>(); Equal(20, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<BroodAssault>(); Equal(19, hp - Enemy.CurrentHp);
            var weaver = KnightSwarm.Find<WeaverlingPet>(Player)!;
            await CreatureCmd.SetCurrentHp(weaver, 3); await Play<TendBrood>(); Equal(9, weaver.CurrentHp);
            var block = Player.Creature.Block; await Play<SilkenAegis>(); Equal(block + 18, Player.Creature.Block);
            Soul.Gain(Player, -99); await Play<BroodOffering>(); Equal(22, CustomResources<Soul>.Get(Player.PlayerCombatState!).Amount);
            hp = Enemy.CurrentHp; await Play<HatchlingBurst>(); Equal(29, hp - Enemy.CurrentHp);
            Equal(0, KnightSwarm.LivingCount(Player));
            Player.RemoveRelicInternal(relic, silent: true); await PowerCmd.Remove(Player.Creature.GetPower<BroodInstinctPower>()!);
        });
        await Check("Soul nourishment: real spell payment grows current and maximum companion HP", async () =>
        {
            await Play<WeaverNest>(); await Play<SoulNourishment>();
            var pet = KnightSwarm.Find<WeaverlingPet>(Player)!;
            var hp = pet.CurrentHp; var max = pet.MaxHp;
            await Play<VengefulSpirit>(); Equal(hp + 2, pet.CurrentHp); Equal(max + 2, pet.MaxHp);
            await PowerCmd.Remove(Player.Creature.GetPower<SoulNourishmentPower>()!);
        });
        await Check("Swarm starter and common enablers", () =>
        {
            var deck = Player.Character.StartingDeck.ToArray();
            Equal(10, deck.Length); Equal(1, deck.Count(c => c is WeaverNest));
            Equal(4, deck.Count(c => c is OldNail)); Equal(3, deck.Count(c => c is Shell));
            Equal(1, deck.Count(c => c is VengefulSpirit)); Equal(1, deck.Count(c => c is Focus));
            if (ModelDb.Card<TendBrood>().Rarity != CardRarity.Common
                || ModelDb.Card<GlowingWomb>().Rarity != CardRarity.Common)
                throw new Exception("Summon enablers must be common");
            return Task.CompletedTask;
        });
        await Check("Empty-board Tend Brood blocks and summons; upgrade grows", async () =>
        {
            await ResetSwarm();
            await Play<TendBrood>(); Equal(5, Player.Creature.Block);
            var pet = KnightSwarm.Find<WeaverlingPet>(Player)!; Equal(3, pet.CurrentHp);
            await Play<TendBrood>(upgraded: true); Equal(12, Player.Creature.Block);
            Equal(8, pet.CurrentHp); Equal(8, pet.MaxHp);
        });
        await Check("Weaver Volley draws and full command sequence pays off", async () =>
        {
            await ResetSwarm(); await Play<WeaverNest>(); SeedDraw();
            var hand = Player.PlayerCombatState!.Hand.Cards.Count;
            var hp = Enemy.CurrentHp; await Play<WeaverVolley>();
            Equal(9, hp - Enemy.CurrentHp); Equal(hand + 1, Player.PlayerCombatState.Hand.Cards.Count);
            hp = Enemy.CurrentHp; await Play<SwarmEcho>(); Equal(16, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<SwarmEcho>(upgraded: true); Equal(20, hp - Enemy.CurrentHp);
        });
        await Check("Weaver SOUL synergy is capped, turn-owned and excludes automatic attacks", async () =>
        {
            await ResetSwarm(); await Play<WeaverNest>(); await Play<SoulNourishment>();
            var power = Player.Creature.GetPower<SoulNourishmentPower>()!;
            Soul.Gain(Player, -99); await Play<WeaverVolley>(); Equal(9, SoulAmount());
            await Play<WeaverVolley>(); Equal(9, SoulAmount());
            var other = MegaCrit.Sts2.Core.Entities.Players.Player.CreateForNewRun<Knight>(UnlockState.all, 2);
            await power.AfterPlayerTurnStart(Context, other);
            await Play<WeaverVolley>(); Equal(9, SoulAmount());
            await power.AfterPlayerTurnStart(Context, Player);
            await KnightSwarm.Strike(Context, Player, KnightSwarm.Find<WeaverlingPet>(Player)!, Enemy, 3, command: false);
            Equal(9, SoulAmount());
            await Play<WeaverVolley>(); Equal(18, SoulAmount());
            await Play<SoulNourishment>(upgraded: true);
            await Play<WeaverVolley>(); Equal(18, SoulAmount());
        });
        await Check("22/8 SOUL payments grow pets only on real cumulative thresholds", async () =>
        {
            await ResetSwarm(); await Play<WeaverNest>(); await Play<SoulNourishment>();
            await Play<GrimmRite>(); Equal(77, SoulAmount());
            await Play<GlowingWomb>(); Equal(91, SoulAmount());
            Equal(6, KnightSwarm.Find<WeaverlingPet>(Player)!.CurrentHp);
            await Play<GlowingWomb>();
            Equal(8, KnightSwarm.Find<WeaverlingPet>(Player)!.CurrentHp);
            Equal(8, KnightSwarm.Find<GrimmkinPet>(Player)!.CurrentHp);
            Equal(14, KnightSwarm.Find<HatchlingPet>(Player)!.CurrentHp);
            await Play<Shell>(); Equal(14, KnightSwarm.Find<HatchlingPet>(Player)!.CurrentHp);
        });
        await Check("Mature Grimm retains 11 automatic damage without multiplying commands", async () =>
        {
            await ResetSwarm();
            for (int i = 0; i < 4; i++) await Play<GrimmRite>();
            var pet = KnightSwarm.Find<GrimmkinPet>(Player)!;
            Equal(24, pet.CurrentHp); Equal(2, pet.GetPower<GrimmMaturityPower>()!.Amount);
            var hp = Enemy.CurrentHp; await Play<SwarmEcho>(); Equal(4, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await KnightSwarm.Strike(Context, Player, pet, Enemy, 5, command: false);
            Equal(11, hp - Enemy.CurrentHp);
            await Play<BroodInstinct>(upgraded: true);
            hp = Enemy.CurrentHp; await KnightSwarm.Strike(Context, Player, pet, Enemy, 5, command: false);
            Equal(13, hp - Enemy.CurrentHp);
        });
        await Check("Sacrifice and retained resurrection form a finite rebuild loop", async () =>
        {
            await ResetSwarm(); await Play<GlowingWomb>();
            var hp = Enemy.CurrentHp; Soul.Gain(Player, -99);
            var offering = await Play<BroodOffering>(); Equal(9, hp - Enemy.CurrentHp); Equal(22, SoulAmount());
            Equal(0, KnightSwarm.LivingCount(Player));
            if (offering.Pile?.Type != PileType.Exhaust) throw new Exception("Offering must exhaust");
            var revive = await Play<BroodReturn>(upgraded: true);
            Equal(9, KnightSwarm.Find<HatchlingPet>(Player)!.CurrentHp);
            if (revive.Pile?.Type != PileType.Exhaust || !revive.CanonicalKeywords.Contains(CardKeyword.Retain))
                throw new Exception("Resurrection must retain before use and exhaust after use");
            if (KnightSwarm.State(Player)!.LastFallen != null) throw new Exception("Revival must clear death memory");
        });
        await Check("Brood Instinct draws once across all three Chorus summons", async () =>
        {
            await ResetSwarm(); SeedDraw(); await Play<BroodInstinct>();
            var power = Player.Creature.GetPower<BroodInstinctPower>()!;
            var hand = Player.PlayerCombatState!.Hand.Cards.Count;
            await Play<BroodChorus>(); Equal(hand + 1, Player.PlayerCombatState.Hand.Cards.Count);
            await Play<TendBrood>(); Equal(hand + 1, Player.PlayerCombatState.Hand.Cards.Count);
            await power.AfterPlayerTurnStart(Context, Player);
            await Play<TendBrood>(); Equal(hand + 2, Player.PlayerCombatState.Hand.Cards.Count);
        });
        await Check("Low SOUL summons: relic never raises costs; text follows temporary discounts", async () =>
        {
            var relic = (SpellTwister)ModelDb.Relic<SpellTwister>().ToMutable(); Player.AddRelicInternal(relic);
            foreach (var (canonical, normal, reduced) in new (CardModel, int, int)[]
                { (ModelDb.Card<GrimmRite>(), 22, 11), (ModelDb.Card<GlowingWomb>(), 8, 4) })
            {
                var card = State.CreateCard(canonical, Player);
                await relic.AfterCardEnteredCombat(card);
                var cost = CustomResources<Soul>.Cost(card)!;
                Equal(normal, cost.GetWithModifiers(CostModifiers.All));
                cost.SetThisCombat(reduced, reduceOnly: true);
                foreach (var language in new[] { "zhs", "eng" })
                {
                    LocManager.Instance.SetLanguage(language);
                    if (!card.GetDescriptionForPile(PileType.Hand).Contains($"{reduced} [img]res://HollowKnight/images/charui/soul_icon_text.png[/img]"))
                        throw new Exception("Discounted summon text is stale: " + language);
                }
            }
            LocManager.Instance.SetLanguage("eng"); Player.RemoveRelicInternal(relic, silent: true);
        });
        await Check("Grimmchild power summons the same entity, no ghost damage after death", async () =>
        {
            await ResetSwarm(); await Play<Grimmchild>();
            var pet = KnightSwarm.Find<GrimmkinPet>(Player)!; Equal(6, pet.CurrentHp);
            var hp = Enemy.CurrentHp;
            await KnightSwarm.State(Player)!.BeforeSideTurnEnd(Context, Player.Creature.Side, [Player.Creature]);
            Equal(7, hp - Enemy.CurrentHp);
            hp = Enemy.CurrentHp; await Play<SwarmEcho>(); Equal(4, hp - Enemy.CurrentHp);
            await CreatureCmd.Kill(pet); hp = Enemy.CurrentHp;
            await Player.Creature.GetPower<GrimmchildPower>()!.BeforeSideTurnEnd(Context, Player.Creature.Side, [Player.Creature]);
            await KnightSwarm.State(Player)!.BeforeSideTurnEnd(Context, Player.Creature.Side, [Player.Creature]);
            Equal(0, hp - Enemy.CurrentHp);
            await Play<BroodReturn>(); hp = Enemy.CurrentHp;
            await KnightSwarm.State(Player)!.BeforeSideTurnEnd(Context, Player.Creature.Side, [Player.Creature]);
            Equal(7, hp - Enemy.CurrentHp);
        });
        await Check("Weaversong bridges a real companion and its existing exhaust token", async () =>
        {
            await ResetSwarm(); await Play<Weaversong>(upgraded: true);
            Equal(9, KnightSwarm.Find<WeaverlingPet>(Player)!.CurrentHp);
            await Player.Creature.GetPower<WeaversongPower>()!.BeforeHandDraw(Player, Context, State);
            Equal(1, Player.PlayerCombatState!.Hand.Cards.Count(c => c is WeaverBite));
            Soul.Gain(Player, -99); var hp = Enemy.CurrentHp;
            var bite = await Play<WeaverBite>(); Equal(6, hp - Enemy.CurrentHp); Equal(0, SoulAmount());
            Equal(0, KnightSwarm.State(Player)!.CommandsThisTurn);
            if (bite.Pile?.Type != PileType.Exhaust) throw new Exception("Bite must exhaust");
        });
        await Check("Retired card IDs migrate through native save loading and preserve upgrade and deck history", () =>
        {
            var aliases = ExpectedRetiredCards();
            Equal(21, aliases.Length);
            foreach (var (_, entry, replacement) in aliases)
            foreach (var upgraded in new[] { false, true })
            {
                var oldId = new ModelId(replacement.Id.Category, entry);
                var floor = upgraded ? 17 : 7;
                var save = new SerializableCard { Id = oldId, CurrentUpgradeLevel = upgraded ? 1 : 0, FloorAddedToDeck = floor };
                var loaded = CardModel.FromSerializable(save);
                if (loaded.GetType() != replacement.GetType() || loaded.Id != replacement.Id)
                {
                    var method = typeof(SaveUtil).GetMethod(nameof(SaveUtil.CardOrDeprecated), [typeof(ModelId)])!;
                    var direct = (CardModel)method.Invoke(null, [oldId])!;
                    var owners = string.Join(",", Harmony.GetPatchInfo(method)?.Owners.ToArray() ?? Array.Empty<string>());
                    throw new Exception($"Old ID {oldId} loaded as {loaded.GetType().Name}/{loaded.Id}, expected {replacement.GetType().Name}/{replacement.Id}; Resolve={RetiredCards.Resolve(oldId)}; reflected SaveUtil={direct.GetType().Name}/{direct.Id}; patch owners={owners}");
                }
                Equal(save.CurrentUpgradeLevel, loaded.CurrentUpgradeLevel);
                if (loaded.FloorAddedToDeck != floor) throw new Exception("Migration lost deck history");
                var nextSave = loaded.ToSerializable();
                if (nextSave.Id != replacement.Id || nextSave.Id == oldId
                    || nextSave.CurrentUpgradeLevel != save.CurrentUpgradeLevel || nextSave.FloorAddedToDeck != floor)
                    throw new Exception("Re-saving a migrated card must preserve state and write the current ID");
            }
            foreach (var (_, entry, replacement) in aliases)
            {
                var foreign = new ModelId(replacement.Id.Category, entry.Replace("HOLLOWKNIGHT-", "ANOTHER_MOD-"));
                var partial = new ModelId(replacement.Id.Category, entry + "_EXTRA");
                var relicId = new ModelId(ModelDb.GetId<SoulVessel>().Category, entry);
                foreach (var untouched in new[] { foreign, partial, relicId, replacement.Id })
                    if (RetiredCards.Resolve(untouched) != untouched)
                        throw new Exception("Retired card aliases rewrote a current, foreign, partial or non-card ID: " + untouched);
            }
            return Task.CompletedTask;
        });
        await Check("Native Sharp enchantment survives retired ID migration, upgrade and actual spell damage", async () =>
        {
            var source = (VengefulSpirit)ModelDb.Card<VengefulSpirit>().ToMutable();
            source.EnchantInternal(ModelDb.Enchantment<Sharp>().ToMutable(), 3);
            source.Enchantment!.ModifyCard(); source.UpgradeInternal(); source.FloorAddedToDeck = 11;
            var save = source.ToSerializable();
            var originalEnchantment = save.Enchantment ?? throw new Exception("Native card serialization lost its enchantment fixture");
            save.Id = new ModelId(source.Id.Category, "HOLLOWKNIGHT-SOUL_BOLT");
            var loaded = CardModel.FromSerializable(save);
            if (loaded is not VengefulSpirit || loaded.Enchantment is not Sharp || loaded.Id != source.Id)
                throw new Exception("Migrated enchanted card or enchantment has the wrong model type");
            Equal(3, loaded.Enchantment.Amount); Equal(1, loaded.CurrentUpgradeLevel);
            if (loaded.FloorAddedToDeck != 11) throw new Exception("Enchanted migration lost deck history");
            Equal(26, loaded.DynamicVars.Damage.BaseValue);
            loaded.DynamicVars.Damage.UpdateCardPreview(loaded, default, Enemy, runGlobalHooks: false);
            Equal(29, loaded.DynamicVars.Damage.PreviewValue);
            var nextSave = loaded.ToSerializable();
            if (nextSave.Enchantment is not { } savedEnchantment || nextSave.Id != source.Id || savedEnchantment.Id != originalEnchantment.Id
                || savedEnchantment.Amount != 3 || nextSave.CurrentUpgradeLevel != 1)
                throw new Exception("Enchanted migration did not round-trip with the new card ID");
            State.AddCard(loaded, Player); Player.PlayerCombatState!.Hand.AddInternal(loaded);
            await PlayerCmd.GainEnergy(10, Player); Soul.Gain(Player, -99); Soul.Gain(Player, 33);
            var energy = Player.PlayerCombatState.Energy;
            var (spent, stars) = await loaded.SpendResources();
            var resources = new ResourceInfo { EnergySpent = spent, EnergyValue = energy, StarsSpent = stars, StarValue = 0 };
            var hp = Enemy.CurrentHp;
            await loaded.OnPlayWrapper(Context, Enemy, false, resources, skipCardPileVisuals: true);
            Equal(29, hp - Enemy.CurrentHp); Equal(0, SoulAmount());
        });
        await Check("Only 83 active card models and 80 reward cards remain; 21 retired IDs are absent from pools and library", () =>
        {
            var aliases = ExpectedRetiredCards();
            var oldEntries = aliases.Select(alias => alias.Entry).ToHashSet();
            Equal(21, oldEntries.Count);
            var types = typeof(OldNail).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t)).ToArray();
            Equal(83, types.Length);
            foreach (var name in aliases.Select(alias => alias.RetiredType))
                if (types.Any(type => type.Name == name)) throw new Exception("Retired model is still compiled: " + name);
            var pool = Player.Character.CardPool.AllCards.ToArray(); Equal(83, pool.Length);
            if (!types.ToHashSet().SetEquals(pool.Select(card => card.GetType())))
                throw new Exception("Card pool and compiled active model types differ");
            Equal(22, pool.Count(card => card.Rarity == CardRarity.Common));
            Equal(37, pool.Count(card => card.Rarity == CardRarity.Uncommon));
            Equal(21, pool.Count(card => card.Rarity == CardRarity.Rare));
            Equal(2, pool.Count(card => card.Rarity == CardRarity.Basic));
            Equal(1, pool.Count(card => card.Rarity == CardRarity.Token));
            var rewards = Player.Character.CardPool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.SingleplayerOnly)
                .Where(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare).ToArray();
            Equal(80, rewards.Length);
            if (pool.Concat(rewards).Concat(ModelDb.AllCards).Any(card => card.Id.Category == ModelDb.GetId<VengefulSpirit>().Category && oldEntries.Contains(card.Id.Entry)))
                throw new Exception("Retired card remains in a pool, reward source or library");
            foreach (var entry in oldEntries)
                if (ModelDb.GetByIdOrNull<CardModel>(new ModelId(ModelDb.GetId<VengefulSpirit>().Category, entry)) != null)
                    throw new Exception("A retired card still has a registered legacy model");
            return Task.CompletedTask;
        });
        await Check("Patch installer: actual Harmony rollback and core/optional failure handling", () =>
        {
            Equal(4, PatchInstallerChecks.Run().Count);
            return Task.CompletedTask;
        });
        await Check("All card models and upgrades load", () =>
        {
            int count = 0;
            foreach (var type in typeof(OldNail).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t)))
            {
                var canonical = (CardModel)typeof(ModelDb).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static, [typeof(Type)])!.Invoke(null, [type])!;
                var card = State.CreateCard(canonical, Player);
                _ = card.DynamicVars.Count; _ = card.Title; _ = card.GetDescriptionForPile(PileType.None);
                _ = card.GetDescriptionForUpgradePreview(); _ = card.ShouldGlowRed;
                if (card.IsUpgradable) card.UpgradeInternal();
                _ = card.GetDescriptionForPile(PileType.None);
                foreach (var path in new[] { card.PortraitPath, ((CustomCardModel)card).CustomPortraitPath })
                    if (ResourceLoader.Load<Texture2D>(path) == null) throw new Exception("Missing portrait: " + path);
                count++;
            }
            Equal(83, count); return Task.CompletedTask;
        });
        await Check("Chinese card descriptions and all relic/power assets", () =>
        {
            LocManager.Instance.SetLanguage("zhs");
            int relics = 0;
            foreach (var type in typeof(OldNail).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t)))
            {
                var canonical = (AbstractModel)typeof(ModelDb).GetMethod("Get", BindingFlags.NonPublic | BindingFlags.Static, [typeof(Type)])!.Invoke(null, [type])!;
                if (canonical is CardModel c)
                {
                    var card = State.CreateCard(c, Player); _ = card.GetDescriptionForPile(PileType.None);
                    if (card.IsUpgradable) card.UpgradeInternal(); _ = card.GetDescriptionForPile(PileType.None);
                }
                if (canonical is RelicModel relic)
                {
                    relics++; if (ResourceLoader.Load<Texture2D>(relic.IconPath) == null) throw new Exception("Missing relic icon: " + relic.IconPath);
                }
                if (canonical is PowerModel power)
                    if (ResourceLoader.Load<Texture2D>(power.IconPath) == null) throw new Exception("Missing power icon: " + power.IconPath);
            }
            Equal(25, relics); return Task.CompletedTask;
        });
    }

    static Task ClearBlock()
    {
        var method = typeof(CreatureCmd).GetMethods().Single(m => m.Name == "LoseBlock");
        return (Task)method.Invoke(null, method.GetParameters().Length == 2
            ? [Player.Creature, 9999m] : [Context, Player.Creature, 9999m, null])!;
    }

    static int SoulAmount() => CustomResources<Soul>.Get(Player.PlayerCombatState!).Amount;

    static void SeedDraw()
    {
        for (int i = 0; i < 5; i++) Player.PlayerCombatState!.DrawPile.AddInternal(State.CreateCard<Shell>(Player));
    }

    static async Task ResetSwarm()
    {
        foreach (var power in Player.Creature.Powers.Where(p => p is SoulNourishmentPower or BroodInstinctPower or GrimmchildPower or WeaversongPower).ToArray())
            await PowerCmd.Remove(power);
        foreach (var pet in KnightSwarm.Living(Player)) await CreatureCmd.Kill(pet);
        if (KnightSwarm.State(Player) is { } guard) await PowerCmd.Remove(guard);
        foreach (var card in Player.PlayerCombatState!.Hand.Cards.ToArray()) await CombatApi.Exhaust(Context, card);
        await ClearBlock(); Soul.Gain(Player, -99);
    }

    static async Task<CardModel> Play<T>(int? soulBefore = 99, bool upgraded = false) where T : CardModel
    {
        var card = State.CreateCard<T>(Player);
        if (upgraded) card.UpgradeInternal();
        Player.PlayerCombatState!.Hand.AddInternal(card);
        await PlayerCmd.GainEnergy(10, Player);
        if (CustomResources<Soul>.CanonicalCost(card) >= 0 && soulBefore.HasValue) { Soul.Gain(Player, -99); Soul.Gain(Player, soulBefore.Value); }
        var energy = Player.PlayerCombatState.Energy;
        var (spent, stars) = await card.SpendResources();
        var resources = new ResourceInfo { EnergySpent = spent, EnergyValue = energy, StarsSpent = stars, StarValue = 0 };
        Creature? target = card.TargetType == TargetType.AnyEnemy ? Enemy : card.TargetType == TargetType.Self ? Player.Creature : null;
        await card.OnPlayWrapper(Context, target, false, resources, skipCardPileVisuals: true);
        if (card.Pile?.Type == PileType.Play) throw new Exception("Card remained in play pile");
        return card;
    }
}
