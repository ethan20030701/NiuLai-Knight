# 核心交互与平衡回归测试

这组 console 测试直接调用本工程编译的 HollowKnight.dll，以及本机的 sts2.dll、BaseLib.dll 和 0Harmony.dll。它不会安装模组、启动游戏场景或读取玩家存档。

Creature、Player、CardModel、PowerModel 的状态通过反射建立最小 fixture，避免启动模型库和 Godot 场景。伤害预览、真实伤害修改通知、SOUL 记账、伙伴选取与兼容桥接运行的是实际 DLL 的方法；没有复制业务算法。Harmony 测试还会在当前测试进程给游戏的六参数 ModifyDamageAdditive 安装补丁，并验证支付前缀、异步包装与 finalizer 能安装到游戏的实际方法；补丁在测试后移除。

真实 Soul.Spend 与 BaseLib.CustomResource.Spend 会直接执行。仅在非 optional 余额不足用例中，为日志输出屏蔽 Godot.OS.GetCmdlineArgs、HasFeature 和 ConsoleLogPrinter.Print 的引擎调用。资源付款、伤害钩子和 BaseLib 业务方法均未替换。测试不建立战斗监听器，传入 null combat state 使 BaseLib 的后续事件分发自然返回。

保留原先 32 组核心交互检查与 23 组平衡检查，目前共 55 组；断言数以本轮实际运行报告为准。核心检查覆盖重复预览纯度、真实修改通知、法术伤害边界、SOUL 扣款和免费 Replay/AutoPlay、手动付款不足、低魂返回、伙伴承伤选取、同卡嵌套、跨 await 作用域、跨战斗隔离和异常清理。

平衡检查执行牌的真实构造函数、CanonicalVars 与 OnUpgrade。为隔离模型库和引擎，只跳过 AbstractModel 的注册构造、BaseLib 内容注册，以及未使用的 Godot.StringName 创建；保留实际卡牌构造参数和升级行为。伙伴 Grow 直接运行 CreatureCmd.SetMaxHp/SetCurrentHp，初召仅替换 PlayerCmd.AddPet 的场景创建边界，实际 Summon、生命设定、重复召唤和上限恢复仍执行。

QuickSlash 和两张动态伤害牌运行真实 OnPlay，并捕获实际 AttackCommand 的段数、伤害和来源；AttackCommand.Execute、CardPileCmd.Draw、PowerCmd.Remove 只在测试进程替换成命令捕获。SoulAegis 运行真实 OnPlay，捕获 CreatureCmd.GainBlock 的基础格挡值、属性和来源，同时实际扣除 SOUL 并检查付款记录。动态预览使用游戏内建 CalculatedDamageVar.UpdateCardPreview/Calculate，建立最小 CombatManager turn-state 与卡牌战斗预览状态并在结束后恢复。SOUL 阶段、连击阈值、升级值、预览与实际命令一致，以及 OnPlay 计算一次后的伤害快照都受到检查。改变连击计数的用例模拟嵌套效果，不表示普通多段攻击逐段触发 HitCounter.AfterAttack。

本轮构筑检查还运行 ChannelledNail 的真实 OnPlay，核对 2 能量、基础及升级的三段 5/6 伤害；MothwingDash 核对 0/65/66/99 灵魂下的抽牌命令、格挡及零灵魂支出，以及升级保持 66 门槛。格挡命令边界内改变灵魂余额，验证额外抽牌使用打出时快照。DarkShrine 构造与升级检查核对 6/9 格挡、11 灵魂及 3/4 祝福；其真实产魂后支付复仇之魂、祝福仅生效一次，以及起手 10 张牌的检查由 `HKAudit.cs` 原生游戏审计执行。

清理版增加 SoulVessel 的真实回合结束逻辑检查：65/66/99 灵魂分别产生 0/8/8 格挡，且不消耗灵魂；别人的回合不会触发。2026-10-28 卡池精简后，累计 21 张退役卡不再保留对应 CardModel。核心测试独立列出全部 21 个旧 ID 的预期替代卡，逐一检查精确映射，以及现役 ID、不同类别、其他模组和相似 ID 保持原样。原生审计独立列出相同预期，经 CardModel.FromSerializable/ToSerializable 对每张验证基础与升级旧存档、加入牌组楼层和保存新 ID，并检查 83 个现役模型与卡池完全对应、80 张奖励来源中不存在旧卡；稀有度分布为普通 22、罕见 37、稀有 21，另有基础 2、衍生 1。CardUiPreview 同时检查全部 21 个退役模型均未注册，以及 19 张耗魂牌的原生 UI 覆盖完整。中英文66门槛文案、格林之子与织者之歌的公共虫群提示也由原生审计检查。

易碎力量退役后，其构造/升级断言改由仍现役的磨刀石覆盖临时骨钉加伤（3/4）；不碎力量的永久骨钉加伤（2/3）及螺旋骨钉费用升级检查继续保留。其余存续机制的测试未因删牌移除。

附魔迁移用原生 Sharp 附魔建立真实升级复仇之魂，再经 ToSerializable 将旧 ID 输入 FromSerializable；核对附魔类型/3 层、升级次数、加入牌组楼层和再次保存的新 ID，检查 26 基础伤害、29 点附魔预览与真实出牌伤害一致。该检查没有验证所有其他附魔或自定义永久属性，不能据此宣称所有存档属性均已逐项测试。

在本地项目根目录先运行 `tools/build_workspace.sh`（现有 WSL 环境），确保 `.build/mods/HollowKnight/HollowKnight.dll` 是最新构建，再用 Windows .NET 9 SDK 执行：

```powershell
.\.build\toolchains\dotnet9\dotnet.exe run --project tools/verification/core/CoreTests.csproj -c Release -- release/POOL_CORE_2026-10-28.json
```

本次便携 SDK 位于忽略的 `.build/toolchains/dotnet9`；如已安装 .NET 9，可将命令中的可执行路径改为 `dotnet`。测试工程直接引用 `.build/mods/HollowKnight/HollowKnight.dll`，不会再次构建 Mod。默认游戏路径是 Steam 的 public-beta 安装，BaseLib 从订阅目录读取。其他路径通过 MSBuild `GameDir` 与 `BaseLibPath` 属性指定；`HOLLOWKNIGHT_GAME_DATA_DIR` 可覆盖测试运行时附属 DLL 查找目录。

JSON 结果包含每个用例的通过情况、断言数和实际测试 DLL 的 SHA256。任一失败会返回非零退出码。Sentry 提示 GDExtension 未加载属于脱离游戏进程运行时的正常信息。`validation/core-interaction-baseline.json` 是修复前恢复 DLL 上的首批 11 组检查，5 组失败用于复现影披风和神龛加伤问题；新增的付款与作用域用例只在修复版运行，不能视为完整检查的修复前对照。`core-interaction-tests.json` 保留上一版 32 组通过报告，本次结果另存 `balance-interaction-tests.json`。

这些检查验证规则、钩子、升级数据与真实命令构建，不能替代完整伤害结算、抽牌和效果移除、动画、联机同步及战斗中读档测试。平衡候选版仍需按验收清单进行游戏内试玩，并根据实际战绩继续调整数值。
