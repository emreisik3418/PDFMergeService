'use strict';

// Toplu yüklemede dosya adından hedef klasörü türetir. Drive Aktarım sayfası ve
// Drive Yolları sayfasındaki önizleme aynı mantığı kullansın diye ayrı dosyada.
//
// "BAŞKENT KURUMSAL ŞUBESİ 2026 - 3.Çeyrek.pdf" + kural
//   { fileSuffix: "KURUMSAL ŞUBESİ", folderSuffix: "KURUMSAL ŞUBELER",
//     rootPath: "/Kurumsal Şubeler", periodFolderFormat: "{YIL} - {CEYREK}. Çeyrek" }
//   → "/Kurumsal Şubeler/BAŞKENT KURUMSAL ŞUBELER/2026 - 3. Çeyrek"
const DrivePathResolver = (() => {

    // Türkçe harfler (İ/i, I/ı, Ö/ö...) düz /i bayrağıyla katlanmadığı için her harf
    // tr-TR büyük/küçük haliyle karakter sınıfına açılır; boşluklar herhangi bir boşluk dizisine uyar.
    function suffixPattern(suffix) {
        return Array.from(suffix.trim()).map(ch => {
            if (/\s/.test(ch)) return '\\s+';
            const upper = ch.toLocaleUpperCase('tr-TR');
            const lower = ch.toLocaleLowerCase('tr-TR');
            if (upper === lower) return ch.replace(/[.*+?^${}()|[\]\\\/-]/g, '\\$&');
            return `[${upper}${lower}]`;
        }).join('').replace(/(\\s\+)+/g, '\\s+');
    }

    function compile(rules) {
        return (rules || [])
            .filter(r => r.fileSuffix && r.fileSuffix.trim() && r.rootPath)
            .map(rule => ({
                rule,
                regex: new RegExp(`([^\\-–—]+?)\\s*(${suffixPattern(rule.fileSuffix)})`, 'i')
            }));
    }

    // Eşleşme yoksa null döner. Birden fazla kural eşleşirse en uzun ek kazanır
    // (ör. "ÖZEL BANKACILIK BÖLGE MÜDÜRLÜĞÜ" içinde "ÖZEL" yerine "BÖLGE MÜDÜRLÜĞÜ").
    function resolve(fileName, compiledRules) {
        const base = fileName.replace(/\.pdf$/i, '');
        const yearMatch = base.match(/\b(20\d{2})\b/);
        const quarterMatch = base.match(/(\d)\s*\.?\s*[Çç]eyrek/);
        if (!yearMatch || !quarterMatch) return null;

        let best = null;
        for (const { rule, regex } of compiledRules) {
            const match = base.match(regex);
            if (match && (!best || match[2].length > best.match[2].length)) best = { rule, match };
        }
        if (!best) return null;

        const { rule, match } = best;
        const region = match[1].replace(/^[\d\s\-–—.]+/, '').trim();
        const folderSuffix = (rule.folderSuffix || '').trim() || match[2];
        const regionFolder = region ? `${region} ${folderSuffix}` : folderSuffix;
        const periodFolder = (rule.periodFolderFormat || '')
            .replaceAll('{YIL}', yearMatch[1])
            .replaceAll('{CEYREK}', quarterMatch[1]);

        return {
            rule,
            path: `${rule.rootPath.replace(/\/+$/, '')}/${regionFolder}/${periodFolder}`
        };
    }

    return { compile, resolve };
})();
