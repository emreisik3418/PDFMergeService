namespace PDFMergeService.Core.Models;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();

    public static ValidationResult Success() => new();

    public static ValidationResult Fail(params string[] errors)
    {
        var result = new ValidationResult();
        result.Errors.AddRange(errors);
        return result;
    }
}
