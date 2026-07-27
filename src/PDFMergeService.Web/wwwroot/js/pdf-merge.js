'use strict';

const { PDFDocument, StandardFonts, rgb } = PDFLib;

// ─── Config (mirrors PdfSettings in appsettings.json) ─────────────────────────
const MAX_FILE_SIZE_MB = 50;
const MAX_FILE_COUNT = 20;

// ─── State ───────────────────────────────────────────────────────────────────
let uploadedFiles = [];   // { fileName, bytes: Uint8Array, pageCount, fileSize, fileSizeFormatted, order }
let customLogoBytes = null;
let customLogoExt = null; // 'png' | 'jpg'
let pendingDownload = null; // { url, fileName }

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
    if (files.length > 0) addFiles(files);
    else showToast('Sadece PDF dosyası kabul edilir.', 'warning');
});

browseBtn.addEventListener('click', () => fileInput.click());
fileInput.addEventListener('change', () => {
    if (fileInput.files.length > 0) addFiles([...fileInput.files]);
    fileInput.value = '';
});

// ─── Local File Loading (tamamen tarayıcıda, sunucuya gönderilmez) ────────────
async function addFiles(files) {
    const errors = [];
    const accepted = [];

    if (uploadedFiles.length + files.length > MAX_FILE_COUNT) {
        showToast(`En fazla ${MAX_FILE_COUNT} dosya yükleyebilirsiniz.`, 'warning');
        return;
    }

    for (const file of files) {
        const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
        if (ext !== '.pdf') {
            errors.push(`'${file.name}' geçersiz dosya türü. Sadece PDF kabul edilir.`);
            continue;
        }
        if (file.size === 0) {
            errors.push(`'${file.name}' boş dosya.`);
            continue;
        }
        if (file.size > MAX_FILE_SIZE_MB * 1024 * 1024) {
            errors.push(`'${file.name}' dosyası ${MAX_FILE_SIZE_MB} MB limitini aşıyor.`);
            continue;
        }
        accepted.push(file);
    }

    if (errors.length > 0) showToast(errors.join('\n'), 'danger');
    if (accepted.length === 0) return;

    const newFiles = [];
    for (const file of accepted) {
        try {
            const bytes = new Uint8Array(await file.arrayBuffer());
            const doc = await PDFDocument.load(bytes, { ignoreEncryption: true });
            newFiles.push({
                fileName: file.name,
                bytes,
                pageCount: doc.getPageCount(),
                fileSize: file.size,
                fileSizeFormatted: formatFileSize(file.size),
                order: 0
            });
        } catch (e) {
            showToast(`'${file.name}' okunamadı, geçerli bir PDF olmayabilir.`, 'danger');
        }
    }

    if (newFiles.length === 0) return;

    newFiles.forEach(f => uploadedFiles.push(f));
    uploadedFiles.sort((a, b) => a.fileName.localeCompare(b.fileName, 'tr', { sensitivity: 'base' }));
    uploadedFiles.forEach((f, i) => f.order = i);

    renderFileList();
    showToast(`${newFiles.length} dosya eklendi.`, 'success');
}

