using BlogCMS.Data.Entities;

namespace BlogCMS.Web.ViewModels;

public class AdminDashboardViewModel
{
    public int UserCount { get; set; }
    public int PostCount { get; set; }
    public int PendingPostCount { get; set; }
    public int PendingCommentCount { get; set; }
    public IEnumerable<Post> RecentPending { get; set; } = [];
}
