// KiraTakip — site.js
// Tooltip (vanilla, [data-tip]), clientTable (Alpine), confirm modal (Alpine + form intercept)

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

    // clientTable (Alpine.data)
    Alpine.data('clientTable', (opts = {}) => ({
        pageSize: opts.pageSize || 25,
        page: 1,
        q: '',
        rows: [],
        filteredRows: [],
        init() {
            const tbody = this.$refs.tbody || this.$el.querySelector('tbody');
            if (!tbody) return;
            this.rows = Array.from(tbody.querySelectorAll('tr[data-row]'));
            this.filter();
            this.$watch('q', () => { this.page = 1; this.filter(); });
            this.$watch('pageSize', () => { this.page = 1; this.apply(); });
            this.$watch('page', () => this.apply());
        },
        filter() {
            const q = (this.q || '').trim().toLowerCase();
            this.filteredRows = q
                ? this.rows.filter(r => (r.dataset.search || '').toLowerCase().includes(q))
                : this.rows.slice();
            this.apply();
        },
        apply() {
            const start = (this.page - 1) * this.pageSize;
            const end = start + this.pageSize;
            this.rows.forEach(r => r.style.display = 'none');
            this.filteredRows.slice(start, end).forEach(r => r.style.display = '');
        },
        get total() { return this.filteredRows.length; },
        get totalPages() { return Math.max(1, Math.ceil(this.total / this.pageSize)); },
        get from() { return this.total === 0 ? 0 : (this.page - 1) * this.pageSize + 1; },
        get to() { return Math.min(this.page * this.pageSize, this.total); },
        get pageList() {
            const tp = this.totalPages, c = this.page;
            const set = new Set([1, tp]);
            for (let i = c - 2; i <= c + 2; i++) if (i >= 1 && i <= tp) set.add(i);
            return Array.from(set).sort((a, b) => a - b);
        },
        next() { if (this.page < this.totalPages) this.page++; },
        prev() { if (this.page > 1) this.page--; },
        goto(p) { this.page = Math.max(1, Math.min(this.totalPages, p)); }
    }));

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
