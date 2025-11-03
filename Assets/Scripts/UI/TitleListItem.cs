using System;

namespace UI
{
    public class TitleListItem : ListItemBase
    {
        public override void Bind(ItemDataBase data)
        {
            if (data is not TitleData levelListItemData)
                throw new Exception("ItemData is not LevelListItemData");
        }

        public class TitleData : ItemDataBase
        {
            public string CategoryName;
        }
    }
}