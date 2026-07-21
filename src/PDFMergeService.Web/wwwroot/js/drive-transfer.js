'use strict';

const driveFile        = document.getElementById('driveFile');
const driveWebPath      = document.getElementById('driveWebPath');
const drivePath         = document.getElementById('drivePath');
const driveFileName     = document.getElementById('driveFileName');
const driveExtraParams  = document.getElementById('driveExtraParams');
const driveIsMergedVersion = document.getElementById('driveIsMergedVersion');
const driveUploadBtn    = document.getElementById('driveUploadBtn');
const driveUploadBtnNormal  = document.getElementById('driveUploadBtnNormal');
const driveUploadBtnLoading = document.getElementById('driveUploadBtnLoading');

driveUploadBtn.addEventListener('click', async () => {
    const file = driveFile.files[0];
    if (!file) { showToast('Lütfen bir PDF dosyası seçin.', 'warning'); return; }
    if (!driveWebPath.value.trim() || !drivePath.value.trim()) {
        showToast('Site alt yolu ve hedef klasör alanları zorunludur.', 'warning');
        return;
    }

    const formData = new FormData();
    formData.append('File', file);
    formData.append('WebPath', driveWebPath.value.trim());
    formData.append('Path', drivePath.value.trim());
    if (driveFileName.value.trim()) formData.append('FileName', driveFileName.value.trim());
    if (driveExtraParams.value.trim()) formData.append('ExtraParams', driveExtraParams.value.trim());
    formData.append('IsMergedVersion', driveIsMergedVersion.checked ? 'true' : 'false');

    setUploading(true);

    try {
        const res = await fetch('/drive-transfer/upload', { method: 'POST', body: formData });
        const data = await res.json().catch(() => ({}));

        if (!res.ok) {
            showToast(data.error || 'Aktarım başarısız.', 'danger');
            return;
        }

        showToast(data.message || 'Dosya SharePoint\'e aktarıldı.', 'success');
    } catch {
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setUploading(false);
    }
});

function setUploading(loading) {
    driveUploadBtn.disabled = loading;
    driveUploadBtnNormal.classList.toggle('d-none', loading);
    driveUploadBtnLoading.classList.toggle('d-none', !loading);
}

// ---- Sekme geçişi (Tekli / Toplu) ----

const driveSinglePanel = document.getElementById('driveSinglePanel');
const driveBulkPanel   = document.getElementById('driveBulkPanel');

document.querySelectorAll('#driveModeTabs [data-drive-tab]').forEach(btn => {
    btn.addEventListener('click', () => {
        document.querySelectorAll('#driveModeTabs [data-drive-tab]').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        const isBulk = btn.dataset.driveTab === 'bulk';
        driveSinglePanel.classList.toggle('d-none', isBulk);
        driveBulkPanel.classList.toggle('d-none', !isBulk);
    });
});

// ---- Toplu Yükleme ----

const driveBulkFiles          = document.getElementById('driveBulkFiles');
const driveBulkRootPath       = document.getElementById('driveBulkRootPath');
const driveBulkSelectAllMerged = document.getElementById('driveBulkSelectAllMerged');
const driveBulkTableBody      = document.getElementById('driveBulkTableBody');
const driveBulkEmptyRow       = document.getElementById('driveBulkEmptyRow');
const driveBulkUploadBtn      = document.getElementById('driveBulkUploadBtn');
const driveBulkUploadBtnNormal  = document.getElementById('driveBulkUploadBtnNormal');
const driveBulkUploadBtnLoading = document.getElementById('driveBulkUploadBtnLoading');

let bulkItems = [];

driveBulkRootPath.addEventListener('change', () => {
    const rootPath = driveBulkRootPath.value;
    bulkItems.forEach(item => { item.path = resolveDrivePath(item.fileName, rootPath); });
    renderBulkTable();
});

function resolveDrivePath(fileName, rootPath) {
    const base = fileName.replace(/\.pdf$/i, '');
    const regionMatch = base.match(/([^\-–—]+?)\s*B[Öö]LGE\s+M[Üü]D[Üü]RL[Üü][Ğğ][Üü]/i);
    const yearMatch = base.match(/\b(20\d{2})\b/);
    const quarterMatch = base.match(/(\d)\s*\.?\s*[Çç]eyrek/);

    if (!regionMatch || !yearMatch || !quarterMatch) return '';

    const region = regionMatch[0].replace(/^[\d\s\-–—.]+/, '').trim();
    return `${rootPath}/${region}/${yearMatch[1]} - ${quarterMatch[1]}. Çeyrek`;
}

driveBulkFiles.addEventListener('change', () => {
    const files = Array.from(driveBulkFiles.files || []);
    const rootPath = driveBulkRootPath.value;
    files.forEach(file => {
        if (!/\.pdf$/i.test(file.name)) {
            showToast(`"${file.name}" bir PDF dosyası değil, atlandı.`, 'warning');
            return;
        }
        const path = resolveDrivePath(file.name, rootPath);
        bulkItems.push({
            file,
            fileName: file.name,
            path,
            isMerged: !!path, // dosya adı bölge/yıl/çeyrek desenine uyuyorsa muhtemelen birleştirilmiş rapordur
            status: null,
            message: ''
        });
    });
    driveBulkFiles.value = '';
    renderBulkTable();
});

