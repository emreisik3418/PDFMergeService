'use strict';

// ─── State ───────────────────────────────────────────────────────────────────
let scannedFolders = [];   // { folderName, folderPath, relativePath, pdfFiles[], outputName, outputDirectory, period }
let scannedRootPath = '';

// Windows dosya adında kullanılamayan karakterler.
const INVALID_FILENAME_CHARS = /[\\/:*?"<>|]/;

// ZIP, taranan ana klasörün adını alır: ".../Kurumsal Şubeler 2026 - 3. Çeyrek" → "Kurumsal Şubeler 2026 - 3. Çeyrek.zip"
function zipFileName() {
    const rootName = scannedRootPath.replace(/[\\/]+$/, '').split(/[\\/]/).pop()
        .replace(/[\\/:*?"<>|]/g, '_').trim();
    return `${rootName && !/^[A-Za-z]:$/.test(rootName) ? rootName : 'TopluBirlestirme'}.zip`;
}

// Varsayılan ad ve ZIP klasörü, yoldaki rapor tipi ve dönemden Drive toplu yükleme kurallarının tersiyle önerilir:
//   "Kurumsal Şubeler 2026 - 3. Çeyrek" › "Başkent Kurumsal" → "BAŞKENT KURUMSAL ŞUBESİ 2026 - 3. Çeyrek"
//   "Bireysel ve Karma Şubeler" › "Akdeniz" › "Akdeniz Bulvarı Antalya" › "2026 - Ağustos"
//     → "Akdeniz/AKDENİZ BULVARI ANTALYA ŞUBESİ 2026 - Ağustos"
// Uyan kural yoksa klasör adı (+ dönem).
function suggestOutput(folder, rootPath) {
    const relativePath = folder.relativePath || folder.folderName;
    if (typeof DrivePathResolver === 'undefined') return { fileName: folder.folderName, directory: '', period: '' };
    return DrivePathResolver.suggestMergedFileName(relativePath, rootPath, window.driveBulkUploadRules || []);
}

// Satır birleştirmeye dahil mi: işaretli ve dönem filtresinde görünüyor.
function isActive(i) {
    const row = folderList.querySelector(`tr[data-idx="${i}"]`);
    return !!row && !row.classList.contains('d-none') && row.querySelector('.folder-check').checked;
}

const visibleChecks = () => [...folderList.querySelectorAll('tr:not(.d-none) .folder-check')];

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
const periodFilterWrap   = document.getElementById('periodFilterWrap');
const periodFilter       = document.getElementById('periodFilter');

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

        scannedRootPath = path;
        scannedFolders = (await res.json()).map(f => {
            const suggestion = suggestOutput(f, path);
            return { ...f, outputName: suggestion.fileName, outputDirectory: suggestion.directory, period: suggestion.period };
        });

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

        // Table row: derin yapıda üst klasörler küçük ve soluk, birleştirilecek klasör kalın gösterilir
        const parents = (folder.relativePath || folder.folderName).split('/').slice(0, -1);
        const tr = document.createElement('tr');
        tr.dataset.idx = idx;
        tr.dataset.period = folder.period || '';
        tr.innerHTML = `
            <td class="text-center text-muted">${idx + 1}</td>
            <td>
                ${parents.length ? `<div class="small text-muted text-truncate" style="max-width: 240px;" title="${escHtml(parents.join(' › '))}">${escHtml(parents.join(' › '))} ›</div>` : ''}
                <i class="bi bi-folder-fill text-warning me-2"></i>
                <button class="btn btn-link btn-sm p-0 text-dark fw-medium text-decoration-none"
                        data-bs-toggle="collapse" data-bs-target="#preview_${idx}">
                    ${escHtml(folder.folderName)}
                </button>
            </td>
            <td>
                <div class="input-group input-group-sm">
                    <input type="text" class="form-control output-name" data-idx="${idx}"
                           value="${escHtml(folder.outputName)}" title="${escHtml(folder.outputName)}" aria-label="Birleştirilmiş dosya adı" />
                    <span class="input-group-text">.pdf</span>
                </div>
                ${folder.outputDirectory ? `<div class="small text-muted mt-1"><i class="bi bi-file-zip me-1"></i>ZIP içinde: ${escHtml(folder.outputDirectory)}/</div>` : ''}
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

    renderPeriodFilter();

    // checkbox events
    document.querySelectorAll('.folder-check').forEach(cb => {
        cb.addEventListener('change', () => { updateSelectAll(); validateOutputNames(); });
    });

    document.querySelectorAll('.output-name').forEach(input => {
        input.addEventListener('input', () => {
            scannedFolders[parseInt(input.dataset.idx)].outputName = input.value;
            input.title = input.value;
            validateOutputNames();
        });
    });
}

// ─── Dönem filtresi ───────────────────────────────────────────────────────────
// Birden fazla dönem bulunduysa (ör. şube klasörlerinde 2026 - 3. Çeyrek, 2026 - Ağustos) listelenir;
// seçilen dönemin klasörleri gösterilip işaretlenir. Varsayılan: en güncel dönem.
const PERIOD_MONTHS = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];

// Sıralama anahtarı: yıl, sonra dönemin yıl içindeki ayı (çeyrek = son ayı) — en günceli en üstte göstermek için
function periodSortKey(label) {
    const p = DrivePathResolver.parsePeriod(label);
    const month = p.quarter ? Number(p.quarter) * 3 : PERIOD_MONTHS.indexOf(p.month) + 1;
    return Number(p.year) * 100 + month + (p.quarter ? 0.5 : 0);
}

function renderPeriodFilter() {
    const periods = [...new Set(scannedFolders.map(f => f.period).filter(Boolean))];
    if (periods.length < 2 || typeof DrivePathResolver === 'undefined') {
        periodFilterWrap.classList.add('d-none');
        periodFilter.innerHTML = '';
        return;
    }

    periods.sort((a, b) => periodSortKey(b) - periodSortKey(a));
    periodFilter.innerHTML = '<option value="">Tüm dönemler</option>' + periods
        .map(p => `<option value="${escHtml(p)}">${escHtml(p)} (${scannedFolders.filter(f => f.period === p).length})</option>`)
        .join('');
    periodFilter.value = periods[0];
    periodFilterWrap.classList.remove('d-none');
    applyPeriodFilter();
}

function applyPeriodFilter() {
    const selected = periodFilter.value;
    let number = 0;
    folderList.querySelectorAll('tr[data-idx]').forEach(row => {
        const visible = !selected || row.dataset.period === selected;
        row.classList.toggle('d-none', !visible);
        row.querySelector('.folder-check').checked = visible;
        if (visible) row.cells[0].textContent = ++number;
    });
    updateSelectAll();
    validateOutputNames();
}

periodFilter?.addEventListener('change', applyPeriodFilter);

// Birleştirmeye dahil klasörlerin çıktı adlarını denetler, hatalı alanları işaretler; ilk hatanın mesajını döner.
// Aynı ad yalnızca aynı ZIP klasöründe çakışma sayılır (farklı bölgelerde aynı ad olabilir).
function validateOutputNames() {
    const inputs = [...document.querySelectorAll('.output-name')];
    const seen = new Map();
    let firstError = null;

    inputs.forEach(input => input.classList.remove('is-invalid'));

    scannedFolders.forEach((folder, i) => {
        if (!isActive(i)) return;
        const name = folder.outputName.trim().replace(/\.pdf$/i, '');
        let error = null;

        if (!name) error = `"${folder.relativePath || folder.folderName}" için dosya adı boş.`;
        else if (INVALID_FILENAME_CHARS.test(name)) error = `"${name}" dosya adında \\ / : * ? " < > | karakterleri kullanılamaz.`;
        else {
            const key = `${folder.outputDirectory || ''}/${name}`.toLocaleUpperCase('tr-TR');
            if (seen.has(key)) {
                error = `"${name}" adı ${folder.outputDirectory ? `"${folder.outputDirectory}" klasöründe ` : ''}birden fazla kez kullanılmış.`;
                inputs[seen.get(key)].classList.add('is-invalid');
            } else {
                seen.set(key, i);
            }
        }

        if (error) {
            inputs[i].classList.add('is-invalid');
            firstError ??= error;
        }
    });

    return firstError;
}

