'use strict';

// ─── State ───────────────────────────────────────────────────────────────────
let uploadedFiles = [];   // { fileName, tempFilePath, pageCount, fileSize, fileSizeFormatted, order }
let customLogoPath = null;

// ─── DOM Refs ─────────────────────────────────────────────────────────────────
const dropZone        = document.getElementById('dropZone');
const fileInput       = document.getElementById('fileInput');
const browseBtn       = document.getElementById('browseBtn');
const fileList        = document.getElementById('fileList');
const fileListSection = document.getElementById('fileListSection');
const fileCount       = document.getElementById('fileCount');
const clearAllBtn     = document.getElementById('clearAllBtn');
const mergeBtn        = document.getElementById('mergeBtn');
const mergeBtnNormal  = document.getElementById('mergeBtnNormal');
const mergeBtnLoading = document.getElementById('mergeBtnLoading');
const uploadProgress  = document.getElementById('uploadProgress');
const uploadBar       = document.getElementById('uploadBar');
const uploadPercent   = document.getElementById('uploadPercent');

// Footer inputs
const pageNumberEnabled   = document.getElementById('pageNumberEnabled');
const pageNumberOptions   = document.getElementById('pageNumberOptions');
const logoEnabled         = document.getElementById('logoEnabled');
const logoOptions         = document.getElementById('logoOptions');
const customLogoFile      = document.getElementById('customLogoFile');

// ─── Drag & Drop ──────────────────────────────────────────────────────────────
dropZone.addEventListener('dragover', e => {
    e.preventDefault();
    dropZone.classList.add('bg-primary-subtle');
});

dropZone.addEventListener('dragleave', () => {
    dropZone.classList.remove('bg-primary-subtle');
});

dropZone.addEventListener('drop', e => {
    e.preventDefault();
    dropZone.classList.remove('bg-primary-subtle');
    const files = [...e.dataTransfer.files].filter(f => f.type === 'application/pdf' || f.name.endsWith('.pdf'));
    if (files.length > 0) uploadFiles(files);
    else showToast('Sadece PDF dosyası kabul edilir.', 'warning');
});

browseBtn.addEventListener('click', () => fileInput.click());
fileInput.addEventListener('change', () => {
    if (fileInput.files.length > 0) uploadFiles([...fileInput.files]);
    fileInput.value = '';
});

// ─── Upload ───────────────────────────────────────────────────────────────────
async function uploadFiles(files) {
    const formData = new FormData();
    files.forEach(f => formData.append('files', f));

    showUploadProgress(true);
    animateProgress();

    try {
        const response = await fetch('/upload', { method: 'POST', body: formData });

        showUploadProgress(false);

        if (!response.ok) {
            const err = await response.json();
            showToast((err.errors || ['Yükleme başarısız.']).join('\n'), 'danger');
            return;
        }

        const newFiles = await response.json();
        newFiles.forEach(f => uploadedFiles.push(f));

        uploadedFiles.sort((a, b) => a.fileName.localeCompare(b.fileName, 'tr', { sensitivity: 'base' }));
        uploadedFiles.forEach((f, i) => f.order = i);

        renderFileList();
        showToast(`${newFiles.length} dosya başarıyla yüklendi.`, 'success');

    } catch (e) {
        showUploadProgress(false);
        showToast('Sunucu bağlantısı hatası.', 'danger');
    }
}

function showUploadProgress(show) {
    uploadProgress.classList.toggle('d-none', !show);
    if (show) { uploadBar.style.width = '0%'; uploadPercent.textContent = '0%'; }
}

function animateProgress() {
    let p = 0;
    const iv = setInterval(() => {
        p = Math.min(p + Math.random() * 15, 90);
        uploadBar.style.width = p + '%';
        uploadPercent.textContent = Math.round(p) + '%';
        if (!uploadProgress.classList.contains('d-none') === false) clearInterval(iv);
    }, 200);
    setTimeout(() => clearInterval(iv), 8000);
}

// ─── File List Rendering ──────────────────────────────────────────────────────
function renderFileList() {
    fileList.innerHTML = '';
    uploadedFiles.forEach((file, idx) => {
        const tr = document.createElement('tr');
        tr.dataset.index = idx;
        tr.style.cursor = 'grab';
        tr.innerHTML = `
            <td class="text-center text-muted">
                <i class="bi bi-grip-vertical me-1"></i>${idx + 1}
            </td>
            <td>
                <i class="bi bi-file-earmark-pdf text-danger me-2"></i>
                <span class="text-truncate" title="${escHtml(file.fileName)}" style="max-width:180px;display:inline-block;vertical-align:middle;">${escHtml(file.fileName)}</span>
            </td>
            <td class="text-center">
                <span class="badge bg-secondary">${file.pageCount > 0 ? file.pageCount : '?'}</span>
            </td>
            <td class="text-center text-muted small">${file.fileSizeFormatted}</td>
            <td class="text-center">
                <button class="btn btn-sm btn-outline-danger border-0 py-0 px-1 remove-btn" data-idx="${idx}" title="Kaldır">
                    <i class="bi bi-x-lg"></i>
                </button>
            </td>`;
        fileList.appendChild(tr);
    });

    document.querySelectorAll('.remove-btn').forEach(btn => {
        btn.addEventListener('click', () => removeFile(parseInt(btn.dataset.idx)));
    });

    const hasFiles = uploadedFiles.length > 0;
    fileListSection.classList.toggle('d-none', !hasFiles);
    fileCount.textContent = uploadedFiles.length;
    mergeBtn.disabled = uploadedFiles.length < 2;

    if (hasFiles && !window._sortableInit) {
        initSortable();
        window._sortableInit = true;
    }
}

