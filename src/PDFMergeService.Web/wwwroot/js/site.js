'use strict';

const prefersReducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

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

// Modal açılırken body'ye taşınır: sayfa içindeki bir kapsayıcı (ör. animasyonlu kart) kendi
// katmanını oluştursa bile modal, backdrop'un altında kalmaz (bkz. a0428b9).
document.addEventListener('show.bs.modal', e => {
    if (e.target.parentElement !== document.body) document.body.appendChild(e.target);
});

// Butonlara tıklama dalgası (ripple).
document.addEventListener('pointerdown', e => {
    if (prefersReducedMotion) return;
    const btn = e.target.closest('.btn');
    if (!btn || btn.disabled || btn.classList.contains('app-menu-btn')) return;

    const rect = btn.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const ripple = document.createElement('span');
    ripple.className = 'rd-ripple';
    ripple.style.width = ripple.style.height = `${size}px`;
    ripple.style.left = `${e.clientX - rect.left - size / 2}px`;
    ripple.style.top = `${e.clientY - rect.top - size / 2}px`;
    btn.appendChild(ripple);
    ripple.addEventListener('animationend', () => ripple.remove());
});

// Başarılı işlemlerden sonra kısa bir konfeti patlaması: window.ReportDeckFx.celebrate()
window.ReportDeckFx = {
    celebrate() {
        if (prefersReducedMotion) return;

        const canvas = document.createElement('canvas');
        canvas.className = 'rd-confetti';
        document.body.appendChild(canvas);
        const ctx = canvas.getContext('2d');
        const dpr = window.devicePixelRatio || 1;
        canvas.width = innerWidth * dpr;
        canvas.height = innerHeight * dpr;
        ctx.scale(dpr, dpr);

        const colors = ['#dc0005', '#ff3b3f', '#ffd43b', '#1d2433', '#ffffff', '#ff8a8c'];
        const pieces = Array.from({ length: 140 }, (_, i) => {
            const fromLeft = i % 2 === 0;
            const angle = (fromLeft ? -60 : -120) + (Math.random() * 40 - 20);
            const speed = 9 + Math.random() * 9;
            return {
                x: fromLeft ? innerWidth * 0.15 : innerWidth * 0.85,
                y: innerHeight * 0.7,
                vx: Math.cos(angle * Math.PI / 180) * speed,
                vy: Math.sin(angle * Math.PI / 180) * speed,
                w: 6 + Math.random() * 6,
                h: 8 + Math.random() * 8,
                rot: Math.random() * Math.PI,
                vr: (Math.random() - 0.5) * 0.3,
                color: colors[i % colors.length]
            };
        });

        const start = performance.now();
        const duration = 2200;

        (function frame(now) {
            const t = now - start;
            ctx.clearRect(0, 0, innerWidth, innerHeight);
            ctx.globalAlpha = Math.max(0, 1 - Math.max(0, t - duration * 0.6) / (duration * 0.4));

            for (const p of pieces) {
                p.vy += 0.35;
                p.vx *= 0.985;
                p.x += p.vx;
                p.y += p.vy;
                p.rot += p.vr;
                ctx.save();
                ctx.translate(p.x, p.y);
                ctx.rotate(p.rot);
                ctx.fillStyle = p.color;
                ctx.fillRect(-p.w / 2, -p.h / 2, p.w, p.h * Math.abs(Math.cos(p.rot * 2)));
                ctx.restore();
            }

            if (t < duration) requestAnimationFrame(frame);
            else canvas.remove();
        })(start);
    }
};
