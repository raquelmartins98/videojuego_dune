using System.IO;
using System.Windows.Media;

namespace Dune.Admin.Client;

public static class SoundManager
{
    private static MediaPlayer? _clickPlayer;
    private static MediaPlayer? _menuMusicPlayer;
    private static MediaPlayer? _gameMusicPlayer;
    private static string? _clickSoundPath;
    private static string? _menuMusicPath;
    private static string? _gameMusicPath;
    private static bool _menuMusicInitialized = false;
    private static bool _gameMusicInitialized = false;
    private static double _musicVolume = 0.3;
    private static double _effectsVolume = 0.5;

    public static void Initialize()
    {
        _clickSoundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "click.mp3");
        _menuMusicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "menu_music.mp3");
        _gameMusicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_music.mp3");
    }

    public static void PlayMenuMusic()
    {
        StopMusic();
        
        if (File.Exists(_menuMusicPath))
        {
            _menuMusicPlayer = new MediaPlayer();
            _menuMusicPlayer.Open(new Uri(_menuMusicPath));
            _menuMusicPlayer.MediaEnded += (s, e) => 
            {
                try { _menuMusicPlayer?.Play(); } catch { }
            };
            _menuMusicPlayer.Volume = _musicVolume;
            _menuMusicPlayer.Play();
            _menuMusicInitialized = true;
        }
    }

    public static void PlayGameMusic()
    {
        StopMusic();
        
        if (File.Exists(_gameMusicPath))
        {
            _gameMusicPlayer = new MediaPlayer();
            _gameMusicPlayer.Open(new Uri(_gameMusicPath));
            _gameMusicPlayer.MediaEnded += (s, e) => 
            {
                try { _gameMusicPlayer?.Play(); } catch { }
            };
            _gameMusicPlayer.Volume = _musicVolume;
            _gameMusicPlayer.Play();
            _gameMusicInitialized = true;
        }
    }

    public static void StopMusic()
    {
        try
        {
            if (_menuMusicPlayer != null)
            {
                _menuMusicPlayer.Stop();
                _menuMusicPlayer.Close();
                _menuMusicPlayer = null;
                _menuMusicInitialized = false;
            }
            if (_gameMusicPlayer != null)
            {
                _gameMusicPlayer.Stop();
                _gameMusicPlayer.Close();
                _gameMusicPlayer = null;
                _gameMusicInitialized = false;
            }
        }
        catch { }
    }

    public static void PlayClick()
    {
        if (string.IsNullOrEmpty(_clickSoundPath) || !File.Exists(_clickSoundPath))
            return;

        try
        {
            var player = new MediaPlayer();
            player.Open(new Uri(_clickSoundPath));
            player.Volume = _effectsVolume;
            player.Play();
        }
        catch { }
    }

    public static void SetMusicVolume(double volume)
    {
        _musicVolume = Math.Clamp(volume, 0, 1);
        
        if (_menuMusicPlayer != null)
            _menuMusicPlayer.Volume = _musicVolume;
        if (_gameMusicPlayer != null)
            _gameMusicPlayer.Volume = _musicVolume;
    }

    public static void SetEffectsVolume(double volume)
    {
        _effectsVolume = Math.Clamp(volume, 0, 1);
    }

    public static double GetMusicVolume() => _musicVolume;
    public static double GetEffectsVolume() => _effectsVolume;
    public static bool IsMenuMusicPlaying() => _menuMusicPlayer != null;
}
