'use strict';

const maxFileCount   = document.getElementById('maxFileCount');
const maxFileSizeMB  = document.getElementById('maxFileSizeMB');
const saveBtn        = document.getElementById('pdfSettingsSaveBtn');
const saveBtnNormal  = document.getElementById('pdfSettingsSaveBtnNormal');
const saveBtnLoading = document.getElementById('pdfSettingsSaveBtnLoading');

saveBtn.addEventListener('click', async () => {
    const payload = {
        maxFileCount: parseInt(maxFileCount.value, 10),
        maxFileSizeMB: parseInt(maxFileSizeMB.value, 10)
    };

    if (!Number.isInteger(payload.maxFileCount) || !Number.isInteger(payload.maxFileSizeMB)) {
        showToast('Lütfen geçerli sayılar girin.', 'warning');
        return;
    }

    setSaving(true);

    try {
        const res = await fetch('/pdf-settings', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json().catch(() => ({}));

        if (!res.ok) {
            showToast(data.error || 'Kaydetme başarısız.', 'danger');
            return;
        }

        showToast(data.message || 'Kaydedildi.', 'success');
    } catch {
        showToast('Sunucu bağlantısı hatası.', 'danger');
    } finally {
        setSaving(false);
    }
});

function setSaving(saving) {
    saveBtn.disabled = saving;
    saveBtnNormal.classList.toggle('d-none', saving);
    saveBtnLoading.classList.toggle('d-none', !saving);
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