function formatFileSize(bytes) {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
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
    if (pendingDownload) {
        URL.revokeObjectURL(pendingDownload.url);
        pendingDownload = null;
    }
    showReopenBtn(false);
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

// ─── Custom Logo (tarayıcıda tutulur, sunucuya gönderilmez) ───────────────────
customLogoFile.addEventListener('change', async () => {
    const file = customLogoFile.files[0];
    if (!file) return;

    const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
    if (!['.png', '.jpg', '.jpeg'].includes(ext)) {
        showToast('Sadece PNG veya JPEG logo kabul edilir.', 'warning');
        customLogoFile.value = '';
        return;
    }

    try {
        customLogoBytes = new Uint8Array(await file.arrayBuffer());
        customLogoExt = ext === '.png' ? 'png' : 'jpg';
        showToast('Özel logo yüklendi.', 'success');
    } catch {
        showToast('Logo okunamadı.', 'danger');
    }
});

// ─── Footer Uygulama (PdfFooterService.cs mantığının pdf-lib karşılığı) ───────
async function applyFooter(pdfDoc, settings) {
    const helvetica = await pdfDoc.embedFont(StandardFonts.Helvetica);

    let logoImage = null;
    if (settings.logoEnabled) {
        try {
            let bytes, ext;
            if (customLogoBytes) {
                bytes = customLogoBytes;
                ext = customLogoExt;
            } else {
                const res = await fetch('/assets/logo/company-logo.png');
                bytes = new Uint8Array(await res.arrayBuffer());
                ext = 'png';
            }
            logoImage = ext === 'png' ? await pdfDoc.embedPng(bytes) : await pdfDoc.embedJpg(bytes);
        } catch (e) {
            showToast('Logo yüklenemedi, footer logosuz oluşturulacak.', 'warning');
        }
    }

    const pages = pdfDoc.getPages();
    pages.forEach((page, pageIndex) => {
        const pageNumber = pageIndex + 1;
        const { width: pageW } = page.getSize();

        if (settings.pageNumberEnabled && pageNumber >= settings.startFromPage) {
            const displayNumber = pageNumber - settings.startFromPage + 1;
            drawPageNumber(page, helvetica, settings, displayNumber, pageW);
        }

        if (settings.logoEnabled && logoImage && !settings.logoSkipPages.includes(pageNumber)) {
            drawLogo(page, logoImage, settings, pageW);
        }
    });
}

function drawPageNumber(page, font, settings, pageNumber, pageW) {
    const text = String(pageNumber);
    const fontSize = settings.fontSize;
    const textWidth = font.widthOfTextAtSize(text, fontSize);
    const color = parseColor(settings.fontColor);

    const x = calcX(pageW, textWidth, settings.pageNumberPosition, settings.marginHorizontal);
    const y = settings.marginBottom;

    page.drawText(text, { x, y, size: fontSize, font, color });
}

function drawLogo(page, logoImage, settings, pageW) {
    let logoW = settings.logoWidth;
    let logoH = settings.logoHeight;

    const ratio = logoImage.width / logoImage.height;
    if (logoW / logoH > ratio) logoW = logoH * ratio;
    else logoH = logoW / ratio;

    const x = calcX(pageW, logoW, settings.logoPosition, settings.marginHorizontal);
    const y = settings.marginBottom;

    page.drawImage(logoImage, { x, y, width: logoW, height: logoH });
}

// position: 0 = BottomLeft, 1 = BottomCenter, 2 = BottomRight (bkz. FooterPosition enum)
function calcX(pageW, elementW, position, marginH) {
    switch (position) {
        case 1: return (pageW - elementW) / 2;
        case 2: return pageW - elementW - marginH;
        default: return marginH;
    }
}

function parseColor(hex) {
    hex = (hex || '').trim().replace('#', '');
    if (hex.length !== 6) return rgb(0, 0, 0);
    const r = parseInt(hex.slice(0, 2), 16) / 255;
    const g = parseInt(hex.slice(2, 4), 16) / 255;
    const b = parseInt(hex.slice(4, 6), 16) / 255;
    return rgb(r, g, b);
}

// ─── Merge (tamamen tarayıcıda, sunucuya hiçbir dosya gönderilmez) ────────────
mergeBtn.addEventListener('click', async () => {
    if (uploadedFiles.length < 2) {
        showToast('En az 2 PDF dosyası gereklidir.', 'warning');
        return;
    }

    setMerging(true);

    try {
        const orderedFiles = [...uploadedFiles].sort((a, b) => a.order - b.order);

        const outDoc = await PDFDocument.create();
        for (const file of orderedFiles) {
            const srcDoc = await PDFDocument.load(file.bytes, { ignoreEncryption: true });
            const pages = await outDoc.copyPages(srcDoc, srcDoc.getPageIndices());
            pages.forEach(p => outDoc.addPage(p));
        }

        const footer = {
            pageNumberEnabled: pageNumberEnabled.checked,
            startFromPage: parseInt(document.getElementById('startFromPage').value),
            pageNumberPosition: parseInt(document.getElementById('pageNumberPosition').value),
            fontSize: parseInt(document.getElementById('fontSize').value),
            fontColor: document.getElementById('fontColor').value,
            logoEnabled: logoEnabled.checked,
            logoPosition: parseInt(document.getElementById('logoPosition').value),
            logoWidth: parseFloat(document.getElementById('logoWidth').value),
            logoHeight: parseFloat(document.getElementById('logoHeight').value),
            marginBottom: parseFloat(document.getElementById('marginBottom').value),
            marginHorizontal: parseFloat(document.getElementById('marginHorizontal').value),
            logoSkipPages: parsePageRanges(document.getElementById('logoSkipPages').value)
        };

        await applyFooter(outDoc, footer);

        const mergedBytes = await outDoc.save();
        const blob = new Blob([mergedBytes], { type: 'application/pdf' });
        const url = URL.createObjectURL(blob);
        const customName = document.getElementById('outputFileName').value.trim();
        const fileName = customName ? `${customName}.pdf` : `merged_${new Date().toISOString().slice(0, 10)}.pdf`;

        if (pendingDownload) URL.revokeObjectURL(pendingDownload.url);
        pendingDownload = { url, fileName };

        document.getElementById('pdfPreviewFrame').src = url;
        new bootstrap.Modal(document.getElementById('previewModal')).show();

        showToast('PDF birleştirildi. Önizlemeyi inceleyip indirebilirsiniz.', 'success');
        showReopenBtn(true);

        fetch('/pdf-merge/log', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ fileCount: orderedFiles.length, pageCount: outDoc.getPageCount() })
        }).catch(() => {});

    } catch (e) {
        console.error(e);
        showToast('Birleştirme başarısız. Dosyalardan biri bozuk veya şifreli olabilir.', 'danger');
    } finally {
        setMerging(false);
    }
});

function setMerging(loading) {
    mergeBtn.disabled = loading;
    mergeBtnNormal.classList.toggle('d-none', loading);
    mergeBtnLoading.classList.toggle('d-none', !loading);
}

// ─── Preview Modal ────────────────────────────────────────────────────────────
document.getElementById('downloadFromPreviewBtn').addEventListener('click', () => {
    if (!pendingDownload) return;
    const a = document.createElement('a');
    a.href = pendingDownload.url;
    a.download = pendingDownload.fileName;
    a.click();
});

document.getElementById('previewModal').addEventListener('hidden.bs.modal', () => {
    document.getElementById('pdfPreviewFrame').src = '';
});

function showReopenBtn(show) {
    document.getElementById('reopenPreviewBtn').classList.toggle('d-none', !show);
}

document.getElementById('reopenPreviewBtn').addEventListener('click', () => {
    if (!pendingDownload) return;
    document.getElementById('pdfPreviewFrame').src = pendingDownload.url;
    new bootstrap.Modal(document.getElementById('previewModal')).show();
});

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
