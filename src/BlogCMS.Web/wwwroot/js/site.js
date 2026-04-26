// ─── Live comment submission ──────────────────────────────────────────────────
document.addEventListener('submit', async (e) => {
  const form = e.target.closest('.comment-form');
  if (!form) return;
  e.preventDefault();

  const textarea = form.querySelector('textarea');
  const content = textarea.value.trim();
  if (!content) return;

  const parentId = form.dataset.parentId || null;
  const fd = new FormData();
  fd.append('content', content);
  if (parentId) fd.append('parentId', parentId);
  fd.append('__RequestVerificationToken', window.ANTI_FORGERY);

  const submitBtn = form.querySelector('button[type="submit"]');
  submitBtn.disabled = true;

  try {
    const res = await fetch(`/posts/${window.POST_ID}/comments`, { method: 'POST', body: fd });
    const data = await res.json();
    if (!data.success) { alert(data.error || 'Failed to post comment.'); return; }

    const node = renderComment(data);
    if (parentId) {
      form.closest('.comment-item').querySelector('.replies').appendChild(node);
      form.closest('.reply-form').style.display = 'none';
    } else {
      document.getElementById('comments-list').prepend(node);
    }
    textarea.value = '';

    const counter = document.getElementById('comment-count');
    if (counter) counter.textContent = parseInt(counter.textContent || '0', 10) + 1;
  } catch (err) {
    alert('Network error. Please try again.');
  } finally {
    submitBtn.disabled = false;
  }
});

// ─── Reply toggles ────────────────────────────────────────────────────────────
document.addEventListener('click', (e) => {
  if (e.target.classList.contains('reply-toggle')) {
    const form = e.target.parentElement.querySelector('.reply-form');
    form.style.display = form.style.display === 'none' ? '' : 'none';
  }
});

// ─── Like button ──────────────────────────────────────────────────────────────
document.getElementById('like-btn')?.addEventListener('click', async (e) => {
  const btn = e.currentTarget;
  if (btn.disabled) return;
  btn.disabled = true;

  const fd = new FormData();
  fd.append('__RequestVerificationToken', window.ANTI_FORGERY);

  try {
    const res = await fetch(`/posts/${btn.dataset.postId}/like`, { method: 'POST', body: fd });
    const data = await res.json();
    btn.querySelector('#like-icon').textContent = data.liked ? '❤' : '♡';
    btn.querySelector('#like-count').textContent = data.count;
    btn.classList.toggle('btn-danger', data.liked);
    btn.classList.toggle('btn-outline-danger', !data.liked);
  } catch {
    /* ignore */
  } finally {
    btn.disabled = false;
  }
});

// ─── Render a comment from server response ────────────────────────────────────
function renderComment(data) {
  const wrapper = document.createElement('div');
  wrapper.className = 'd-flex gap-3 mb-3 comment-item';
  wrapper.dataset.commentId = data.id;
  const avatar = data.avatarUrl || `https://i.pravatar.cc/40?u=${encodeURIComponent(data.username)}`;
  wrapper.innerHTML = `
    <img src="${escapeAttr(avatar)}" class="rounded-circle flex-shrink-0" style="width:40px;height:40px;object-fit:cover" alt="" />
    <div class="flex-grow-1">
      <div class="d-flex align-items-baseline">
        <strong>${escapeHtml(data.username)}</strong>
        <span class="text-muted small ms-2">just now</span>
      </div>
      <p class="mb-1">${escapeHtml(data.content)}</p>
      <button type="button" class="btn btn-sm btn-link p-0 reply-toggle">Reply</button>
      <div class="reply-form mt-2" style="display:none">
        <form class="comment-form" data-parent-id="${data.id}">
          <textarea class="form-control form-control-sm" rows="2" placeholder="Write a reply..." required maxlength="2000"></textarea>
          <button class="btn btn-sm btn-primary mt-1" type="submit">Reply</button>
        </form>
      </div>
      <div class="ms-3 mt-3 replies"></div>
    </div>`;
  return wrapper;
}

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, c =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}
function escapeAttr(s) { return escapeHtml(s); }
