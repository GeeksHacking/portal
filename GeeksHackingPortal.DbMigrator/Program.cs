using System.Runtime.CompilerServices;
using GeeksHackingPortal.Api.Entities;
using GeeksHackingPortal.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using XenoAtom.CommandLine;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("db");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:db is required to run the database migrator.");
}

var sql = new SqlSugarScope(
    new ConnectionConfig
    {
        DbType = DbType.MySql,
        ConnectionString = connectionString,
        IsAutoCloseConnection = true,
        MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true },
    },
    _ => { }
);

var allowDestructive = false;
var seedDevelopmentTemplate = false;

var app = new CommandApp("db-migrator")
{
    new CommandUsage(),
    "",
    "Available commands:",
    new Command("diff")
    {
        "Inspect pending SqlSugar schema differences without applying changes.",
        new HelpOption(),
        (_, _) => ValueTask.FromResult(RunDiff(sql)),
    },
    new Command("apply")
    {
        "Apply pending SqlSugar schema changes after diff review.",
        "Options:",
        { "allow-destructive", "Allow column deletions when applying schema changes.", value => allowDestructive = value is not null },
        { "seed-development-template", "Seed the development template data after applying schema changes.", value => seedDevelopmentTemplate = value is not null },
        new HelpOption(),
        (_, _) => ValueTask.FromResult(RunApply(sql, allowDestructive, seedDevelopmentTemplate)),
    },
    new HelpOption(),
    (_, _) => ValueTask.FromResult(RunDiff(sql)),
};

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int RunDiff(ISqlSugarClient sql)
{
    var entityTypes = GetEntityTypes();
    var differenceProvider = sql.CodeFirst.GetDifferenceTables(entityTypes);
    var schemaDifferences = differenceProvider.ToDiffList().Where(table => table.IsDiff).ToArray();
    WriteDiffToConsole(differenceProvider.ToDiffString()?.Trim() ?? string.Empty, schemaDifferences);
    return schemaDifferences.Length > 0 ? 2 : 0;
}

static int RunApply(
    ISqlSugarClient sql,
    bool allowDestructive,
    bool seedDevelopmentTemplate
)
{
    var entityTypes = GetEntityTypes();
    var differenceProvider = sql.CodeFirst.GetDifferenceTables(entityTypes);
    var schemaDifferences = differenceProvider.ToDiffList().Where(table => table.IsDiff).ToArray();
    WriteDiffToConsole(differenceProvider.ToDiffString()?.Trim() ?? string.Empty, schemaDifferences);

    if (schemaDifferences.Length > 0)
    {
        if (schemaDifferences.Any(table => table.DeleteColums.Count > 0) && !allowDestructive)
        {
            throw new InvalidOperationException(
                "Destructive database changes detected. Review the diff and rerun with --allow-destructive after approval."
            );
        }

        sql.CodeFirst.InitTables(entityTypes);
    }

    if (seedDevelopmentTemplate)
    {
        SeedDevelopmentTemplate(sql);
    }

    return 0;
}

static Type[] GetEntityTypes() =>
    typeof(User).Assembly.GetTypes()
        .Where(type =>
            type is
            {
                IsClass: true,
                IsAbstract: false,
                IsNested: false,
                IsGenericTypeDefinition: false,
            }
            && !Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: false)
            && string.Equals(type.Namespace, "GeeksHackingPortal.Api.Entities", StringComparison.Ordinal)
        )
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

static void WriteDiffToConsole(string rawText, IReadOnlyList<TableDifferenceInfo> schemaDifferences)
{
    if (schemaDifferences.Count == 0)
    {
        Console.WriteLine("No pending database schema changes.");
        return;
    }

    Console.WriteLine(
        $"Pending database schema changes detected across {schemaDifferences.Count} table(s)."
    );
    Console.WriteLine();

    foreach (var table in schemaDifferences)
    {
        Console.WriteLine(
            $"{table.TableName}: +{table.AddColums.Count} ~{table.UpdateColums.Count} -{table.DeleteColums.Count} remarks:{table.UpdateRemark.Count}"
        );

        foreach (var message in GetDiffMessages(table))
        {
            Console.WriteLine($"  {message}");
        }

        Console.WriteLine();
    }

    if (!string.IsNullOrWhiteSpace(rawText))
    {
        Console.WriteLine(rawText);
    }
}

