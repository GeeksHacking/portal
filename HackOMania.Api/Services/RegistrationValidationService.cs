using System.Text.Json;
using System.Text.RegularExpressions;
using HackOMania.Api.Entities;

namespace HackOMania.Api.Services;

/// <summary>
/// Service for validating registration question responses against validation rules
/// </summary>
public class RegistrationValidationService
{
    /// <summary>
    /// Represents the validation rules that can be applied to a registration question
    /// </summary>
    public class ValidationRules
    {
        /// <summary>
        /// Minimum value for numeric inputs
        /// </summary>
        public double? Min { get; set; }

        /// <summary>
        /// Maximum value for numeric inputs
        /// </summary>
        public double? Max { get; set; }

        /// <summary>
        /// Minimum length for text inputs
        /// </summary>
        public int? MinLength { get; set; }

        /// <summary>
        /// Maximum length for text inputs
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Regex pattern for text inputs
        /// </summary>
        public string? Pattern { get; set; }

        /// <summary>
        /// Custom error message for pattern validation
        /// </summary>
        public string? PatternMessage { get; set; }

        /// <summary>
        /// Minimum number of selections for multiple choice
        /// </summary>
        public int? MinSelections { get; set; }

        /// <summary>
        /// Maximum number of selections for multiple choice
        /// </summary>
        public int? MaxSelections { get; set; }

        /// <summary>
        /// Minimum date for date inputs (ISO 8601 format)
        /// </summary>
        public string? MinDate { get; set; }

        /// <summary>
        /// Maximum date for date inputs (ISO 8601 format)
        /// </summary>
        public string? MaxDate { get; set; }

        /// <summary>
        /// Serializes the validation rules to a JSON string for storage
        /// </summary>
        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        /// <summary>
        /// Creates a ValidationRules instance for numeric range validation
        /// </summary>
        public static ValidationRules NumericRange(double? min = null, double? max = null) =>
            new() { Min = min, Max = max };

        /// <summary>
        /// Creates a ValidationRules instance for text length validation
        /// </summary>
        public static ValidationRules TextLength(int? minLength = null, int? maxLength = null) =>
            new() { MinLength = minLength, MaxLength = maxLength };

        /// <summary>
        /// Creates a ValidationRules instance for pattern validation
        /// </summary>
        public static ValidationRules TextPattern(string pattern, string? patternMessage = null) =>
            new() { Pattern = pattern, PatternMessage = patternMessage };

        /// <summary>
        /// Creates a ValidationRules instance for multiple choice selection limits
        /// </summary>
        public static ValidationRules SelectionLimits(
            int? minSelections = null,
            int? maxSelections = null
        ) => new() { MinSelections = minSelections, MaxSelections = maxSelections };

        /// <summary>
        /// Creates a ValidationRules instance for date range validation
        /// </summary>
        public static ValidationRules DateRange(string? minDate = null, string? maxDate = null) =>
            new() { MinDate = minDate, MaxDate = maxDate };
    }

    /// <summary>
    /// Result of validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = [];
        public Dictionary<Guid, List<string>> ErrorsByQuestionId { get; set; } = [];

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Failure(string error) =>
            new() { IsValid = false, Errors = [error] };

        public static ValidationResult Failure(IEnumerable<string> errors) =>
            new() { IsValid = false, Errors = [.. errors] };