// ─── Select All (yalnızca filtrede görünen satırlar) ─────────────────────────
selectAll.addEventListener('change', () => {
    visibleChecks().forEach(cb => cb.checked = selectAll.checked);
    updateMergeBtn();
    validateOutputNames();
});

function updateSelectAll() {
    const checks = visibleChecks();
    selectAll.checked = checks.length > 0 && checks.every(c => c.checked);
    selectAll.indeterminate = checks.some(c => c.checked) && !checks.every(c => c.checked);
    updateMergeBtn();
}

function updateMergeBtn() {
    mergeAllBtn.disabled = !visibleChecks().some(c => c.checked);
}

// ─── Merge All ────────────────────────────────────────────────────────────────
mergeAllBtn.addEventListener('click', async () => {
    const selected = scannedFolders.filter((_, i) => isActive(i));

    if (selected.length === 0) { showToast('En az bir klasör seçin.', 'warning'); return; }

    const nameError = validateOutputNames();
    if (nameError) { showToast(nameError, 'warning'); return; }

    setMerging(true);
    showMergeProgress(true, `0 / ${selected.length} birleştiriliyor...`, 0);

    const payload = {
        folders: selected.map(f => ({
            folderName: f.folderName,
            folderPath: f.folderPath,
            pdfFiles: f.pdfFiles,
            outputFileName: f.outputName.trim().replace(/\.pdf$/i, ''),
            outputDirectory: f.outputDirectory || ''
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
        a.download = zipFileName();
        a.click();
        URL.revokeObjectURL(url);

        showToast(`${selected.length} klasör birleştirildi ve ZIP olarak indirildi!`, 'success');
        window.ReportDeckFx?.celebrate();

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
