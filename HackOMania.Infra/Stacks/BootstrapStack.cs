using Pulumi;
using Pulumi.Gcp.Iam;
using Pulumi.Gcp.Iam.Inputs;
using Pulumi.Gcp.ServiceAccount;
using Pulumi.Gcp.Storage;
using Pulumi.Gcp.Storage.Inputs;
using IAMMember = Pulumi.Gcp.Projects.IAMMember;
using IAMMemberArgs = Pulumi.Gcp.Projects.IAMMemberArgs;

namespace HackOMania.Infra.Stacks;

public class BootstrapStack : Stack
{
    public BootstrapStack()
    {
        var config = new Config("gcp");
        var projectId = config.Require("project");

        var stateBucket = new Bucket(
            "hackomania-pulumi-state",
            new BucketArgs
            {
                Name = "hackomania-pulumi-state",
                Location = "ASIA-SOUTHEAST1",
                ForceDestroy = false,
                Versioning = new BucketVersioningArgs { Enabled = true },
                UniformBucketLevelAccess = true,
            }
        );

        var deploymentSa = new Account(
            "pulumi-deployer",
            new AccountArgs
            {
                AccountId = "pulumi-deployer",
                DisplayName = "Pulumi Deployment Service Account",
                Description =
                    "Service account used by Pulumi for infrastructure deployments via GitHub Actions",
            }
        );

        var deploymentRoles = new[] { "roles/editor" };

        for (var i = 0; i < deploymentRoles.Length; i++)
        {
            var role = deploymentRoles[i];
            _ = new IAMMember(
                $"deployer-role-{i}",
                new IAMMemberArgs
                {
                    Project = projectId,
                    Role = role,
                    Member = deploymentSa.Email.Apply(email => $"serviceAccount:{email}"),
                }
            );
        }

        var workloadIdentityPool = new WorkloadIdentityPool(
            "github",
            new WorkloadIdentityPoolArgs
            {
                WorkloadIdentityPoolId = "github",
                DisplayName = "GitHub Actions",
                Description = "Workload Identity Pool for GitHub Actions OIDC",
            }
        );

        var workloadIdentityProvider = new WorkloadIdentityPoolProvider(
            "github",
            new WorkloadIdentityPoolProviderArgs
            {
                WorkloadIdentityPoolId = workloadIdentityPool.WorkloadIdentityPoolId,
                WorkloadIdentityPoolProviderId = "github",
                DisplayName = "GitHub",
                Description = "GitHub Actions OIDC Provider",
                Disabled = false,
                AttributeCondition = """
                attribute.repository == "GeeksHacking/hackomania-event-platform"
                """,
                AttributeMapping = new InputMap<string>
                {
                    { "google.subject", "assertion.sub" },
                    { "attribute.actor", "assertion.actor" },
                    { "attribute.aud", "assertion.aud" },
                    { "attribute.repository", "assertion.repository" },
                    { "attribute.ref", "assertion.ref" },
                    { "attribute.ref_type", "assertion.ref_type" },
                },
                Oidc = new WorkloadIdentityPoolProviderOidcArgs
                {
                    IssuerUri = "https://token.actions.githubusercontent.com",
                },
            }
        );

        _ = new IAMBinding(
            "workload-identity-binding",
            new IAMBindingArgs
            {
                ServiceAccountId = deploymentSa.Name,
                Role = "roles/iam.workloadIdentityUser",
                Members = new[]
                {
                    workloadIdentityPool.Name.Apply(poolName =>
                        $"principalSet://iam.googleapis.com/{poolName}/attribute.repository/GeeksHacking/hackomania-event-platform"
                    ),
                },
            }
        );

        _ = new BucketIAMMember(
            "state-bucket-access",
            new BucketIAMMemberArgs
            {
                Bucket = stateBucket.Name,
                Role = "roles/storage.objectAdmin",
                Member = deploymentSa.Email.Apply(email => $"serviceAccount:{email}"),
            }
        );

        StateBucketUrl = stateBucket.Url;
        StateBucketName = stateBucket.Name;
        DeploymentServiceAccountEmail = deploymentSa.Email;
        WorkloadIdentityPoolName = workloadIdentityPool.Name;
        WorkloadIdentityProviderName = workloadIdentityProvider.Name;

        WorkloadIdentityProviderFullName = Output
            .Tuple(
                workloadIdentityPool.Name,
                workloadIdentityProvider.WorkloadIdentityPoolProviderId
            )
            .Apply(t => $"{t.Item1}/providers/{t.Item2}");
    }

    [Output]
    public Output<string> StateBucketUrl { get; set; }

    [Output]
    public Output<string> StateBucketName { get; set; }

    [Output]
    public Output<string> DeploymentServiceAccountEmail { get; set; }

    [Output]
    public Output<string> WorkloadIdentityPoolName { get; set; }

    [Output]
    public Output<string> WorkloadIdentityProviderName { get; set; }

    [Output]
    public Output<string> WorkloadIdentityProviderFullName { get; set; }
}
