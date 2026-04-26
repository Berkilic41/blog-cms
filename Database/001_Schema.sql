USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BlogCmsDb')
    CREATE DATABASE BlogCmsDb;
GO

USE BlogCmsDb;
GO

-- USERS
CREATE TABLE Users (
    Id           INT           IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    PasswordSalt NVARCHAR(512) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL DEFAULT 'Reader',
    DisplayName  NVARCHAR(100),
    Bio          NVARCHAR(500),
    AvatarUrl    NVARCHAR(500),
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT CK_Users_Role     CHECK (Role IN ('Admin','Author','Reader'))
);

-- CATEGORIES
CREATE TABLE Categories (
    Id          INT           IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Slug        NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500),
    CONSTRAINT UQ_Categories_Slug UNIQUE (Slug)
);

-- TAGS
CREATE TABLE Tags (
    Id   INT          IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(60) NOT NULL,
    Slug NVARCHAR(80) NOT NULL,
    CONSTRAINT UQ_Tags_Slug UNIQUE (Slug)
);

-- POSTS
CREATE TABLE Posts (
    Id            INT            IDENTITY(1,1) PRIMARY KEY,
    Title         NVARCHAR(250)  NOT NULL,
    Slug          NVARCHAR(280)  NOT NULL,
    Excerpt       NVARCHAR(500),
    Content       NVARCHAR(MAX)  NOT NULL,
    CoverImageUrl NVARCHAR(500),
    AuthorId      INT            NOT NULL,
    CategoryId    INT            NOT NULL,
    Status        NVARCHAR(20)   NOT NULL DEFAULT 'Pending',
    CreatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    PublishedAt   DATETIME2,
    CONSTRAINT FK_Posts_Author     FOREIGN KEY (AuthorId)   REFERENCES Users(Id),
    CONSTRAINT FK_Posts_Category   FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    CONSTRAINT UQ_Posts_Slug       UNIQUE (Slug),
    CONSTRAINT CK_Posts_Status     CHECK (Status IN ('Draft','Pending','Published','Rejected'))
);

CREATE INDEX IX_Posts_Status_Published ON Posts(Status, PublishedAt DESC);
CREATE INDEX IX_Posts_Author           ON Posts(AuthorId);

-- POST <-> TAG (junction)
CREATE TABLE PostTags (
    PostId INT NOT NULL,
    TagId  INT NOT NULL,
    CONSTRAINT PK_PostTags PRIMARY KEY (PostId, TagId),
    CONSTRAINT FK_PostTags_Post FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PostTags_Tag  FOREIGN KEY (TagId)  REFERENCES Tags(Id)  ON DELETE CASCADE
);

-- COMMENTS (with parent for replies)
CREATE TABLE Comments (
    Id         INT            IDENTITY(1,1) PRIMARY KEY,
    PostId     INT            NOT NULL,
    UserId     INT            NOT NULL,
    ParentId   INT            NULL,
    Content    NVARCHAR(2000) NOT NULL,
    IsApproved BIT            NOT NULL DEFAULT 1,
    CreatedAt  DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Comments_Post   FOREIGN KEY (PostId)   REFERENCES Posts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Comments_User   FOREIGN KEY (UserId)   REFERENCES Users(Id),
    CONSTRAINT FK_Comments_Parent FOREIGN KEY (ParentId) REFERENCES Comments(Id)
);

CREATE INDEX IX_Comments_Post ON Comments(PostId, CreatedAt);

-- LIKES
CREATE TABLE Likes (
    Id        INT       IDENTITY(1,1) PRIMARY KEY,
    PostId    INT       NOT NULL,
    UserId    INT       NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Likes_Post  FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Likes_User  FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_Likes_Pair  UNIQUE (PostId, UserId)
);
GO
