'use strict';

// Kullanıcının henüz görmediği bir güncelleme varsa hamburger butonunda nokta, yan menüdeki
// "Yenilikler" bağlantısında "Yeni" rozeti göster. Görülen son kayıt tarayıcıda tutulur
// (Yenilikler sayfası açılınca güncellenir).
(() => {
    const latest = document.querySelector('[data-changelog-latest]')?.dataset.changelogLatest;
    if (!latest) return;

    let seen = null;
    try { seen = localStorage.getItem('reportdeck.changelog.seen'); } catch { }

    if (seen !== latest) {
        document.querySelectorAll('[data-changelog-dot]').forEach(el => el.classList.remove('d-none'));
    }
})();