driveBulkSelectAllMerged.addEventListener('change', () => {
    bulkItems.forEach(item => { item.isMerged = driveBulkSelectAllMerged.checked; });
    renderBulkTable();
});

function updateSelectAllMergedState() {
    if (bulkItems.length === 0) {
        driveBulkSelectAllMerged.checked = false;
        driveBulkSelectAllMerged.indeterminate = false;
        return;
    }
    const mergedCount = bulkItems.filter(i => i.isMerged).length;
    driveBulkSelectAllMerged.checked = mergedCount === bulkItems.length;
    driveBulkSelectAllMerged.indeterminate = mergedCount > 0 && mergedCount < bulkItems.length;
}

function renderBulkTable() {
    driveBulkTableBody.innerHTML = '';

    if (bulkItems.length === 0) {
        driveBulkTableBody.appendChild(driveBulkEmptyRow);
        updateSelectAllMergedState();
        return;
    }

    bulkItems.forEach((item, idx) => {
        const tr = document.createElement('tr');

        const nameTd = document.createElement('td');
        nameTd.textContent = item.fileName;
        tr.appendChild(nameTd);

        const pathTd = document.createElement('td');
        const pathInput = document.createElement('input');
        pathInput.type = 'text';
        pathInput.className = 'form-control form-control-sm' + (item.path ? '' : ' is-invalid');
        pathInput.placeholder = 'Hedef klasör bulunamadı, elle girin';
        pathInput.value = item.path;
        pathInput.addEventListener('input', () => {
            bulkItems[idx].path = pathInput.value;
            pathInput.classList.toggle('is-invalid', !pathInput.value.trim());
        });
        pathTd.appendChild(pathInput);
        tr.appendChild(pathTd);

        const mergedTd = document.createElement('td');
        mergedTd.className = 'text-center';
        const mergedCheck = document.createElement('input');
        mergedCheck.type = 'checkbox';
        mergedCheck.className = 'form-check-input';
        mergedCheck.checked = item.isMerged;
        mergedCheck.addEventListener('change', () => {
            bulkItems[idx].isMerged = mergedCheck.checked;
            updateSelectAllMergedState();
        });
        mergedTd.appendChild(mergedCheck);
        tr.appendChild(mergedTd);

        const statusTd = document.createElement('td');
        statusTd.innerHTML = renderBulkStatus(item);
        tr.appendChild(statusTd);

        const removeTd = document.createElement('td');
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-sm btn-outline-danger';
        removeBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
        removeBtn.addEventListener('click', () => {
            bulkItems.splice(idx, 1);
            renderBulkTable();
        });
        removeTd.appendChild(removeBtn);
        tr.appendChild(removeTd);

        driveBulkTableBody.appendChild(tr);
    });

    updateSelectAllMergedState();
}

function renderBulkStatus(item) {
    if (item.status === 'success') return `<span class="text-success"><i class="bi bi-check-circle-fill me-1"></i>${escHtml(item.message)}</span>`;
    if (item.status === 'error') return `<span class="text-danger"><i class="bi bi-x-circle-fill me-1"></i>${escHtml(item.message)}</span>`;
    return '<span class="text-muted">-</span>';
}

driveBulkUploadBtn.addEventListener('click', async () => {
    if (bulkItems.length === 0) { showToast('Lütfen en az bir PDF dosyası seçin.', 'warning'); return; }

    const formData = new FormData();
    bulkItems.forEach((item, idx) => {
        formData.append(`Items[${idx}].File`, item.file);
        formData.append(`Items[${idx}].Path`, item.path);
        formData.append(`Items[${idx}].IsMergedVersion`, item.isMerged ? 'true' : 'false');
    });

    setBulkUploading(true);

    try {
        const res = await fetch('/drive-transfer/upload-bulk', { method: 'POST', body: formData });
        const data = await res.json().catch(() => ({}));

        if (!res.ok) {
            showToast(data.error || 'Toplu aktarım başarısız.', 'danger');
            return;
        }

        const results = data.results || [];
        results.forEach((r, idx) => {
            if (bulkItems[idx]) {
                bulkItems[idx].status = r.success ? 'success' : 'error';
                bulkItems[idx].message = r.message || (r.success ? 'Aktarıldı.' : 'Başarısız.');
            }
        });
        renderBulkTable();

        const successCount = results.filter(r => r.success).length;
        showToast(`${successCount}/${results.length} dosya başarıyla aktarıldı.`, successCount === results.length ? 'success' : 'warning');
    } catch {
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setBulkUploading(false);
    }
});

function setBulkUploading(loading) {
    driveBulkUploadBtn.disabled = loading;
    driveBulkUploadBtnNormal.classList.toggle('d-none', loading);
    driveBulkUploadBtnLoading.classList.toggle('d-none', !loading);
}

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
