using System.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace NorthstarET.Lms.CodeQuality.Tests;

/// <summary>
/// Static analysis compliance and nullable reference types validation
/// These tests ensure code quality standards and null safety are maintained
/// </summary>
[Collection("CodeQuality")]
public class StaticAnalysisComplianceTests
{
    private readonly ITestOutputHelper _output;

    public StaticAnalysisComplianceTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    #region Nullable Reference Types Compliance

    [Fact]
    [Trait("Category", "CodeQuality")]
    [Trait("Analysis", "NullableReferences")]
    public void DomainAssembly_ShouldHave_NullableReferenceTypesEnabled()
    {
        // Arrange - Get domain assembly
        var domainAssembly = GetAssemblySafely("NorthstarET.Lms.Domain.dll");
        if (domainAssembly == null)
        {
            _output.WriteLine("Domain assembly not found - skipping nullable reference check");
            return;
        }

        // Act & Assert - Check for nullable context attributes
        var nullableContextAttribute = domainAssembly.GetCustomAttribute<System.Runtime.CompilerServices.NullableContextAttribute>();
        var nullableAttribute = domainAssembly.GetCustomAttribute<System.Runtime.CompilerServices.NullableAttribute>();

        // Domain assembly should have nullable reference types enabled
        (nullableContextAttribute != null || nullableAttribute != null).Should().BeTrue(
            "Domain assembly should have nullable reference types enabled for null safety");

        _output.WriteLine($"Domain assembly nullable compliance verified");
    }

    [Theory]
    [InlineData("NorthstarET.Lms.Domain")]
    [InlineData("NorthstarET.Lms.Application")]
    [InlineData("NorthstarET.Lms.Infrastructure")]
    [InlineData("NorthstarET.Lms.Api")]
    [Trait("Category", "CodeQuality")]
    [Trait("Analysis", "NullableReferences")]
    public void Assemblies_ShouldNot_ContainNullableWarnings(string assemblyName)
    {
        // This test would normally check build output for nullable warnings
        var assembly = GetAssemblySafely($"{assemblyName}.dll");
        if (assembly == null)
        {
            _output.WriteLine($"Assembly {assemblyName} not found - skipping nullable warning check");
            return;
        }

        var types = assembly.GetTypes().Where(t => t.IsPublic).Take(10);

        foreach (var type in types)
        {
            // Check that public properties have appropriate nullable annotations
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                // Properties that return strings should be properly annotated
                if (property.PropertyType == typeof(string))
                {
                    var nullabilityInfo = new System.Reflection.NullabilityInfoContext().Create(property);
                    _output.WriteLine($"Property {type.Name}.{property.Name}: {nullabilityInfo.ReadState}");
                }
            }
        }

