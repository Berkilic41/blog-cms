USE BlogCmsDb;
GO

-- Default users. Password for ALL seed users: "password123"
-- The hash/salt below was generated with HMACSHA512 (matching PasswordHasher in code).
-- You can sign in with any of them, then change passwords in the UI.
DECLARE @Hash NVARCHAR(512) = 'dNenHFzqIK7wTHP3rNRkWw/tqSBIttAjKbks5Tgt5KVD9Rhdnnwqsbtos28hfQ3dpOGciFK1kHO1PAYqGmSETw==';
DECLARE @Salt NVARCHAR(512) = 'y21nmTHP1Vwrtv6X7V+mLm30Xrh74VS6yVJTPjX6qGQO1qmlAUqyDPEODItndn+hacqZNPjczFgVk7qVBK8oOn3/QUfZgz0tuMJ5Jde9nBzQik2ZW8nEgIctMjS8ypPqqliYaB/CA2FJNmBqoOx7vypsuOmR6C8EyzIOst+sXQw=';

INSERT INTO Users (Username, Email, PasswordHash, PasswordSalt, Role, DisplayName, Bio, AvatarUrl)
VALUES
('admin',  'admin@blog.test',  @Hash, @Salt, 'Admin',  'Site Admin',  'Keeps the lights on.',                          'https://i.pravatar.cc/150?u=admin'),
('alice',  'alice@blog.test',  @Hash, @Salt, 'Author', 'Alice Wright',  'Writes about software architecture.',          'https://i.pravatar.cc/150?u=alice'),
('bob',    'bob@blog.test',    @Hash, @Salt, 'Author', 'Bob Patel',     'Devops, infra, and the occasional rant.',      'https://i.pravatar.cc/150?u=bob'),
('reader', 'reader@blog.test', @Hash, @Salt, 'Reader', 'Curious Reader','Just here for the comments.',                  'https://i.pravatar.cc/150?u=reader');

INSERT INTO Categories (Name, Slug, Description) VALUES
('Programming',   'programming',   'Languages, paradigms, and code design.'),
('Architecture',  'architecture',  'System design and architecture patterns.'),
('DevOps',        'devops',        'CI/CD, infrastructure, and deployment.'),
('Career',        'career',        'Professional growth and soft skills.'),
('Tutorials',     'tutorials',     'Step-by-step walkthroughs.');

INSERT INTO Tags (Name, Slug) VALUES
('csharp',     'csharp'),
('dotnet',     'dotnet'),
('sql-server', 'sql-server'),
('docker',     'docker'),
('kubernetes', 'kubernetes'),
('testing',    'testing'),
('clean-code', 'clean-code'),
('beginner',   'beginner');

-- Sample posts
INSERT INTO Posts (Title, Slug, Excerpt, Content, CoverImageUrl, AuthorId, CategoryId, Status, PublishedAt) VALUES
('Why I Stopped Using ORMs for Read Paths',
 'why-i-stopped-using-orms-for-read-paths',
 'After three years of EF Core in production, here is what I learned about query control.',
 '<p>Object-relational mappers are great for CRUD scaffolding, but the moment your queries get interesting, abstractions start leaking. In this post I walk through three real cases where dropping down to raw SQL with ADO.NET gave us 10x perf, simpler debugging, and tests that actually run fast.</p><p>The TL;DR: <strong>use the right tool for the right job</strong>. Writes through an ORM, reads through hand-tuned SQL.</p><h3>The N+1 problem</h3><p>Every ORM has it. Every team gets bitten. The fix is almost always to write the query yourself.</p>',
 'https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=900',
 2, 2, 'Published', GETUTCDATE()),

('Stored Procedures Are Underrated in 2026',
 'stored-procedures-are-underrated-in-2026',
 'A measured defense of putting business logic where it belongs: close to the data.',
 '<p>I know, I know. <em>Stored procedures are old.</em> Hear me out.</p><p>For years the dev community has pushed application-side logic. ORMs, query builders, micro-services calling micro-services. But for read-heavy systems with complex joins, a well-named SP can be the cleanest API your team has.</p><p>This isn''t about putting the entire business logic in T-SQL. It''s about acknowledging that some queries are <strong>data shapes</strong>, and they belong in the database.</p>',
 'https://images.unsplash.com/photo-1544383835-bda2bc66a55d?w=900',
 2, 1, 'Published', GETUTCDATE()),

('Setting Up a Self-Hosted CI in 30 Minutes',
 'self-hosted-ci-in-30-minutes',
 'A quick walkthrough of getting GitHub Actions runners on your own hardware.',
 '<p>If you have an old laptop and a weekend, you have everything you need to stop paying for CI minutes. This guide walks through provisioning a self-hosted runner, locking it down, and wiring it into a real-world repo.</p>',
 'https://images.unsplash.com/photo-1518770660439-4636190af475?w=900',
 3, 3, 'Published', GETUTCDATE()),

('What I Wish Junior Devs Asked in Their First 90 Days',
 'what-i-wish-junior-devs-asked',
 'Five questions that would make any new engineer ramp faster.',
 '<p>I''ve onboarded a lot of junior engineers. The strong ones all asked similar questions in their first quarter. Here they are, in order.</p>',
 'https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=900',
 3, 4, 'Published', GETUTCDATE()),

('A Pending Post For Admin Review',
 'a-pending-post-for-admin-review',
 'This post is waiting for moderation — useful to demo the admin flow.',
 '<p>This entry is in the <strong>Pending</strong> state and should not appear on the public site until approved.</p>',
 NULL, 2, 1, 'Pending', NULL);

-- Tag the posts
INSERT INTO PostTags (PostId, TagId) VALUES
(1, 1), (1, 2), (1, 3), (1, 7),
(2, 3), (2, 7),
(3, 4), (3, 5),
(4, 8),
(5, 1);

-- Some sample comments + replies
INSERT INTO Comments (PostId, UserId, ParentId, Content, IsApproved) VALUES
(1, 4, NULL, 'Loved this. Do you have a follow-up on the write path?', 1),
(1, 2, 1,    'Thanks! Yes — drafting it now.',                          1),
(1, 3, NULL, 'Counterpoint: ORM tooling has gotten really good.',      1),
(2, 4, NULL, 'I needed this article five years ago.',                  1),
(2, 4, NULL, 'Pending comment that admin must approve.',               0);

-- Likes
INSERT INTO Likes (PostId, UserId) VALUES
(1, 4), (1, 3),
(2, 4),
(3, 4), (3, 2);
GO
