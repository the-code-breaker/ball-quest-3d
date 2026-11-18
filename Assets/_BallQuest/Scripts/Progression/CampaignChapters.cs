using System.Linq;

namespace BallQuest
{
    // A chapter is a contiguous progression range, independent of visual biome.
    // IDs remain stable so existing unlocks, currency and records still apply.
    public static class CampaignChapters
    {
        public const int Size=50;
        public static LevelDefinition[] Courses(GameCatalog catalog,int chapter=-1)
            =>catalog.levels.Where(l=>l&&(chapter<0||(l.id-1)/Size==chapter)).OrderBy(l=>l.id).ToArray();
        public static string Title(int chapter)=>"CHAPTER "+(chapter+1);
        public static LevelDefinition Next(GameCatalog catalog,int id)
            =>Courses(catalog).FirstOrDefault(l=>l.id>id);
    }
}
