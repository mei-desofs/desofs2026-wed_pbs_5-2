using FluentAssertions;
using LawyerApp.Shared;
using Xunit;

namespace LawyerApp.Tests.Unit.Shared;

public class ResultTests
{
    // ── Result<T>.Success ────────────────────────────────────────────────────

    [Fact]
    public void Success_SetsIsSuccessTrue()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Success_ExposesValue()
    {
        var result = Result<int>.Success(42);

        result.Value.Should().Be(42);
    }

    [Fact]
    public void Success_ErrorIsNone()
    {
        var result = Result<string>.Success("v");

        result.Error.Should().Be(Error.None);
    }

    // ── Result<T>.Failure ────────────────────────────────────────────────────

    [Fact]
    public void Failure_SetsIsFailureTrue()
    {
        var result = Result<string>.Failure(400, "Bad request");

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Failure_ExposesErrorCodeAndMessage()
    {
        var result = Result<string>.Failure(404, "Not found");

        result.Error.Code.Should().Be("404");
        result.Error.Message.Should().Be("Not found");
    }

    [Fact]
    public void Failure_ValueIsDefault()
    {
        var result = Result<string>.Failure(500, "Error");

        result.Value.Should().BeNull();
    }

    [Fact]
    public void Failure_WithDefaultStringCode_UsesCode500()
    {
        var result = Result<string>.Failure("generic error");

        result.Error.Code.Should().Be("500");
    }

    // ── Result<T>.Ok ─────────────────────────────────────────────────────────

    [Fact]
    public void Ok_IsEquivalentToSuccess()
    {
        var result = Result<string>.Ok("data");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("data");
    }

    // ── Result (non-generic) ─────────────────────────────────────────────────

    [Fact]
    public void NonGenericOk_IsSuccess()
    {
        var result = Result.Ok();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void NonGenericFail_IsFailure()
    {
        var result = Result.Fail("something went wrong");

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("something went wrong");
    }
}

public class ErrorTests
{
    [Fact]
    public void None_HasEmptyCodeAndMessage()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_StoresCodeAndMessage()
    {
        var error = new Error("422", "Unprocessable entity");

        error.Code.Should().Be("422");
        error.Message.Should().Be("Unprocessable entity");
    }

    [Fact]
    public void TwoErrors_WithSameValues_AreEqual()
    {
        var e1 = new Error("400", "Bad");
        var e2 = new Error("400", "Bad");

        e1.Should().Be(e2);
    }

    [Fact]
    public void TwoErrors_WithDifferentCodes_AreNotEqual()
    {
        var e1 = new Error("400", "Bad");
        var e2 = new Error("500", "Bad");

        e1.Should().NotBe(e2);
    }
}
