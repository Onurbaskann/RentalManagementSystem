// KiraTakip — site.js
// Tooltip (vanilla, [data-tip]), confirm modal (Alpine + form intercept)

(function () {
    // --- Tooltip ---
    let tipEl = null;
    function tipShow(target) {
        const text = target.getAttribute('data-tip');
        if (!text) return;
        // data-tip-when="sidebar-collapsed": yalnız masaüstünde, sidebar daraltılmışken göster
        if (target.getAttribute('data-tip-when') === 'sidebar-collapsed' &&
            !(document.documentElement.classList.contains('sidebar-collapsed') && window.matchMedia('(min-width: 56.26em)').matches)) return;
        tipEl = document.createElement('div');
        tipEl.className = 'tooltip';
        tipEl.textContent = text;
        document.body.appendChild(tipEl);
        const r = target.getBoundingClientRect();
        // data-tip-side="right": öğenin sağında, dikeyde ortalı (sidebar ikonları); varsayılan: üstte, yatayda ortalı
        const toRight = target.getAttribute('data-tip-side') === 'right';
        const top = toRight
            ? r.top + r.height / 2 - tipEl.offsetHeight / 2 + window.scrollY
            : r.top - tipEl.offsetHeight - 8 + window.scrollY;
        const left = toRight
            ? r.right + 10 + window.scrollX
            : r.left + r.width / 2 - tipEl.offsetWidth / 2 + window.scrollX;
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

    // sidebar store: mobil çekmece (sidebarOpen) + masaüstü daraltma (collapsed; sidebar-init.js sınıfı önceden uygular)
    Alpine.store('ui', {
        sidebarOpen: false,
        collapsed: document.documentElement.classList.contains('sidebar-collapsed'),
        toggle() { this.sidebarOpen = !this.sidebarOpen; },
        close() { this.sidebarOpen = false; },
        toggleCollapse() {
            this.collapsed = !this.collapsed;
            document.documentElement.classList.toggle('sidebar-collapsed', this.collapsed);
            try { localStorage.setItem('sidebarCollapsed', this.collapsed ? '1' : '0'); } catch (e) { /* localStorage kapalı olabilir */ }
        }
    });
});

// --- Sidebar tooltip: her menü öğesinin etiketi, daraltılmış sidebar'da ikonun sağında gösterilir ---
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.sidebar .nav-item').forEach(a => {
        const label = a.querySelector('.nav-label');
        if (!label) return;
        a.setAttribute('data-tip', label.textContent.trim());
        a.setAttribute('data-tip-side', 'right');
        a.setAttribute('data-tip-when', 'sidebar-collapsed');
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

// --- Buton onayı: data-confirm-action="/Yol" [data-confirm-needs-reason] [data-confirm-field] ---
// Onay (ve istenirse neden) alındıktan sonra dinamik bir POST formu gönderir.
document.addEventListener('click', async function (e) {
    const btn = e.target.closest('[data-confirm-action]');
    if (!btn || btn.disabled) return;
    e.preventDefault();

    const needsReason = btn.dataset.confirmNeedsReason === 'true';
    const message = btn.dataset.confirm || btn.dataset.confirmPrompt;
    const title = btn.dataset.confirmTitle || 'Onay';
    const answer = await window.Alpine.store('confirm').ask(message, title, needsReason, btn.dataset.confirmReasonLabel || 'Neden');
    if (!answer) return;

    const form = document.createElement('form');
    form.method = btn.dataset.confirmMethod || 'POST';
    form.action = btn.dataset.confirmAction;

    const addField = (name, value) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        form.appendChild(input);
    };
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) addField('__RequestVerificationToken', token.value);
    if (needsReason) addField(btn.dataset.confirmField || 'Reason', answer);

    document.body.appendChild(form);
    form.submit();
});

// --- Toast helper (geriye uyumlu) ---
window.showToast = function (msg, type) {
    const t = document.getElementById('toast');
    if (!t) return;
    t.textContent = msg;
    t.classList.remove('toast-error', 'toast-warning');
    if (type === 'error') t.classList.add('toast-error');
    else if (type === 'warning') t.classList.add('toast-warning');
    t.classList.add('show');
    setTimeout(() => t.classList.remove('show'), 3200);
};

// --- Marka rengi yardımcısı ---
// tokens.css'teki --rgb-* kanalından hex döndürür (ApexCharts gibi CSS değişkeni okumayan kütüphaneler için).
window.brandColor = function (name) {
    const ch = getComputedStyle(document.documentElement).getPropertyValue('--rgb-' + name).trim().split(/\s+/).map(Number);
    return '#' + ch.map(n => n.toString(16).padStart(2, '0')).join('');
};
