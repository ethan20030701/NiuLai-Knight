using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using HollowKnight.HollowKnightCode;
using HollowKnight.HollowKnightCode.Cards;
using HollowKnight.HollowKnightCode.Compatibility;
using HollowKnight.HollowKnightCode.Powers;
using HollowKnight.HollowKnightCode.Resources;
using HollowKnight.HollowKnightCode.Summons;
using HollowKnight.HollowKnightCode.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Powers;

var gameData=Environment.GetEnvironmentVariable("HOLLOWKNIGHT_GAME_DATA_DIR")
    ?? Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().Single(a=>a.Key=="GameDataDir").Value
    ?? throw new InvalidOperationException("No GameDataDir configured for regression tests");
var referenceDir=Path.GetFullPath(gameData);
AssemblyLoadContext.Default.Resolving += (_,name) => {
    var p=Path.Combine(referenceDir,name.Name+".dll");
    return File.Exists(p)?AssemblyLoadContext.Default.LoadFromAssemblyPath(p):null;
};
var checks=new List<object>();int failed=0;int assertions=0;
void Check(string name,Action body){try{body();checks.Add(new {name,passed=true});}catch(Exception e){failed++;checks.Add(new{name,passed=false,error=e.ToString()});}}
void Assert(bool b,string message="Unexpected result"){assertions++;if(!b)throw new Exception(message);}
void Set(object o,Type declaring,string field,object? value)=>declaring.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly)!.SetValue(o,value);
T Empty<T>() where T:class=>(T)RuntimeHelpers.GetUninitializedObject(typeof(T));
T Mutable<T>() where T:AbstractModel{var o=Empty<T>();Set(o,typeof(AbstractModel),"<IsMutable>k__BackingField",true);return o;}
Creature Creature(CombatSide side){var c=Empty<Creature>();Set(c,typeof(Creature),"<Side>k__BackingField",side);Set(c,typeof(Creature),"_powers",new List<PowerModel>());Set(c,typeof(Creature),"_currentHp",100);Set(c,typeof(Creature),"_maxHp",100);return c;}
Player Player(Creature c){var p=Empty<Player>();Set(c,typeof(Creature),"<Player>k__BackingField",p);Set(p,typeof(Player),"<Creature>k__BackingField",c);Set(p,typeof(Player),"_relics",new List<RelicModel>());Set(p,typeof(Player),"_runPiles",Array.Empty<CardPile>());return p;}
T Card<T>(Player owner,CardType type=CardType.Attack,int soulCost=-1) where T:CardModel{var c=Mutable<T>();Set(c,typeof(CardModel),"_owner",owner);Set(c,typeof(CardModel),"<Type>k__BackingField",type);Set(c,typeof(CardModel),"_tags",new HashSet<CardTag>());CustomResources<Soul>.SetCanonicalCost(c,soulCost);return c;}
T Power<T>(Creature owner,int amount) where T:PowerModel{var p=Mutable<T>();Set(p,typeof(PowerModel),"_owner",owner);Set(p,typeof(PowerModel),"_amount",amount);return p;}
CardPlay Play(CardModel c,Player player)=>new(){Card=c,Player=player,Target=null,ResultPile=PileType.Discard,Resources=default!,IsAutoPlay=false,PlayIndex=0,PlayCount=1};
CardPlay Replay(CardModel c,Player player,int playIndex,bool auto=false)=>new(){Card=c,Player=player,Target=null,ResultPile=PileType.Discard,Resources=default!,IsAutoPlay=auto,PlayIndex=playIndex,PlayCount=2};
Soul SetSoul(Player p,int amount){
    var state=Empty<PlayerCombatState>();Set(state,typeof(PlayerCombatState),"_player",p);Set(state,typeof(PlayerCombatState),"_pets",new List<Creature>());Set(state,typeof(PlayerCombatState),"_piles",Array.Empty<CardPile>());
    Set(p,typeof(Player),"<PlayerCombatState>k__BackingField",state);
    var resource=(NotNullSpireField<PlayerCombatState,Soul>)typeof(CustomResources<Soul>).GetProperty("Resource",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
    var soul=new Soul{Owner=p,Amount=amount};resource[state]=soul;return soul;
}
void SetBaseSpend(CardPlay play,int amount){
    _=CustomResources<Soul>.AmountSpent(play);
    var recorded=(SpireField<CardPlay,int>)typeof(CustomResources<Soul>).GetField("_recordedSpend",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
    recorded[play]=amount;
}
object? Tracker(string name,params object[] a){
    var method=typeof(SoulSpendTracker).GetMethod(name,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!;
    var parameters=method.GetParameters();var arguments=parameters.Select((p,i)=>i<a.Length?a[i]:p.DefaultValue).ToArray();
    return method.Invoke(null,arguments);
}
object? SoulCall(string name,params object[] a)=>typeof(Soul).GetMethod(name,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(null,a);
void OnPlay(CardModel card,CardPlay play){
    var method=card.GetType().GetMethod("OnPlay",BindingFlags.Instance|BindingFlags.NonPublic)!;
    ((Task)method.Invoke(card,new object?[]{null,play})!).GetAwaiter().GetResult();
}
Creature Pet<T>(Player p,int hp) where T:KnightPet{
    var c=Creature(CombatSide.Player);Set(c,typeof(Creature),"<Monster>k__BackingField",Mutable<T>());Set(c,typeof(Creature),"_petOwner",p);Set(c,typeof(Creature),"_currentHp",hp);
    var pets=(List<Creature>)typeof(PlayerCombatState).GetField("_pets",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p.PlayerCombatState)!;pets.Add(c);return c;
}
T Canonical<T>(Player p) where T:CardModel,new(){
    // Constructors keep their actual data. Only ModelDb/content registration is bypassed below.
    var c=new T();Set(c,typeof(AbstractModel),"<IsMutable>k__BackingField",true);Set(c,typeof(CardModel),"_owner",p);return c;
}
void Upgrade(CardModel c)=>c.GetType().GetMethod("OnUpgrade",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,null);
Player NewPlayer(){var p=Player(Creature(CombatSide.Player));Set(p,typeof(Player),"<Character>k__BackingField",Mutable<global::HollowKnight.HollowKnightCode.Character.HollowKnight>());_=SetSoul(p,0);return p;}
object? BeginPreviewCombat(CardModel card,Player p){
    var manager=CombatManager.Instance;var field=typeof(CombatManager).GetField("_turnState",BindingFlags.Instance|BindingFlags.NonPublic)!;var old=field.GetValue(manager);
    var state=Empty<CombatState>();var turnType=typeof(CombatManager).Assembly.GetType("MegaCrit.Sts2.Core.Combat.CombatTurnState")!;var turn=RuntimeHelpers.GetUninitializedObject(turnType);
    Set(turn,turnType,"<State>k__BackingField",state);Set(turn,turnType,"<IsInProgress>k__BackingField",true);field.SetValue(manager,turn);
    Set(p.Creature,typeof(Creature),"<CombatState>k__BackingField",state);Set(card,typeof(CardModel),"_upgradePreviewType",CardUpgradePreviewType.Combat);return old;
}
void EndPreviewCombat(object? old)=>typeof(CombatManager).GetField("_turnState",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(CombatManager.Instance,old);

var owner=Creature(CombatSide.Player);var player=Player(owner);
var enemy=Creature(CombatSide.Enemy);var ally=Creature(CombatSide.Player);var other=Player(ally);
CardPlay TargetedPlay(CardModel c,Player p)=>new(){Card=c,Player=p,Target=enemy,ResultPile=PileType.Discard,Resources=default!,IsAutoPlay=false,PlayIndex=0,PlayCount=1};
var nail=Card<CoiledNail>(player);var spell=Card<VengefulSpirit>(player,CardType.Attack,33);var play=Play(spell,player);

Check("shade.preview_repeated_is_pure",()=>{
    var p=Power<ShadeCloakPower>(owner,1);int flashes=0;p.Flashed+=_=>flashes++;
    for(int i=0;i<5;i++)Assert(p.ModifyHpLostAfterOsty(owner,12,ValueProp.Move,enemy,null)==0,"Shade preview must reduce attack to zero");
    var pending=(bool)typeof(ShadeCloakPower).GetField("_dodgePending",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p)!;
    Assert(!pending&&flashes==0,"Preview mutated pending dodge or emitted Flash");
});
Check("shade.blocked_and_nonhostile_preview_preserve_state",()=>{
    var p=Power<ShadeCloakPower>(owner,1);
    Assert(p.ModifyHpLostAfterOsty(owner,0,ValueProp.Move,enemy,null)==0);
    Assert(p.ModifyHpLostAfterOsty(owner,12,ValueProp.Move,ally,null)==12);
    Assert(p.ModifyHpLostAfterOsty(owner,12,ValueProp.Unpowered,enemy,null)==12);
    Assert(p.ModifyHpLostAfterOsty(ally,12,ValueProp.Move,enemy,null)==12);
    Assert(!(bool)typeof(ShadeCloakPower).GetField("_dodgePending",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p)!);
});
Check("shade.actual_modifier_notification_marks_once",()=>{
    var p=Power<ShadeCloakPower>(owner,1);int flashes=0;p.Flashed+=_=>flashes++;
    p.AfterModifyingHpLostAfterOsty().GetAwaiter().GetResult();
    Assert((bool)typeof(ShadeCloakPower).GetField("_dodgePending",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p)!);
    Assert(flashes==1,"Real modifier notification must Flash once");
});
Check("shrine.preview_matches_real_play",()=>{
    var p=Power<ShrineBlessingPower>(owner,7);
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,owner,spell,null)==7,"Preview omitted shrine damage");
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,owner,spell,play)==7);
});
Check("shrine.only_owner_powered_spell_damage",()=>{
    var p=Power<ShrineBlessingPower>(owner,7);
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Unpowered,owner,spell,play)==0,"Unpowered damage gained shrine bonus");
    Assert(p.KnightDamageAdditive(owner,10,ValueProp.Move,enemy,spell,play)==0,"Enemy damage gained owner's shrine bonus");
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,ally,spell,play)==0,"Ally damage gained owner's shrine bonus");
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,owner,nail,Play(nail,player))==0);
    var otherSpell=Card<VengefulSpirit>(other,CardType.Attack,33);
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,owner,otherSpell,Play(otherSpell,other))==0);
});
Check("compat.current_play_isolated_and_order_safe",()=>{
    var first=Play(spell,player);var second=Play(spell,player);var otherPlay=Play(nail,player);
    CombatApi.Begin(first);Assert(CombatApi.CurrentPlay(spell)==first&&CombatApi.CurrentPlay(nail)==null);
    CombatApi.Begin(second);CombatApi.End(first);Assert(CombatApi.CurrentPlay(spell)==second);
    CombatApi.Begin(otherPlay);CombatApi.End(second);Assert(CombatApi.CurrentPlay(spell)==null&&CombatApi.CurrentPlay(nail)==otherPlay);
    CombatApi.End(otherPlay);Assert(CombatApi.CurrentPlay(null)==null&&CombatApi.CurrentPlay(nail)==null);
});
Check("compat.real_game_six_argument_hook_dispatch",()=>{
    var harmony=new Harmony("hollowknight.coretests");CombatApi.Install(harmony);
    try{
        AbstractModel p=Power<ShrineBlessingPower>(owner,7);
        Assert(p.ModifyDamageAdditive(enemy,10,ValueProp.Move,owner,spell,play)==7,"Game hook failed to dispatch IKnightDamageModifier");
        Assert(p.ModifyDamageAdditive(enemy,10,ValueProp.Move,owner,spell,null)==7,"Game hook preview differs from actual damage");
    }finally{harmony.UnpatchAll(harmony.Id);}
});
Check("soul.bounds_turn_carry_and_combat_reset",()=>{
    var soul=new Soul();soul.Amount=150;Assert(soul.Amount==99);
    soul.Amount=-5;Assert(soul.Amount==0);soul.Amount=44;soul.StartOfTurnReset(null!,null!);Assert(soul.Amount==44);
    soul.PrepForCombat<Soul>(null!);Assert(soul.Amount==0);
});
Check("soul.affordability_uses_actual_amount",()=>{
    var soul=new Soul{Amount=32};Assert(!soul.CanAfford(spell,33));soul.ModifyAmount(1);Assert(soul.CanAfford(spell,33));
    soul.ModifyAmount(-33);Assert(soul.Amount==0);soul.ModifyAmount(11);Assert(soul.Amount==11);
});
Check("soul.manual_spend_is_per_play_and_combines_base_record",()=>{
    var first=Play(spell,player);var second=Play(spell,player);
    Tracker("QueuePayment",spell,8);Assert(SoulSpendTracker.AmountSpent(first)==8);
    SoulSpendTracker.RecordManual(first,11);SoulSpendTracker.RecordManual(first,22);SoulSpendTracker.RecordManual(first,-99);
    Assert(SoulSpendTracker.AmountSpent(first)==41&&SoulSpendTracker.AmountSpent(second)==0);
    Tracker("Bind",first);Assert(SoulSpendTracker.AmountSpent(first)==41);
});
Check("nail.hit_classification_excludes_spells_unpowered_and_pets",()=>{
    Assert(NailRules.IsNailHit(owner,owner,ValueProp.Move,nail));
    Assert(!NailRules.IsNailHit(owner,owner,ValueProp.Move,spell));
    Assert(!NailRules.IsNailHit(owner,owner,ValueProp.Unpowered,nail));
    Assert(!NailRules.IsNailHit(ally,owner,ValueProp.Move,nail));
    Assert(!NailRules.IsNailHit(owner,owner,ValueProp.Move,null));
    Set(nail,typeof(CardModel),"_tags",new HashSet<CardTag>{CardTag.Shiv});Assert(!NailRules.IsNailHit(owner,owner,ValueProp.Move,nail));
});

