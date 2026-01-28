namespace UI.LevelSelection
{
    public class LevelListItemData
    {
        public CategoryData Category;
        public int DifficultyIndex;
        public MaidataReferenceCountPair MaidataReferenceCountPair;
    }

    public class MaidataReferenceCountPair
    {
        public Maidata Maidata;
        public int ReferenceCount;
    }

    public class CategoryData
    {
        public string CategoryNameEntryString;
    }
}