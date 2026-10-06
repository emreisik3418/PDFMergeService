'use strict';

// Menüdeki "Yenilikler" bağlantısında, kullanıcının henüz görmediği bir güncelleme varsa nokta göster.
// Görülen son kayıt tarayıcıda tutulur (Yenilikler sayfası açılınca güncellenir).
(() => {
    const link = document.querySelector('[data-changelog-latest]');
    const latest = link?.dataset.changelogLatest;
    if (!latest) return;

    let seen = null;
    try { seen = localStorage.getItem('reportdeck.changelog.seen'); } catch { }

    if (seen !== latest) link.querySelector('[data-changelog-dot]')?.classList.remove('d-none');
})();