Check("soul.automatic_receipt_consumed_once_and_replay_is_free",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var first=Replay(c,player,0);var replay=Replay(c,player,1);
    Tracker("QueuePayment",c,33);SetBaseSpend(first,33);SetBaseSpend(replay,33);
    Tracker("Bind",first);Assert((int)Tracker("AutomaticAmountSpent",first)! == 33);
    Tracker("Bind",first);Assert(SoulSpendTracker.AmountSpent(first)==33,"Repeated binding/query duplicated or erased payment");
    Tracker("Bind",replay);Assert(SoulSpendTracker.AmountSpent(replay)==0,"Replay inherited prior spend");
});
Check("soul.free_autoplay_ignores_stale_baselib_spend",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var autoplay=Replay(c,player,0,true);
    SetBaseSpend(autoplay,33);Tracker("Bind",autoplay);Assert(SoulSpendTracker.AmountSpent(autoplay)==0);
});
Check("soul.prepaid_autoplay_keeps_real_receipt",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var autoplay=Replay(c,player,0,true);
    Tracker("QueuePayment",c,24);SetBaseSpend(autoplay,33);Tracker("Bind",autoplay);
    Assert((int)Tracker("AutomaticAmountSpent",autoplay)! == 24&&SoulSpendTracker.AmountSpent(autoplay)==24);
});
Check("soul.manual_payment_atomic_and_records_actual_balance",()=>{
    var c=Card<SoulTotem>(player,CardType.Skill);var p=Play(c,player);var soul=SetSoul(player,32);
    Assert(!(bool)SoulCall("TrySpendManual",player,p,33)!);Assert(soul.Amount==32&&SoulSpendTracker.AmountSpent(p)==0);
    soul.Amount=44;Assert((bool)SoulCall("TrySpendManual",player,p,33)!);Assert(soul.Amount==11&&SoulSpendTracker.AmountSpent(p)==33);
    var next=Play(c,player);Assert((int)SoulCall("SpendAll",player,next)! == 11);Assert(soul.Amount==0&&SoulSpendTracker.AmountSpent(next)==11);
});
Check("soul.totem_insufficient_soul_does_not_grant_energy_or_record_spend",()=>{
    var soul=SetSoul(player,32);var c=Card<SoulTotem>(player,CardType.Skill);var p=Play(c,player);
    OnPlay(c,p);Assert(soul.Amount==32&&SoulSpendTracker.AmountSpent(p)==0);
});
Check("soul.unbound_insufficient_replay_does_not_attack_or_spend",()=>{
    var soul=SetSoul(player,0);var c=Card<SoulUnbound>(player);var p=Replay(c,player,1);
    OnPlay(c,p);Assert(soul.Amount==0&&SoulSpendTracker.AmountSpent(p)==0,"Replay should return before reading damage/target or calling attack");
});
Check("swarm.guard_priority_skips_dead_companions",()=>{
    _=SetSoul(player,0);var grimm=Pet<GrimmkinPet>(player,12);var weaver=Pet<WeaverlingPet>(player,8);var hatchling=Pet<HatchlingPet>(player,5);
    var p=Power<SwarmGuardPower>(owner,1);Assert(p.ModifyUnblockedDamageTarget(owner,10,ValueProp.Move,enemy)==hatchling);
    Set(hatchling,typeof(Creature),"_currentHp",0);Assert(p.ModifyUnblockedDamageTarget(owner,10,ValueProp.Move,enemy)==weaver);
    Set(weaver,typeof(Creature),"_currentHp",0);Assert(p.ModifyUnblockedDamageTarget(owner,10,ValueProp.Move,enemy)==grimm);
    Set(grimm,typeof(Creature),"_currentHp",0);Assert(p.ModifyUnblockedDamageTarget(owner,10,ValueProp.Move,enemy)==owner);
});
Check("swarm.guard_excludes_blocked_allied_and_unpowered_damage",()=>{
    _=SetSoul(player,0);_=Pet<HatchlingPet>(player,5);var p=Power<SwarmGuardPower>(owner,1);
    Assert(p.ModifyUnblockedDamageTarget(owner,0,ValueProp.Move,enemy)==owner);
    Assert(p.ModifyUnblockedDamageTarget(owner,10,ValueProp.Unpowered,enemy)==owner);
    Assert(p.ModifyUnblockedDamageTarget(owner,10,ValueProp.Move,ally)==owner);
    Assert(p.ModifyUnblockedDamageTarget(owner,10,ValueProp.Move,null)==owner);
    Assert(p.ModifyUnblockedDamageTarget(ally,10,ValueProp.Move,enemy)==ally);
});

