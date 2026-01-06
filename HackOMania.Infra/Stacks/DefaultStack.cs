using Pulumi;
using Pulumi.Gcp.ArtifactRegistry;
using Pulumi.Gcp.CloudRunV2.Inputs;
using Pulumi.Gcp.ServiceAccount;
using CloudRun = Pulumi.Gcp.CloudRunV2;
using CloudRunV1 = Pulumi.Gcp.CloudRun;
using ProjectIam = Pulumi.Gcp.Projects;
using SecretManager = Pulumi.Gcp.SecretManager;

namespace HackOMania.Infra.Stacks;

public class DefaultStack : Stack
{
    [Output]
    public Output<string> CloudRunServiceUrl { get; set; }

    [Output]
    public Output<string> CloudRunServiceAccountEmail { get; set; }

    public DefaultStack()
    {
        var pkg = new Repository(
            "hackomania-api-repo",
            new RepositoryArgs
            {
                RepositoryId = "hackomania-api",
                Location = "asia-southeast1",
                Format = "DOCKER",
                Description = "Docker repository for HackOMania API",
                CleanupPolicyDryRun = false,
            }
        );

        var cloudRunServiceAccount = new Account(
            "hackomania-api-sa",
            new AccountArgs
            {
                AccountId = "hackomania-api",
                DisplayName = "HackOMania API Cloud Run Service Account",
                Description = "Service account used by Cloud Run to run the HackOMania API",
            }
        );

        var deployerServiceAccount = new Account(
            "hackomania-api-deployer-sa",
            new AccountArgs
            {
                AccountId = "hackomania-api-deployer",
                DisplayName = "HackOMania Deployer Service Account",
                Description = "Service account used by GitHub Actions to deploy the HackOMania API",
            }
        );

        var githubClientIdSecret = new SecretManager.Secret(
            "github-client-id",
            new SecretManager.SecretArgs
            {
                SecretId = "github-client-id",
                Replication = new SecretManager.Inputs.SecretReplicationArgs
                {
                    Auto = new SecretManager.Inputs.SecretReplicationAutoArgs(),
                },
            }
        );

        var githubClientSecretSecret = new SecretManager.Secret(
            "github-client-secret",
            new SecretManager.SecretArgs
            {
                SecretId = "github-client-secret",
                Replication = new SecretManager.Inputs.SecretReplicationArgs
                {
                    Auto = new SecretManager.Inputs.SecretReplicationAutoArgs(),
                },
            }
        );

        var tidbConnectionString = new SecretManager.Secret(
            "tidb-connection-string",
            new SecretManager.SecretArgs
            {
                SecretId = "tidb-connection-string",
                Replication = new SecretManager.Inputs.SecretReplicationArgs
                {
                    Auto = new SecretManager.Inputs.SecretReplicationAutoArgs(),
                },
            }
        );

        var githubClientIdAccessor = new SecretManager.SecretIamMember(
            "github-client-id-accessor",
            new SecretManager.SecretIamMemberArgs
            {
                SecretId = githubClientIdSecret.SecretId,
                Role = "roles/secretmanager.secretAccessor",
                Member = Output.Format($"serviceAccount:{cloudRunServiceAccount.Email}"),
            }
        );

        var githubClientSecretAccessor = new SecretManager.SecretIamMember(
            "github-client-secret-accessor",
            new SecretManager.SecretIamMemberArgs
            {
                SecretId = githubClientSecretSecret.SecretId,
                Role = "roles/secretmanager.secretAccessor",
                Member = Output.Format($"serviceAccount:{cloudRunServiceAccount.Email}"),
            }
        );

        var tidbConnectionStringAccessor = new SecretManager.SecretIamMember(
            "tidb-connection-string-accessor",
            new SecretManager.SecretIamMemberArgs
            {
                SecretId = tidbConnectionString.SecretId,
                Role = "roles/secretmanager.secretAccessor",
                Member = Output.Format($"serviceAccount:{cloudRunServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-artifact-registry-writer",
            new ProjectIam.IAMMemberArgs
            {
                Project = "hackomania-event-portal",
                Role = "roles/artifactregistry.writer",
                Member = Output.Format($"serviceAccount:{deployerServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-cloud-run-admin",
            new ProjectIam.IAMMemberArgs
            {
                Project = "hackomania-event-portal",
                Role = "roles/run.admin",
                Member = Output.Format($"serviceAccount:{deployerServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-service-account-user",
            new ProjectIam.IAMMemberArgs
            {
                Project = "hackomania-event-portal",
                Role = "roles/iam.serviceAccountUser",
                Member = Output.Format($"serviceAccount:{deployerServiceAccount.Email}"),
            }
        );

        _ = new IAMBinding(
            "hackoamania-api-deployer-workload-identity-binding",
            new IAMBindingArgs
            {
                ServiceAccountId = deployerServiceAccount.Name,
                Role = "roles/iam.workloadIdentityUser",
                Members = new[]
                {
                    "principalSet://iam.googleapis.com/projects/242247218750/locations/global/workloadIdentityPools/github/attribute.repository/GeeksHacking/hackomania-event-platform",
                },
            }
        );

        var cloudRunService = new CloudRun.Service(
            "hackomania-api",
            new CloudRun.ServiceArgs
            {
                Name = "hackomania-api",
                Location = "asia-southeast1",
                Description = "HackOMania API Service",
                Ingress = "INGRESS_TRAFFIC_ALL",
                Template = new ServiceTemplateArgs
                {
                    ServiceAccount = cloudRunServiceAccount.Email,
                    MaxInstanceRequestConcurrency = 128,
                    Scaling = new ServiceTemplateScalingArgs
                    {
                        MinInstanceCount = 0,
                        MaxInstanceCount = 10,
                    },
                    Containers = new[]
                    {
                        new ServiceTemplateContainerArgs
                        {
                            Image = Output.Format($"{pkg.RegistryUri}/hackomania-api"),
                            Ports = new ServiceTemplateContainerPortsArgs
                            {
                                ContainerPort = 8080,
                                Name = "http1",
                            },
                            Resources = new ServiceTemplateContainerResourcesArgs
                            {
                                Limits = { { "cpu", "1" }, { "memory", "512Mi" } },
                            },
                            Envs = new[]
                            {
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "ASPNETCORE_ENVIRONMENT",
                                    Value = "Production",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "ASPNETCORE_FORWARDEDHEADERS_ENABLED",
                                    Value = "true",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__FrontendUrl",
                                    Value = "https://hackomania.geekshacking.com/dash",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__0",
                                    Value = "qin-guan",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__1",
                                    Value = "julwrites",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__2",
                                    Value = "whipermr5",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "GitHub__ClientId",
                                    ValueSource = new ServiceTemplateContainerEnvValueSourceArgs
                                    {
                                        SecretKeyRef =
                                            new ServiceTemplateContainerEnvValueSourceSecretKeyRefArgs
                                            {
                                                Secret = githubClientIdSecret.SecretId,
                                                Version = "latest",
                                            },
                                    },
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "GitHub__ClientSecret",
                                    ValueSource = new ServiceTemplateContainerEnvValueSourceArgs
                                    {
                                        SecretKeyRef =
                                            new ServiceTemplateContainerEnvValueSourceSecretKeyRefArgs
                                            {
                                                Secret = githubClientSecretSecret.SecretId,
                                                Version = "latest",
                                            },
                                    },
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "ConnectionStrings__db",
                                    ValueSource = new ServiceTemplateContainerEnvValueSourceArgs
                                    {
                                        SecretKeyRef =
                                            new ServiceTemplateContainerEnvValueSourceSecretKeyRefArgs
                                            {
                                                Secret = tidbConnectionString.SecretId,
                                                Version = "latest",
                                            },
                                    },
                                },
                            },
                        },
                    },
                },
            }
        );

        _ = new CloudRun.ServiceIamBinding(
            "hackomania-api-public-access",
            new CloudRun.ServiceIamBindingArgs
            {
                Name = cloudRunService.Name,
                Location = "asia-southeast1",
                Role = "roles/run.invoker",
                Members = new[] { "allUsers" },
            }
        );

        _ = new CloudRunV1.DomainMapping(
            "hackomania-api-domain",
            new CloudRunV1.DomainMappingArgs
            {
                Name = "hackomania-api.geekshacking.com",
                Location = "asia-southeast1",
                Metadata = new CloudRunV1.Inputs.DomainMappingMetadataArgs
                {
                    Namespace = cloudRunService.Project,
                },
                Spec = new CloudRunV1.Inputs.DomainMappingSpecArgs
                {
                    RouteName = cloudRunService.Name,
                    CertificateMode = "AUTOMATIC",
                },
            }
        );

        CloudRunServiceUrl = cloudRunService.Uri;
        CloudRunServiceAccountEmail = cloudRunServiceAccount.Email;
    }
}