function initSortable() {
    Sortable.create(fileList, {
        animation: 150,
        ghostClass: 'table-primary',
        handle: 'tr',
        onEnd: () => {
            const rows = fileList.querySelectorAll('tr');
            const reordered = [];
            rows.forEach(row => reordered.push(uploadedFiles[parseInt(row.dataset.index)]));
            uploadedFiles = reordered;
            uploadedFiles.forEach((f, i) => f.order = i);
            renderFileList();
        }
    });
}

function removeFile(idx) {
    uploadedFiles.splice(idx, 1);
    uploadedFiles.forEach((f, i) => f.order = i);
    if (uploadedFiles.length === 0) window._sortableInit = false;
    renderFileList();
}

clearAllBtn.addEventListener('click', () => {
    uploadedFiles = [];
    window._sortableInit = false;
    renderFileList();
    showToast('Tüm dosyalar temizlendi.', 'info');
});

// ─── Footer Toggle ────────────────────────────────────────────────────────────
pageNumberEnabled.addEventListener('change', () => {
    pageNumberOptions.style.opacity = pageNumberEnabled.checked ? '1' : '0.4';
    pageNumberOptions.style.pointerEvents = pageNumberEnabled.checked ? 'auto' : 'none';
});

logoEnabled.addEventListener('change', () => {
    logoOptions.style.opacity = logoEnabled.checked ? '1' : '0.4';
    logoOptions.style.pointerEvents = logoEnabled.checked ? 'auto' : 'none';
});

// ─── Custom Logo Upload ───────────────────────────────────────────────────────
customLogoFile.addEventListener('change', async () => {
    if (!customLogoFile.files[0]) return;
    const fd = new FormData();
    fd.append('logo', customLogoFile.files[0]);
    try {
        const res = await fetch('/upload-logo', { method: 'POST', body: fd });
        if (res.ok) {
            const data = await res.json();
            customLogoPath = data.logoPath;
            showToast('Özel logo yüklendi.', 'success');
        } else {
            showToast('Logo yüklenemedi.', 'warning');
        }
    } catch { showToast('Logo yükleme hatası.', 'danger'); }
});

// ─── Merge ────────────────────────────────────────────────────────────────────
mergeBtn.addEventListener('click', async () => {
    if (uploadedFiles.length < 2) {
        showToast('En az 2 PDF dosyası gereklidir.', 'warning');
        return;
    }

    setMerging(true);

    const payload = {
        files: uploadedFiles.map((f, i) => ({
            fileName: f.fileName,
            tempFilePath: f.tempFilePath,
            pageCount: f.pageCount,
            fileSize: f.fileSize,
            fileSizeFormatted: f.fileSizeFormatted,
            order: i
        })),
        footer: {
            pageNumberEnabled: pageNumberEnabled.checked,
            startFromPage: parseInt(document.getElementById('startFromPage').value),
            pageNumberPosition: parseInt(document.getElementById('pageNumberPosition').value),
            fontSize: parseInt(document.getElementById('fontSize').value),
            fontColor: document.getElementById('fontColor').value,
            logoEnabled: logoEnabled.checked,
            customLogoPath: customLogoPath,
            logoPosition: parseInt(document.getElementById('logoPosition').value),
            logoWidth: parseFloat(document.getElementById('logoWidth').value),
            logoHeight: parseFloat(document.getElementById('logoHeight').value),
            marginBottom: parseFloat(document.getElementById('marginBottom').value),
            marginHorizontal: parseFloat(document.getElementById('marginHorizontal').value),
            logoSkipPages: parsePageRanges(document.getElementById('logoSkipPages').value)
        }
    };

    try {
        const res = await fetch('/merge', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (!res.ok) {
            const err = await res.json().catch(() => ({ errors: ['Birleştirme başarısız.'] }));
            showToast((err.errors || ['Birleştirme başarısız.']).join('\n'), 'danger');
            setMerging(false);
            return;
        }

        const blob = await res.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `merged_${new Date().toISOString().slice(0,10)}.pdf`;
        a.click();
        URL.revokeObjectURL(url);

        showToast('PDF başarıyla birleştirildi ve indirildi!', 'success');
        uploadedFiles = [];
        customLogoPath = null;
        window._sortableInit = false;
        renderFileList();

    } catch (e) {
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setMerging(false);
    }
});

function setMerging(loading) {
    mergeBtn.disabled = loading;
    mergeBtnNormal.classList.toggle('d-none', loading);
    mergeBtnLoading.classList.toggle('d-none', !loading);
}

// ─── Toast ────────────────────────────────────────────────────────────────────
function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer');
    const icons = { success: 'check-circle-fill', danger: 'exclamation-triangle-fill', warning: 'exclamation-circle-fill', info: 'info-circle-fill' };
    const id = 'toast_' + Date.now();
    const html = `
        <div id="${id}" class="toast align-items-center text-bg-${type} border-0 shadow" role="alert">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi bi-${icons[type] || 'info-circle-fill'} me-2"></i>${escHtml(message)}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>`;
    container.insertAdjacentHTML('beforeend', html);
    const toastEl = document.getElementById(id);
    const toast = new bootstrap.Toast(toastEl, { delay: 4000 });
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

function escHtml(str) {
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
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
