using System;
using TMPro;

namespace UI
{
    public class TitleListItem : ListItemBase
    {
        public TextMeshProUGUI categoryNameText;

        public override void Bind(ItemDataBase data)
        {
            if (data is not TitleData levelListItemData)
                throw new Exception("ItemData is not LevelListItemData");

            categoryNameText.text = levelListItemData.CategoryName;
        }

        public class TitleData : ItemDataBase
        {
            public string CategoryName;
        }
    }
}