Check("soul.real_spend_override_records_payment_and_optional_failure",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var soul=SetSoul(player,66);
    Assert(soul.Spend<Soul>(null!,c,33,false).GetAwaiter().GetResult());Assert(soul.Amount==33);
    var paid=Play(c,player);Assert(SoulSpendTracker.AutomaticAmountSpent(paid)==33);
    soul.Amount=12;Assert(!soul.Spend<Soul>(null!,c,33,true).GetAwaiter().GetResult());Assert(soul.Amount==12);
    var unpaid=Play(c,player);Assert(SoulSpendTracker.AmountSpent(unpaid)==0);
});
Check("soul.real_spend_nonoptional_insufficient_records_actual_amount",()=>{
    // Godot OS calls only identify the logger's host. Stub that engine boundary in this console process.
    var harmony=new Harmony("hollowknight.coretests.logger");
    harmony.Patch(typeof(Godot.OS).GetMethod(nameof(Godot.OS.GetCmdlineArgs)),new HarmonyMethod(typeof(EngineBoundary),nameof(EngineBoundary.CommandLine)));
    harmony.Patch(typeof(Godot.OS).GetMethod(nameof(Godot.OS.HasFeature)),new HarmonyMethod(typeof(EngineBoundary),nameof(EngineBoundary.HasFeature)));
    harmony.Patch(typeof(MegaCrit.Sts2.Core.Logging.ConsoleLogPrinter).GetMethod("Print"),new HarmonyMethod(typeof(EngineBoundary),nameof(EngineBoundary.LogPrint)));
    try{
        var c=Card<VengefulSpirit>(player,CardType.Attack,33);var soul=SetSoul(player,12);
        Assert(soul.Spend<Soul>(null!,c,33,false).GetAwaiter().GetResult());Assert(soul.Amount==0);
        Assert(SoulSpendTracker.AmountSpent(Play(c,player))==12,"Clamped spend recorded requested amount instead of actual amount");
    }finally{harmony.UnpatchAll(harmony.Id);}
});
Check("soul.payment_prefix_binds_before_model_hooks",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var p=Play(c,player);Tracker("QueuePayment",c,33);
    var patch=typeof(Soul).Assembly.GetType("HollowKnight.HollowKnightCode.Patches.SoulPaymentPatch")!;
    patch.GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{p});
    Assert(SoulSpendTracker.AutomaticAmountSpent(p)==33);
});
Check("soul.payment_cleanup_clears_success_fault_and_cancellation",()=>{
    var patch=typeof(Soul).Assembly.GetType("HollowKnight.HollowKnightCode.Patches.SoulPaymentCleanupPatch")!;
    var finish=patch.GetMethod("Finish",BindingFlags.Static|BindingFlags.NonPublic)!;
    foreach(var task in new[]{Task.CompletedTask,Task.FromException(new InvalidOperationException("fixture failure")),Task.FromCanceled(new CancellationToken(true))}){
        var c=Card<VengefulSpirit>(player,CardType.Attack,33);Tracker("QueuePayment",c,33);
        var scope=SoulSpendTracker.BeginScope(c);var wrapped=(Task)finish.Invoke(null,new object[]{task,scope})!;
        Exception? caught=null;try{wrapped.GetAwaiter().GetResult();}catch(Exception e){caught=e;}
        Assert(task.IsCompletedSuccessfully ? caught==null : task.IsCanceled ? caught is OperationCanceledException : caught is InvalidOperationException,"Cleanup changed task completion state");
        Assert(SoulSpendTracker.AmountSpent(Play(c,player))==0,"Pending receipt survived completed/failed/cancelled card series");
        SoulSpendTracker.RestoreCallerScope(scope);
    }
});
Check("full_soul_free_replay_does_not_reconstruct_phantom_full_meter",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var replay=Replay(c,player,1);_=SetSoul(player,33);SetBaseSpend(replay,33);
    var p=Power<FullSoulPower>(owner,8);p.BeforeCardPlayed(replay).GetAwaiter().GetResult();
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,owner,c,replay)==0,"Stale BaseLib spend incorrectly reached full-soul threshold");
});
Check("full_soul_real_payment_preserves_pre_spend_threshold",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var play=Play(c,player);_=SetSoul(player,33);Tracker("QueuePayment",c,33);
    var p=Power<FullSoulPower>(owner,8);p.BeforeCardPlayed(play).GetAwaiter().GetResult();
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,owner,c,play)==8);
});
Check("soul.pending_payment_cannot_cross_combat_states",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);_=SetSoul(player,66);Tracker("QueuePayment",c,33);_=SetSoul(player,0);
    Assert(SoulSpendTracker.AmountSpent(Play(c,player))==0,"Payment from a prior combat was claimed in a new combat");
});
Check("soul.same_card_nested_free_play_cannot_steal_outer_receipt",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);Tracker("QueuePayment",c,33);
    var outer=SoulSpendTracker.BeginScope(c);var inner=SoulSpendTracker.BeginScope(c);
    try{
        var nested=Replay(c,player,0,true);Assert(SoulSpendTracker.AmountSpent(nested)==0);
        SoulSpendTracker.RestoreCallerScope(inner);SoulSpendTracker.FinishScope(inner);
        Assert(SoulSpendTracker.AmountSpent(Play(c,player))==33);
    }finally{SoulSpendTracker.RestoreCallerScope(inner);SoulSpendTracker.RestoreCallerScope(outer);SoulSpendTracker.FinishScope(inner);SoulSpendTracker.FinishScope(outer);}
});
Check("soul.same_card_nested_paid_play_has_own_receipt",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);Tracker("QueuePayment",c,33);
    var outer=SoulSpendTracker.BeginScope(c);Tracker("QueuePayment",c,24);var inner=SoulSpendTracker.BeginScope(c);
    try{
        Assert(SoulSpendTracker.AmountSpent(Play(c,player))==24);
        SoulSpendTracker.RestoreCallerScope(inner);SoulSpendTracker.FinishScope(inner);
        Assert(SoulSpendTracker.AmountSpent(Play(c,player))==33);
    }finally{SoulSpendTracker.RestoreCallerScope(inner);SoulSpendTracker.RestoreCallerScope(outer);SoulSpendTracker.FinishScope(inner);SoulSpendTracker.FinishScope(outer);}
});
Check("soul.async_continuation_retains_reserved_scope_after_caller_restore",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);Tracker("QueuePayment",c,33);
    var outer=SoulSpendTracker.BeginScope(c);var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    async Task<int> ContinuePlay(){await gate.Task;return SoulSpendTracker.AmountSpent(Play(c,player));}
    var continuation=ContinuePlay();SoulSpendTracker.RestoreCallerScope(outer);
    var nestedScope=SoulSpendTracker.BeginScope(c);Assert(SoulSpendTracker.AmountSpent(Play(c,player))==0);
    SoulSpendTracker.RestoreCallerScope(nestedScope);SoulSpendTracker.FinishScope(nestedScope);gate.SetResult();
    Assert(continuation.GetAwaiter().GetResult()==33);SoulSpendTracker.FinishScope(outer);
});
Check("full_soul_payment_snapshot_excludes_soul_generated_by_payment_hooks",()=>{
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);var soul=SetSoul(player,55);
    Assert(soul.Spend<Soul>(null!,c,33,false).GetAwaiter().GetResult());soul.ModifyAmount(22);
    var play=Play(c,player);var p=Power<FullSoulPower>(owner,8);p.BeforeCardPlayed(play).GetAwaiter().GetResult();
    Assert(SoulSpendTracker.AmountBeforePayment(play)==55);
    Assert(p.KnightDamageAdditive(enemy,10,ValueProp.Move,owner,c,play)==0,"Post-payment soul gain was counted as pre-payment fullness");
});
Check("soul.real_harmony_payment_patch_targets_install",()=>{
    var harmony=new Harmony("hollowknight.coretests.payment");
    var before=typeof(Hook).GetMethod(nameof(Hook.BeforeCardPlayed))!;var wrapper=typeof(CardModel).GetMethod(nameof(CardModel.OnPlayWrapper))!;
    try{
        foreach(string type in new[]{"SoulPaymentPatch","SoulPaymentCleanupPatch"})
            harmony.CreateClassProcessor(typeof(Soul).Assembly.GetType("HollowKnight.HollowKnightCode.Patches."+type)!).Patch();
        var beforeInfo=Harmony.GetPatchInfo(before);var wrapperInfo=Harmony.GetPatchInfo(wrapper);
        Assert(beforeInfo.Prefixes.Any(p=>p.owner==harmony.Id),"Payment prefix did not install on real Hook.BeforeCardPlayed");
        Assert(wrapperInfo.Prefixes.Any(p=>p.owner==harmony.Id)&&wrapperInfo.Postfixes.Any(p=>p.owner==harmony.Id)&&wrapperInfo.Finalizers.Any(p=>p.owner==harmony.Id),"Payment scope wrapper/finalizer did not install on game OnPlayWrapper");
    }finally{harmony.UnpatchAll(harmony.Id);}
});
Check("soul.wrapper_postfix_and_finalizer_restore_scope_idempotently",()=>{
    var patch=typeof(Soul).Assembly.GetType("HollowKnight.HollowKnightCode.Patches.SoulPaymentCleanupPatch")!;
    var prefix=patch.GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic)!;
    var postfix=patch.GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic)!;
    var finalizer=patch.GetMethod("Finalizer",BindingFlags.Static|BindingFlags.NonPublic)!;
    var c=Card<VengefulSpirit>(player,CardType.Attack,33);Tracker("QueuePayment",c,33);var prefixArgs=new object?[]{c,null};prefix.Invoke(null,prefixArgs);var scope=prefixArgs[1]!;
    var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    async Task ReservedPlay(){await gate.Task;Assert(SoulSpendTracker.AmountSpent(Play(c,player))==33);}
    var playTask=ReservedPlay();var postfixArgs=new object[]{scope,playTask};postfix.Invoke(null,postfixArgs);
    finalizer.Invoke(null,new object[]{scope});finalizer.Invoke(null,new object[]{scope});
    var freeScope=SoulSpendTracker.BeginScope(c);Assert(SoulSpendTracker.AmountSpent(Play(c,player))==0);
    SoulSpendTracker.RestoreCallerScope(freeScope);SoulSpendTracker.FinishScope(freeScope);gate.SetResult();
    ((Task)postfixArgs[1]).GetAwaiter().GetResult();Assert(SoulSpendTracker.AmountSpent(Play(c,player))==0);
});

