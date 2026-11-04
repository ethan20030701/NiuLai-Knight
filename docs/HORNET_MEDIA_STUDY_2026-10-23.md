# 大黄蜂 mod 的声音、动画与特效研究（2026-10-23）

本研究针对本机已订阅版本，只读检查工坊包、反编译 C#、读取 Godot 场景和编译脚本常量；未使用网页描述替代实际实现，未复制大黄蜂的声音、图片、动画或代码进入小骑士资源。小骑士保持沉默，新增声音均为原创程序合成。

## 研究对象与证据

- 工坊包：`C:/Program Files (x86)/Steam/steamapps/workshop/content/2868840/3747589529/HornetMod.pck`。
- PCK SHA-256：`77EE62AE0A31EA8C2A2D10DD7208138965C5D41228988B0776E2105C0C9DEDDF`。
- PCK 格式 `(3, 4, 5, 1, 2)`，491 项；读取器为项目现有 `tools/pck_extract.py`，本次用 `--list` 及 `read_table()` 在内存读取需要的条目，未修改工坊包。
- C# 证据：`.build/hornet-research-20261023/source/`。
- 当前游戏音频 API 证据：`.build/hornet-research-20261023/api/` 下 `NAudioManager.cs`、`NDebugAudioManager.cs`、`NSfxVolumeSlider.cs`、`NMasterVolumeSlider.cs`、`SettingsSave.cs`、`PrefsSave.cs`。

## 到底有什么声音

包内有 46 个原始 WAV（4,316,572 字节）、1 个 `HornetMod.bank`（287,840 字节）、1 个 `GUIDs.txt`，另有 4 个历史导入 sidecar 和 4 个导入后的 `.sample`。WAV 头已实际解析，45 个为 48 kHz、立体声、16 bit；`hornet_bind.wav` 为 44.1 kHz、立体声、16 bit。所有声音时长为 0.20–1.239 秒。

以下分类依据调用用途，而非只依据文件名；本次未做逐文件听音转写，因而不声称还原每段实际台词。

| 类别 | 数量 | 实际调用证据 | 时长 |
| --- | ---: | --- | --- |
| 普通攻击语音 | 14 | `HornetAudio.DefaultAttackVoices`，由 `HornetAttackVoicePatch` 在攻击牌开始时调用 | 0.20–0.36 秒 |
| 丝线/蓄力攻击语音 | 10 | `SilkAttackVoices`，对 `HornetChargeSlash` 或 `ISilkCostCard` 分流 | 0.28–0.60 秒 |
| 环刃语音 | 4 | `RingVoices`，对 `HornetThrowingRing` 分流 | 0.76–0.88 秒 |
| 受伤语音 | 6 | `HornetHurtVoicePatch` 调用 `HurtVoices` | 0.20–0.40 秒 |
| 挑战语音 | 3 | `HornetChallenge.OnPlay` 自己调用 | 0.60–0.64 秒 |
| 决斗语音 | 5 | `HornetDuel.OnPlay` 自己调用 | 0.70–1.233 秒 |
| 绑定语音 | 3 | `HornetBindThread.OnPlay` 的 `hornet_bind1/3/4` 随机数组 | 0.52–0.60 秒 |
| 绑定动作声音 | 1 | 同一张牌另行 `PlayWav("hornet_bind.wav")`，和语音独立播放 | 1.239 秒 |

此外，受伤会叠加原版 `event:/sfx/characters/silent/silent_hurt`，死亡音效被改为原版 `silent_die`。因此，听到的部分声音来自游戏原有音库，不全是 mod 自带 WAV。

**本版未发现自定义 BGM 的实现。** 检索 C# 没有音乐播放/替换入口，`CharacterAudioAssetSet` 为 `null`，8 个场景没有音频播放器节点；`GUIDs.txt` 中 46 个自定义事件均位于 `event:/sfx/hornet/` 并对应上述 WAV。GUID 文件里的 `bus:/master/music` 是总线路由标识，不能据此认定它带了背景音乐。包内没有长音乐 WAV/OGG/MP3，也没有自定义音乐事件映射。工坊截图不能证明运行时曾替换音乐。

## 播放链路、设置与边界

`Entry.TryInitFmodBank()` 通过 RitsuLib 注册 bank 和 GUID 映射；收到 `DeferredInitializationCompletedEvent` 后，切换到 FMOD 事件模式。

