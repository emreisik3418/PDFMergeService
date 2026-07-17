'use strict';

// ─── State ───────────────────────────────────────────────────────────────────
let scannedFolders = [];   // { folderName, folderPath, pdfFiles[] }

// ─── DOM Refs ─────────────────────────────────────────────────────────────────
const rootPathInput      = document.getElementById('rootPath');
const scanBtn            = document.getElementById('scanBtn');
const scanSpinner        = document.getElementById('scanSpinner');
const folderListSection  = document.getElementById('folderListSection');
const folderList         = document.getElementById('folderList');
const folderCount        = document.getElementById('folderCount');
const totalPdfBadge      = document.getElementById('totalPdfBadge');
const filePreviewAccordion = document.getElementById('filePreviewAccordion');
const selectAll          = document.getElementById('selectAll');
const emptyState         = document.getElementById('emptyState');
const mergeAllBtn        = document.getElementById('mergeAllBtn');
const mergeAllBtnNormal  = document.getElementById('mergeAllBtnNormal');
const mergeAllBtnLoading = document.getElementById('mergeAllBtnLoading');
const mergeProgress      = document.getElementById('mergeProgress');
const mergeProgressBar   = document.getElementById('mergeProgressBar');
const mergeProgressLabel = document.getElementById('mergeProgressLabel');
const mergeProgressPercent = document.getElementById('mergeProgressPercent');
const transferBtn        = document.getElementById('transferBtn');
const transferBtnNormal  = document.getElementById('transferBtnNormal');
const transferBtnLoading = document.getElementById('transferBtnLoading');
const transferResults    = document.getElementById('transferResults');

// ─── Scan ─────────────────────────────────────────────────────────────────────
scanBtn.addEventListener('click', scanFolders);
rootPathInput.addEventListener('keydown', e => { if (e.key === 'Enter') scanFolders(); });

async function scanFolders() {
    const path = rootPathInput.value.trim();
    if (!path) { showToast('Lütfen bir klasör yolu girin.', 'warning'); return; }

    setScanLoading(true);
    folderListSection.classList.add('d-none');
    emptyState.classList.add('d-none');

    try {
        const res = await fetch('/folder-merge/scan', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ rootPath: path })
        });

        if (!res.ok) {
            const err = await res.json();
            showToast(err.error || 'Tarama başarısız.', 'danger');
            return;
        }

        scannedFolders = await res.json();

        if (scannedFolders.length === 0) {
            emptyState.classList.remove('d-none');
        } else {
            renderFolderList();
            folderListSection.classList.remove('d-none');
            showToast(`${scannedFolders.length} klasör bulundu.`, 'success');
        }

    } catch {
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setScanLoading(false);
    }
}

function setScanLoading(loading) {
    scanBtn.disabled = loading;
    scanSpinner.classList.toggle('d-none', !loading);
    scanBtn.innerHTML = loading
        ? '<span class="spinner-border spinner-border-sm me-1"></span>Tarıyor...'
        : '<i class="bi bi-search me-2"></i>Tara';
}

