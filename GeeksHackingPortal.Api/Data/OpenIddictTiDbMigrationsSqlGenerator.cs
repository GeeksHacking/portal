using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Migrations;

namespace GeeksHackingPortal.Api.Data;

public class OpenIddictTiDbMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    ICommandBatchPreparer commandBatchPreparer,
    IMySqlOptions options
) : MySqlMigrationsSqlGenerator(dependencies, commandBatchPreparer, options)
{
    protected override void Generate(
        CreateIndexOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder,
        bool terminate = true
    )
    {
        if (
            operation.Table == "OpenIddictTokens"
            && operation.Name == OpenIddictMySqlIndexConfiguration.TokenAppStatusSubjectTypeIndexName
            && operation["MySql:IndexPrefixLength"] is null
        )
        {
            operation["MySql:IndexPrefixLength"] =
                OpenIddictMySqlIndexConfiguration.TokenAppStatusSubjectTypePrefixLengths;
        }

        base.Generate(operation, model, builder, terminate);
    }
}
