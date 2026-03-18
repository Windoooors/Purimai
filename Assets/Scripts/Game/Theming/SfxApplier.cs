using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Game.Theming
{
    public static class SfxApplier
    {
        private static Dictionary<string, AudioSoundNameData> _defaultSfxData;

        public static void LoadSfx()
        {
            if (_defaultSfxData == null)
            {
                _defaultSfxData = new Dictionary<string, AudioSoundNameData>();

                foreach (var pair in SfxManager.Instance.GameSoundNameData.audioSoundNameDataDict)
                    _defaultSfxData.Add(pair.Key, pair.Value);
            }

            ThemeManager.SkinDataList.ForEach(x =>
            {
                if ((x.AppliedModules & (1 << 5)) == 0)
                    return;

                var list = ThemeApplier.Instance.GetSfxPieceDataList();
                LoadSingleSfxData(x, list);
            });
        }

        private static void LoadSingleSfxData(ThemeData themeData, List<SfxPieceData> list)
        {
            foreach (var sfxPieceData in list)
            {
                var match = themeData.themeDataDto.SfxData.ToList().Find(x => x.Key == sfxPieceData.key);

                var path = match != null ? Path.Combine(themeData.Path, match.Path) : "";

                if (!File.Exists(path)) path = Path.Combine(themeData.Path, sfxPieceData.key + ".wav");
                if (!File.Exists(path))
                    path = Path.Combine(themeData.Path, "GameSfx/" + sfxPieceData.key + ".wav");
                if (!File.Exists(path))
                {
                    SfxManager.Instance.GameSoundNameData.audioSoundNameDataDict[sfxPieceData.key] =
                        _defaultSfxData[sfxPieceData.key];
                    continue;
                }

                SfxManager.Instance.GameSoundNameData.audioSoundNameDataDict[sfxPieceData.key] =
                    new AudioSoundNameData(path)
                    {
                        inStreamingAssets = false
                    };
            }

            SfxManager.Instance.AdaptToSettings();
        }
    }
}