- 事件模式：`PlayOneShot("event:/sfx/hornet/" + basename, 0.6f)`。
- 初始化完成前的文件模式：`FmodStudioStreamingFiles.TryPlaySoundFile("res://audio/" + fileName, 0.6f, 1f)`。
- bank 初始化异常有日志，保持文件模式；切换到事件模式后，单次事件失败没有在这层显式重新尝试 WAV。
- mod 自己没有音量 UI、语音开关、播放间隔、并发限制或音高随机变化；音量固定 0.6，文件模式的 pitch 固定 1。
- 随机变体使用 `Rng.Chaotic`，不是牌序或掉落的确定性随机流。`PlayRandomVoice(Player player, ...)` 的 `player` 参数未被使用；没有按本地玩家过滤。这不等于已证实联机故障，只说明每台客户端对其看到的事件都可播放声音。
- 一般攻击由 `CardPlayStarted` 触发一次；挑战、决斗、绑定显式排除，避免自己播完再播普通语音。受伤语音用 `LoseHpInternal`，额外受伤 SFX 用 `Hook.AfterDamageReceived`；多段伤害可能密集触发，未见节流。
- C# 层没有音频实例句柄、停止或释放逻辑；具体一次性实例回收由 RitsuLib/FMOD 承担，本次未反编译库实现，因此不能宣称已完整验证该层清理策略。
- 没有基于游戏快进速度调整语音或 pitch 的实现；声音是短的一次性反馈，未参与等待和战斗结算。

## 动画与特效的实际组成

角色使用 `hornet_character.tscn`：`%Visuals` 是带区域裁切的 `Sprite2D`，挂 `hornet_visuals.gd`；`SlashVfx` 是独立同级精灵。包内有实际导入的 idle、weak、attack、hurt、cast、die、blade 七张 sprite sheet，不是仅有静态选人图。

C# 将引擎事件桥接到脚本：

- `NCreature.SetAnimationTrigger` 的 Attack/Hit/Cast，分别 deferred 调用 `trigger_attack` / `trigger_hurt` / `trigger_cast`。事件桥对具备对应方法的精灵进行能力检测，没有强制转换所有角色。
- `StartDeathAnim`、游戏结束及场景初始死亡同步，调用 `trigger_die`。
- `ImmediatelySetIdle` 调用 `trigger_idle`。
- HP 变化和场景初始化同步低血量状态；阈值为剩余 HP 比例 ≤ 25%，调用 `set_low_hp`。
- 角色脚本有 `set_facing`、帧计时器、`_process(delta)`、独立刀光更新等方法。对编译 GDC 做 zstd 解压后读取了标识符及常量，确认这些方法与七张 sheet 的路径；不是根据贴图命名猜测功能。常量包括 6/9/10/27/24 帧及 8/12/15 FPS 等，但没有完整复原其 token 流，本报告不据此武断给每个动作指定确切播放时间或完整优先级。
- 角色卡牌命中大量复用原版 `vfx/vfx_attack_slash`；`GetArchitectAttackVfx` 也是原版 blunt/heavy_blunt/slash/bloody_impact/rock_shatter 资源列表。未发现大规模独立技能粒子或自定义着色器系统。

场景没有自带音频节点；声音和动作分别由战斗事件驱动。动态动画使用已有精灵区域，未见每击无限添加临时 VFX 节点的 C# 实现。商店使用同一个脚本，营火是独立静态场景。没有足够证据承诺脚本内部所有时序、打断与节点回收都完美，仍需真机体验。

## 我们已有的基础与适合借鉴之处

现有小骑士已经具备：

- `KnightCombatRig`：头、披风、握持骨钉和脚部独立动作，接触时点 0.065 秒，总攻击动作 0.22 秒；站立脚不随整体上下浮动。
- `KnightFeedback`：按实际 creature 查找自己的视觉节点，弱引用动画状态、受伤 tween 打断，魂/技能/能力各有 6 帧 24 FPS 的短特效；完成即 `QueueFree`。
- `KnightCombatAnimationPatch`：骨钉和法术分流，法术使用 Cast；魂法术命中替换为原版 starry impact。
- 自制魂能量球、卡牌拖尾、营火/商店场景和 79 帧约 2.7 秒死亡动画。
- 研究开始前，项目资源没有 WAV/OGG/MP3/bank，也没有短音效播放层。

最值得借鉴的是“行动类型分流、短反馈、专属技能覆盖通用动作、受伤与状态反馈”，并非把大黄蜂语音、长动作或每张牌同一种刀光搬过来。小骑士不需要说话；骨钉空气扫动、空壳受击、魂能与聚神的区别能更准确传达角色。已有骨架、死亡动画和帧特效应继续使用。

## 已新增的原创音效层

`HollowKnightCode/Character/KnightAudio.cs` 提供 `KnightAudio.Play(Node owner, string kind)` 和 `KnightAudio.Stop(Node owner)`，具体战斗调用由反馈层统一接入。kind 支持 nail、soul、guard、focus、hurt、void、power。

- 每个视觉 owner 只有一个可复用 `AudioStreamPlayer`，并发上限 1；父节点离开场景时自动停止并随父释放，不保留全局 owner 引用。
- 同类间隔 130–350 毫秒；不同类在 90 毫秒窗口内按优先级取舍，聚神/虚空可覆盖通用施法，受伤优先级最高，避免同一动作的通用和专属声音叠播。
- 所有声音 0.16–0.48 秒、不循环、不等待，不消耗任何随机数，也不修改结算或联机状态。
- 缺失文件只报一次并静音；无有效视觉 owner、未初始化设置、测试模式、非交互模式或缺少原生 SFX bus 时跳过。
- 使用原始 WAV 的 `AudioStreamWav.LoadFromFile`，兼容项目 quick PCK，无须从 Godot 导入 sidecar 才能加载。

