using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Accesia.Application.Features.Authentication.Validators;
using Accesia.Application.Common.Behaviors;

namespace Accesia.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registrar todos los validadores del ensamblado de Application
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();

        // Registrar MediatR
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        });
        
        // Registrar ValidationBehavior como un pipeline behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
