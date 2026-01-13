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
                    QuestionText = "Phone Number",
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
                    QuestionText = "Telegram Handle",
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
            QuestionText = "Gender",
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
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText = "What is your nationality?",
                    QuestionKey = "nationality",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = true,
                    Category = "Personal Information",
                },
                []
            )
        );

        // Employment Status
        var employmentStatusQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Are you currently working? Or are you currently a student?",
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
                OptionText = "Student",
                OptionValue = "student",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = employmentStatusQuestion.Id,
                OptionText = "Job Seeker",
                OptionValue = "job_seeker",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = employmentStatusQuestion.Id,
                OptionText = "Homemaker",
                OptionValue = "homemaker",
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
                    QuestionText = "What is/was your company name?",
                    QuestionKey = "company_name",
                    Type = QuestionType.Text,
                    DisplayOrder = order++,
                    IsRequired = false,
                    Category = "Professional Background",
                    ConditionalLogic = "{\"employment_status\": \"working\"}",
                    HelpText = "Optional for homemakers and job seekers",
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

        // Highest Education Level
        var educationLevelQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What is your highest level of education?",
            QuestionKey = "education_level",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Educational Background",
        };
        var educationOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = educationLevelQuestion.Id,
                OptionText = "High School",
                OptionValue = "high_school",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = educationLevelQuestion.Id,
                OptionText = "Associate Degree",
                OptionValue = "associate",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = educationLevelQuestion.Id,
                OptionText = "Bachelor's Degree",
                OptionValue = "bachelor",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = educationLevelQuestion.Id,
                OptionText = "Master's Degree",
                OptionValue = "master",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = educationLevelQuestion.Id,
                OptionText = "Doctorate (PhD)",
                OptionValue = "phd",
                DisplayOrder = 4,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = educationLevelQuestion.Id,
                OptionText = "Other",
                OptionValue = "other",
                DisplayOrder = 5,
            },
        };
        questions.Add((educationLevelQuestion, educationOptions));

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
        questions.Add(
            (
                new RegistrationQuestion
                {
                    Id = Guid.NewGuid(),
                    HackathonId = hackathonId,
                    QuestionText =
                        "What is your area of expertise / area of focus / area of interest?",
                    QuestionKey = "area_of_interest",
                    Type = QuestionType.LongText,
                    DisplayOrder = order++,
                    IsRequired = true,
                    Category = "Skills & Interests",
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
                    QuestionText = "Your Github Profile",
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

        // Desired Role
        var desiredRoleQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "What is your desired role in this hackathon?",
            QuestionKey = "desired_role",
            Type = QuestionType.MultipleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Hackathon Preferences",
        };
        var roleOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = desiredRoleQuestion.Id,
                OptionText = "Developer",
                OptionValue = "developer",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = desiredRoleQuestion.Id,
                OptionText = "Designer",
                OptionValue = "designer",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = desiredRoleQuestion.Id,
                OptionText = "Project Manager",
                OptionValue = "project_manager",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = desiredRoleQuestion.Id,
                OptionText = "Ideator",
                OptionValue = "ideator",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = desiredRoleQuestion.Id,
                OptionText = "Presenter",
                OptionValue = "presenter",
                DisplayOrder = 4,
            },
        };
        questions.Add((desiredRoleQuestion, roleOptions));

        // Dietary Restrictions
        var dietaryRestrictionsQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Do you have any dietary restrictions?",
            QuestionKey = "dietary_restrictions",
            Type = QuestionType.MultipleChoice,
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
                OptionText = "None",
                OptionValue = "none",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = dietaryRestrictionsQuestion.Id,
                OptionText = "Vegetarian",
                OptionValue = "vegetarian",
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
                OptionText = "Others",
                OptionValue = "others",
                DisplayOrder = 3,
                HasFollowUpText = true,
                FollowUpPlaceholder = "Please specify your dietary restrictions",
            },
        };
        questions.Add((dietaryRestrictionsQuestion, dietaryOptions));

        // Food Allergies
        var foodAllergiesQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Do you have any food allergies?",
            QuestionKey = "food_allergies",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Dietary & Preferences",
        };
        var allergyOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = foodAllergiesQuestion.Id,
                OptionText = "No",
                OptionValue = "no",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = foodAllergiesQuestion.Id,
                OptionText = "Yes",
                OptionValue = "yes",
                DisplayOrder = 1,
                HasFollowUpText = true,
                FollowUpPlaceholder = "Please specify your allergies",
            },
        };
        questions.Add((foodAllergiesQuestion, allergyOptions));

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
                OptionText = "2XS",
                OptionValue = "2xs",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "XS",
                OptionValue = "xs",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "S",
                OptionValue = "s",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "M",
                OptionValue = "m",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "L",
                OptionValue = "l",
                DisplayOrder = 4,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "XL",
                OptionValue = "xl",
                DisplayOrder = 5,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "2XL",
                OptionValue = "2xl",
                DisplayOrder = 6,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = tshirtSizeQuestion.Id,
                OptionText = "3XL",
                OptionValue = "3xl",
                DisplayOrder = 7,
            },
        };
        questions.Add((tshirtSizeQuestion, sizeOptions));

        // Has Team
        var hasTeamQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Do you already have a team?",
            QuestionKey = "has_team",
            Type = QuestionType.SingleChoice,
            DisplayOrder = order++,
            IsRequired = true,
            Category = "Team Information",
        };
        var hasTeamOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hasTeamQuestion.Id,
                OptionText = "Yes",
                OptionValue = "yes",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hasTeamQuestion.Id,
                OptionText = "No",
                OptionValue = "no",
                DisplayOrder = 1,
            },
        };
        questions.Add((hasTeamQuestion, hasTeamOptions));

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
                OptionText = "Yes, please help me find a team",
                OptionValue = "yes",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = needTeamHelpQuestion.Id,
                OptionText = "No, I'll find one myself",
                OptionValue = "no",
                DisplayOrder = 1,
            },
        };
        questions.Add((needTeamHelpQuestion, needHelpOptions));

        // How did you hear about the hackathon
        var hearAboutQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "How do you find out about the hackathon?",
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
                OptionText = "Social Media",
                OptionValue = "social_media",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Friend/Word of Mouth",
                OptionValue = "friend",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Email",
                OptionValue = "email",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "University/School",
                OptionValue = "university",
                DisplayOrder = 3,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Previous GeeksHacking Event",
                OptionValue = "previous_event",
                DisplayOrder = 4,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = hearAboutQuestion.Id,
                OptionText = "Others",
                OptionValue = "others",
                DisplayOrder = 5,
                HasFollowUpText = true,
                FollowUpPlaceholder = "Please specify",
            },
        };
        questions.Add((hearAboutQuestion, hearAboutOptions));

        // Looking for a job
        var lookingForJobQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Are you currently looking for a job?",
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
                OptionText = "Yes",
                OptionValue = "yes",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = lookingForJobQuestion.Id,
                OptionText = "No",
                OptionValue = "no",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = lookingForJobQuestion.Id,
                OptionText = "Open to opportunities",
                OptionValue = "open",
                DisplayOrder = 2,
            },
        };
        questions.Add((lookingForJobQuestion, jobOptions));

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
        questions.Add((mailingListQuestion, []));

        // Social Media Follow
        var socialMediaQuestion = new RegistrationQuestion
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            QuestionText = "Have you followed our social channels?",
            QuestionKey = "social_media_follow",
            Type = QuestionType.MultipleChoice,
            DisplayOrder = order++,
            IsRequired = false,
            Category = "Communication Preferences",
            HelpText = "Check the platforms you follow us on",
        };
        var socialOptions = new List<RegistrationQuestionOption>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = socialMediaQuestion.Id,
                OptionText = "Instagram (@geekshacking)",
                OptionValue = "instagram",
                DisplayOrder = 0,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = socialMediaQuestion.Id,
                OptionText = "LinkedIn (GeeksHacking)",
                OptionValue = "linkedin",
                DisplayOrder = 1,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = socialMediaQuestion.Id,
                OptionText = "Facebook (GeeksHacking)",
                OptionValue = "facebook",
                DisplayOrder = 2,
            },
            new()
            {
                Id = Guid.NewGuid(),
                QuestionId = socialMediaQuestion.Id,
                OptionText = "Not yet",
                OptionValue = "none",
                DisplayOrder = 3,
            },
        };
        questions.Add((socialMediaQuestion, socialOptions));

        return questions;
    }
}
