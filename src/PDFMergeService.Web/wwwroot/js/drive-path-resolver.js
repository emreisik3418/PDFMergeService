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
    const upperTr = text => text.toLocaleUpperCase('tr-TR');

    // ── Dönem: "2026 - 3. Çeyrek" ya da "2026 - Ağustos" ───────────────────────
    const MONTHS = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
    const LETTER = 'A-Za-zÇĞİÖŞÜçğıöşü';
    const PERIOD_REGEX = new RegExp(
        `(20\\d{2})\\s*[-–—]?\\s*(?:(\\d)\\s*\\.?\\s*${literalPattern('Çeyrek')}|(${MONTHS.map(literalPattern).join('|')})(?![${LETTER}]))`,
        'i');

    // { year, quarter, month, label, match }. label normalize edilmiş dönemdir: "2026 - 3. Çeyrek" / "2026 - Ağustos".
    // match: metinde dönemin geçtiği kısım (yalnızca "yıl + çeyrek/ay" bitişik yazıldıysa dolu).
    function parsePeriod(text) {
        const source = text || '';
        const m = source.match(PERIOD_REGEX);
        if (m) {
            if (m[2]) return { year: m[1], quarter: m[2], month: '', label: `${m[1]} - ${m[2]}. Çeyrek`, match: m[0] };
            const month = MONTHS.find(name => upperTr(name) === upperTr(m[3])) || m[3];
            return { year: m[1], quarter: '', month, label: `${m[1]} - ${month}`, match: m[0] };
        }
        // Eski dosya adları: yıl ve çeyrek ayrı yerlerde ("ANKARA 2026 RAPORU 1. Çeyrek")
        const year = source.match(/\b(20\d{2})\b/)?.[1] || '';
        const quarter = source.match(/(\d)\s*\.?\s*[Çç]eyrek/)?.[1] || '';
        return { year, quarter, month: '', label: year && quarter ? `${year} - ${quarter}. Çeyrek` : '', match: '' };
    }

    // {YIL}/{CEYREK}/{DONEM} içeren şablon, dosya adında karşılığı yoksa doldurulamaz (null).
    // {CEYREK} aylık dosyada boş olduğundan çeyreklik kurallar aylık dosyalara uygulanmaz.
    function fillPeriod(template, period) {
        if (/\{YIL\}/.test(template) && !period.year) return null;
        if (/\{CEYREK\}/.test(template) && !period.quarter) return null;
        if (/\{DONEM\}/.test(template) && !period.label) return null;
        return template.replaceAll('{YIL}', period.year).replaceAll('{CEYREK}', period.quarter).replaceAll('{DONEM}', period.label);
    }

    // {BOLGE}: toplu yüklemede klasör seçilince dosyanın bulunduğu alt klasör (ör. "Akdeniz" → "AKDENİZ").
    function fillRegion(template, region) {
        if (!template.includes('{BOLGE}')) return template;
        return region ? template.replaceAll('{BOLGE}', upperTr(region.trim())) : null;
    }

    const joinPath = (...parts) => '/' + parts
        .map(p => p.trim().replace(/^\/+|\/+$/g, ''))
        .filter(Boolean)
        .join('/');

    // Eşleşme yoksa null döner; varsa { path, label } (label: hangi eşleştirme/kural kullanıldı).
    // Birden fazla aday eşleşirse sabit metni en uzun olan kazanır
    // (ör. "ÖZEL BANKACILIK BÖLGE MÜDÜRLÜĞÜ" içinde "ÖZEL" yerine "BÖLGE MÜDÜRLÜĞÜ").
    // context.region: dosyanın seçilen klasördeki bölge klasörü ({BOLGE} için).
    function resolve(fileName, compiled, context) {
        const base = fileName.replace(/\.pdf$/i, '');
        const period = parsePeriod(base);
        const region = context?.region || '';

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
            const rootPath = fillRegion(candidate.rule.rootPath || '', region);
            const folderTemplate = fillRegion((candidate.rule.folderSuffix || '').trim(), region);
            if (periodFolder === null || rootPath === null || folderTemplate === null) continue;
            if (!best || candidate.specificity > best.specificity) best = { ...candidate, match, periodFolder, rootPath, folderTemplate };
        }
        if (!best) return null;

        const { rule, match, named, periodFolder, rootPath, folderTemplate } = best;
        const name = cleanName(match[1]);

        let regionFolder;
        if (folderTemplate.includes(NAME_TOKEN)) regionFolder = folderTemplate.replaceAll(NAME_TOKEN, name);
        else if (folderTemplate) regionFolder = name ? `${name} ${folderTemplate}` : folderTemplate;
        else if (named) regionFolder = cleanName(match[0]);
        else regionFolder = name ? `${name} ${match[2]}` : match[2];

        return {
            path: joinPath(rootPath, regionFolder, periodFolder),
            label: `Kural: ${rule.fileSuffix}`
        };
    }

    // ── Klasör Birleştirme için dosya adı önerisi ──────────────────────────────
    // Kuralları tersinden kullanır: yoldaki rapor tipine uyan kuralın kalıbıyla, toplu yüklemenin
    // tanıyacağı adı üretir. Böylece birleştirilen dosya sonra Drive'a yüklenirken aynı kuralla doğru
    // klasöre gider.
    //   "Kurumsal Şubeler 2026 - 3. Çeyrek" › "Başkent Kurumsal"
    //     → "BAŞKENT KURUMSAL ŞUBESİ 2026 - 3. Çeyrek"                       (kural: "KURUMSAL ŞUBESİ")
    //   "Bireysel ve Karma Şubeler" › "Akdeniz" › "Akdeniz Bulvarı Antalya" › "2026 - Ağustos"
    //     → "AKDENİZ BULVARI ANTALYA ŞUBESİ 2026 - Ağustos", ZIP klasörü "Akdeniz"  (kural: "{AD} ŞUBESİ")

    const words = text => upperTr(text).split(/\s+/).filter(Boolean);
    // Tekil/çoğul farkını tolere etmek için kelimenin ilk 4 harfi: ŞUBESİ / ŞUBELER → ŞUBE, MÜDÜRLÜĞÜ / MÜDÜRLÜKLERİ → MÜDÜ
    const stem = word => word.slice(0, 4);
    const splitPath = path => (path || '').trim().split(/[\\/]+/).filter(Boolean);
    const lastSegment = path => splitPath(path).pop() || '';
    const trimSeparators = text => text.replace(/^[\s\-–—_]+|[\s\-–—_]+$/g, '').replace(/\s+/g, ' ').trim();

    // Klasör adından dönemi ayırır: "Kurumsal Şubeler 2026 - 3. Çeyrek" → { period: "2026 - 3. Çeyrek", rest: "Kurumsal Şubeler" }
    function splitSegment(segment) {
        const p = parsePeriod(segment);
        return p.match
            ? { period: p.label, rest: trimSeparators(segment.replace(p.match, ' ')) }
            : { period: '', rest: trimSeparators(segment) };
    }

    // Rapor tipine uyan kural: önce Ana Klasör'ün son parçası ya da Klasör Adı birebir, sonra kalıptaki
    // sabit kelimelerin tamamının (kök olarak) rapor tipinde geçmesi.
    function findRuleForType(type, rules) {
        if (!type) return null;
        const typeKey = words(type).join(' ');
        const typeStems = new Set(words(type).map(stem));
        let best = null;

        for (const rule of rules || []) {
            const pattern = (rule.fileSuffix || '').trim();
            const literalWords = words(pattern.replace(NAME_TOKEN, ' '));
            if (literalWords.length === 0) continue;

            const exact = [lastSegment((rule.rootPath || '').replaceAll('{BOLGE}', '')), (rule.folderSuffix || '').replace(NAME_TOKEN, ' ')]
                .some(candidate => candidate.trim() && words(candidate).join(' ') === typeKey);
            const stemMatch = literalWords.every(w => typeStems.has(stem(w)));
            if (!exact && !stemMatch) continue;

            const score = (exact ? 1000 : 0) + specificity(pattern);
            if (!best || score > best.score) best = { rule, score };
        }
        return best?.rule || null;
    }

    // Alt klasör adında kalıbın sabit kelimeleri zaten varsa tekrar etmesin:
    // "Başkent Kurumsal" + "{AD} KURUMSAL ŞUBESİ" → ad "BAŞKENT"; "Özel B.Bağdat" + "ÖZEL {AD} ŞUBESİ" → ad "B.BAĞDAT".
    function trimOverlap(nameWords, beforeWords, afterWords) {
        let result = [...nameWords];
        for (let n = Math.min(beforeWords.length, result.length); n > 0; n--) {
            if (beforeWords.slice(-n).every((w, i) => stem(w) === stem(result[i]))) { result = result.slice(n); break; }
        }
        for (let n = Math.min(afterWords.length, result.length); n > 0; n--) {
            if (afterWords.slice(0, n).every((w, i) => stem(w) === stem(result[result.length - n + i]))) { result = result.slice(0, -n); break; }
        }
        return result;
    }

    // relativePath: taranan ana klasöre göre birleştirilecek klasörün yolu ("Akdeniz/Akdeniz Bulvarı Antalya/2026 - Ağustos").
    // Dönüş: { fileName, directory, period } — directory ZIP içindeki klasördür ("Akdeniz"; tek seviyede boş).
    function suggestMergedFileName(relativePath, rootPath, rules) {
        const rootSegments = splitPath(rootPath);
        const segments = [...rootSegments, ...splitPath(relativePath)].map(splitSegment);
        const relStart = rootSegments.length;

        // Dönem: yoldaki en derin dönem bilgisi (Bireysel: dönem klasörü; Kurumsal: ana klasör adı)
        const period = [...segments].reverse().find(s => s.period)?.period || '';

        // Ad: taranan kökün altındaki, dönemden arındırılınca boş kalmayan en derin klasör
        let nameIdx = -1;
        for (let i = segments.length - 1; i >= relStart; i--) {
            if (segments[i].rest) { nameIdx = i; break; }
        }
        const withPeriod = name => (period ? `${name} ${period}` : name).trim();
        if (nameIdx < 0) return { fileName: withPeriod(lastSegment(relativePath)), directory: '', period };

        // Rapor tipi: ad klasörünün üstündeki, bir kurala uyan en yakın klasör ("Bireysel ve Karma Şubeler")
        let rule = null, typeIdx = -1;
        for (let i = nameIdx - 1; i >= 0 && !rule; i--) {
            rule = findRuleForType(segments[i].rest, rules);
            if (rule) typeIdx = i;
        }

        // ZIP klasörü: rapor tipi ile ad arasındaki klasörler (bölge); kural yoksa taranan kökün altındakiler
        const directory = segments
            .slice(rule ? typeIdx + 1 : relStart, nameIdx)
            .map(s => s.rest)
            .filter(Boolean)
            .join('/');

        const sub = segments[nameIdx].rest;
        if (!rule) return { fileName: withPeriod(sub), directory, period };

        const pattern = rule.fileSuffix.trim();
        const hasName = pattern.includes(NAME_TOKEN);
        const at = hasName ? pattern.indexOf(NAME_TOKEN) : 0;
        const before = hasName ? pattern.slice(0, at) : '';
        const after = hasName ? pattern.slice(at + NAME_TOKEN.length) : pattern;

        const name = trimOverlap(words(sub), words(before), words(after));
        if (name.length === 0) return { fileName: withPeriod(sub), directory, period };

        return { fileName: withPeriod([...words(before), ...name, ...words(after)].join(' ')), directory, period };
    }

    return { compile, resolve, parsePeriod, suggestMergedFileName };
})();
