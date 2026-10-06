using System.Globalization;

namespace PDFMergeService.Infrastructure.Services;

// Dosya/klasör adlarını içerdikleri sayıların değerine göre sıralar: "2 - X" < "10 - X" < "100 - X".
// Düz metin karşılaştırmasında "100" "11"den önce gelir; 100+ dosyalık setlerde sıra bu yüzden bozuluyordu.
// Sayı olmayan kısımlar tr-TR kültürüyle, büyük/küçük harf duyarsız karşılaştırılır.
public sealed class NaturalStringComparer : IComparer<string>
{
    public static readonly NaturalStringComparer Instance = new();

    private static readonly CompareInfo TurkishCompare = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsAsciiDigit(x[i]) && char.IsAsciiDigit(y[j]))
            {
                var startX = i; var startY = j;
                while (i < x.Length && char.IsAsciiDigit(x[i])) i++;
                while (j < y.Length && char.IsAsciiDigit(y[j])) j++;

                var result = CompareDigits(x.AsSpan(startX, i - startX), y.AsSpan(startY, j - startY));
                if (result != 0) return result;
            }
            else
            {
                var startX = i; var startY = j;
                while (i < x.Length && !char.IsAsciiDigit(x[i])) i++;
                while (j < y.Length && !char.IsAsciiDigit(y[j])) j++;

                var result = TurkishCompare.Compare(
                    x.AsSpan(startX, i - startX), y.AsSpan(startY, j - startY), CompareOptions.IgnoreCase);
                if (result != 0) return result;
            }
        }

        if (i < x.Length) return 1;
        if (j < y.Length) return -1;

        // Doğal sıralamada eşit görünenler ("01" / "1") için sabit bir sıra.
        return string.CompareOrdinal(x, y);
    }

    // Uzunluk sınırı olmadan sayısal karşılaştırma: baştaki sıfırlar atılır, uzun olan büyüktür, eşitse basamak basamak.
    private static int CompareDigits(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        a = a.TrimStart('0');
        b = b.TrimStart('0');
        if (a.Length != b.Length) return a.Length.CompareTo(b.Length);
        return a.SequenceCompareTo(b);
    }
}