static IEnumerable<string> GetDiffMessages(TableDifferenceInfo table) =>
    table.AddColums
        .Concat(table.UpdateColums)
        .Concat(table.DeleteColums)
        .Concat(table.UpdateRemark)
        .Select(column => column.Message)
        .Where(message => !string.IsNullOrWhiteSpace(message))
        .Select(message => message!);

static void SeedDevelopmentTemplate(ISqlSugarClient sql)
{
    if (!sql.DbMaintenance.IsAnyTable(nameof(Hackathon), false))
    {
        Console.WriteLine("Skipping development seed because the database schema has not been migrated yet.");
        return;
    }

    if (sql.Queryable<Hackathon>().Any())
    {
        Console.WriteLine("Skipping development seed because hackathon data already exists.");
        return;
    }

    Console.WriteLine("Seeding development template data...");

    var hackathonId = new Guid("1e2beba8-0dd2-484f-b5b2-4b1b71a084e4");
    var eventStart = new DateTimeOffset(2026, 3, 13, 9, 0, 0, TimeSpan.Zero);
    var eventEnd = new DateTimeOffset(2026, 3, 15, 18, 0, 0, TimeSpan.Zero);

    var hackathon = new Hackathon
    {
        Id = hackathonId,
        Name = "HackOMania 2026",
        Description =
            "HackOMania 2026 brings builders together for a weekend of rapid prototyping, learning, and community.",
        Venue = "GeeksHacking Innovation Hub",
        HomepageUri = new Uri("https://hackomania.geekshacking.com/2026"),
        ShortCode = "HACKO26",
        IsPublished = true,
        EventStartDate = eventStart,
        EventEndDate = eventEnd,
        SubmissionsStartDate = new DateTimeOffset(2026, 3, 13, 10, 0, 0, TimeSpan.Zero),
        ChallengeSelectionEndDate = new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero),
        SubmissionsEndDate = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero),
        JudgingStartDate = new DateTimeOffset(2026, 3, 15, 13, 0, 0, TimeSpan.Zero),
        JudgingEndDate = new DateTimeOffset(2026, 3, 15, 18, 0, 0, TimeSpan.Zero),
    };
    sql.Insertable(hackathon).ExecuteCommand();

    var standardQuestions = RegistrationQuestionTemplateService.CreateStandardQuestions(hackathonId);
    var questions = standardQuestions.Select(item => item.Question).ToList();
    var options = standardQuestions.SelectMany(item => item.Options).ToList();

    if (questions.Count > 0)
    {
        sql.Insertable(questions).ExecuteCommand();
    }

    if (options.Count > 0)
    {
        sql.Insertable(options).ExecuteCommand();
    }

    var resources = new List<Resource>
    {
        new()
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            Name = "Mentor Office Hours",
            Description = "Redeem a 30-minute session with a domain mentor.",
            RedemptionStmt = "true",
            IsPublished = true,
        },
        new()
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            Name = "Cloud Credits Pack",
            Description = "Get starter credits for deployment and testing.",
            RedemptionStmt = "true",
            IsPublished = true,
        },
        new()
        {
            Id = Guid.NewGuid(),
            HackathonId = hackathonId,
            Name = "Prototype Kit",
            Description = "Pick up a hardware kit for rapid prototyping.",
            RedemptionStmt = "true",
            IsPublished = true,
        },
    };
    sql.Insertable(resources).ExecuteCommand();

    var testUserId = Guid.NewGuid();
    var testUser = new User
    {
        Id = testUserId,
        FirstName = "gunnicorn",
        LastName = "",
        Email = "anggunq@hotmail.com",
    };
    sql.Insertable(testUser).ExecuteCommand();

    var testGitHub = new GitHubOnlineAccount
    {
        Id = Guid.NewGuid(),
        UserId = testUserId,
        GitHubId = 47025159,
        GitHubLogin = "gunnicorn",
    };
    sql.Insertable(testGitHub).ExecuteCommand();

    var testOrganizer = new Organizer
    {
        Id = Guid.NewGuid(),
        HackathonId = hackathonId,
        UserId = testUserId,
        Type = OrganizerType.Admin,
    };
    sql.Insertable(testOrganizer).ExecuteCommand();
}
