using System.Reflection;
using MouseJiggler.Application;
using MouseJiggler.Domain;
using MouseJiggler.Infrastructure;
using NetArchTest.Rules;

namespace MouseJiggler.ConsoleApp.Tests;

/// <summary>Enforces the Clean Architecture dependency rule: dependencies only point inwards.</summary>
public sealed class ArchitectureTests
{
    private const string DomainNamespace = "MouseJiggler.Domain";
    private const string ApplicationNamespace = "MouseJiggler.Application";
    private const string InfrastructureNamespace = "MouseJiggler.Infrastructure";
    private const string ConsoleNamespace = "MouseJiggler.ConsoleApp";

    private static readonly Assembly DomainAssembly = typeof(ScreenPoint).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(JiggleMouse).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(UnsupportedPlatformPointer).Assembly;

    [Fact]
    public void Domain_DependsOnNoOtherLayerOrFramework()
    {
        var result = Types.InAssembly(DomainAssembly).ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ConsoleNamespace, "Microsoft.Extensions")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrPresentation()
    {
        var result = Types.InAssembly(ApplicationAssembly).ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ConsoleNamespace, "System.Runtime.InteropServices")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnPresentation()
    {
        var result = Types.InAssembly(InfrastructureAssembly).ShouldNot()
            .HaveDependencyOn(ConsoleNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(TestResult result) =>
        "Offending types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
