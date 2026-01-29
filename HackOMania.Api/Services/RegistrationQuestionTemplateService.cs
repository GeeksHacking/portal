using HackOMania.Api.Entities;
using static HackOMania.Api.Services.RegistrationValidationService;

namespace HackOMania.Api.Services;

/// <summary>
/// Service for creating and managing registration questions for hackathons
/// </summary>
public class RegistrationQuestionTemplateService
{
    /// <summary>
    /// Creates a standard set of registration questions for a hackathon based on the requirements
    /// </summary>
    public static List<(
        RegistrationQuestion Question,
        List<RegistrationQuestionOption> Options
    )> CreateStandardQuestions(Guid hackathonId)
    {
        var questions = new List<(RegistrationQuestion, List<RegistrationQuestionOption>)>();
        var order = 0;

        // Phone Number
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your phone number?",
                    QuestionKey = "phone_number",
                    Type = QuestionType.Phone,
                    DisplayOrder = order++,
                    IsRequired = true,
                    Category = "Contact Information",
                    ValidationRules = new ValidationRules
                    {
                        Pattern = @"^\+?[0-9\s\-()]{7,20}$",
                        PatternMessage = "Please enter a valid phone number",
                    }.ToJson(),
                },
                []
            )
        );

        // Telegram Handle
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your Telegram Handle?",
                    QuestionKey = "telegram_handle",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Contact Information",
                    HelpText = "Your Telegram username (e.g., @username)",
                    ValidationRules = new ValidationRules
                    {
                        Pattern = @"^@[a-zA-Z0-9_]{5,32}$",
                        PatternMessage =
                            "Telegram handle must start with @ and be 5-32 characters (letters, numbers, underscores)",
                    }.ToJson(),
                },
                []
            )
        );

        // GitHub Profile (verified separately)
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your GitHub Handle?",
                    QuestionKey = "github_profile",
                    Type = QuestionType.Url,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Online Profiles",
                    HelpText =
                        "This will be verified. You can also link your GitHub account in your profile settings.",
                },
                []
            )
        );

        // LinkedIn Profile/Personal Website
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "Your LinkedIn Profile/Personal website",
                    QuestionKey = "linkedin_website",
                    Type = QuestionType.Url,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Online Profiles",
                },
                []
            )
        );

        // Age
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your age?",
                    QuestionKey = "age",
                    Type = QuestionType.Number,
                    DisplayOrder = order++,
                    IsRequired = true,
                    Category = "Personal Information",
                    ValidationRules = ValidationRules.NumericRange(min: 13, max: 150).ToJson(),
                },
                []
            )
        );

        // Gender
        var genderQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What is your gender?",
            QuestionKey = "gender",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Personal Information",
        };
        var genderOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = genderQuestion.Id,
                OptionText = "Male",
                OptionValue = "male",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = genderQuestion.Id,
                OptionText = "Female",
                OptionValue = "female",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = genderQuestion.Id,
                OptionText = "Prefer not to say",
                OptionValue = "prefer_not_to_say",
                DisplayOrder = 2,
            },
        };
        questions.Add((genderQuestion, genderOptions));

        // Nationality
        var nationalityQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What is your nationality?",
            QuestionKey = "nationality",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Personal Information",
        };
        var nationalityOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = nationalityQuestion.Id,
                OptionText = "Singapore Citizen",
                OptionValue = "singapore_citizen",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = nationalityQuestion.Id,
                OptionText = "Singapore Permanent Resident",
                OptionValue = "singapore_pr",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = nationalityQuestion.Id,
                OptionText = "Others",
                OptionValue = "others",
                DisplayOrder = 0,
                HasFollowUpText = true,
                FollowUpPlaceholder = "Please specify:",
            }
        };
        questions.Add((nationalityQuestion, nationalityOptions));

        // Dietary Restrictions
        var dietaryRestrictionsQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Do you have any dietary restrictions/allergies?",
            QuestionKey = "dietary_restrictions",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Dietary & Preferences",
        };
        var dietaryOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = dietaryRestrictionsQuestion.Id,
                OptionText = "No Restrictions",
                OptionValue = "none",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = dietaryRestrictionsQuestion.Id,
                OptionText = "No Beef",
                OptionValue = "no_beef",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = dietaryRestrictionsQuestion.Id,
                OptionText = "Halal",
                OptionValue = "halal",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = dietaryRestrictionsQuestion.Id,
                OptionText = "Vegetarian",
                OptionValue = "vegetarian",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = dietaryRestrictionsQuestion.Id,
                OptionText = "Vegan",
                OptionValue = "vegan",
                DisplayOrder = 4,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = dietaryRestrictionsQuestion.Id,
                OptionText = "Others",
                OptionValue = "others",
                DisplayOrder = 5,
                HasFollowUpText = true,
                FollowUpPlaceholder = "Please specify:",
            },
        };

        // Need Team Help (conditional - if no team)
        var needTeamHelpQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Do you need help with finding a team?",
            QuestionKey = "need_team_help",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = false,
            Category = "Team Information",
            ConditionalLogic = "{\"has_team\": \"no\"}",
        };
        var needHelpOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = needTeamHelpQuestion.Id,
                OptionText = "Yes, please find a team for me if I am not in any team. My email will be shared to the other members.",
                OptionValue = "yes",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = needTeamHelpQuestion.Id,
                OptionText = "No, I already have a team",
                OptionValue = "no",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = needTeamHelpQuestion.Id,
                OptionText = "No, if I cannot find a team I will not participate in the hackathon",
                OptionValue = "nogoodbye",
                DisplayOrder = 2,
            }
        };
        questions.Add((needTeamHelpQuestion, needHelpOptions));

        var hackathonRoleQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Do you need help with finding a team?",
            QuestionKey = "need_team_help",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = false,
            Category = "Team Information",
            ConditionalLogic = "{\"need_team_help\": \"yes\"}",
        };
        var hackathonRoleOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = needTeamHelpQuestion.Id,
                OptionText = "Developing Software",
                OptionValue = "developingsoftware",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = needTeamHelpQuestion.Id,
                OptionText = "Designing",
                OptionValue = "designing",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = needTeamHelpQuestion.Id,
                OptionText = "Business",
                OptionValue = "business",
                DisplayOrder = 2,
            }
        }
        questions.Add((hackathonRoleQuestion, hackathonRoleOptions));

        // T-Shirt Size
        var tshirtSizeQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What is your T-shirt size?",
            QuestionKey = "tshirt_size",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Dietary & Preferences",
        };
        var sizeOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "XS",
                OptionValue = "xs",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "S",
                OptionValue = "s",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "M",
                OptionValue = "m",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "L",
                OptionValue = "l",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "XL",
                OptionValue = "xl",
                DisplayOrder = 4,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "2XL",
                OptionValue = "2xl",
                DisplayOrder = 5,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "3XL",
                OptionValue = "3xl",
                DisplayOrder = 6,
            },
        };
        questions.Add((tshirtSizeQuestion, sizeOptions));

        // Employment Status
        var employmentStatusQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What are you doing now?",
            QuestionKey = "employment_status",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Professional Background",
        };
        var employmentOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = employmentStatusQuestion.Id,
                OptionText = "Working",
                OptionValue = "working",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = employmentStatusQuestion.Id,
                OptionText = "Studying",
                OptionValue = "studying",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = employmentStatusQuestion.Id,
                OptionText = "Between Jobs",
                OptionValue = "between_jobs",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = employmentStatusQuestion.Id,
                OptionText = "Retired",
                OptionValue = "retired",
                DisplayOrder = 3,
            },
        };
        questions.Add((employmentStatusQuestion, employmentOptions));

        // Job Title (conditional - if working)
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What do/did you work as?",
                    QuestionKey = "job_title",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Professional Background",
                    ConditionalLogic = "{\"employment_status\": \"working\"}",
                },
                []
            )
        );

        // Company Name (conditional - if working, not required for homemaker/job seeker)
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your company name?",
                    QuestionKey = "company_name",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Professional Background",
                    ConditionalLogic = "{\"employment_status\": \"working\"}",
                },
                []
            )
        );

        // Job Title (conditional - if working, not required for homemaker/job seeker)
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your job title?",
                    QuestionKey = "job_title",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Professional Background",
                    ConditionalLogic = "{\"employment_status\": \"working\"}",
                },
                []
            )
        );

        // Years of Experience (conditional - if working)
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "How many years of experience do you have?",
                    QuestionKey = "years_of_experience",
                    Type = QuestionType.Number,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Professional Background",
                    ConditionalLogic = "{\"employment_status\": \"working\"}",
                    ValidationRules = ValidationRules.NumericRange(min: 0, max: 70).ToJson(),
                },
                []
            )
        );

        var wydQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What are you doing now?",
            QuestionKey = "wyd",
            Type = QuestionType.MultipleChoice,
            DisplayOrder = order++,
            IsRequired = false,
            Category = "Professional Background",
            ConditionalLogic = "{\"employment_status\": \"working\"}",
            ValidationRules = ValidationRules.NumericRange(min: 0, max: 70).ToJson(),
        };
        var wydOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = wydQuestion.Id,
                OptionText = "Working",
                OptionValue = "working",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = wydQuestion.Id,
                OptionText = "Studying",
                OptionValue = "studying",
                DisplayOrder = 1,
            },
        };
        questions.Add((wydQuestion, wydOptions));

        // Highest Education Level
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your highest level of education?",
                    QuestionKey = "education_level",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = true,
                    Category = "Educational Background",
                    ConditionalLogic = "{\"employment_status\": \"working\"}",
                },
                []
            )
        );

        // School Name (conditional - if student)
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your school name?",
                    QuestionKey = "school_name",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Educational Background",
                    ConditionalLogic = "{\"employment_status\": \"student\"}",
                },
                []
            )
        );

        // Major/Course Name (conditional - if student)
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your major / course name?",
                    QuestionKey = "major_course",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Educational Background",
                    ConditionalLogic = "{\"employment_status\": \"student\"}",
                },
                []
            )
        );

        // Area of Expertise/Interest
        var expertiseQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What is your area of expertise, focus or interest?"
            QuestionKey = "area_of_interest",
            Type = QuestionType.LongText,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Skills & Interests",
        };
        var expertiseOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = expertiseQuestion.Id,
                OptionText = "Web Development",
                OptionValue = "web_dev",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = expertiseQuestion.Id,
                OptionText = "Mobile Development",
                OptionValue = "mobile_dev",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = expertiseQuestion.Id,
                OptionText = "Software Engineering",
                OptionValue = "software_eng",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = expertiseQuestion.Id,
                OptionText = "AI & Machine Learning",
                OptionValue = "ai_ml",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = expertiseQuestion.Id,
                OptionText = "DevOps",
                OptionValue = "devops",
                DisplayOrder = 4,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = expertiseQuestion.Id,
                OptionText = "UI/UX",
                OptionValue = "uiux",
                DisplayOrder = 5,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = expertiseQuestion.Id,
                OptionText = "Others",
                OptionValue = "others",
                DisplayOrder = 6,
                HasFollowUpText = true,
                FollowUpPlaceholder = "Please specify:",
            }
        }

        // Looking for a job
        var lookingForJobQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Are you open to job opportunities?",
            QuestionKey = "looking_for_job",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Career & Opportunities",
        };
        var jobOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = lookingForJobQuestion.Id,
                OptionText = "Actively Looking",
                OptionValue = "actively_looking",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = lookingForJobQuestion.Id,
                OptionText = "Open to Discussion",
                OptionValue = "open",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = lookingForJobQuestion.Id,
                OptionText = "Not Interested",
                OptionValue = "no",
                DisplayOrder = 2,
            },
        };
        questions.Add((lookingForJobQuestion, jobOptions));

        // How did you hear about the hackathon
        var hearAboutQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "How did you find out about this event?",
            QuestionKey = "hear_about_source",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Marketing & Outreach",
        };
        var hearAboutOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Facebook",
                OptionValue = "facebook",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "LinkedIn",
                OptionValue = "linkedin",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Instagram",
                OptionValue = "instagram",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Google Search",
                OptionValue = "googlesearch",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "School/Organisation Email",
                OptionValue = "school_org",
                DisplayOrder = 4,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Meetup Group",
                OptionValue = "meetup_group",
                DisplayOrder = 5,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Friend/Colleague",
                OptionValue = "friend",
                DisplayOrder = 6,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Others",
                OptionValue = "others",
                DisplayOrder = 7,
                HasFollowUpText = true,
                FollowUpPlaceholder = "Please specify:",
            },
        };
        questions.Add((hearAboutQuestion, hearAboutOptions));

        

        // Mailing List Consent
        var mailingListQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText =
                "Would you be open to hear about future GeeksHacking's events and be included in our partners' mailing lists?",
            QuestionKey = "mailing_list_consent",
            Type = QuestionType.Boolean,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Communication Preferences",
        };
        var mailingListOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = mailingListQuestion.Id,
                OptionText = "Yes",
                OptionValue = "yes",
                DisplayOrder = 0
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = mailingListQuestion.Id,
                OptionText = "No",
                OptionValue = "no",
                DisplayOrder = 1,
            }
        };
        questions.Add((mailingListQuestion, mailingListOptions));

        // Disclaimer
        var disclaimer = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText =
                "Submitting this form does not guarantee you a spot at Hackomania 2026. Registrants will receive an email regarding confirmation or rejection of their registration. Please check your spam folder for this email.",
            QuestionKey = "mailing_list_consent",
            Type = QuestionType.Boolean,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Communication Preferences",
        };
        var disclaimerOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = mailingListQuestion.Id,
                OptionText = "Yes, I have read and understood the disclaimer",
                OptionValue = "yes",
                DisplayOrder = 0
            }
        };
        questions.Add((disclaimer, disclaimerOptions));

    }
}
