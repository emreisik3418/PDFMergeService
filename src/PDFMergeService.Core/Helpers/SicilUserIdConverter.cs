using System.Globalization;

namespace PDFMergeService.Core.Helpers;

// AD girişindeki N'li sicili SharePoint servisinin beklediği sayısal kullanıcı id'sine çevirir:
// baştaki N atılır, 9'un yanına bir 0 eklenir → "N9026012" → "9026012" → 90026012.
// "DOMAIN\N9026012" ve "N9026012@domain" biçimlerinde domain kısmı yok sayılır.
public static class SicilUserIdConverter
{
    public static bool TryConvert(string? username, out int userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(username))
            return false;

        var sicil = username.Trim();

        var slash = sicil.LastIndexOf('\\');
        if (slash >= 0)
            sicil = sicil[(slash + 1)..];

        var at = sicil.IndexOf('@');
        if (at >= 0)
            sicil = sicil[..at];

        if (sicil.Length < 3 || (sicil[0] != 'N' && sicil[0] != 'n') || sicil[1] != '9')
            return false;

        var digits = sicil[1..];
        if (!digits.All(char.IsAsciiDigit))
            return false;

        return int.TryParse("90" + digits[1..], NumberStyles.None, CultureInfo.InvariantCulture, out userId);
    }
}
