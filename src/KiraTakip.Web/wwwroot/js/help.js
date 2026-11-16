// Yardim Merkezi (Alpine). Sync script olarak body sonunda yuklenir; defer'li Alpine'den once calisip alpine:init'e kaydolur.
document.addEventListener('alpine:init', () => {
    Alpine.data('helpCenter', () => ({
        role: 'all',
        query: '',
        tocOpen: false,
        lightboxSrc: '',
        activeId: '',
        visibleCards: 0,
        observer: null,

        init() {
            this.$watch('query', () => this.applyFilters());
            this.$watch('role', () => this.applyFilters());
            this.observeSections();
            if (location.hash.length > 1) {
                let id = '';
                try { id = decodeURIComponent(location.hash.slice(1)); } catch { /* bozuk hash yok sayilir */ }
                const el = id ? document.getElementById(id) : null;
                if (el) this.$nextTick(() => el.scrollIntoView());
            }
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape') { this.lightboxSrc = ''; return; }
                const tag = (e.target.tagName || '').toLowerCase();
                if (e.key === '/' && !['input', 'textarea', 'select'].includes(tag) && !e.target.isContentEditable) {
                    e.preventDefault();
                    this.$refs.search?.focus();
                }
            });
        },

        get roleLabel() {
            return this.role === 'admin' ? 'Yönetim Paneli' : this.role === 'tenant' ? 'Kiracı Portalı' : 'Tüm Roller';
        },
        get filtered() { return this.query.trim() !== '' || this.role !== 'all'; },
        get statusText() {
            const q = this.query.trim();
            if (!this.filtered) return '';
            if (this.visibleCards === 0) return 'Sonuç bulunamadı.';
            return q
                ? `"${q}" araması için (${this.roleLabel}) ${this.visibleCards} bölüm kartı bulundu.`
                : `Aktif görünüm filtresi: ${this.roleLabel}`;
        },
        get noResult() { return this.filtered && this.visibleCards === 0; },

        applyFilters() {
            const q = this.query.trim().toLowerCase();
            let visible = 0;
            this.$root.querySelectorAll('.module-section').forEach(sec => {
                const secRole = sec.dataset.role || 'all';
                if (!(this.role === 'all' || secRole === 'all' || secRole === this.role)) {
                    sec.classList.add('hidden-by-filter');
                    return;
                }
                let any = false;
                sec.querySelectorAll('.page-card').forEach(card => {
                    const match = !q || card.textContent.toLowerCase().includes(q);
                    card.classList.toggle('hidden-by-filter', !match);
                    if (match) { any = true; visible++; }
                });
                sec.querySelectorAll('.screen-preview-card').forEach(p =>
                    p.classList.toggle('hidden-by-filter', !!q && !any));
                sec.classList.toggle('hidden-by-filter', !any);
            });
            this.$root.querySelectorAll('.toc-link').forEach(link => {
                const r = link.dataset.tocRole;
                link.classList.toggle('hidden-by-filter', !(this.role === 'all' || !r || r === this.role));
            });
            this.visibleCards = visible;
        },

        reset() { this.query = ''; this.role = 'all'; },

        observeSections() {
            if (!('IntersectionObserver' in window)) return;
            this.observer = new IntersectionObserver(entries => {
                entries.forEach(e => { if (e.isIntersecting) this.activeId = e.target.id; });
            }, { rootMargin: '-10% 0px -75% 0px' });
            this.$root.querySelectorAll('.module-section[id]').forEach(s => this.observer.observe(s));
        },

        go(id) { this.activeId = id; this.tocOpen = false; },
    }));
});