// ─── Render Folder List ───────────────────────────────────────────────────────
function renderFolderList() {
    folderList.innerHTML = '';
    filePreviewAccordion.innerHTML = '';

    let totalPdf = 0;

    scannedFolders.forEach((folder, idx) => {
        totalPdf += folder.pdfCount;

        // Table row
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td class="text-center text-muted">${idx + 1}</td>
            <td>
                <i class="bi bi-folder-fill text-warning me-2"></i>
                <button class="btn btn-link btn-sm p-0 text-dark fw-medium text-decoration-none"
                        data-bs-toggle="collapse" data-bs-target="#preview_${idx}">
                    ${escHtml(folder.folderName)}
                </button>
            </td>
            <td class="text-center">
                <span class="badge bg-secondary">${folder.pdfCount}</span>
            </td>
            <td class="text-center">
                <input type="checkbox" class="form-check-input folder-check" data-idx="${idx}" checked />
            </td>`;
        folderList.appendChild(tr);

        // File preview accordion
        const files = folder.pdfFiles.map((f, fi) =>
            `<li class="list-group-item list-group-item-action py-1 px-3 small">
                <i class="bi bi-file-earmark-pdf text-danger me-2"></i>
                <span class="me-2 text-muted">${fi + 1}.</span>${escHtml(f)}
             </li>`
        ).join('');

        const acc = document.createElement('div');
        acc.className = 'accordion-item';
        acc.innerHTML = `
            <div id="preview_${idx}" class="accordion-collapse collapse">
                <div class="accordion-body p-0">
                    <ul class="list-group list-group-flush">${files}</ul>
                </div>
            </div>`;
        filePreviewAccordion.appendChild(acc);
    });

    folderCount.textContent = scannedFolders.length;
    totalPdfBadge.textContent = `Toplam ${totalPdf} PDF`;
    mergeAllBtn.disabled = false;
    transferBtn.disabled = false;
    document.getElementById('f_detectLogoBtn').disabled = false;

    // checkbox events
    document.querySelectorAll('.folder-check').forEach(cb => {
        cb.addEventListener('change', updateSelectAll);
    });
}

// ─── Select All ───────────────────────────────────────────────────────────────
selectAll.addEventListener('change', () => {
    document.querySelectorAll('.folder-check').forEach(cb => cb.checked = selectAll.checked);
    updateMergeBtn();
});

function updateSelectAll() {
    const checks = [...document.querySelectorAll('.folder-check')];
    selectAll.checked = checks.every(c => c.checked);
    selectAll.indeterminate = checks.some(c => c.checked) && !checks.every(c => c.checked);
    updateMergeBtn();
}

function updateMergeBtn() {
    const anyChecked = [...document.querySelectorAll('.folder-check')].some(c => c.checked);
    mergeAllBtn.disabled = !anyChecked;
    transferBtn.disabled = !anyChecked;
}

// ─── Merge All ────────────────────────────────────────────────────────────────
mergeAllBtn.addEventListener('click', async () => {
    const checks = [...document.querySelectorAll('.folder-check')];
    const selected = scannedFolders.filter((_, i) => checks[i]?.checked);

    if (selected.length === 0) { showToast('En az bir klasör seçin.', 'warning'); return; }

    setMerging(true);
    showMergeProgress(true, `0 / ${selected.length} birleştiriliyor...`, 0);

    const payload = {
        folders: selected.map(f => ({
            folderName: f.folderName,
            folderPath: f.folderPath,
            pdfFiles: f.pdfFiles
        })),
        footer: collectFooterSettings()
    };

    // fake progress animation
    let prog = 0;
    const progInterval = setInterval(() => {
        prog = Math.min(prog + 5, 85);
        mergeProgressBar.style.width = prog + '%';
        mergeProgressPercent.textContent = Math.round(prog) + '%';
    }, 400);

    try {
        const res = await fetch('/folder-merge/merge-all', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        clearInterval(progInterval);

        if (!res.ok) {
            const err = await res.json().catch(() => ({ error: 'Birleştirme başarısız.' }));
            showToast(err.error || 'Birleştirme başarısız.', 'danger');
            return;
        }

        mergeProgressBar.style.width = '100%';
        mergeProgressPercent.textContent = '100%';
        mergeProgressLabel.textContent = 'Tamamlandı!';

        const blob = await res.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `TopluBirlestirme_${new Date().toISOString().slice(0,10)}.zip`;
        a.click();
        URL.revokeObjectURL(url);

        showToast(`${selected.length} klasör birleştirildi ve ZIP olarak indirildi!`, 'success');

    } catch {
        clearInterval(progInterval);
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setMerging(false);
        setTimeout(() => showMergeProgress(false), 3000);
    }
});

function setMerging(loading) {
    mergeAllBtn.disabled = loading;
    mergeAllBtnNormal.classList.toggle('d-none', loading);
    mergeAllBtnLoading.classList.toggle('d-none', !loading);
}

function showMergeProgress(show, label = '', percent = 0) {
    mergeProgress.classList.toggle('d-none', !show);
    if (show) {
        mergeProgressLabel.textContent = label;
        mergeProgressBar.style.width = percent + '%';
        mergeProgressPercent.textContent = percent + '%';
    }
}

// ─── SharePoint Transfer ──────────────────────────────────────────────────────
transferBtn.addEventListener('click', async () => {
    const checks = [...document.querySelectorAll('.folder-check')];
    const selected = scannedFolders.filter((_, i) => checks[i]?.checked);

    if (selected.length === 0) { showToast('En az bir klasör seçin.', 'warning'); return; }

    const webPath = document.getElementById('t_webPath').value.trim();
    const path = document.getElementById('t_path').value.trim();
    if (!webPath || !path) { showToast('Site alt yolu ve hedef klasör alanları zorunludur.', 'warning'); return; }

    setTransferring(true);
    transferResults.innerHTML = '';

    const payload = {
        folders: selected.map(f => ({
            folderName: f.folderName,
            folderPath: f.folderPath,
            pdfFiles: f.pdfFiles
        })),
        footer: collectFooterSettings(),
        webPath,
        path,
        extraParams: document.getElementById('t_extraParams').value.trim() || null
    };

    try {
        const res = await fetch('/folder-merge/transfer', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res.ok) {
            const err = await res.json().catch(() => ({ error: 'Aktarım başarısız.' }));
            showToast(err.error || 'Aktarım başarısız.', 'danger');
            return;
        }

        const data = await res.json();
        renderTransferResults(data.results);

        const successCount = data.results.filter(r => r.success).length;
        showToast(`${successCount} / ${data.results.length} klasör SharePoint'e aktarıldı.`,
            successCount === data.results.length ? 'success' : 'warning');

    } catch {
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setTransferring(false);
    }
});

