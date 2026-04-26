using BlogCMS.Business.Services.Interfaces;
using BlogCMS.Data.Repositories.Interfaces;

namespace BlogCMS.Business.Services;

public class LikeService : ILikeService
{
    private readonly ILikeRepository _repo;

    public LikeService(ILikeRepository repo) => _repo = repo;

    public async Task<(bool Liked, int Count)> ToggleAsync(int postId, int userId)
    {
        var liked = await _repo.ToggleAsync(postId, userId);
        var count = await _repo.CountForPostAsync(postId);
        return (liked, count);
    }
}
