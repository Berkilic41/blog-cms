namespace BlogCMS.Data.Repositories.Interfaces;

public interface ILikeRepository
{
    Task<bool> ToggleAsync(int postId, int userId);
    Task<bool> IsLikedAsync(int postId, int userId);
    Task<int> CountForPostAsync(int postId);
}
