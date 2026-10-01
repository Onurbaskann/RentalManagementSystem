// Sidebar daraltma tercihi. <head>'de SENKRON çalışmalı: sınıf ilk boyamadan önce uygulanır, böylece
// sayfa geçişinde/yenilemede genişten daralmaya "titreme" olmaz. Geçiş animasyonu ilk boyamadan sonra açılır.
(function () {
    try {
        if (localStorage.getItem('sidebarCollapsed') === '1') {
            document.documentElement.classList.add('sidebar-collapsed');
        }
    } catch (e) { /* localStorage kapalı olabilir; varsayılan: geniş */ }

    document.addEventListener('DOMContentLoaded', function () {
        requestAnimationFrame(function () {
            document.documentElement.classList.add('sidebar-anim');
        });
    });
})();
