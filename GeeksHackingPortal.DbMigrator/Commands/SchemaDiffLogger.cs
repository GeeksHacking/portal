using System.Runtime.CompilerServices;
using GeeksHackingPortal.Api.Entities;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace GeeksHackingPortal.DbMigrator.Commands;

internal static class SchemaDiffLogger
{
    public static Type[] GetEntityTypes() =>
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

    public static void Write(
        ILogger logger,
        string rawText,
        IReadOnlyList<TableDifferenceInfo> schemaDifferences
    )
    {
        if (schemaDifferences.Count == 0)
        {
            logger.LogInformation("No pending database schema changes.");
            return;
        }

        logger.LogInformation(
            "Pending database schema changes detected across {TableCount} table(s).",
            schemaDifferences.Count
        );

        foreach (var table in schemaDifferences)
        {
            logger.LogInformation(
                "{TableName}: +{AddedColumns} ~{UpdatedColumns} -{DeletedColumns} remarks:{UpdatedRemarks}",
                table.TableName,
                table.AddColums.Count,
                table.UpdateColums.Count,
                table.DeleteColums.Count,
                table.UpdateRemark.Count
            );

            foreach (var message in GetDiffMessages(table))
            {
                logger.LogInformation("  {Message}", message);
            }
        }

        if (!string.IsNullOrWhiteSpace(rawText))
        {
            logger.LogInformation("{SchemaDiff}", rawText);
        }
    }

    private static IEnumerable<string> GetDiffMessages(TableDifferenceInfo table) =>
        table.AddColums
            .Concat(table.UpdateColums)
            .Concat(table.DeleteColums)
            .Concat(table.UpdateRemark)
            .Select(column => column.Message)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!);
}
