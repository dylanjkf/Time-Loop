namespace TimeLoop.Achievements
{
    /// <summary>Static metadata for one achievement — the id is the persisted key, Title/Description are display-only.</summary>
    public class AchievementDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }

        public AchievementDefinition(string id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }
    }
}
