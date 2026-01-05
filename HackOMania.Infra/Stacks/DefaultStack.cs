using Pulumi;
using Pulumi.Gcp.ArtifactRegistry;
using Pulumi.Gcp.CloudRunV2.Inputs;
using Pulumi.Gcp.ServiceAccount;
using CloudRun = Pulumi.Gcp.CloudRunV2;
using ProjectIam = Pulumi.Gcp.Projects;

namespace HackOMania.Infra.Stacks;

public class DefaultStack : Stack
{
    [Output]
    public Output<string> CloudRunServiceUrl { get; set; }

    [Output]
    public Output<string> CloudRunServiceAccountEmail { get; set; }

    public DefaultStack()
    {
        _ = new Repository(
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

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-artifact-registry-writer",
            new ProjectIam.IAMMemberArgs
            {
                Project = "hackomania-event-platform",
                Role = "roles/artifactregistry.writer",
                Member = Output.Format($"serviceAccount:{deployerServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-cloud-run-admin",
            new ProjectIam.IAMMemberArgs
            {
                Project = "hackomania-event-platform",
                Role = "roles/run.admin",
                Member = Output.Format($"serviceAccount:{deployerServiceAccount.Email}"),
            }
        );

        _ = new ProjectIam.IAMMember(
            "hackomania-api-deployer-service-account-user",
            new ProjectIam.IAMMemberArgs
            {
                Project = "hackomania-event-platform",
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
                    $"principalSet://iam.googleapis.com/projects/242247218750/locations/global/workloadIdentityPools/github/attribute.repository/GeeksHacking/hackomania-event-platform",
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
                            },
                        },
                    },
                },
            }
        );

        _ = new CloudRun.ServiceIamMember(
            "hackomania-api-invoker",
            new CloudRun.ServiceIamMemberArgs
            {
                Name = cloudRunService.Name,
                Location = "asia-southeast1",
                Role = "roles/run.invoker",
                Member = "allUsers",
            }
        );

        CloudRunServiceUrl = cloudRunService.Uri;
        CloudRunServiceAccountEmail = cloudRunServiceAccount.Email;
    }
}
