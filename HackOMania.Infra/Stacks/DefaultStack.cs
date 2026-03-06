using Pulumi;
using Pulumi.Gcp.ArtifactRegistry;
using Pulumi.Gcp.CloudRun.Inputs;
using Pulumi.Gcp.CloudRunV2.Inputs;
using Pulumi.Gcp.SecretManager.Inputs;
using Pulumi.Gcp.ServiceAccount;
using Pulumi.Gcp.Storage;
using Pulumi.Gcp.Storage.Inputs;
using CloudRun = Pulumi.Gcp.CloudRunV2;
using CloudRunV1 = Pulumi.Gcp.CloudRun;
using ProjectIam = Pulumi.Gcp.Projects;
using SecretManager = Pulumi.Gcp.SecretManager;
using ServiceTemplateArgs = Pulumi.Gcp.CloudRunV2.Inputs.ServiceTemplateArgs;

namespace HackOMania.Infra.Stacks;

public class DefaultStack : Stack
{
    [Output]
    public Output<string> CloudRunServiceUrl { get; set; }

    [Output]
    public Output<string> CloudRunServiceAccountEmail { get; set; }

    public DefaultStack()
    {
        var config = new Config("gcp");
        var projectId = config.Get("project") ?? "hackomania-event-portal";

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

        var dataProtectionBucket = new Bucket(
            "hackomania-data-protection",
            new BucketArgs
            {
                Name = $"{projectId}-hackomania-data-protection",
                Location = "ASIA-SOUTHEAST1",
                ForceDestroy = false,
                Versioning = new BucketVersioningArgs { Enabled = true },
                UniformBucketLevelAccess = true,
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
                Replication = new SecretReplicationArgs { Auto = new SecretReplicationAutoArgs() },
            }
        );

        var githubClientSecretSecret = new SecretManager.Secret(
            "github-client-secret",
            new SecretManager.SecretArgs
            {
                SecretId = "github-client-secret",
                Replication = new SecretReplicationArgs { Auto = new SecretReplicationAutoArgs() },
            }
        );

        var tidbConnectionString = new SecretManager.Secret(
            "tidb-connection-string",
            new SecretManager.SecretArgs
            {
                SecretId = "tidb-connection-string",
                Replication = new SecretReplicationArgs { Auto = new SecretReplicationAutoArgs() },
            }
        );

        var postmarkServerToken = new SecretManager.Secret(
            "postmark-server-token",
            new SecretManager.SecretArgs
            {
                SecretId = "postmark-server-token",
                Replication = new SecretReplicationArgs { Auto = new SecretReplicationAutoArgs() },
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

        var postmarkServerTokenAccessor = new SecretManager.SecretIamMember(
            "postmark-server-token-accessor",
            new SecretManager.SecretIamMemberArgs
            {
                SecretId = postmarkServerToken.SecretId,
                Role = "roles/secretmanager.secretAccessor",
                Member = Output.Format($"serviceAccount:{cloudRunServiceAccount.Email}"),
            }
        );

        _ = new BucketIAMMember(
            "hackomania-data-protection-access",
            new BucketIAMMemberArgs
            {
                Bucket = dataProtectionBucket.Name,
                Role = "roles/storage.objectAdmin",
                Member = Output.Format($"serviceAccount:{cloudRunServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-logging-writer",
            new ProjectIam.IAMMemberArgs
            {
                Project = projectId,
                Role = "roles/logging.logWriter",
                Member = Output.Format($"serviceAccount:{cloudRunServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-trace-agent",
            new ProjectIam.IAMMemberArgs
            {
                Project = projectId,
                Role = "roles/cloudtrace.agent",
                Member = Output.Format($"serviceAccount:{cloudRunServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-artifact-registry-writer",
            new ProjectIam.IAMMemberArgs
            {
                Project = projectId,
                Role = "roles/artifactregistry.writer",
                Member = Output.Format($"serviceAccount:{deployerServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-cloud-run-admin",
            new ProjectIam.IAMMemberArgs
            {
                Project = projectId,
                Role = "roles/run.admin",
                Member = Output.Format($"serviceAccount:{deployerServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-service-account-user",
            new ProjectIam.IAMMemberArgs
            {
                Project = projectId,
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
                Scaling = new ServiceScalingArgs { MinInstanceCount = 1, MaxInstanceCount = 1 },
                Template = new ServiceTemplateArgs
                {
                    ServiceAccount = cloudRunServiceAccount.Email,
                    MaxInstanceRequestConcurrency = 512,
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
                                // With min instances = 1, CPU is always allocated.
                                CpuIdle = false,
                                Limits = { { "cpu", "0.2" }, { "memory", "512Mi" } },
                            },
                            StartupProbe = new ServiceTemplateContainerStartupProbeArgs
                            {
                                HttpGet = new ServiceTemplateContainerStartupProbeHttpGetArgs
                                {
                                    Path = "/health",
                                    Port = 8080,
                                },
                                InitialDelaySeconds = 0,
                                TimeoutSeconds = 1,
                                PeriodSeconds = 3,
                                FailureThreshold = 10,
                            },
                            LivenessProbe = new ServiceTemplateContainerLivenessProbeArgs
                            {
                                HttpGet = new ServiceTemplateContainerLivenessProbeHttpGetArgs
                                {
                                    Path = "/alive",
                                    Port = 8080,
                                },
                                InitialDelaySeconds = 0,
                                TimeoutSeconds = 1,
                                PeriodSeconds = 10,
                                FailureThreshold = 3,
                            },
                            Envs = new[]
                            {
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "TZ",
                                    Value = "Asia/Singapore",
                                },
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
                                    Value = "https://portal.geekshacking.com",
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
                                    Name = "App__AdminGitHubLogins__3",
                                    Value = "tohkailiang",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__4",
                                    Value = "gunnicorn",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__5",
                                    Value = "joeltio",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__6",
                                    Value = "ethan-chew",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__7",
                                    Value = "NEOGE07",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__8",
                                    Value = "rogeryeosgit",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__9",
                                    Value = "DerenC",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__10",
                                    Value = "UserNotFound7",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__11",
                                    Value = "tiramisuxh",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__12",
                                    Value = "misterdoobdoob",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__13",
                                    Value = "kepat",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "App__AdminGitHubLogins__14",
                                    Value = "ravern",
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
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "Postmark__ServerToken",
                                    ValueSource = new ServiceTemplateContainerEnvValueSourceArgs
                                    {
                                        SecretKeyRef =
                                            new ServiceTemplateContainerEnvValueSourceSecretKeyRefArgs
                                            {
                                                Secret = postmarkServerToken.SecretId,
                                                Version = "latest",
                                            },
                                    },
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "DataProtection__BucketName",
                                    Value = dataProtectionBucket.Name,
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "DataProtection__KeyPrefix",
                                    Value = "data-protection",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "Postmark__FromEmail",
                                    Value = "hackomania-noreply@geekshacking.com",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "Postmark__FromName",
                                    Value = "HackOMania 2026",
                                },
                                new ServiceTemplateContainerEnvArgs
                                {
                                    Name = "Postmark__Enabled",
                                    Value = "true",
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
                Metadata = new DomainMappingMetadataArgs { Namespace = cloudRunService.Project },
                Spec = new DomainMappingSpecArgs
                {
                    RouteName = cloudRunService.Name,
                    CertificateMode = "AUTOMATIC",
                },
            },
            new CustomResourceOptions
            {
                DeleteBeforeReplace = true,
                IgnoreChanges = ["metadata"],
                ReplaceOnChanges = ["spec", "name", "location"],
            }
        );

        CloudRunServiceUrl = cloudRunService.Uri;
        CloudRunServiceAccountEmail = cloudRunServiceAccount.Email;
    }
}
