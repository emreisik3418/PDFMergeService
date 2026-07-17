'use strict';

const driveFile        = document.getElementById('driveFile');
const driveWebPath      = document.getElementById('driveWebPath');
const drivePath         = document.getElementById('drivePath');
const driveFileName     = document.getElementById('driveFileName');
const driveExtraParams  = document.getElementById('driveExtraParams');
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
