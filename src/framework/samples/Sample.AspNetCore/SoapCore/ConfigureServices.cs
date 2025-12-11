using SoapCore;
using System.ServiceModel.Channels;

namespace Sample.AspNetCore.SoapCore;

public static class ConfigureServices
{
    public static IServiceCollection AddAppSoapCore(this IServiceCollection services)
    {
        services.AddSoapCore();
        //services.AddSoapMessageProcessor<SoapMessageProcessor>();
        services.AddScoped<ISoapCoreService, SoapCoreService>();

        return services;
    }

    public static IApplicationBuilder UseAppSoapCore(this IApplicationBuilder app)
    {
        app.UseSoapEndpoint<ISoapCoreService>("/Service.asmx", new SoapEncoderOptions()
        {
            // Use SOAP version 1.2 (aka Soap12)
            MessageVersion = MessageVersion.Soap12WSAddressingAugust2004,
        }, SoapSerializer.XmlSerializer, caseInsensitivePath: true);

        return app;
    }
}
