USE BlogCmsDb;
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- sp_GetPostsPaged: search/filter + pagination, returns (rows, total count)
-- ───────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_GetPostsPaged
    @Search     NVARCHAR(250) = NULL,
    @CategoryId INT           = NULL,
    @TagSlug    NVARCHAR(80)  = NULL,
    @Status     NVARCHAR(20)  = 'Published',
    @AuthorId   INT           = NULL,
    @Page       INT           = 1,
    @PageSize   INT           = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    ;WITH Filtered AS (
        SELECT DISTINCT p.Id
        FROM Posts p
        LEFT JOIN PostTags pt ON pt.PostId = p.Id
        LEFT JOIN Tags     t  ON t.Id     = pt.TagId
        WHERE
            (@Status     IS NULL OR p.Status     = @Status)
            AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
            AND (@AuthorId   IS NULL OR p.AuthorId   = @AuthorId)
            AND (@TagSlug    IS NULL OR t.Slug       = @TagSlug)
            AND (@Search     IS NULL
                 OR p.Title   LIKE '%' + @Search + '%'
                 OR p.Content LIKE '%' + @Search + '%')
    )
    SELECT
        p.Id, p.Title, p.Slug, p.Excerpt, p.CoverImageUrl, p.Status,
        p.CreatedAt, p.PublishedAt,
        p.AuthorId,    u.Username AS AuthorName, u.AvatarUrl AS AuthorAvatar,
        p.CategoryId,  c.Name     AS CategoryName, c.Slug AS CategorySlug,
        (SELECT COUNT(*) FROM Comments cm WHERE cm.PostId = p.Id AND cm.IsApproved = 1) AS CommentCount,
        (SELECT COUNT(*) FROM Likes    l  WHERE l.PostId  = p.Id) AS LikeCount,
        (SELECT COUNT(*) FROM Filtered) AS TotalCount
    FROM Filtered f
    INNER JOIN Posts      p ON p.Id = f.Id
    INNER JOIN Users      u ON u.Id = p.AuthorId
    INNER JOIN Categories c ON c.Id = p.CategoryId
    ORDER BY COALESCE(p.PublishedAt, p.CreatedAt) DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- sp_GetPostBySlug: full detail with author, category, tags, counts
-- Returns 3 result sets: post / tags / (nothing extra)
-- ───────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_GetPostBySlug
    @Slug NVARCHAR(280)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1
        p.Id, p.Title, p.Slug, p.Excerpt, p.Content, p.CoverImageUrl, p.Status,
        p.CreatedAt, p.UpdatedAt, p.PublishedAt,
        p.AuthorId,   u.Username AS AuthorName, u.DisplayName AS AuthorDisplay,
                       u.Bio AS AuthorBio,       u.AvatarUrl  AS AuthorAvatar,
        p.CategoryId, c.Name     AS CategoryName, c.Slug AS CategorySlug,
        (SELECT COUNT(*) FROM Comments cm WHERE cm.PostId = p.Id AND cm.IsApproved = 1) AS CommentCount,
        (SELECT COUNT(*) FROM Likes    l  WHERE l.PostId  = p.Id) AS LikeCount
    FROM Posts p
    INNER JOIN Users      u ON u.Id = p.AuthorId
    INNER JOIN Categories c ON c.Id = p.CategoryId
    WHERE p.Slug = @Slug;

    SELECT t.Id, t.Name, t.Slug
    FROM Tags t
    INNER JOIN PostTags pt ON pt.TagId = t.Id
    INNER JOIN Posts    p  ON p.Id     = pt.PostId
    WHERE p.Slug = @Slug
    ORDER BY t.Name;
END
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- sp_GetCommentsForPost: flat list, ordered for client-side tree assembly
-- ───────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_GetCommentsForPost
    @PostId          INT,
    @IncludePending  BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.Id, c.PostId, c.UserId, c.ParentId, c.Content, c.IsApproved, c.CreatedAt,
        u.Username, u.AvatarUrl
    FROM Comments c
    INNER JOIN Users u ON u.Id = c.UserId
    WHERE c.PostId = @PostId
      AND (@IncludePending = 1 OR c.IsApproved = 1)
    ORDER BY COALESCE(c.ParentId, c.Id), c.CreatedAt;
END
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- sp_GetUserList: for admin user-management screen, with stats
-- ───────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_GetUserList
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.Id, u.Username, u.Email, u.Role, u.DisplayName, u.IsActive, u.CreatedAt,
        (SELECT COUNT(*) FROM Posts p WHERE p.AuthorId = u.Id) AS PostCount,
        (SELECT COUNT(*) FROM Comments c WHERE c.UserId = u.Id) AS CommentCount
    FROM Users u
    ORDER BY u.CreatedAt DESC;
END
GO

-- ───────────────────────────────────────────────────────────────────────────────
-- sp_GetPendingComments: for admin moderation
-- ───────────────────────────────────────────────────────────────────────────────
CREATE OR ALTER PROCEDURE sp_GetPendingComments
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.Id, c.PostId, c.UserId, c.ParentId, c.Content, c.IsApproved, c.CreatedAt,
        u.Username, u.AvatarUrl,
        p.Title AS PostTitle, p.Slug AS PostSlug
    FROM Comments c
    INNER JOIN Users u ON u.Id = c.UserId
    INNER JOIN Posts p ON p.Id = c.PostId
    WHERE c.IsApproved = 0
    ORDER BY c.CreatedAt DESC;
END
GO
