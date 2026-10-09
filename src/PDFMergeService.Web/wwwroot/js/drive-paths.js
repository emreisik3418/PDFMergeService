'use strict';

const config = window.drivePathConfig || {};

// Her liste satırı { alan: değer } nesnesi; fields, satırda hangi input'ların hangi sırayla
// çizileceğini (değer: placeholder), defaults yeni satırın ön değerlerini belirler.
const lists = {
    webPathOptions: {
        items: (config.webPathOptions || []).map(o => ({ label: o.label, value: o.value })),
        body: document.getElementById('webPathOptionsBody'),
        fields: { label: 'Performans', value: 'performans' }
    },
    bulkUploadRules: {
        items: (config.bulkUploadRules || []).map(r => ({
            fileSuffix: r.fileSuffix,
            folderSuffix: r.folderSuffix,
            rootPath: r.rootPath,
            periodFolderFormat: r.periodFolderFormat
        })),
        body: document.getElementById('bulkUploadRulesBody'),
        fields: {
            fileSuffix: 'ÖZEL {AD} ŞUBESİ',
            folderSuffix: '(boş: dosyadaki ifade)',
            rootPath: '/Özel Şubeler',
            periodFolderFormat: '{YIL} - {CEYREK}. Çeyrek'
        },
        defaults: { periodFolderFormat: '{YIL} - {CEYREK}. Çeyrek' }
    },
    bulkPathOverrides: {
        items: (config.bulkPathOverrides || []).map(o => ({ contains: o.contains, targetPath: o.targetPath })),
        body: document.getElementById('bulkPathOverridesBody'),
        fields: {
            contains: 'YÖNETİM KURULU',
            targetPath: '/Genel Müdürlük/Yönetim Kurulu/{YIL}'
        }
    }
};

const bulkWebPath    = document.getElementById('bulkWebPath');
const saveBtn        = document.getElementById('drivePathsSaveBtn');
const saveBtnNormal  = document.getElementById('drivePathsSaveBtnNormal');
const saveBtnLoading = document.getElementById('drivePathsSaveBtnLoading');

bulkWebPath.value = config.bulkWebPath || '';

document.querySelectorAll('[data-add-row]').forEach(btn => {
    btn.addEventListener('click', () => {
        const list = lists[btn.dataset.addRow];
        list.items.push(Object.fromEntries(Object.keys(list.fields).map(key => [key, list.defaults?.[key] || ''])));
        renderList(list);
        updatePreview();
        list.body.querySelector('tr:last-child input')?.focus();
    });
});

function renderList(list) {
    list.body.innerHTML = '';

    if (list.items.length === 0) {
        const tr = document.createElement('tr');
        tr.innerHTML = `<td colspan="${Object.keys(list.fields).length + 1}" class="text-muted text-center py-3">Henüz kayıt yok.</td>`;
        list.body.appendChild(tr);
        return;
    }

    list.items.forEach((item, idx) => {
        const tr = document.createElement('tr');
        Object.entries(list.fields).forEach(([key, placeholder]) => tr.appendChild(inputCell(item, key, placeholder)));

        const removeTd = document.createElement('td');
        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-sm btn-outline-danger';
        removeBtn.title = 'Sil';
        removeBtn.innerHTML = '<i class="bi bi-trash"></i>';
        removeBtn.addEventListener('click', () => {
            list.items.splice(idx, 1);
            renderList(list);
            updatePreview();
        });
        removeTd.appendChild(removeBtn);
        tr.appendChild(removeTd);

        list.body.appendChild(tr);
    });
}

function inputCell(item, key, placeholder) {
    const td = document.createElement('td');
    const input = document.createElement('input');
    input.type = 'text';
    input.className = 'form-control form-control-sm';
    input.placeholder = placeholder;
    input.value = item[key] || '';
    input.addEventListener('input', () => { item[key] = input.value; updatePreview(); });
    td.appendChild(input);
    return td;
}

saveBtn.addEventListener('click', async () => {
    const payload = {
        webPathOptions: lists.webPathOptions.items,
        bulkWebPath: bulkWebPath.value.trim(),
        bulkUploadRules: lists.bulkUploadRules.items,
        bulkPathOverrides: lists.bulkPathOverrides.items
    };

    setSaving(true);

    try {
        const res = await fetch('/drive-paths', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const data = await res.json().catch(() => ({}));

        if (!res.ok) {
            showToast(data.error || 'Kaydetme başarısız.', 'danger');
            return;
        }

        // Sunucu boş satırları atladığı için listeleri temizlenmiş haliyle yeniden çiz.
        Object.values(lists).forEach(list => {
            list.items = list.items
                .map(o => Object.fromEntries(Object.entries(o).map(([key, val]) => [key, (val || '').trim()])))
                .filter(o => Object.values(o).some(Boolean));
            renderList(list);
        });

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

// ---- Kural önizleme ----

const rulePreviewInput  = document.getElementById('rulePreviewInput');
const rulePreviewRegion = document.getElementById('rulePreviewRegion');
const rulePreviewResult = document.getElementById('rulePreviewResult');

function updatePreview() {
    const fileName = rulePreviewInput.value.trim();
    if (!fileName) { rulePreviewResult.innerHTML = ''; return; }

    const compiled = DrivePathResolver.compile(lists.bulkUploadRules.items, lists.bulkPathOverrides.items);
    const result = DrivePathResolver.resolve(fileName, compiled, { region: rulePreviewRegion.value.trim() });
    rulePreviewResult.innerHTML = result
        ? `<i class="bi bi-arrow-return-right me-1 text-success"></i><code>${escHtml(result.path)}</code>
           <span class="text-muted">(${escHtml(result.label)})</span>`
        : '<i class="bi bi-x-circle me-1 text-danger"></i><span class="text-danger">Eşleşen kural/eşleştirme yok, dosya adında dönem bulunamadı ya da {BOLGE} içeren kural için bölge girilmedi.</span>';
}

rulePreviewInput.addEventListener('input', updatePreview);
rulePreviewRegion.addEventListener('input', updatePreview);

Object.values(lists).forEach(renderList);
updatePreview();