int retainedCoreGroups=checks.Count;
var balanceHarness=new Harmony("hollowknight.balance.constructor-boundary");
balanceHarness.Patch(typeof(Godot.StringName).GetConstructor(new[]{typeof(string)}),new HarmonyMethod(typeof(EngineBoundary),nameof(EngineBoundary.SkipRegistration)));
balanceHarness.Patch(typeof(AbstractModel).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,Type.EmptyTypes,null),new HarmonyMethod(typeof(EngineBoundary),nameof(EngineBoundary.SkipRegistration)));
balanceHarness.Patch(typeof(BaseLib.Patches.Content.CustomContentDictionary).GetMethod("AddModel"),new HarmonyMethod(typeof(EngineBoundary),nameof(EngineBoundary.SkipRegistration)));
try{
Check("balance.focus_and_deepfocus_real_constructor_upgrade_data",()=>{
    var p=NewPlayer();var focus=Canonical<Focus>(p);var deep=Canonical<DeepFocus>(p);
    Assert(focus.EnergyCost.Canonical==1&&focus.DynamicVars.Heal.BaseValue==6&&CustomResources<Soul>.CanonicalCost(focus)==33);
    Assert(focus.CanonicalKeywords.Contains(CardKeyword.Exhaust));Upgrade(focus);Assert(focus.DynamicVars.Heal.BaseValue==9);
    Assert(deep.EnergyCost.Canonical==2&&deep.DynamicVars.Heal.BaseValue==16&&CustomResources<Soul>.CanonicalCost(deep)==33);
    Assert(deep.CanonicalKeywords.Contains(CardKeyword.Exhaust));Upgrade(deep);Assert(deep.DynamicVars.Heal.BaseValue==22);
    Assert(Canonical<SoulFlow>(p).CanonicalKeywords.Contains(CardKeyword.Exhaust));
});
Check("balance.strength_and_cost_changes_use_real_upgrade_methods",()=>{
    // FragileStrength is retired; keep permanent and temporary nail-strength
    // upgrade coverage on the two surviving roles instead.
    var p=NewPlayer();var permanent=Canonical<UnbreakableStrength>(p);var temporary=Canonical<Whetstone>(p);
    Assert(permanent.DynamicVars["UnbreakableStrength"].BaseValue==2);Upgrade(permanent);Assert(permanent.DynamicVars["UnbreakableStrength"].BaseValue==3);
    Assert(temporary.DynamicVars["Whetstone"].BaseValue==3);Upgrade(temporary);Assert(temporary.DynamicVars["Whetstone"].BaseValue==4);
    var coiled=Canonical<CoiledNail>(p);Assert(coiled.EnergyCost.Canonical==3);Upgrade(coiled);Assert(coiled.EnergyCost.GetWithModifiers((CostModifiers)0)==2);
    Assert(Canonical<HatchlingBurst>(p).EnergyCost.Canonical==2);Assert(Power<SoulTyrantPower>(p.Creature,1).StackType==PowerStackType.Single);
});
Check("balance.darkshrine_actual_canonical_and_upgrade_values",()=>{
    var p=NewPlayer();var c=Canonical<DarkShrine>(p);Assert(c.DynamicVars.Block.BaseValue==6&&c.DynamicVars["Blessing"].BaseValue==3);
    Assert(c.DynamicVars["SoulGain"].BaseValue==11&&c.GainsBlock&&CustomResources<Soul>.CanonicalCost(c)==-1);
    Upgrade(c);Assert(c.DynamicVars.Block.BaseValue==9&&c.DynamicVars["Blessing"].BaseValue==4);
    Assert(c.DynamicVars["SoulGain"].BaseValue==11);
});
Check("balance.coiled_adds_one_hit_and_never_creates_zero_hit_attack",()=>{
    var p=NewPlayer();var card=Canonical<OldNail>(p);var attack=DamageCmd.Attack(6).FromKnightCard(card,Play(card,p));var power=Power<CoiledNailPower>(p.Creature,9);
    Assert(power.StackType==PowerStackType.Single);Assert(power.ModifyAttackHitCount(attack,0)==0&&power.ModifyAttackHitCount(attack,-1)==-1);
    Assert(power.ModifyAttackHitCount(attack,1)==2&&power.ModifyAttackHitCount(attack,3)==4,"Coiled must add exactly one hit independent of Amount");
    var spell=Canonical<VengefulSpirit>(p);Assert(power.ModifyAttackHitCount(DamageCmd.Attack(6).FromKnightCard(spell,Play(spell,p)),1)==1);
});
Check("balance.sharpened_zero_hit_does_not_consume_first_attack_bonus",()=>{
    var p=NewPlayer();var card=Canonical<OldNail>(p);var attack=DamageCmd.Attack(6).FromKnightCard(card,Play(card,p));var relic=Mutable<SharpenedNail>();Set(relic,typeof(RelicModel),"_owner",p);
    Assert(relic.ModifyAttackHitCount(attack,0)==0&&relic.ModifyAttackHitCount(attack,-2)==-2);
    Assert(relic.ModifyAttackHitCount(attack,1)==2);Assert(relic.ModifyAttackHitCount(attack,1)==1);
    relic.AfterPlayerTurnStart(null!,p).GetAwaiter().GetResult();Assert(relic.ModifyAttackHitCount(attack,1)==2);
});
Check("balance.soul_aegis_block_floor_and_card_classification",()=>{
    var p=NewPlayer();var c=Canonical<SoulAegis>(p);Assert(c.GainsBlock&&c.CanonicalKeywords.Contains(CardKeyword.Exhaust));
    foreach(var pair in new[]{(-4,0),(0,0),(1,0),(2,1),(33,16),(66,33),(99,49)})Assert(SoulAegis.BlockFromSoul(pair.Item1)==pair.Item2);
    Assert(c.EnergyCost.Canonical==1);Upgrade(c);Assert(c.EnergyCost.GetWithModifiers((CostModifiers)0)==0);
});
Check("balance.soul_aegis_actual_onplay_converts_half_soul_and_records_full_payment",()=>{
    var harmony=new Harmony("hollowknight.balance.block-boundary");
    harmony.Patch(typeof(CreatureCmd).GetMethod(nameof(CreatureCmd.GainBlock),new[]{typeof(Creature),typeof(decimal),typeof(ValueProp),typeof(CardPlay),typeof(bool)}),new HarmonyMethod(typeof(CommandBoundary),nameof(CommandBoundary.GainBlock)));
    try{
        foreach(var pair in new[]{(1,0m),(33,16m),(99,49m)}){
            var p=NewPlayer();var c=Canonical<SoulAegis>(p);var soul=CustomResources<Soul>.Get(p.PlayerCombatState!);soul.Amount=pair.Item1;var cast=Play(c,p);CommandBoundary.Blocks.Clear();
            OnPlay(c,cast);var block=CommandBoundary.Blocks.Single();Assert(block.Amount==pair.Item2&&block.Props==ValueProp.Unpowered&&block.Play==cast);
            Assert(soul.Amount==0&&SoulSpendTracker.AmountSpent(cast)==pair.Item1);
        }
    }finally{harmony.UnpatchAll(harmony.Id);}
});
Check("balance.silken_aegis_uses_actual_variables_and_caps_base_block",()=>{
    var p=NewPlayer();var card=Canonical<SilkenAegis>(p);var pet=Pet<WeaverlingPet>(p,8);
    Assert(card.GainsBlock&&card.DynamicVars["Multiplier"].BaseValue==2&&card.DynamicVars["MaxBlock"].BaseValue==30);
    Assert(card.BlockFromCompanion(pet)==16);Set(pet,typeof(Creature),"_currentHp",24);Assert(card.BlockFromCompanion(pet)==30);
    Upgrade(card);Assert(card.DynamicVars["Multiplier"].BaseValue==3&&card.DynamicVars["MaxBlock"].BaseValue==40);
    Assert(card.BlockFromCompanion(pet)==40);Set(pet,typeof(Creature),"_currentHp",8);Assert(card.BlockFromCompanion(pet)==24);
});
Check("balance.companion_growth_cap_heals_even_when_maxed",()=>{
    Assert(KnightSwarm.MaxCompanionHp==24);
    Assert(KnightSwarm.CalculateGrowth(8,12,8)==(16,20));Assert(KnightSwarm.CalculateGrowth(12,20,10)==(22,24));
    Assert(KnightSwarm.CalculateGrowth(7,24,9)==(16,24));Assert(KnightSwarm.CalculateGrowth(24,24,9)==(24,24));
    Assert(KnightSwarm.CalculateGrowth(7,24,0)==(7,24));Assert(KnightSwarm.CalculateGrowth(7,24,-5)==(7,24));
    Assert(KnightSwarm.CalculateGrowth(12,20,int.MaxValue)==(24,24));
});
Check("balance.companion_grow_executes_real_creature_hp_commands",()=>{
    var p=NewPlayer();var pet=Pet<WeaverlingPet>(p,7);Set(pet,typeof(Creature),"_maxHp",24);
    KnightSwarm.Grow(pet,9).GetAwaiter().GetResult();Assert(pet.CurrentHp==16&&pet.MaxHp==24);
    KnightSwarm.Grow(pet,99).GetAwaiter().GetResult();Assert(pet.CurrentHp==24&&pet.MaxHp==24);
    Set(pet,typeof(Creature),"_currentHp",0);KnightSwarm.Grow(pet,9).GetAwaiter().GetResult();Assert(pet.CurrentHp==0&&pet.MaxHp==24);
});
Check("balance.initial_summon_clamps_hp_and_repeated_summon_grows_same_pet",()=>{
    var harmony=new Harmony("hollowknight.balance.pet-scene-boundary");
    var addPet=typeof(PlayerCmd).GetMethods().Single(m=>m.Name==nameof(PlayerCmd.AddPet)&&m.IsGenericMethodDefinition).MakeGenericMethod(typeof(WeaverlingPet));
    harmony.Patch(addPet,new HarmonyMethod(typeof(CommandBoundary),nameof(CommandBoundary.AddPet)));
    try{
        foreach(int health in new[]{0,99}){
            var p=NewPlayer();var powers=(List<PowerModel>)typeof(Creature).GetField("_powers",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p.Creature)!;powers.Add(Power<SwarmGuardPower>(p.Creature,1));
            var pet=Creature(CombatSide.Player);Set(pet,typeof(Creature),"<Monster>k__BackingField",Mutable<WeaverlingPet>());Set(pet,typeof(Creature),"_petOwner",p);
            CommandBoundary.CreatedPet=pet;CommandBoundary.AddPetCount=0;
            var summoned=KnightSwarm.Summon<WeaverlingPet>(null!,p,health).GetAwaiter().GetResult();int expected=health==0?1:24;
            Assert(summoned==pet&&pet.CurrentHp==expected&&pet.MaxHp==expected);Assert(CommandBoundary.AddPetCount==1&&p.PlayerCombatState!.Pets.Count==1);
            if(health==99){
                Set(pet,typeof(Creature),"_currentHp",5);
                Assert(KnightSwarm.Summon<WeaverlingPet>(null!,p,10).GetAwaiter().GetResult()==pet);
                Assert(pet.CurrentHp==15&&pet.MaxHp==24&&CommandBoundary.AddPetCount==1&&p.PlayerCombatState.Pets.Count==1);
            }
        }
    }finally{CommandBoundary.CreatedPet=null;harmony.UnpatchAll(harmony.Id);}
});
Check("balance.attack_spell_classification_preserves_resource_skill_identity",()=>{
    var p=NewPlayer();foreach(var c in new CardModel[]{Canonical<Focus>(p),Canonical<DeepFocus>(p),Canonical<SoulFlow>(p)})
        Assert(NailRules.IsSpellCard(c)&&!NailRules.IsAttackSpell(c));
    Assert(NailRules.IsAttackSpell(Canonical<VengefulSpirit>(p)));Assert(!NailRules.IsAttackSpell(Canonical<OldNail>(p)));Assert(!NailRules.IsAttackSpell(null));
});
Check("balance.spellcharge_resource_skills_keep_charges_and_attack_uses_snapshot",()=>{
    var p=NewPlayer();var power=Power<SpellchargePower>(p.Creature,3);
    foreach(var c in new CardModel[]{Canonical<Focus>(p),Canonical<DeepFocus>(p),Canonical<SoulFlow>(p)}){
        var cp=Play(c,p);power.BeforeCardPlayed(cp).GetAwaiter().GetResult();power.AfterCardPlayed(null!,cp).GetAwaiter().GetResult();
    }
    var spell=Canonical<VengefulSpirit>(p);Assert(power.DisplayAmount==3&&power.KnightDamageAdditive(enemy,5,ValueProp.Move,p.Creature,spell,null)==9);
    var cast=Play(spell,p);power.BeforeCardPlayed(cast).GetAwaiter().GetResult();Assert(power.DisplayAmount==0);
    Assert(power.KnightDamageAdditive(enemy,5,ValueProp.Move,p.Creature,spell,cast)==9);Assert(power.KnightDamageAdditive(enemy,5,ValueProp.Move,p.Creature,spell,cast)==9);
    power.AfterCardPlayed(null!,cast).GetAwaiter().GetResult();Assert(power.KnightDamageAdditive(enemy,5,ValueProp.Move,p.Creature,spell,cast)==0);
});
Check("balance.shrine_survives_resource_skills_and_consumes_attack_spell",()=>{
    var p=NewPlayer();var power=Power<ShrineBlessingPower>(p.Creature,4);var harmony=new Harmony("hollowknight.balance.remove-boundary");CommandBoundary.Removed.Clear();
    harmony.Patch(typeof(PowerCmd).GetMethod(nameof(PowerCmd.Remove),new[]{typeof(PowerModel)}),new HarmonyMethod(typeof(CommandBoundary),nameof(CommandBoundary.RemovePower)));
    try{
        foreach(var c in new CardModel[]{Canonical<Focus>(p),Canonical<DeepFocus>(p),Canonical<SoulFlow>(p)})power.AfterCardPlayed(null!,Play(c,p)).GetAwaiter().GetResult();
        Assert(CommandBoundary.Removed.Count==0);var spell=Canonical<VengefulSpirit>(p);Assert(power.KnightDamageAdditive(enemy,5,ValueProp.Move,p.Creature,spell,null)==4);
        power.AfterCardPlayed(null!,Play(spell,p)).GetAwaiter().GetResult();Assert(CommandBoundary.Removed.SequenceEqual(new PowerModel[]{power}));
    }finally{harmony.UnpatchAll(harmony.Id);}
});
Check("balance.soul_nourishment_still_rewards_real_skill_resource_payment",()=>{
    var p=NewPlayer();var focus=Canonical<Focus>(p);var soul=CustomResources<Soul>.Get(p.PlayerCombatState!);soul.Amount=33;
    Assert(soul.Spend<Soul>(null!,focus,33,false).GetAwaiter().GetResult());var cast=Play(focus,p);var power=Power<SoulNourishmentPower>(p.Creature,2);int flashes=0;power.Flashed+=_=>flashes++;
    power.AfterCardPlayed(null!,cast).GetAwaiter().GetResult();Assert(flashes==1&&SoulSpendTracker.AmountSpent(cast)==33);
});

var attackHarness=new Harmony("hollowknight.balance.attack-execution-boundary");
attackHarness.Patch(typeof(AttackCommand).GetMethod(nameof(AttackCommand.Execute)),new HarmonyMethod(typeof(CommandBoundary),nameof(CommandBoundary.Attack)));
attackHarness.Patch(typeof(CardPileCmd).GetMethod(nameof(CardPileCmd.Draw),new[]{typeof(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext),typeof(decimal),typeof(Player),typeof(bool)}),new HarmonyMethod(typeof(CommandBoundary),nameof(CommandBoundary.Draw)));
attackHarness.Patch(typeof(CreatureCmd).GetMethod(nameof(CreatureCmd.GainBlock),new[]{typeof(Creature),typeof(decimal),typeof(ValueProp),typeof(CardPlay),typeof(bool)}),new HarmonyMethod(typeof(CommandBoundary),nameof(CommandBoundary.GainBlock)));
try{
Check("balance.channelled_nail_real_onplay_costs_two_energy_and_keeps_three_hits",()=>{
    var p=NewPlayer();var card=Canonical<ChannelledNail>(p);
    foreach(decimal expected in new[]{5m,6m}){
        Assert(card.EnergyCost.Canonical==2&&card.DynamicVars.Repeat.IntValue==3);
        Assert(card.DynamicVars.Damage.BaseValue==expected&&card.GathersSoul&&!NailRules.IsSpellCard(card));
        CommandBoundary.Attacks.Clear();OnPlay(card,TargetedPlay(card,p));var captured=CommandBoundary.Attacks.Single();
        Assert(captured.Hits==3&&captured.Damage==expected&&captured.Source==card);
        Assert(captured.Hits*captured.Damage==(expected==5?15:18));
        if(expected==5)Upgrade(card);
    }
});
Check("balance.mothwing_real_onplay_0_65_66_99_soul_boundary_and_upgrade",()=>{
    foreach(bool upgraded in new[]{false,true}){
        var p=NewPlayer();var card=Canonical<MothwingDash>(p);if(upgraded)Upgrade(card);
        Assert(card.EnergyCost.Canonical==1&&card.GainsBlock&&CustomResources<Soul>.CanonicalCost(card)==-1);
        Assert(card.DynamicVars.Cards.IntValue==1&&card.DynamicVars["BonusCards"].IntValue==1&&card.DynamicVars["SoulThreshold"].IntValue==66);
        foreach(int amount in new[]{0,65,66,99}){
            var soul=CustomResources<Soul>.Get(p.PlayerCombatState!);soul.Amount=amount;var cast=Play(card,p);
            CommandBoundary.Blocks.Clear();CommandBoundary.Draws.Clear();OnPlay(card,cast);
            var block=CommandBoundary.Blocks.Single();Assert(block.Amount==(upgraded?9:6)&&block.Props==ValueProp.Move&&block.Play==cast);
            Assert(CommandBoundary.Draws.Single()==(amount>=66?2:1));
            Assert(soul.Amount==amount&&SoulSpendTracker.AmountSpent(cast)==0,"Mothwing must bank SOUL without paying it");
        }
    }
});
Check("balance.mothwing_snapshots_soul_before_block_listeners",()=>{
    var p=NewPlayer();var card=Canonical<MothwingDash>(p);var soul=CustomResources<Soul>.Get(p.PlayerCombatState!);
    foreach(var pair in new[]{(65,99,1m),(66,0,2m)}){
        soul.Amount=pair.Item1;CommandBoundary.Draws.Clear();CommandBoundary.AfterBlock=()=>soul.Amount=pair.Item2;
        try{OnPlay(card,Play(card,p));}finally{CommandBoundary.AfterBlock=null;}
        Assert(CommandBoundary.Draws.Single()==pair.Item3,"Block-triggered SOUL changes altered the already-snapshotted draw bonus");
        Assert(soul.Amount==pair.Item2);
    }
});
Check("balance.soul_vessel_real_threshold_block_and_owner_turn",()=>{
    var p=NewPlayer();var relic=Mutable<SoulVessel>();Set(relic,typeof(RelicModel),"_owner",p);
    var soul=CustomResources<Soul>.Get(p.PlayerCombatState!);
    Assert(SoulVessel.Threshold==Soul.SpellCost*2&&SoulVessel.Threshold==66&&SoulVessel.BlockOnHold==8);
    foreach(int amount in new[]{65,66,99}){
        soul.Amount=amount;CommandBoundary.Blocks.Clear();
        relic.BeforeSideTurnEnd(null!,CombatSide.Player,new[]{p.Creature}).GetAwaiter().GetResult();
        Assert(CommandBoundary.Blocks.Count==(amount>=SoulVessel.Threshold?1:0));
        if(amount>=SoulVessel.Threshold){var block=CommandBoundary.Blocks.Single();Assert(block.Amount==SoulVessel.BlockOnHold&&block.Props==ValueProp.Unpowered&&block.Play==null);}
        Assert(soul.Amount==amount,"Soul Vessel checks the bank without spending it");
    }
    CommandBoundary.Blocks.Clear();relic.BeforeSideTurnEnd(null!,CombatSide.Enemy,new[]{enemy}).GetAwaiter().GetResult();
    Assert(CommandBoundary.Blocks.Count==0,"Soul Vessel must only block at its owner's turn end");
});
Check("balance.retired_card_aliases_are_scoped_to_exact_knight_card_ids",()=>{
    var category=ModelDb.GetId<VengefulSpirit>().Category;
    // Keep this explicit and independent of RetiredCards.Resolve: reading the
    // production table would hide an accidentally omitted or wrong migration.
    var expected=new[]{
        ("HOLLOWKNIGHT-SOUL_BOLT",ModelDb.GetId<VengefulSpirit>()),
        ("HOLLOWKNIGHT-HUM",ModelDb.GetId<QuickSlash>()),
        ("HOLLOWKNIGHT-HALLOWNESTS_ECHO",ModelDb.GetId<AbyssShriek>()),
        ("HOLLOWKNIGHT-BONE_BREAK",ModelDb.GetId<HeavyBlow>()),
        ("HOLLOWKNIGHT-FRAGILE_STRENGTH",ModelDb.GetId<UnbreakableStrength>()),
        ("HOLLOWKNIGHT-NAIL_FLURRY",ModelDb.GetId<NailStorm>()),
        ("HOLLOWKNIGHT-SPORE_BREATH",ModelDb.GetId<DarkShrine>()),
        ("HOLLOWKNIGHT-CATALYST",ModelDb.GetId<Whetstone>()),
        ("HOLLOWKNIGHT-DEFENDER_CREST",ModelDb.GetId<ThornsOfAgony>()),
        ("HOLLOWKNIGHT-VENOM_BLADE",ModelDb.GetId<WebSpin>()),
        ("HOLLOWKNIGHT-NAIL_ENVENOM",ModelDb.GetId<CoiledNail>()),
        ("HOLLOWKNIGHT-SHELL_SLAM",ModelDb.GetId<Dash>()),
        ("HOLLOWKNIGHT-IMPREGNABLE",ModelDb.GetId<Dreamshield>()),
        ("HOLLOWKNIGHT-WISP_AFTERIMAGE",ModelDb.GetId<BrokenShell>()),
        ("HOLLOWKNIGHT-WRAITH_FORM",ModelDb.GetId<ShadeCloak>()),
        ("HOLLOWKNIGHT-VOID_SURGE",ModelDb.GetId<Kingsoul>()),
        ("HOLLOWKNIGHT-REFINEMENT",ModelDb.GetId<SoulPact>()),
        ("HOLLOWKNIGHT-VOID_FEAST",ModelDb.GetId<SoulHarvest>()),
        ("HOLLOWKNIGHT-RAGING_NAIL",ModelDb.GetId<QuickSlash>()),
        ("HOLLOWKNIGHT-SOUL_RUSH",ModelDb.GetId<SoulFlow>()),
        ("HOLLOWKNIGHT-NAIL_SHOCKWAVE",ModelDb.GetId<CycloneSlash>())
    };
    Assert(expected.Length==21&&expected.Select(pair=>pair.Item1).Distinct().Count()==21);
    foreach(var (entry,replacement) in expected){
        var oldId=new ModelId(category,entry);Assert(RetiredCards.Resolve(oldId)==replacement,"Missing or incorrect migration: "+entry);
        var otherCategory=new ModelId(ModelDb.GetId<SoulVessel>().Category,entry);
        var foreign=new ModelId(category,entry.Replace("HOLLOWKNIGHT-","OTHER_MOD-"));
        var partial=new ModelId(category,entry+"_EXTRA");
        foreach(var id in new[]{replacement,otherCategory,foreign,partial})
            Assert(RetiredCards.Resolve(id)==id,"Current, foreign, partial or non-card IDs must stay unchanged: "+id);
    }
});
Check("balance.quickslash_real_onplay_builds_one_hit_4_or_6_damage",()=>{
    var p=NewPlayer();var card=Canonical<QuickSlash>(p);Assert(card.EnergyCost.Canonical==0&&card.DynamicVars.Damage.BaseValue==4);
    foreach(decimal expected in new[]{4m,6m}){
        CommandBoundary.Attacks.Clear();OnPlay(card,TargetedPlay(card,p));var captured=CommandBoundary.Attacks.Single();Assert(captured.Hits==1&&captured.Damage==expected&&captured.Source==card);
        if(expected==4)Upgrade(card);
    }
});
Check("balance.soul_unbound_actual_preview_and_command_share_charge_formula",()=>{
    var p=NewPlayer();var card=Canonical<SoulUnbound>(p);var old=BeginPreviewCombat(card,p);
    try{
        Assert(card.DynamicVars["CalculatedDamage"] is CalculatedDamageVar);
        foreach(var pair in new[]{(33,30m),(65,30m),(66,45m),(98,45m),(99,60m)}){
            var soul=CustomResources<Soul>.Get(p.PlayerCombatState!);soul.Amount=pair.Item1;var variable=card.DynamicVars.CalculatedDamage;
            variable.UpdateCardPreview(card,default,enemy,false);Assert(variable.PreviewValue==pair.Item2&&variable.Calculate(enemy)==pair.Item2);
            CommandBoundary.Attacks.Clear();var cast=TargetedPlay(card,p);OnPlay(card,cast);var captured=CommandBoundary.Attacks.Single();
            Assert(captured.Damage==pair.Item2&&captured.Calculated==null&&captured.Hits==1,"OnPlay must capture damage once before SOUL changes");Assert(soul.Amount==0&&SoulSpendTracker.AmountSpent(cast)==pair.Item1);
            Assert((decimal)typeof(AttackCommand).GetField("_damagePerHit",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(captured.Command)! == pair.Item2);
        }
        Upgrade(card);Assert(card.DynamicVars.CalculationBase.BaseValue==20);CustomResources<Soul>.Get(p.PlayerCombatState!).Amount=66;
        card.DynamicVars.CalculatedDamage.UpdateCardPreview(card,default,enemy,false);Assert(card.DynamicVars.CalculatedDamage.PreviewValue==50);
    }finally{EndPreviewCombat(old);}
});
Check("balance.sharp_assault_actual_preview_and_command_share_hit_threshold",()=>{
    var p=NewPlayer();var card=Canonical<SharpAssault>(p);var counter=Power<HitCounterPower>(p.Creature,1);((List<PowerModel>)typeof(Creature).GetField("_powers",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p.Creature)!).Add(counter);
    var hits=(SpireField<HitCounterPower,int>)typeof(HitCounterPower).GetField("Hits",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;var old=BeginPreviewCombat(card,p);
    try{
        Assert(card.DynamicVars["CalculatedDamage"] is CalculatedDamageVar);
        foreach(var pair in new[]{(0,5m),(3,5m),(4,10m),(5,10m)}){
            hits[counter]=pair.Item1;var variable=card.DynamicVars.CalculatedDamage;variable.UpdateCardPreview(card,default,enemy,false);Assert(variable.PreviewValue==pair.Item2);
            // Simulate a nested effect changing the counter, not ordinary within-command hit counting.
            CommandBoundary.Attacks.Clear();CommandBoundary.AfterCapture=()=>hits[counter]++;
            try{OnPlay(card,TargetedPlay(card,p));}finally{CommandBoundary.AfterCapture=null;}
            var captured=CommandBoundary.Attacks.Single();Assert(captured.Damage==pair.Item2&&captured.Calculated==null&&captured.Hits==1,"OnPlay must capture damage before nested effects alter the combo counter");
            Assert((decimal)typeof(AttackCommand).GetField("_damagePerHit",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(captured.Command)! == pair.Item2);
        }
        Upgrade(card);Assert(card.DynamicVars.CalculationBase.BaseValue==7);hits[counter]=4;card.DynamicVars.CalculatedDamage.UpdateCardPreview(card,default,enemy,false);Assert(card.DynamicVars.CalculatedDamage.PreviewValue==12);
    }finally{EndPreviewCombat(old);}
});
}finally{attackHarness.UnpatchAll(attackHarness.Id);}
}finally{balanceHarness.UnpatchAll(balanceHarness.Id);}

var dll=typeof(ShadeCloakPower).Assembly.Location;
var report=new {passed=failed==0,checkCount=checks.Count,retainedCoreGroups,newBalanceGroups=checks.Count-retainedCoreGroups,assertionCount=assertions,failedCount=failed,assemblyPath=dll,assemblySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dll))).ToLowerInvariant(),mode="Real mod/game/BaseLib DLL methods; synthetic managed state; no game scene or player save",engineBoundary="Godot OS/log printing are stubbed for one clamp check. Godot.StringName construction and ModelDb/content registration are bypassed for managed fixtures. AttackCommand.Execute, CardPileCmd.Draw, PowerCmd.Remove, CreatureCmd.GainBlock and PlayerCmd.AddPet scene creation are captured at command execution boundaries; complete damage/block/scene effects are not exercised. CreatureCmd HP commands, resource spending, canonical/upgraded card data and mod formulas are real.",checks};
string json=JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true});Console.WriteLine(json);
if(args.Length>0)File.WriteAllText(args[0],json);
Environment.ExitCode=failed==0?0:1;

