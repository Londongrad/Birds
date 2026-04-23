using Birds.Domain.Entities;
using FluentAssertions;

namespace Birds.Tests.Domain;

public sealed class DomainDependencyTests
{
    [Fact]
    public void DomainAssembly_Should_NotReferenceSharedLocalizationOrPresentationResources()
    {
        var references = typeof(Bird).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        references.Should().NotContain("Birds.Shared");
    }
}
