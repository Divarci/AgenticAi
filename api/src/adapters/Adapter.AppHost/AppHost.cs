var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Adapter_Restful>("adapter-restful")
    .WithUrls(context =>
    {
        var baseUrl = context.Urls.FirstOrDefault();
        if (baseUrl is not null)
        {
            context.Urls.Add(new()
            {
                Url = baseUrl.Url.TrimEnd('/') + "/devui",
                DisplayText = "DevUI Visual App"
            });
        }
    });

builder.AddProject<Projects.Adapter_Scheduler>("adapter-scheduler");

builder.Build().Run();


