using System;
using FMOD;
using FMODUnity;
using UI.GameSettings;
using UnityEngine;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using E7.Native;
#endif

namespace Game
{
    public static class SoundEffectManager
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private static NativeAudioPointer _perfectSound;
        private static NativeAudioPointer _greatSound;
        private static NativeAudioPointer _goodSound;
        private static NativeAudioPointer _breakExtraSound;
        private static NativeAudioPointer _breakPerfectSound;
        private static NativeAudioPointer _breakGreatSound;
        private static NativeAudioPointer _slideSound;

        private static NativeSource _tapNativeSource;
        private static NativeSource _breakNativeSource;
        private static NativeSource _breakExtraNativeSource;
        private static NativeSource _slideNativeSource;
#endif

        public static Sound CriticalSound;
        private static Sound _preparatoryBeatSound;

        public static void PlayPreparatoryBeatSound()
        {
            throw new NotImplementedException();
        }

        public static void PlayPerfectSound()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_tapVolume == 0)
                return;
            
            if (!_tapNativeSource.IsValid)
            {
                _tapNativeSource = NativeAudio.GetNativeSource(0);
                _tapNativeSource.SetVolume(_tapVolume);
            }
            
            _tapNativeSource.Play(_perfectSound);
#endif
        }

        public static void PlayGreatSound()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_tapVolume == 0)
                return;
            
            if (!_tapNativeSource.IsValid)
            {
                _tapNativeSource = NativeAudio.GetNativeSource(0);
                _tapNativeSource.SetVolume(_tapVolume);
            }
            
            _tapNativeSource.Play(_greatSound);
#endif
        }

        public static void PlayGoodSound()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_tapVolume == 0)
                return;
            
            if (!_tapNativeSource.IsValid)
            {
                _tapNativeSource = NativeAudio.GetNativeSource(0);
                _tapNativeSource.SetVolume(_tapVolume);
            }
            
            _tapNativeSource.Play(_goodSound);
#endif
        }

        public static void PlayBreakExtraScoreSound()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_breakVolume == 0)
                return;
            
            if (!_breakExtraNativeSource.IsValid)
            {
                _breakExtraNativeSource = NativeAudio.GetNativeSource(2);
                _breakExtraNativeSource.SetVolume(_breakVolume);
            }
            
            _breakExtraNativeSource.Play(_breakExtraSound);
#endif
        }

        public static void PlayBreakPerfectSound()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_breakVolume == 0)
                return;
            
            if (!_breakNativeSource.IsValid)
            {
                _breakNativeSource = NativeAudio.GetNativeSource(3);
                _breakNativeSource.SetVolume(_breakVolume);
            }
            
            _tapNativeSource.Play(_breakPerfectSound);
#endif
        }

        public static void PlayBreakGreatSound()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_breakVolume == 0)
                return;
            
            if (!_breakNativeSource.IsValid)
            {
                _breakNativeSource = NativeAudio.GetNativeSource(3);
                _breakNativeSource.SetVolume(_breakVolume);
            }
            
            _tapNativeSource.Play(_breakGreatSound);
#endif
        }

        public static void PlaySlideSound()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (_slideVolume == 0)
                return;
            
            if (!_slideNativeSource.IsValid)
            {
                _slideNativeSource = NativeAudio.GetNativeSource(1);
                _slideNativeSource.SetVolume(_slideVolume);
            }
            
            _slideNativeSource.Play(_slideSound);
#endif
        }

        private static float _tapVolume = 1;
        private static float _breakVolume = 1;
        private static float _slideVolume = 1;

        public static FMOD.System System;

        private static ChannelGroup _channelGroup;

        public static ChannelGroup GetChannelGroup()
        {
            if (_channelGroup.hasHandle())
                return _channelGroup;
            
            System.createChannelGroup("master", out _channelGroup);
            return _channelGroup;
        }

        public static void ReleaseSystem()
        {
            //System.close();
        }

        public static void LoadAllSound(SoundPathData soundPathData)
        {
            Factory.System_Create(out System);
            System.init(512, INITFLAGS.NORMAL, IntPtr.Zero);

            System.createChannelGroup("master", out _channelGroup);
            
            _tapVolume = SettingsPool.GetValue("game.volume.tap") / 10f;
            _breakVolume = SettingsPool.GetValue("game.volume.break") / 10f;
            _slideVolume = SettingsPool.GetValue("game.volume.slide") / 10f;
            SettingsController.OnSettingsChanged += (_, _) =>
            {
                _tapVolume = SettingsPool.GetValue("game.volume.tap") / 10f;
                _breakVolume = SettingsPool.GetValue("game.volume.break") / 10f;
                _slideVolume = SettingsPool.GetValue("game.volume.slide") / 10f;
            };

            ReleaseFMODSound(CriticalSound);
            ReleaseFMODSound(_preparatoryBeatSound);

            CriticalSound = LoadSingleFMODSound(soundPathData.criticalSoundPath);
            _preparatoryBeatSound = LoadSingleFMODSound(soundPathData.preparatoryBeatSoundPath);

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            NativeAudio.Initialize();

            _perfectSound = NativeAudio.Load(soundPathData.perfectSoundPath, NativeAudio.LoadOptions.defaultOptions);
            _breakExtraSound =
 NativeAudio.Load(soundPathData.breakExtraSoundPath, NativeAudio.LoadOptions.defaultOptions);
            _breakPerfectSound =
 NativeAudio.Load(soundPathData.breakPerfectSoundPath, NativeAudio.LoadOptions.defaultOptions);
            _breakGreatSound =
 NativeAudio.Load(soundPathData.breakGreatSoundPath, NativeAudio.LoadOptions.defaultOptions);
            _slideSound = NativeAudio.Load(soundPathData.slideSoundPath, NativeAudio.LoadOptions.defaultOptions);
            _greatSound = NativeAudio.Load(soundPathData.greatSoundPath, NativeAudio.LoadOptions.defaultOptions);
            _goodSound = NativeAudio.Load(soundPathData.goodSoundPath, NativeAudio.LoadOptions.defaultOptions);
#endif

            return;

            Sound LoadSingleFMODSound(string path)
            {
                var system = SoundEffectManager.System;

                var mode = MODE.DEFAULT | MODE._2D | MODE.CREATESAMPLE;

                var result = system.createSound(path, mode, out var sound);

                if (result != RESULT.OK) return default;

                return sound;
            }

            void ReleaseFMODSound(Sound sound)
            {
                if (!sound.hasHandle())
                    return;

                sound.release();
                sound.clearHandle();
            }
        }

        [Serializable]
        public class SoundPathData
        {
            public string perfectSoundPath;
            public string greatSoundPath;
            public string goodSoundPath;
            public string breakExtraSoundPath;
            public string breakPerfectSoundPath;
            public string breakGreatSoundPath;
            public string slideSoundPath;
            public string criticalSoundPath;
            public string preparatoryBeatSoundPath;
        }
    }
}