internal static class EngineBoundary{
    public static bool CommandLine(ref string[] __result){__result=[];return false;}
    public static bool HasFeature(ref bool __result){__result=false;return false;}
    public static bool LogPrint(){return false;}
    public static bool SkipRegistration(){return false;}
}

internal static class CommandBoundary{
    internal sealed record AttackRecord(int Hits,decimal Damage,CardModel? Source,CalculatedDamageVar? Calculated,AttackCommand Command);
    public static List<AttackRecord> Attacks{get;}=[];
    public static List<PowerModel> Removed{get;}=[];
    internal sealed record BlockRecord(decimal Amount,ValueProp Props,CardPlay? Play);
    public static List<BlockRecord> Blocks{get;}=[];
    public static List<decimal> Draws{get;}=[];
    public static Action? AfterCapture;
    public static Action? AfterBlock;
    public static Creature? CreatedPet;
    public static int AddPetCount;
    public static bool Attack(AttackCommand __instance,ref Task<AttackCommand> __result){
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var calculated=(CalculatedDamageVar?)typeof(AttackCommand).GetField("_calculatedDamageVar",flags)!.GetValue(__instance);
        decimal damage=calculated?.Calculate(null)??(decimal)typeof(AttackCommand).GetField("_damagePerHit",flags)!.GetValue(__instance)!;
        int hits=(int)typeof(AttackCommand).GetField("_hitCount",flags)!.GetValue(__instance)!;
        Attacks.Add(new(hits,damage,__instance.ModelSource as CardModel,calculated,__instance));AfterCapture?.Invoke();__result=Task.FromResult(__instance);return false;
    }
    public static bool Draw(decimal __1,ref Task<IEnumerable<CardModel>> __result){Draws.Add(__1);__result=Task.FromResult(Enumerable.Empty<CardModel>());return false;}
    public static bool RemovePower(PowerModel power,ref Task __result){Removed.Add(power);__result=Task.CompletedTask;return false;}
    public static bool GainBlock(decimal amount,ValueProp props,CardPlay? cardPlay,ref Task<decimal> __result){Blocks.Add(new(amount,props,cardPlay));AfterBlock?.Invoke();__result=Task.FromResult(amount);return false;}
    public static bool AddPet(Player player,ref Task<Creature> __result){
        var pet=CreatedPet??throw new InvalidOperationException("No pet scene fixture configured");
        ((List<Creature>)typeof(PlayerCombatState).GetField("_pets",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(player.PlayerCombatState)!).Add(pet);
        AddPetCount++;__result=Task.FromResult(pet);return false;
    }
}