**音量路由经过实际 API 核查。** 游戏的 FMOD `NAudioManager` 和 Godot `NDebugAudioManager` 都使用平方曲线；主音量和音效滑块同时更新这两条音频链。新增播放器直接走游戏已有 Godot `SFX`→`Master`，仅设置自身 cue gain 为 0.65，不能再重复乘一遍 master²×sfx²。`SettingsGain()` 的平方乘积仅用于静音判定，并在每个播放帧检查 `PrefsSave.MuteInBackground` 与 `NGame.IsGameFocusedWindow()`；背景静音时停止短声，不在回到游戏时补播。代码不新增、重命名或改写任何总线，不写入音量设置，也不触碰 FMOD 配置。

`tools/generate_knight_audio.py` 用 Python 标准库和固定种子生成 7 个原创短音效：空气噪声扫动、非谐振荡器、短包络、DC 去偏和首尾淡入淡出，不输入任何商业音频。来源说明在 `HollowKnight/audio/SOURCES.md`，`manifest.json` 记录种子、时长、峰值、RMS、SHA-256。7 个 WAV 总大小为 187,292 字节。

## 继承的占位角色声音需要明确处理

针对当前项目实际引用的 `.build/mods/BaseLib/BaseLib.dll`，而非只参考旧版本，进一步反编译确认：

- `PlaceholderCharacterModel.PlaceholderID` 默认 `ironclad`，`CustomAttackSfx`、`CustomCastSfx`、`CustomDeathSfx` 默认返回相应铁甲战士事件；这是新增小骑士音效可能和铁甲声音叠加的来源。
- 当前 `CharacterModel.AttackSfx`、`CastSfx`、`DeathSfx` 是非 virtual 属性，不能在小骑士类直接 `override`；BaseLib 提供的 `Custom*` 才是覆盖入口。`PowerUpSfx` 直接返回 `CastSfx`。角色没有对应的 `HurtSfx` 属性，受伤路径不要和怪物 `MonsterModel.HurtSfx` 混淆。
- `CreatureCmd.TriggerAnim` 在 Attack/Cast/PowerUp 时仍会直接调用相应角色 Sfx，之后才设置动画。
- BaseLib 的三个 getter patch 将 `Custom*` 赋给返回值；仅当结果为 null 时继续运行原版 getter。因此 null 表示回落到角色 ID 的原版事件路径，不能当作静音。
- 空字符串虽然会覆盖 getter，但 `SfxCmd.Play` 与 `NAudioManager.PlayOneShot` 都不做空值检查，仍将其交给 FMOD。未证实下游代理对空字符串静默处理前，不建议只把 `Custom*` 改成 `""`。

最终实现使用模组专用静默标记覆盖三个 `Custom*`，并在 `NAudioManager.PlayOneShot` 的 string/volume 与 string/parameters/volume 两个入口精准拦截该标记；这样不会给 FMOD 传入无效事件，也不改变其他角色的声音。相关证据保存在 `api/PlaceholderCharacterModel.cs`、`CustomCharacterModel.cs`、`BaseLib_AttackSfx.cs`、`BaseLib_CastSfx.cs`、`BaseLib_DeathSfx.cs`、`CharacterModel.cs`、`CreatureCmd.cs`、`SfxCmd.cs`。主任务已接入 `KnightInheritedAudioPatch`，原生媒体检查验证了两个重载的补丁与其他声音事件放行。

## 验证与待体验项目

音频生成验证记录在 `.build/hornet-research-20261023/audio-generation-verification.json`：7/7 通过；所有文件可解析、44.1 kHz mono 16 bit、首尾样本为零、峰值约 0.70 无削波、DC 绝对值 < 0.00001、重新生成字节完全一致。代码构建、PCK 装载、战斗钩子、单人及双小骑士的播放器隔离由主任务统一验证。这里只报告本次已经完成的检查，不把文件格式验证等同于耳听质量或联机实测。

本机确实装有原作 `C:/Program Files (x86)/Steam/steamapps/common/Hollow Knight/hollow_knight_Data`；资源位于 Unity `.assets` / `.resS`，不是可直接引用的松散 WAV。本次 Python 环境没有 UnityPy，未提取原作音频；新实现也不依赖原作安装。背景音乐继续沿用游戏，未因参考 mod 的标题或总线名额外添加音乐。

后续真机体验重点是：骨钉接触与声音是否合拍、聚神是否覆盖通用施法、快速多段攻击是否仍清晰、音效与音乐的响度关系、拖动 SFX/主音量到零和切出窗口时是否立即静音、两位小骑士各自拥有一个播放节点。声音质感可以在不改玩法的前提下继续细调。
