namespace BlogCMS.Business.Services.Interfaces;

public interface ILikeService
{
    Task<(bool Liked, int Count)> ToggleAsync(int postId, int userId);
}
