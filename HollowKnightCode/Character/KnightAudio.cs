using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace HollowKnight.HollowKnightCode.Character;

/// <summary>
/// Non-vocal, original combat cues. Each visual owner has one reusable player.
/// Godot file playback uses the game's existing SFX -> Master bus chain.
/// It never changes either audio engine's buses or gameplay random state.
/// </summary>
public static class KnightAudio
{
    // BaseLib's placeholder getters otherwise retain Ironclad combat voices.
    // KnightInheritedAudioPatch consumes this exact marker before FMOD sees it.
    public const string ProceduralCue = "HollowKnight:procedural-cue";
    private const string PlayerName = "KnightAudio";
    private static readonly Dictionary<string, AudioStreamWav?> Streams = new();
    private static bool _warned;

    public static void Play(Node? owner, string kind)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner) || owner.IsQueuedForDeletion()
            || !owner.IsInsideTree() || SettingsGain() <= 0f)
        {
            return;
        }
        var (priority, cooldown) = kind switch
        {
            "nail" => (1, 130UL),
            "guard" => (1, 160UL),
            "power" => (2, 220UL),
            "soul" => (2, 170UL),
            "focus" => (3, 350UL),
            "void" => (3, 260UL),
            "hurt" => (4, 180UL),
            _ => (0, 0UL)
        };
        if (priority == 0) return;
        try
        {
            var player = owner.GetNodeOrNull<KnightAudioPlayer>(PlayerName);
            var now = Time.GetTicksMsec();
            if (player != null && !player.Accept(kind, priority, cooldown, now)) return;
            var stream = Load(kind);
            if (stream == null) return;
            if (player == null)
            {
                player = new KnightAudioPlayer { Name = PlayerName, MaxPolyphony = 1, Bus = "SFX" };
                owner.AddChild(player);
            }
            player.StartCue(stream, kind, priority, now);
        }
        catch (Exception e)
        {
            WarnOnce($"Knight audio skipped: {e.Message}");
        }
    }

    public static void Stop(Node? owner)
    {
        if (owner != null && GodotObject.IsInstanceValid(owner))
            owner.GetNodeOrNull<KnightAudioPlayer>(PlayerName)?.StopCue();
    }

    /// <summary>
    /// NAudioManager.SetMasterVol/SetSfxVol square their 0..1 slider values.
    /// NDebugAudioManager applies those same squared values to the existing
    /// Godot SFX/Master buses; the sliders update both audio engines live.
    /// The product here gates mute only: applying it again would attenuate
    /// twice. Read on every playing frame for background mute and fail closed
    /// if SFX is absent (Godot would otherwise silently route to Master).
    /// Missing/uninitialized settings fail closed, never at full volume.
    /// </summary>
    public static float SettingsGain()
    {
        try
        {
            if (TestMode.IsOn || NonInteractiveMode.IsActive || NGame.Instance == null
                || AudioServer.GetBusIndex("SFX") < 0) return 0f;
            var settings = SaveManager.Instance.SettingsSave;
            var prefs = SaveManager.Instance.PrefsSave;
            if (settings == null || prefs == null
                || (prefs.MuteInBackground && !NGame.IsGameFocusedWindow())) return 0f;
            float master = Mathf.Clamp(settings.VolumeMaster, 0f, 1f);
            float sfx = Mathf.Clamp(settings.VolumeSfx, 0f, 1f);
            return float.IsFinite(master) && float.IsFinite(sfx)
                ? master * master * sfx * sfx : 0f;
        }
        catch { return 0f; }
    }

    private static AudioStreamWav? Load(string kind)
    {
        if (Streams.TryGetValue(kind, out var cached)) return cached;
        var path = $"res://HollowKnight/audio/knight_{kind}.wav";
        AudioStreamWav? stream = null;
        // LoadFromFile also works with the quick PCK packer, without .import
        // sidecars. The exported pack contains these same original WAV bytes.
        if (Godot.FileAccess.FileExists(path)) stream = AudioStreamWav.LoadFromFile(path);
        if (stream == null) WarnOnce($"Knight audio resource missing or unreadable: {path}");
        Streams[kind] = stream;
        return stream;
    }

    private static void WarnOnce(string message)
    {
        if (_warned) return;
        _warned = true;
        MainFile.Logger.Warn(message);
    }
}

/// <summary>Owned by its creature's visual node; no global node references.</summary>
internal partial class KnightAudioPlayer : AudioStreamPlayer
{
    private string _kind = "";
    private ulong _started;
    private int _priority;
    private const float CueGain = 0.65f;

    public bool Accept(string kind, int priority, ulong cooldown, ulong now)
    {
        ulong elapsed = now - _started;
        if (kind == _kind && elapsed < cooldown) return false;
        // One animation command and a card-specific cue can arrive together.
        // Let the specific focus/void cue replace a generic cast, never reverse.
        return elapsed >= 90UL || priority > _priority;
    }

    public void StartCue(AudioStreamWav stream, string kind, int priority, ulong now)
    {
        Stop();
        Stream = stream;
        _kind = kind;
        _priority = priority;
        _started = now;
        if (!ApplyGain()) return;
        SetProcess(true);
        Play();
    }

    public void StopCue()
    {
        Stop();
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (!Playing) { SetProcess(false); return; }
        ApplyGain();
    }

    private bool ApplyGain()
    {
        float gain = KnightAudio.SettingsGain();
        // -80 dB is the player's floor; stop on actual mute, rather than
        // leaking an almost-quiet sound or resuming an old cue after refocus.
        if (gain <= 0f) { VolumeDb = -80f; StopCue(); return false; }
        VolumeDb = Mathf.LinearToDb(CueGain);
        return true;
    }

    public override void _ExitTree() => StopCue();
}
