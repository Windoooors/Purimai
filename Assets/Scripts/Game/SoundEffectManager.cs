using System;
using FMOD;
using FMODUnity;

namespace Game
{
    public static class SoundEffectManager
    {
        private static Sound _perfectSound;
        private static Sound _greatSound;
        private static Sound _goodSound;
        private static Sound _breakExtraSound;
        private static Sound _breakPerfectSound;
        private static Sound _breakGreatSound;
        private static Sound _slideSound;
        public static Sound CriticalSound;
        private static Sound _preparatoryBeatSound;

        private static Channel _criticalSoundChannel;

        public static void PlayPreparatoryBeatSound()
        {
            RuntimeManager.CoreSystem.playSound(_preparatoryBeatSound, default, false, out _);
        }

        public static void PlayPerfectSound()
        {
            RuntimeManager.CoreSystem.playSound(_perfectSound, default, false, out _);
        }

        public static void PlayGreatSound()
        {
            RuntimeManager.CoreSystem.playSound(_greatSound, default, false, out _);
        }

        public static void PlayGoodSound()
        {
            RuntimeManager.CoreSystem.playSound(_goodSound, default, false, out _);
        }

        public static void PlayBreakExtraScoreSound()
        {
            RuntimeManager.CoreSystem.playSound(_breakExtraSound, default, false, out _);
        }

        public static void PlayBreakPerfectSound()
        {
            RuntimeManager.CoreSystem.playSound(_breakPerfectSound, default, false, out _);
        }

        public static void PlayBreakGreatSound()
        {
            RuntimeManager.CoreSystem.playSound(_breakGreatSound, default, false, out _);
        }

        public static void PlaySlideSound()
        {
            RuntimeManager.CoreSystem.playSound(_slideSound, default, false, out _);
        }

        public static void LoadAllSound(SoundPathData soundPathData)
        {
            ReleaseSound(_perfectSound);
            ReleaseSound(_greatSound);
            ReleaseSound(_goodSound);
            ReleaseSound(_breakExtraSound);
            ReleaseSound(_breakPerfectSound);
            ReleaseSound(_breakGreatSound);
            ReleaseSound(_slideSound);
            ReleaseSound(CriticalSound);
            ReleaseSound(_preparatoryBeatSound);

            _perfectSound = LoadSingleSound(soundPathData.perfectSoundPath);
            _greatSound = LoadSingleSound(soundPathData.greatSoundPath);
            _goodSound = LoadSingleSound(soundPathData.goodSoundPath);
            _breakExtraSound = LoadSingleSound(soundPathData.breakExtraSoundPath);
            _breakPerfectSound = LoadSingleSound(soundPathData.breakPerfectSoundPath);
            _breakGreatSound = LoadSingleSound(soundPathData.breakGreatSoundPath);
            _slideSound = LoadSingleSound(soundPathData.slideSoundPath);
            CriticalSound = LoadSingleSound(soundPathData.criticalSoundPath);
            _preparatoryBeatSound = LoadSingleSound(soundPathData.preparatoryBeatSoundPath);

            return;

            Sound LoadSingleSound(string path)
            {
                var system = RuntimeManager.CoreSystem;

                var mode = MODE.DEFAULT | MODE._2D;

                var result = system.createSound(path, mode, out var sound);

                if (result != RESULT.OK) return default;

                return sound;
            }

            void ReleaseSound(Sound sound)
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