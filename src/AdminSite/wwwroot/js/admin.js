// ── Delete Modal ──────────────────────────────────────
function confirmDelete(formAction, message) {
    const modal = document.getElementById('deleteModal');
    const form  = document.getElementById('deleteModalForm');
    const msg   = document.getElementById('deleteModalMessage');
    if (msg && message) msg.textContent = message;
    form.action = formAction;
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
}
function closeDeleteModal() {
    document.getElementById('deleteModal').style.display = 'none';
    document.body.style.overflow = '';
}
document.addEventListener('keydown', e => {
    if (e.key === 'Escape') closeDeleteModal();
});
document.getElementById('deleteModal')?.addEventListener('click', function(e) {
    if (e.target === this) closeDeleteModal();
});

// ── Live search filter ────────────────────────────────
document.querySelectorAll('[data-search]').forEach(input => {
    const targetSelector = input.dataset.search;
    input.addEventListener('input', () => {
        const q = input.value.toLowerCase();
        document.querySelectorAll(targetSelector).forEach(row => {
            const text = row.textContent.toLowerCase();
            row.style.display = text.includes(q) ? '' : 'none';
        });
        const empty = document.querySelector('[data-empty]');
        if (empty) {
            const visible = [...document.querySelectorAll(targetSelector)].some(r => r.style.display !== 'none');
            empty.style.display = visible ? 'none' : '';
        }
    });
});

// ── Password strength meter ───────────────────────────
document.querySelectorAll('[data-pw-strength]').forEach(input => {
    const bar = document.querySelector(input.dataset.pwStrength);
    if (!bar) return;
    input.addEventListener('input', () => {
        const v = input.value;
        let score = 0;
        if (v.length >= 8)  score++;
        if (v.length >= 12) score++;
        if (/[A-Z]/.test(v)) score++;
        if (/[0-9]/.test(v)) score++;
        if (/[^A-Za-z0-9]/.test(v)) score++;
        const widths = ['0%', '20%', '40%', '65%', '85%', '100%'];
        const colors = ['', '#ef4444', '#f59e0b', '#f59e0b', '#3b82f6', '#10b981'];
        bar.style.width = widths[score];
        bar.style.background = colors[score];
    });
});

// ── Auto-dismiss alerts ───────────────────────────────
document.querySelectorAll('.alert').forEach(el => {
    setTimeout(() => {
        el.style.transition = 'opacity 0.4s';
        el.style.opacity = '0';
        setTimeout(() => el.remove(), 400);
    }, 4000);
});
