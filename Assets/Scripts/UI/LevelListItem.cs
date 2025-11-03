using System;
using UnityEngine;

namespace UI
{
    public class LevelListItem : ListItemBase
    {
        public override void Bind(ItemDataBase data)
        {
            if (data is not LevelListItemData levelListItemData)
                throw new Exception("ItemData is not LevelListItemData");
        }

        public class LevelListItemData : ItemDataBase
        {
            public string LevelName;
            public Sprite Sprite;
        }
    }
}