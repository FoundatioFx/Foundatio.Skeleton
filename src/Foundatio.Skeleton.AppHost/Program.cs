var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("Redis")
    .WithImageTag("7.4")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("Foundatio-Redis");

var mail = builder.AddContainer("Mail", "axllent/mailpit")
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("Foundatio-Mail")
    .WithHttpEndpoint(targetPort: 8025, name: "http")
    .WithUrlForEndpoint("http", u => u.DisplayText = "Mail")
    .WithEndpoint(targetPort: 1025, name: "smtp");

builder.AddProject<Projects.Foundatio_Skeleton_Web>("Api")
    .WithReference(cache)
    .WithEnvironment("ConnectionStrings__Email", mail.GetEndpoint("smtp").Property(EndpointProperty.Url))
    .WaitFor(cache)
    .WaitFor(mail)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Foundatio_Skeleton_Jobs>("Jobs")
    .WithReference(cache)
    .WaitFor(cache);

await builder.Build().RunAsync();
