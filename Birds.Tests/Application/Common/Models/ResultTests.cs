using Birds.Application.Common.Models;
using FluentAssertions;

namespace Birds.Tests.Application.Common.Models;

public class ResultTests
{
    [Fact]
    public void Failure_Should_Carry_Structured_AppError_And_Compatibility_Message()
    {
        var error = AppErrors.NotFound("Bird not found");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.AppError.Should().Be(error);
        result.ErrorCode.Should().Be(AppErrorCodes.BirdNotFound);
        result.Error.Should().Be("Bird not found");
        result.ErrorMessage.Should().Be("Bird not found");
    }

    [Fact]
    public void GenericFailure_Should_Carry_Structured_AppError_And_Compatibility_Message()
    {
        var error = AppErrors.ConcurrencyConflict("Conflict");

        var result = Result<string>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.AppError.Should().Be(error);
        result.ErrorCode.Should().Be(AppErrorCodes.BirdConcurrencyConflict);
        result.Error.Should().Be("Conflict");
        result.ErrorMessage.Should().Be("Conflict");
    }

    [Fact]
    public void StringFailure_Should_Create_Generic_Structured_Error_For_Backward_Compatibility()
    {
        var result = Result.Failure("legacy message");

        result.IsSuccess.Should().BeFalse();
        result.AppError.Should().NotBeNull();
        result.ErrorCode.Should().Be(AppErrorCodes.ApplicationFailure);
        result.Error.Should().Be("legacy message");
    }
}