function renderTransferResults(results) {
    transferResults.innerHTML = results.map(r => `
        <div class="d-flex align-items-start gap-2 mb-1">
            <i class="bi bi-${r.success ? 'check-circle-fill text-success' : 'x-circle-fill text-danger'}"></i>
            <span><strong>${escHtml(r.folder)}</strong> — ${escHtml(r.message || (r.success ? 'Aktarıldı' : 'Başarısız'))}</span>
        </div>`).join('');
}

function setTransferring(loading) {
    transferBtn.disabled = loading;
    transferBtnNormal.classList.toggle('d-none', loading);
    transferBtnLoading.classList.toggle('d-none', !loading);
}

// ─── Logo Detection ───────────────────────────────────────────────────────────
document.getElementById('f_detectLogoBtn').addEventListener('click', async () => {
    const checks = [...document.querySelectorAll('.folder-check')];
    const selected = scannedFolders.filter((_, i) => checks[i]?.checked);

    if (selected.length === 0) { showToast('En az bir klasör seçin.', 'warning'); return; }

    setFolderDetecting(true);

    const payload = {
        folders: selected.map(f => ({
            folderName: f.folderName,
            folderPath: f.folderPath,
            pdfFiles: f.pdfFiles
        }))
    };

    try {
        const res = await fetch('/folder-merge/detect-logo', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res.ok) { showToast('Tespit başarısız.', 'danger'); return; }

        const data = await res.json();
        document.getElementById('f_logoSkipPages').value = data.logoPages.join(', ');

        if (data.logoPages.length > 0) {
            showToast(`${data.checkedFiles} dosyada tarandı — ${data.logoPages.length} sayfada logo bulundu, alan güncellendi.`, 'success');
        } else {
            showToast(`${data.checkedFiles} dosya tarandı, logo tespit edilemedi.`, 'info');
        }
    } catch {
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setFolderDetecting(false);
    }
});

function setFolderDetecting(loading) {
    const btn = document.getElementById('f_detectLogoBtn');
    btn.disabled = loading;
    btn.innerHTML = loading
        ? '<span class="spinner-border spinner-border-sm me-1" role="status"></span>Taranıyor...'
        : '<i class="bi bi-search me-1"></i>Klasörlerde Logo Tespit Et';
}

// ─── Footer Settings ──────────────────────────────────────────────────────────
document.getElementById('f_pageNumberEnabled').addEventListener('change', function () {
    const opts = document.getElementById('f_pageNumberOptions');
    opts.style.opacity = this.checked ? '1' : '0.4';
    opts.style.pointerEvents = this.checked ? 'auto' : 'none';
});

document.getElementById('f_logoEnabled').addEventListener('change', function () {
    const opts = document.getElementById('f_logoOptions');
    opts.style.opacity = this.checked ? '1' : '0.4';
    opts.style.pointerEvents = this.checked ? 'auto' : 'none';
});

function collectFooterSettings() {
    return {
        pageNumberEnabled: document.getElementById('f_pageNumberEnabled').checked,
        startFromPage: parseInt(document.getElementById('f_startFromPage').value),
        pageNumberPosition: parseInt(document.getElementById('f_pageNumberPosition').value),
        fontSize: parseInt(document.getElementById('f_fontSize').value),
        fontColor: document.getElementById('f_fontColor').value,
        logoEnabled: document.getElementById('f_logoEnabled').checked,
        customLogoPath: null,
        logoPosition: parseInt(document.getElementById('f_logoPosition').value),
        logoWidth: parseFloat(document.getElementById('f_logoWidth').value),
        logoHeight: parseFloat(document.getElementById('f_logoHeight').value),
        marginBottom: parseFloat(document.getElementById('f_marginBottom').value),
        marginHorizontal: parseFloat(document.getElementById('f_marginHorizontal').value),
        logoSkipPages: parsePageRanges(document.getElementById('f_logoSkipPages').value)
    };
}

function parsePageRanges(input) {
    if (!input || !input.trim()) return [];
    const pages = new Set();
    input.split(',').forEach(part => {
        part = part.trim();
        if (part.includes('-')) {
            const [a, b] = part.split('-').map(n => parseInt(n.trim(), 10));
            if (!isNaN(a) && !isNaN(b) && a <= b) {
                for (let i = a; i <= b; i++) pages.add(i);
            }
        } else {
            const n = parseInt(part, 10);
            if (!isNaN(n) && n > 0) pages.add(n);
        }
    });
    return [...pages].sort((a, b) => a - b);
}

// ─── Toast ────────────────────────────────────────────────────────────────────
function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer');
    const icons = { success: 'check-circle-fill', danger: 'exclamation-triangle-fill', warning: 'exclamation-circle-fill', info: 'info-circle-fill' };
    const id = 'toast_' + Date.now();
    container.insertAdjacentHTML('beforeend', `
        <div id="${id}" class="toast align-items-center text-bg-${type} border-0 shadow" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi bi-${icons[type] || 'info-circle-fill'} me-2"></i>${escHtml(message)}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`);
    const toastEl = document.getElementById(id);
    const toast = new bootstrap.Toast(toastEl, { delay: 5000 });
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

function escHtml(str) {
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