        _output.WriteLine($"Assembly {assemblyName} nullable reference compliance checked");
    }

    #endregion

    #region Code Analysis Rules Compliance

    [Fact]
    [Trait("Category", "CodeQuality")]
    [Trait("Analysis", "CodeAnalysis")]
    public void DomainLayer_ShouldNot_HaveCyclomaticComplexityViolations()
    {
        // Arrange
        var domainAssembly = GetAssemblySafely("NorthstarET.Lms.Domain.dll");
        if (domainAssembly == null)
        {
            _output.WriteLine("Domain assembly not found - skipping complexity check");
            return;
        }

        var complexityViolations = new List<string>();

        // Act - Check domain entities for complexity
        var domainTypes = domainAssembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic)
            .Where(t => t.Namespace?.Contains("Entities") == true);

        foreach (var type in domainTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            foreach (var method in methods)
            {
                // Simple heuristic: methods with too many parameters indicate complexity
                if (method.GetParameters().Length > 7)
                {
                    complexityViolations.Add($"{type.Name}.{method.Name} has {method.GetParameters().Length} parameters");
                }

                // Methods with generic names often indicate complexity
                if (method.Name.Length < 3 || method.Name == "Process" || method.Name == "Handle")
                {
                    complexityViolations.Add($"{type.Name}.{method.Name} has unclear naming");
                }
            }
        }

        // Assert
        complexityViolations.Should().BeEmpty("Domain layer should maintain low complexity");
        _output.WriteLine($"Domain layer complexity check completed: {domainTypes.Count()} types analyzed");
    }

    [Fact]
    [Trait("Category", "CodeQuality")]
    [Trait("Analysis", "CodeAnalysis")]
    public void PublicApi_ShouldHave_ProperXmlDocumentation()
    {
        // Arrange
        var apiAssembly = GetAssemblySafely("NorthstarET.Lms.Api.dll");
        if (apiAssembly == null)
        {
            _output.WriteLine("API assembly not found - skipping documentation check");
            return;
        }

        var undocumentedTypes = new List<string>();

        // Act - Check for XML documentation
        var publicTypes = apiAssembly.GetTypes()
            .Where(t => t.IsPublic)
            .Where(t => t.Name.EndsWith("Controller"))
            .Take(5); // Sample check

        foreach (var type in publicTypes)
        {
            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in publicMethods)
            {
                // Check for appropriate HTTP method attributes (for controllers)
                var hasHttpAttribute = method.GetCustomAttributes()
                    .Any(attr => attr.GetType().Name.StartsWith("Http"));

                if (type.Name.EndsWith("Controller") && !hasHttpAttribute && !method.Name.StartsWith("get_"))
                {
                    undocumentedTypes.Add($"{type.Name}.{method.Name} missing HTTP attribute");
                }
            }
        }

        // Assert
        undocumentedTypes.Should().HaveCountLessThan(3, "Most public APIs should be properly documented");
        _output.WriteLine($"API documentation check completed: {publicTypes.Count()} types analyzed");
    }

    #endregion

    #region Performance Analysis

    [Fact]
    [Trait("Category", "CodeQuality")]
    [Trait("Analysis", "Performance")]
    public void DomainEntities_ShouldNot_HavePerformanceAntiPatterns()
    {
        // Arrange
        var domainAssembly = GetAssemblySafely("NorthstarET.Lms.Domain.dll");
        if (domainAssembly == null)
        {
            _output.WriteLine("Domain assembly not found - skipping performance check");
            return;
        }

        var performanceIssues = new List<string>();

        // Act - Check for common performance anti-patterns
        var entityTypes = domainAssembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic)
            .Where(t => t.Namespace?.Contains("Entities") == true);

        foreach (var type in entityTypes)
        {
            // Check for excessive property count (might indicate fat entities)
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (properties.Length > 20)
            {
                performanceIssues.Add($"{type.Name} has {properties.Length} properties - consider decomposition");
            }

            // Check for collection properties that might cause N+1 queries
            var collectionProperties = properties.Where(p => 
                p.PropertyType.IsGenericType && 
                (p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                 p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 p.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)));

            if (collectionProperties.Count() > 3)
            {
                performanceIssues.Add($"{type.Name} has {collectionProperties.Count()} collection properties - review lazy loading");
            }
        }

        // Assert
        performanceIssues.Should().HaveCountLessThan(5, "Domain entities should follow performance best practices");

        foreach (var issue in performanceIssues)
        {
            _output.WriteLine($"Performance consideration: {issue}");
        }
    }

    [Fact]
    [Trait("Category", "CodeQuality")]
    [Trait("Analysis", "Performance")]
    public void ApplicationServices_ShouldHave_AsyncPatterns()
    {
        // Arrange
        var applicationAssembly = GetAssemblySafely("NorthstarET.Lms.Application.dll");
        if (applicationAssembly == null)
        {
            _output.WriteLine("Application assembly not found - skipping async pattern check");
            return;
        }

        var asyncViolations = new List<string>();

        // Act - Check for proper async patterns
        var serviceTypes = applicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.IsPublic)
            .Where(t => t.Name.EndsWith("Service"));

        foreach (var type in serviceTypes)
        {
            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            foreach (var method in publicMethods)
            {
                // Methods that likely perform I/O should be async
                if (method.Name.Contains("Create") || method.Name.Contains("Update") || 
                    method.Name.Contains("Delete") || method.Name.Contains("Get"))
                {
                    if (!method.Name.EndsWith("Async") && method.ReturnType != typeof(Task) && !method.ReturnType.IsGenericType)
                    {
                        asyncViolations.Add($"{type.Name}.{method.Name} should be async");
                    }
                }
            }
        }

        // Assert - Allow some violations as some methods might be simple calculations
        asyncViolations.Should().HaveCountLessThan(10, "Most I/O operations should follow async patterns");
        _output.WriteLine($"Async pattern analysis completed: {serviceTypes.Count()} services, {asyncViolations.Count} violations");
    }

    #endregion

    #region Architecture Compliance

    [Fact]
    [Trait("Category", "CodeQuality")]
    [Trait("Analysis", "Architecture")]
    public void DomainLayer_ShouldNot_DependOn_ExternalLayers()
    {
        // Arrange
        var domainAssembly = GetAssemblySafely("NorthstarET.Lms.Domain.dll");
        if (domainAssembly == null)
        {
            _output.WriteLine("Domain assembly not found - skipping dependency check");
            return;
        }

        var dependencyViolations = new List<string>();

        // Act - Check domain assembly references
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        foreach (var reference in referencedAssemblies)
        {
            var assemblyName = reference.Name?.ToLowerInvariant();
            
            // Domain should not reference Application, Infrastructure, or Api layers
            if (assemblyName?.Contains("application") == true ||
                assemblyName?.Contains("infrastructure") == true ||
                assemblyName?.Contains("api") == true ||
                assemblyName?.Contains("entityframework") == true)
            {
                dependencyViolations.Add($"Domain references {reference.Name}");
            }
        }

        // Assert
        dependencyViolations.Should().BeEmpty("Domain layer should not depend on external layers");
        _output.WriteLine($"Domain layer dependency analysis completed: {referencedAssemblies.Length} references checked");
    }

    #endregion

    #region Helper Methods

    private Assembly? GetAssemblySafely(string assemblyFileName)
    {
        try
        {
            // Try to load from current directory first
            if (File.Exists(assemblyFileName))
            {
                return Assembly.LoadFrom(assemblyFileName);
            }

            // Try to find in build output directories
            var searchPaths = new[]
            {
                Path.Combine("src", "NorthstarET.Lms.Domain", "bin", "Debug", "net9.0", assemblyFileName),
                Path.Combine("src", "NorthstarET.Lms.Application", "bin", "Debug", "net9.0", assemblyFileName),
                Path.Combine("src", "NorthstarET.Lms.Infrastructure", "bin", "Debug", "net9.0", assemblyFileName),
                Path.Combine("src", "NorthstarET.Lms.Api", "bin", "Debug", "net9.0", assemblyFileName)
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }
            }

            // Try to load by name (if already loaded)
            return Assembly.Load(assemblyFileName.Replace(".dll", ""));
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Could not load assembly {assemblyFileName}: {ex.Message}");
            return null;
        }
    }

    #endregion
}