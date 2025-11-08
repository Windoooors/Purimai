using System;
using TMPro;
using UI.LevelSelection;
using UnityEngine;

namespace UI
{
    public class LevelListItem : ListItemBase
    {
        public TextMeshProUGUI songTitleText;
        public TextMeshProUGUI songArtistText;
        public TextMeshProUGUI songBpmText;
        public int difficultyIndex;
        public LevelListController.Maidata chartData;

        public override void Bind(ItemDataBase data)
        {
            if (data is not LevelListItemData levelListItemData)
                throw new Exception("ItemData is not LevelListItemData");

            songTitleText.text = levelListItemData.LevelName;
            songBpmText.text = $"BPM {levelListItemData.ChartData.Bpm}";
            songArtistText.text = levelListItemData.ChartData.Artist;
            difficultyIndex = levelListItemData.DefaultDifficultyIndex;
            chartData = levelListItemData.ChartData;
        }
    }

    public class LevelListItemData : ItemDataBase
    {
        public LevelListController.Maidata ChartData;
        public string LevelName;
        public int DefaultDifficultyIndex;
        public Sprite Sprite;
    }
}