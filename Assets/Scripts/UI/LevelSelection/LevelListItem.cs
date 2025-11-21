using System;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.LevelSelection
{
    public class LevelListItem : ListItemBase
    {
        public TextMeshProUGUI songTitleText;
        public TextMeshProUGUI songTitleTextBackground;

        public Image background;

        public Image songCover;

        public TextMeshProUGUI songArtistText;
        public TextMeshProUGUI songBpmText;
        public int difficultyIndex;

        private bool _coverLoaded;
        public Maidata maidata;

        private void Update()
        {
            if (maidata.SongCover && !_coverLoaded)
            {
                songCover.sprite = maidata.SongCover;
                _coverLoaded = true;
            }
        }

        public override void Bind(ItemDataBase data)
        {
            if (data is not LevelListItemData levelListItemData)
                throw new Exception("ItemData is not LevelListItemData");

            songTitleText.text = levelListItemData.Maidata.Title;
            songTitleTextBackground.text = levelListItemData.Maidata.Title;
            songBpmText.text = levelListItemData.Maidata.Bpm.ToString("0");
            songArtistText.text = levelListItemData.Maidata.Artist;
            difficultyIndex = levelListItemData.DefaultDifficultyIndex;
            maidata = levelListItemData.Maidata;
        }

        public override void ProcessSelect()
        {
            AddMotionHandle(LMotion.Create(new Color(255, 255, 255, 0), Color.white, 0.5f).WithEase(Ease.OutExpo)
                .Bind(x =>
                {
                    songTitleTextBackground.color = new Color(songTitleTextBackground.color.r,
                        songTitleTextBackground.color.g, songTitleTextBackground.color.b,
                        255 * 0.1f * x.a
                    );
                    background.color = x;
                }));
        }

        public override void ProcessDeselect()
        {
            AddMotionHandle(LMotion.Create(Color.white, new Color(1, 1, 1, 0), 0.5f).WithEase(Ease.OutExpo)
                .Bind(x =>
                {
                    songTitleTextBackground.color = new Color(songTitleTextBackground.color.r,
                        songTitleTextBackground.color.g, songTitleTextBackground.color.b, 1 * 0.1f * x.a);
                    background.color = x;
                }));
        }
    }

    public class LevelListItemData : ItemDataBase
    {
        public int DefaultDifficultyIndex;
        public Maidata Maidata;
    }
}