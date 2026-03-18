using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UI;
using UnityEngine;

namespace Game.Theming
{
    public static class SkinApplier
    {
        public static void LoadSkin()
        {
            for (var i = 0; i < ThemeApplier.ModuleCount; i++)
            {
                if (i == 5)
                    continue;

                var mask = 1 << i;

                ThemeManager.SkinDataList.ForEach(x =>
                {
                    if ((x.AppliedModules & mask) == 0)
                        return;

                    if (i == 1) ThemeManager.HoldColorRelatedHoldEffect = x.themeDataDto.HoldColorRelatedHoldEffect;

                    if (i == 4) ThemeManager.HasJudgeCircleColor = x.themeDataDto.HasJudgeCircleColor;

                    var list = ThemeApplier.Instance.GetSkinPieceDataList(i);
                    LoadSingleSkinData(x, list);
                });
            }

            return;

            void LoadSingleSkinData(ThemeData themeData, List<SkinPieceData> skinPieceDataArray)
            {
                foreach (var skinPieceData in skinPieceDataArray)
                {
                    var skinPieceDataDto = themeData.themeDataDto.Data.ToList().Find(x => x.Key == skinPieceData.key);

                    var streaming = themeData.InStreamingAssets;

                    if (!streaming)
                        LoadFromPersistentData(themeData, skinPieceDataDto, skinPieceData, () =>
                        {
                            var defaultSkinPieceDataDto = ThemeManager.DefaultTheme.themeDataDto.Data.ToList()
                                .Find(x => x.Key == skinPieceData.key);

                            LoadFromStreamingAssets(ThemeManager.DefaultTheme, defaultSkinPieceDataDto, skinPieceData);
                        });
                    else
                        /*if (BetterStreamingAssets.DirectoryExists(skinData.Path + "/"))
                            return;*/
                        LoadFromStreamingAssets(themeData, skinPieceDataDto, skinPieceData);
                }
            }

            void LoadFromStreamingAssets(ThemeData themeData, SkinPieceDataDto skinPieceDataDto,
                SkinPieceData skinPieceData)
            {
                var path = skinPieceDataDto != null ? Path.Combine(themeData.Path, skinPieceDataDto.Path) : "";

                if (path == "" || !BetterStreamingAssets.FileExists(path))
                    path = Path.Combine(themeData.Path, skinPieceData.key + ".png");
                if (!BetterStreamingAssets.FileExists(path))
                    path = Path.Combine(themeData.Path, "game_skins/" + skinPieceData.key + ".png");
                if (!BetterStreamingAssets.FileExists(path)) return;

                var data = BetterStreamingAssets.ReadAllBytes(path);

                LoadTextureFromBytes(data, skinPieceData.sprite);
            }

            void LoadFromPersistentData(ThemeData themeData, SkinPieceDataDto skinPieceDataDto,
                SkinPieceData skinPieceData, Action onFileNotFound)
            {
                var path = skinPieceDataDto != null ? Path.Combine(themeData.Path, skinPieceDataDto.Path) : "";

                if (!File.Exists(path)) path = Path.Combine(themeData.Path, skinPieceData.key + ".png");
                if (!File.Exists(path))
                    path = Path.Combine(themeData.Path, "GameSkins/" + skinPieceData.key + ".png");
                if (!File.Exists(path))
                {
                    onFileNotFound?.Invoke();
                    return;
                }

                LoadTexture(path, skinPieceData.sprite);
            }

            void LoadTexture(string path, Sprite sprite)
            {
                using var image = Image.Load<Rgba32>(path);

                using var decoded = new DecodedImage(image);

                sprite.texture.Reinitialize(decoded.Width, decoded.Height,
                    TextureFormat.RGBA32, false);
                sprite.texture.SetPixelData(decoded.PixelData, 0);
                sprite.texture.Apply();
            }

            void LoadTextureFromBytes(byte[] data, Sprite sprite)
            {
                using var image = Image.Load<Rgba32>(data);

                using var decoded = new DecodedImage(image);

                sprite.texture.Reinitialize(decoded.Width, decoded.Height,
                    TextureFormat.RGBA32, false);
                sprite.texture.SetPixelData(decoded.PixelData, 0);
                sprite.texture.Apply();
            }
        }
    }
}