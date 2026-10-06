'use strict';

// Toplu yüklemede dosya adından hedef klasörü türetir. Drive Aktarım sayfası ve
// Drive Yolları sayfasındaki önizleme aynı mantığı kullansın diye ayrı dosyada.
//
// 1) Özel eşleştirmeler önce denenir: dosya adı "contains" metnini içeriyorsa hedef yol
//    doğrudan "targetPath" olur ({YIL}/{CEYREK} yerine dosya adındaki değerler yazılır).
// 2) Sonra kurallar: "fileSuffix" bir kalıptır; {AD} şube/bölge adını yakalar.
//    {AD} yazılmamışsa ad ekin önündedir ("KURUMSAL ŞUBESİ" = "{AD} KURUMSAL ŞUBESİ").
//
//   "BAŞKENT KURUMSAL ŞUBESİ 2026 - 3. Çeyrek.pdf" + { fileSuffix: "KURUMSAL ŞUBESİ", folderSuffix: "KURUMSAL ŞUBELER",
//       rootPath: "/Kurumsal Şubeler", periodFolderFormat: "{YIL} - {CEYREK}. Çeyrek" }
//     → "/Kurumsal Şubeler/BAŞKENT KURUMSAL ŞUBELER/2026 - 3. Çeyrek"
//   "ÖZEL BAŞKENT ŞUBESİ 2026 - 3. Çeyrek.pdf" + { fileSuffix: "ÖZEL {AD} ŞUBESİ", folderSuffix: "", rootPath: "/Özel Şubeler", ... }
//     → "/Özel Şubeler/ÖZEL BAŞKENT ŞUBESİ/2026 - 3. Çeyrek"
const DrivePathResolver = (() => {

    const NAME_TOKEN = '{AD}';
    // Şube/bölge adı: tire içermez (dosya adlarında "01 - ANKARA ..." gibi ön ekler tireyle ayrılır).
    const NAME_LAZY = '([^\\-–—]+?)';
    // {AD} kalıbın sonundaysa ad, yıl/çeyrek rakamlarına ya da tireye kadar uzanır.
    const NAME_TAIL = '([^\\-–—\\d]+)';

    // Türkçe harfler (İ/i, I/ı, Ö/ö...) düz /i bayrağıyla katlanmadığı için her harf
    // tr-TR büyük/küçük haliyle karakter sınıfına açılır; boşluklar herhangi bir boşluk dizisine uyar.
    function literalPattern(text) {
        return Array.from(text.trim()).map(ch => {
            if (/\s/.test(ch)) return '\\s+';
            const upper = ch.toLocaleUpperCase('tr-TR');
            const lower = ch.toLocaleLowerCase('tr-TR');
            if (upper === lower) return ch.replace(/[.*+?^${}()|[\]\\\/-]/g, '\\$&');
            return `[${upper}${lower}]`;
        }).join('').replace(/(\\s\+)+/g, '\\s+');
    }

    // Eşleşmeler arasında seçim için: kalıptaki sabit metnin uzunluğu (boşluk ve {AD} hariç).
    const specificity = text => text.replace(NAME_TOKEN, '').replace(/\s+/g, '').length;

    function compileRule(rule) {
        const pattern = rule.fileSuffix.trim();

        if (pattern.includes(NAME_TOKEN)) {
            const at = pattern.indexOf(NAME_TOKEN);
            const before = pattern.slice(0, at).trim();
            const after = pattern.slice(at + NAME_TOKEN.length).trim();
            const source = (before ? literalPattern(before) + '\\s*' : '')
                + (after ? NAME_LAZY + '\\s*' + literalPattern(after) : NAME_TAIL);
            return { rule, regex: new RegExp(source, 'i'), named: true, specificity: specificity(pattern) };
        }

        return {
            rule,
            regex: new RegExp(`${NAME_LAZY}\\s*(${literalPattern(pattern)})`, 'i'),
            named: false,
            specificity: specificity(pattern)
        };
    }

    function compile(rules, overrides) {
        return {
            rules: (rules || [])
                .filter(r => r.fileSuffix && r.fileSuffix.replace(NAME_TOKEN, '').trim() && r.rootPath)
                .map(compileRule),
            overrides: (overrides || [])
                .filter(o => o.contains && o.contains.trim() && o.targetPath && o.targetPath.trim())
                .map(o => ({ override: o, regex: new RegExp(literalPattern(o.contains), 'i'), specificity: specificity(o.contains) }))
        };
    }

    const cleanName = text => text.replace(/^[\d\s\-–—.]+/, '').replace(/\s+/g, ' ').trim();

    // {YIL}/{CEYREK} içeren bir şablon, dosya adında yıl/çeyrek yoksa doldurulamaz (null).
    function fillPeriod(template, period) {
        if (/\{YIL\}/.test(template) && !period.year) return null;
        if (/\{CEYREK\}/.test(template) && !period.quarter) return null;
        return template.replaceAll('{YIL}', period.year).replaceAll('{CEYREK}', period.quarter);
    }

    const joinPath = (...parts) => '/' + parts
        .map(p => p.trim().replace(/^\/+|\/+$/g, ''))
        .filter(Boolean)
        .join('/');

    // Eşleşme yoksa null döner; varsa { path, label } (label: hangi eşleştirme/kural kullanıldı).
    // Birden fazla aday eşleşirse sabit metni en uzun olan kazanır
    // (ör. "ÖZEL BANKACILIK BÖLGE MÜDÜRLÜĞÜ" içinde "ÖZEL" yerine "BÖLGE MÜDÜRLÜĞÜ").
    function resolve(fileName, compiled) {
        const base = fileName.replace(/\.pdf$/i, '');
        const period = {
            year: base.match(/\b(20\d{2})\b/)?.[1] || '',
            quarter: base.match(/(\d)\s*\.?\s*[Çç]eyrek/)?.[1] || ''
        };

        let bestOverride = null;
        for (const candidate of compiled.overrides || []) {
            if (!candidate.regex.test(base)) continue;
            const path = fillPeriod(candidate.override.targetPath, period);
            if (path !== null && (!bestOverride || candidate.specificity > bestOverride.specificity)) {
                bestOverride = { ...candidate, path };
            }
        }
        if (bestOverride) {
            return { path: joinPath(bestOverride.path), label: `Özel eşleştirme: ${bestOverride.override.contains}` };
        }

        let best = null;
        for (const candidate of compiled.rules || []) {
            const match = base.match(candidate.regex);
            if (!match) continue;
            const periodFolder = fillPeriod(candidate.rule.periodFolderFormat || '', period);
            if (periodFolder === null) continue;
            if (!best || candidate.specificity > best.specificity) best = { ...candidate, match, periodFolder };
        }
        if (!best) return null;

        const { rule, match, named, periodFolder } = best;
        const name = cleanName(match[1]);
        const folderTemplate = (rule.folderSuffix || '').trim();

        let regionFolder;
        if (folderTemplate.includes(NAME_TOKEN)) regionFolder = folderTemplate.replaceAll(NAME_TOKEN, name);
        else if (folderTemplate) regionFolder = name ? `${name} ${folderTemplate}` : folderTemplate;
        else if (named) regionFolder = cleanName(match[0]);
        else regionFolder = name ? `${name} ${match[2]}` : match[2];

        return {
            path: joinPath(rule.rootPath, regionFolder, periodFolder),
            label: `Kural: ${rule.fileSuffix}`
        };
    }

    return { compile, resolve };
})();
