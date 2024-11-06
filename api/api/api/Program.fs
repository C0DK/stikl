namespace api

#nowarn "20"

open System.IdentityModel.Tokens.Jwt
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.Tokens
open Microsoft.OpenApi.Models
open Swashbuckle.AspNetCore.Filters
open Swashbuckle.AspNetCore.SwaggerGen


module Program =
    let exitCode = 0


    let configureSwagger (options: SwaggerGenOptions) =
        //options.SwaggerDoc("stikling.io", OpenApiInfo(Title = "Stikling.io API"))

        options.AddSecurityDefinition(
            "Bearer",
            OpenApiSecurityScheme(
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                Name = "Authorization",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: `Authorization: Bearer {token}`"
            )
        )
        //options.OperationFilter<SecurityRequirementsOperationFilter>()

        let mutable requirements = OpenApiSecurityRequirement()

        requirements.Add(
            OpenApiSecurityScheme(
                Name = "Bearer",
                In = ParameterLocation.Header,
                Reference = OpenApiReference(Id = "Bearer", Type = ReferenceType.SecurityScheme)
            ),
            Array.empty
        )

        options.AddSecurityRequirement(requirements)


    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        builder.Services |> Composition.registerAll


        builder.Services.AddAuthorization()

        builder.Services |> Composition.Authentication.configureJwtAuth


        builder.Services.AddSwaggerGen(configureSwagger) |> ignore

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthentication()
        app.UseAuthorization()

        app.MapControllers()

        app.UseSwagger()
        app.UseSwaggerUI()

        app.Run()

        exitCode
