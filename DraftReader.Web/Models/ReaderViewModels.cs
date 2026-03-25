using DraftReader.Domain.Entities;

namespace DraftReader.Web.Models;

public class TopLevelViewModel
{
    public IReadOnlyList<Section> TopLevelSections { get; set; } = new List<Section>();
    public string ProjectName { get; set; } = string.Empty;
}

public class SectionContentsViewModel
{
    public Section TopLevelSection { get; set; } = default!;
    public IReadOnlyList<ContentGroup> Groups { get; set; } = new List<ContentGroup>();
    public string ProjectName { get; set; } = string.Empty;
}

public class ContentGroup
{
    public string Heading { get; set; } = string.Empty;
    public int Depth { get; set; }
    public Section? ChapterSection { get; set; }
    public IReadOnlyList<Section> Scenes { get; set; } = new List<Section>();
    public IReadOnlyList<ContentGroup> SubGroups { get; set; } = new List<ContentGroup>();
}

public class ChapterReadViewModel
{
    public Section Chapter { get; set; } = default!;
    public IReadOnlyList<string> Breadcrumb { get; set; } = new List<string>();
    public IReadOnlyList<SceneWithComments> Scenes { get; set; } = new List<SceneWithComments>();
    public IReadOnlyList<Comment> ChapterComments { get; set; } = new List<Comment>();
    public SectionContentsViewModel? BookContents { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}

public class SceneWithComments
{
    public Section Scene { get; set; } = default!;
    public IReadOnlyList<Comment> Comments { get; set; } = new List<Comment>();
}

public class AddCommentViewModel
{
    public Guid SectionId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public Guid? ParentCommentId { get; set; }
}

public class ReaderDashboardViewModel
{
    public string? ProjectName { get; set; }
    public List<ChapterProgressViewModel> PublishedChapters { get; set; } = new();
    public int TotalChapters { get; set; }
    public int ReadChapters { get; set; }
}

public class ChapterProgressViewModel
{
    public DraftReader.Domain.Entities.Section Chapter { get; set; } = default!;
    public bool HasRead { get; set; }
}
