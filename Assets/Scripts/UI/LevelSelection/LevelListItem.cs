using System;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Task = System.Threading.Tasks.Task;

namespace UI.LevelSelection
{
    public class LevelListItem : ListItemBase
    {
        public TextMeshProUGUI songTitleText;
        public TextMeshProUGUI songTitleTextBackground;

        public Image background;

        public Image songCover;

        public Image songCoverMask;

        public TextMeshProUGUI songArtistText;
        public TextMeshProUGUI songBpmText;

        private Coroutine _showRoutine;

        private bool _textureUpdated;

        public Maidata Maidata;

        private void Update()
        {
            if (Maidata is not null && Maidata.CoverDataLoaded && !_textureUpdated && shownOnScreen)
            {
                _textureUpdated = true;

                songCover.sprite = Maidata.SongCoverDecodedImage.GetSprite();

                AddMotionHandle(LMotion.Create(1f, 0, 0.1f).Bind(x =>
                {
                    songCoverMask.color = new Color(songCoverMask.color.r, songCoverMask.color.g, songCoverMask.color.b,
                        x);
                }), false);
            }
        }

        public override void ProcessBind()
        {
            if (Data is not LevelListItemData levelListItemData)
                throw new Exception("ItemData is not LevelListItemData");

            songTitleText.text = levelListItemData.Maidata.Title;
            songTitleTextBackground.text = levelListItemData.Maidata.Title;
            songBpmText.text = levelListItemData.Maidata.Bpm.ToString("0");
            songArtistText.text = levelListItemData.Maidata.Artist;
            Maidata = levelListItemData.Maidata;

            Task.Run(() => { Maidata.LoadSongCover(); });
        }

        public override void ProcessSelect(bool animated = true)
        {
            if (animated)
            {
                AddMotionHandle(LMotion.Create(new Color(1, 1, 1, 0), Color.white, 0.5f).WithEase(Ease.OutExpo)
                    .Bind(x =>
                    {
                        songTitleTextBackground.color = new Color(songTitleTextBackground.color.r,
                            songTitleTextBackground.color.g, songTitleTextBackground.color.b,
                            x.a
                        );
                        background.color = x;
                    }));
            }
            else
            {
                ClearMotion();

                songTitleTextBackground.color = new Color(songTitleTextBackground.color.r,
                    songTitleTextBackground.color.g, songTitleTextBackground.color.b,
                    1f
                );
                background.color = Color.white;
            }
        }

        public override void ProcessDeallocate()
        {
            bool maidataIsShown = false;
            
            foreach (var itemObject in List.ItemObjectPool)
            {
                if (itemObject is LevelListItem levelListItem && levelListItem != this && levelListItem.Maidata == Maidata && levelListItem.shownOnScreen)
                {
                    maidataIsShown = true;
                    break;
                }
            }

            if (!maidataIsShown && Maidata != null)
            {
                Maidata.SongCoverDecodedImage?.Dispose();
                Maidata.SongCoverDecodedImage = null;
                Maidata.CoverDataLoaded = false;
            }
            
            songCoverMask.color = new Color(songCoverMask.color.r, songCoverMask.color.g, songCoverMask.color.b, 1);

            _textureUpdated = false;
        }

        public override void ProcessDeselect(bool animated = true)
        {
            if (animated)
            {
                AddMotionHandle(LMotion.Create(Color.white, new Color(1, 1, 1, 0), 0.5f).WithEase(Ease.OutExpo)
                    .Bind(x =>
                    {
                        songTitleTextBackground.color = new Color(songTitleTextBackground.color.r,
                            songTitleTextBackground.color.g, songTitleTextBackground.color.b, x.a);
                        background.color = x;
                    }));
            }
            else
            {
                ClearMotion();

                songTitleTextBackground.color = new Color(songTitleTextBackground.color.r,
                    songTitleTextBackground.color.g, songTitleTextBackground.color.b, 0);
                background.color = new Color(1, 1, 1, 0);
            }
        }
    }

    public class LevelListItemData : ItemDataBase
    {
        public int DifficultyIndex;
        public Maidata Maidata;
    }
}