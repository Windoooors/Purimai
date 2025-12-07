using System;
using TMPro;
using UnityEngine.Localization.Components;

namespace UI
{
    public class TitleListItem : ListItemBase
    {
        public TextMeshProUGUI categoryNameText;
        public LocalizeStringEvent categoryNameStringEvent;

        public override void ProcessBind()
        {
            if (Data is not TitleData levelListItemData)
                throw new Exception("ItemData is not LevelListItemData");

            if (levelListItemData.ManagedLocalization)
                categoryNameStringEvent.SetEntry(levelListItemData.CategoryNameEntryString);
            else
                categoryNameText.text = levelListItemData.CategoryNameEntryString;
        }

        public class TitleData : ItemDataBase
        {
            public string CategoryNameEntryString;
            public bool ManagedLocalization;
        }
    }
}