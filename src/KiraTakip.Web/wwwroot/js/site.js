// KiraTakip — site.js
// Tooltip (vanilla, [data-tip]), confirm modal (Alpine + form intercept)

(function () {
    // --- Tooltip ---
    let tipEl = null;
    function tipShow(target) {
        const text = target.getAttribute('data-tip');
        if (!text) return;
        tipEl = document.createElement('div');
        tipEl.className = 'tooltip';
        tipEl.textContent = text;
        document.body.appendChild(tipEl);
        const r = target.getBoundingClientRect();
        const top = r.top - tipEl.offsetHeight - 8 + window.scrollY;
        const left = r.left + r.width / 2 - tipEl.offsetWidth / 2 + window.scrollX;
        tipEl.style.top = Math.max(4, top) + 'px';
        tipEl.style.left = Math.max(4, left) + 'px';
        requestAnimationFrame(() => tipEl && tipEl.classList.add('show'));
    }
    function tipHide() {
        if (tipEl) { tipEl.remove(); tipEl = null; }
    }
    document.addEventListener('mouseover', e => {
        const t = e.target.closest && e.target.closest('[data-tip]');
        if (t && !tipEl) tipShow(t);
    });
    document.addEventListener('mouseout', e => {
        const t = e.target.closest && e.target.closest('[data-tip]');
        if (t) tipHide();
    });
    document.addEventListener('focusin', e => {
        const t = e.target.closest && e.target.closest('[data-tip]');
        if (t && !tipEl) tipShow(t);
    });
    document.addEventListener('focusout', tipHide);
    document.addEventListener('keydown', e => { if (e.key === 'Escape') tipHide(); });
    document.addEventListener('scroll', tipHide, true);
})();

// --- Alpine bileşenleri ---
document.addEventListener('alpine:init', () => {
    // confirm store
    Alpine.store('confirm', {
        open: false,
        message: '',
        title: 'Onay',
        needsInput: false,
        inputLabel: '',
        inputValue: '',
        _resolve: null,
        ask(message, title, needsInput = false, inputLabel = '') {
            this.message = message || 'Devam edilsin mi?';
            this.title = title || 'Onay';
            this.needsInput = needsInput;
            this.inputLabel = inputLabel;
            this.inputValue = '';
            this.open = true;
            return new Promise(r => { this._resolve = r; });
        },
        answer(yes) {
            this.open = false;
            const r = this._resolve;
            this._resolve = null;
            if (r) {
                if (yes) {
                    r(this.needsInput ? (this.inputValue || '').trim() : true);
                } else {
                    r(false);
                }
            }
        }
    });

    // mobile sidebar store
    Alpine.store('ui', {
        sidebarOpen: false,
        toggle() { this.sidebarOpen = !this.sidebarOpen; },
        close() { this.sidebarOpen = false; }
    });
});

// --- Form intercept: data-confirm="..." ---
document.addEventListener('submit', async function (e) {
    const form = e.target;
    if (!(form instanceof HTMLFormElement)) return;
    const msg = form.getAttribute('data-confirm');
    if (!msg) return;
    if (form._confirmed) { form._confirmed = false; return; }
    e.preventDefault();
    const ok = await window.Alpine.store('confirm').ask(msg);
    if (ok) {
        form._confirmed = true;
        if (typeof form.requestSubmit === 'function') form.requestSubmit(); else form.submit();
    }
});

// --- Toast helper (geriye uyumlu) ---
window.showToast = function (msg, type) {
    const t = document.getElementById('toast');
    if (!t) return;
    t.textContent = msg;
    t.style.background = type === 'error' ? '#dc2626' : (type === 'warning' ? '#b45309' : '#1a2332');
    t.classList.add('show');
    setTimeout(() => t.classList.remove('show'), 3200);
};