        public static ValidationResult Failure(Guid questionId, IEnumerable<string> errors)
        {
            var errorList = errors.ToList();
            return new ValidationResult
            {
                IsValid = false,
                Errors = errorList,
                ErrorsByQuestionId = new Dictionary<Guid, List<string>>
                {
                    [questionId] = errorList,
                },
            };
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Validates a submission value against the question's validation rules
    /// </summary>
    /// <param name="question">The registration question with validation rules</param>
    /// <param name="value">The submitted value to validate</param>
    /// <returns>Validation result with any errors</returns>
    public static ValidationResult Validate(RegistrationQuestion question, string value)
    {
        // If no validation rules, the value is valid
        if (string.IsNullOrWhiteSpace(question.ValidationRules))
        {
            return ValidationResult.Success();
        }

        ValidationRules? rules;
        try
        {
            rules = JsonSerializer.Deserialize<ValidationRules>(
                question.ValidationRules,
                JsonOptions
            );
        }
        catch (JsonException)
        {
            // Invalid JSON in validation rules - skip validation
            return ValidationResult.Success();
        }

        if (rules is null)
        {
            return ValidationResult.Success();
        }

        var errors = new List<string>();

        // Apply validation based on question type
        switch (question.Type)
        {
            case QuestionType.Number:
                ValidateNumber(value, rules, question.QuestionText, errors);
                break;

            case QuestionType.Text:
            case QuestionType.LongText:
            case QuestionType.Phone:
                ValidateText(value, rules, question.QuestionText, errors);
                break;

            case QuestionType.Email:
                ValidateEmail(value, rules, question.QuestionText, errors);
                break;

            case QuestionType.Url:
                ValidateUrl(value, rules, question.QuestionText, errors);
                break;

            case QuestionType.Date:
                ValidateDate(value, rules, question.QuestionText, errors);
                break;

            case QuestionType.MultipleChoice:
                ValidateMultipleChoice(value, rules, question.QuestionText, errors);
                break;

            case QuestionType.SingleChoice:
            case QuestionType.Dropdown:
            case QuestionType.Boolean:
                // These types primarily validate against options, not validation rules
                // But we can still apply text length rules if provided
                ValidateText(value, rules, question.QuestionText, errors);
                break;
        }

        return errors.Count > 0
            ? ValidationResult.Failure(question.Id, errors)
            : ValidationResult.Success();
    }

    private static void ValidateNumber(
        string value,
        ValidationRules rules,
        string questionText,
        List<string> errors
    )
    {
        if (!double.TryParse(value, out var numericValue))
        {
            errors.Add($"'{questionText}' must be a valid number.");
            return;
        }

        if (rules.Min.HasValue && numericValue < rules.Min.Value)
        {
            errors.Add($"'{questionText}' must be at least {rules.Min.Value}.");
        }

        if (rules.Max.HasValue && numericValue > rules.Max.Value)
        {
            errors.Add($"'{questionText}' must be at most {rules.Max.Value}.");
        }
    }

    private static void ValidateText(
        string value,
        ValidationRules rules,
        string questionText,
        List<string> errors
    )
    {
        if (rules.MinLength.HasValue && value.Length < rules.MinLength.Value)
        {
            errors.Add(
                $"'{questionText}' must be at least {rules.MinLength.Value} characters long."
            );
        }

        if (rules.MaxLength.HasValue && value.Length > rules.MaxLength.Value)
        {
            errors.Add(
                $"'{questionText}' must be at most {rules.MaxLength.Value} characters long."
            );
        }

        if (!string.IsNullOrEmpty(rules.Pattern))
        {
            try
            {
                var regex = new Regex(rules.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                if (!regex.IsMatch(value))
                {
                    var message = !string.IsNullOrEmpty(rules.PatternMessage)
                        ? rules.PatternMessage
                        : $"'{questionText}' does not match the required format.";
                    errors.Add(message);
                }
            }
            catch (RegexParseException)
            {
                // Invalid regex pattern in validation rules - skip this validation
            }
            catch (RegexMatchTimeoutException)
            {
                // Regex took too long - skip this validation for safety
                errors.Add($"'{questionText}' took too long for regex match.");
            }
        }
    }

    private static void ValidateEmail(
        string value,
        ValidationRules rules,
        string questionText,
        List<string> errors
    )
    {
        // Basic email validation
        if (!IsValidEmail(value))
        {
            errors.Add($"'{questionText}' must be a valid email address.");
            return;
        }

        // Apply text validation rules (minLength, maxLength, pattern)
        ValidateText(value, rules, questionText, errors);
    }

    private static void ValidateUrl(
        string value,
        ValidationRules rules,
        string questionText,
        List<string> errors
    )
    {
        // Basic URL validation
        if (
            !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        )
        {
            errors.Add($"'{questionText}' must be a valid URL.");
            return;
        }

        // Apply text validation rules (minLength, maxLength, pattern)
        ValidateText(value, rules, questionText, errors);
    }

    private static void ValidateDate(
        string value,
        ValidationRules rules,
        string questionText,
        List<string> errors
    )
    {
        if (!DateTimeOffset.TryParse(value, out var dateValue))
        {
            errors.Add($"'{questionText}' must be a valid date.");
            return;
        }

        if (
            !string.IsNullOrEmpty(rules.MinDate)
            && DateTimeOffset.TryParse(rules.MinDate, out var minDate)
            && dateValue < minDate
        )
        {
            errors.Add($"'{questionText}' must be on or after {minDate:yyyy-MM-dd}.");
        }

        if (
            !string.IsNullOrEmpty(rules.MaxDate)
            && DateTimeOffset.TryParse(rules.MaxDate, out var maxDate)
            && dateValue > maxDate
        )
        {
            errors.Add($"'{questionText}' must be on or before {maxDate:yyyy-MM-dd}.");
        }
    }

    private static void ValidateMultipleChoice(
        string value,
        ValidationRules rules,
        string questionText,
        List<string> errors
    )
    {
        // Multiple choice values are stored as JSON arrays
        List<string>? selections;
        try
        {
            selections = JsonSerializer.Deserialize<List<string>>(value);
        }
        catch (JsonException)
        {
            // If not valid JSON array, treat as single selection
            selections = [value];
        }

        var count = selections?.Count ?? 0;

        if (rules.MinSelections.HasValue && count < rules.MinSelections.Value)
        {
            errors.Add(
                $"'{questionText}' requires at least {rules.MinSelections.Value} selection(s)."
            );
        }

        if (rules.MaxSelections.HasValue && count > rules.MaxSelections.Value)
        {
            errors.Add(
                $"'{questionText}' allows at most {rules.MaxSelections.Value} selection(s)."
            );
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Use MailAddress for basic email validation
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
