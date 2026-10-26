using System.Reflection;
using NetArchTest.Rules;
using TaskBoard.Domain.Common;

namespace TaskBoard.ArchitectureTests;

/// <summary>
/// Fails the build when a layer references something it must not (the Clean Architecture dependency rule).
/// </summary>
public class LayerDependencyTests
{
    private const string ApplicationNamespace = "TaskBoard.Application";
    private const string InfrastructureNamespace = "TaskBoard.Infrastructure";
    private const string ApiNamespace = "TaskBoard.Api";

    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnOtherLayersOrFrameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Handlers_AreSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    private static string FailingTypes(TestResult result) =>
        string.Join(", ", result.FailingTypeNames ?? []);
}
