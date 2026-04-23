using Birds.Domain.Common;
using Birds.Domain.Common.Exceptions;
using Birds.Domain.Enums;
using FluentAssertions;

namespace Birds.Tests.Domain;

public class GuardHelperTests
{
    #region [ AgainstNullOrEmpty ]

    [Fact]
    public void AgainstNullOrEmpty_ShouldThrowException_ForNullOrEmptyStrings()
    {
        // Arrange
        string? nullString = null;
        var emptyString = "";
        var whitespaceString = "   ";

        // Act
        var act1 = () => GuardHelper.AgainstNullOrEmpty(nullString, "testArg");
        var act2 = () => GuardHelper.AgainstNullOrEmpty(emptyString, "testArg");
        var act3 = () => GuardHelper.AgainstNullOrEmpty(whitespaceString, "testArg");

        // Assert
        act1.Should().Throw<DomainValidationException>().WithMessage("*testArg*");
        act2.Should().Throw<DomainValidationException>().WithMessage("*testArg*");
        act3.Should().Throw<DomainValidationException>().WithMessage("*testArg*");
    }

    [Fact]
    public void AgainstNullOrEmpty_ShouldNotThrow_ForValidString()
    {
        // Arrange
        var validString = "Sparrow";

        // Act
        var act = () => GuardHelper.AgainstNullOrEmpty(validString, "testArg");

        // Assert
        act.Should().NotThrow();
    }

    #endregion [ AgainstNullOrEmpty ]

    #region [ AgainstExceedsMaxLength ]

    [Fact]
    public void AgainstExceedsMaxLength_ShouldThrowException_ForTooLongString()
    {
        var longValue = new string('A', 6);

        var act = () => GuardHelper.AgainstExceedsMaxLength(longValue, 5, "testArg");

        act.Should().Throw<DomainValidationException>().WithMessage("*testArg*");
    }

    [Fact]
    public void AgainstExceedsMaxLength_ShouldNotThrow_ForNullOrShortString()
    {
        string? nullString = null;
        var shortString = "Bird";

        var act1 = () => GuardHelper.AgainstExceedsMaxLength(nullString, 5, "testArg");
        var act2 = () => GuardHelper.AgainstExceedsMaxLength(shortString, 5, "testArg");

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    #endregion [ AgainstExceedsMaxLength ]

    #region [ AgainstInvalidDateOnly ]

    [Fact]
    public void AgainstInvalidDateOnly_ShouldThrowException_ForInvalidDates()
    {
        // Arrange
        DateOnly defaultDate = default;
        var futureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
        var pastDate = DateOnly.FromDateTime(new DateTime(2019, 12, 31));

        // Act
        var act1 = () => GuardHelper.AgainstInvalidDateOnly(defaultDate, "testDate");
        var act2 = () => GuardHelper.AgainstInvalidDateOnly(futureDate, "testDate");
        var act3 = () => GuardHelper.AgainstInvalidDateOnly(pastDate, "testDate");

        // Assert
        act1.Should().Throw<DomainValidationException>().WithMessage("*testDate*");
        act2.Should().Throw<DomainValidationException>().WithMessage("*testDate*");
        act3.Should().Throw<DomainValidationException>().WithMessage("*testDate*");
    }

    [Fact]
    public void AgainstInvalidDateOnly_ShouldNotThrow_ForValidDates()
    {
        // Arrange
        var validPastDate = DateOnly.FromDateTime(new DateTime(2021, 6, 15));
        var validFutureDate = DateOnly.FromDateTime(DateTime.Now.AddDays(10));
        var now = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var actPast = () => GuardHelper.AgainstInvalidDateOnly(validPastDate, "testDate");
        var actFuture = () => GuardHelper.AgainstInvalidDateOnly(validFutureDate, "testDate", true);
        var actNow = () => GuardHelper.AgainstInvalidDateOnly(now, "testDate");

        // Assert
        actPast.Should().NotThrow();
        actFuture.Should().NotThrow();
        actNow.Should().NotThrow();
    }

    #endregion [ AgainstInvalidDateOnly ]

    #region [ AgainstInvalidDateTime ]

    [Fact]
    public void AgainstInvalidDateTime_ShouldThrowException_ForInvalidDates()
    {
        // Arrange
        DateTime defaultDate = default;
        var futureDate = DateTime.Now.AddDays(1);
        DateTime pastDate = new(2019, 12, 31);

        // Act
        var act1 = () => GuardHelper.AgainstInvalidDateTime(defaultDate, "testDateTime");
        var act2 = () => GuardHelper.AgainstInvalidDateTime(futureDate, "testDateTime");
        var act3 = () => GuardHelper.AgainstInvalidDateTime(pastDate, "testDateTime");

        // Assert
        act1.Should().Throw<DomainValidationException>().WithMessage("*testDateTime*");
        act2.Should().Throw<DomainValidationException>().WithMessage("*testDateTime*");
        act3.Should().Throw<DomainValidationException>().WithMessage("*testDateTime*");
    }

    [Fact]
    public void AgainstInvalidDateTime_ShouldNotThrow_ForValidDates()
    {
        // Arrange
        DateTime validPastDate = new(2021, 6, 15);
        var validFutureDate = DateTime.Now.AddDays(10);
        var now = DateTime.Now;

        // Act
        var actPast = () => GuardHelper.AgainstInvalidDateTime(validPastDate, "testDateTime");
        var actFuture = () => GuardHelper.AgainstInvalidDateTime(validFutureDate, "testDateTime", true);
        var actNow = () => GuardHelper.AgainstInvalidDateTime(now, "testDateTime");

        // Assert
        actPast.Should().NotThrow();
        actFuture.Should().NotThrow();
        actNow.Should().NotThrow();
    }

    #endregion [ AgainstInvalidDateTime ]

    #region [ AgainstEmptyGuid ]

    [Fact]
    public void AgainstEmptyGuid_ShouldThrowException_ForEmptyGuid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var act = () => GuardHelper.AgainstEmptyGuid(emptyGuid, "testGuid");

        // Assert
        act.Should().Throw<DomainValidationException>().WithMessage("*testGuid*");
    }

    [Fact]
    public void AgainstEmptyGuid_ShouldNotThrow_ForValidGuid()
    {
        // Arrange
        var validGuid = Guid.NewGuid();

        // Act
        var act = () => GuardHelper.AgainstEmptyGuid(validGuid, "testGuid");

        // Assert
        act.Should().NotThrow();
    }

    #endregion [ AgainstEmptyGuid ]

    #region [ AgainstNull ]

    [Fact]
    public void AgainstNull_ShouldThrowException_ForNullReference()
    {
        // Arrange
        object? nullObject = null;

        // Act
        var act = () => GuardHelper.AgainstNull(nullObject, "testObject");

        // Assert
        act.Should().Throw<DomainValidationException>().WithMessage("*testObject*");
    }

    [Fact]
    public void AgainstNull_ShouldNotThrow_ForNonNullReference()
    {
        // Arrange
        object validObject = new();

        // Act
        var act = () => GuardHelper.AgainstNull(validObject, "testObject");

        // Assert
        act.Should().NotThrow();
    }

    #endregion [ AgainstNull ]

    #region [ AgainstInvalidEnum ]

    [Fact]
    public void AgainstInvalidEnum_ShouldThrowException_ForInvalidEnumValue()
    {
        // Arrange
        var invalidEnumValue = (BirdSpecies)999;

        // Act
        var act = () => GuardHelper.AgainstInvalidEnum(invalidEnumValue, "testEnum");

        // Assert
        act.Should().Throw<DomainValidationException>().WithMessage("*testEnum*");
    }

    [Fact]
    public void AgainstInvalidEnum_ShouldNotThrow_ForValidEnumValue()
    {
        // Arrange
        var validEnumValue = BirdSpecies.Sparrow;

        // Act
        var act = () => GuardHelper.AgainstInvalidEnum(validEnumValue, "testEnum");

        // Assert
        act.Should().NotThrow();
    }

    #endregion [ AgainstInvalidEnum ]

    #region [ AgainstInvalidStringToEnum ]

    [Fact]
    public void AgainstInvalidStringToEnum_ShouldThrowException_ForInvalidString()
    {
        // Arrange
        var invalidString = "InvalidBird";

        // Act
        var act = () => GuardHelper.AgainstInvalidStringToEnum<BirdSpecies>(invalidString, "testEnum");

        // Assert
        act.Should().Throw<DomainValidationException>().WithMessage("*testEnum*");
    }

    [Fact]
    public void AgainstInvalidStringToEnum_ShouldNotThrow_ForValidString()
    {
        // Arrange
        var validString = "Sparrow";

        // Act
        var act = () => GuardHelper.AgainstInvalidStringToEnum<BirdSpecies>(validString, "testEnum");

        // Assert
        act.Should().NotThrow();
    }

    #endregion [ AgainstInvalidStringToEnum ]

    #region [ AgainstInvalidDateRange ]

    [Fact]
    public void AgainstInvalidDateRange_ShouldThrowException_ForInvalidDateRange()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(new DateTime(2022, 1, 1));
        DateOnly? endDate = DateOnly.FromDateTime(new DateTime(2021, 12, 31));
        DateOnly defaultDate = default;

        // Act
        var act1 = () => GuardHelper.AgainstInvalidDateRange(startDate, endDate);
        var act2 = () => GuardHelper.AgainstInvalidDateRange(defaultDate, endDate);

        // Assert
        act1.Should().Throw<DomainValidationException>();
        act2.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void AgainstInvalidDateRange_ShouldNotThrow_ForValidDateRange()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(new DateTime(2021, 1, 1));
        DateOnly? endDate = DateOnly.FromDateTime(new DateTime(2022, 1, 1));
        DateOnly? nullDate = null;

        // Act
        var act1 = () => GuardHelper.AgainstInvalidDateRange(startDate, endDate);
        var act2 = () => GuardHelper.AgainstInvalidDateRange(startDate, nullDate);

        // Assert
        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    #endregion [ AgainstInvalidDateRange ]

    #region [ AgainstInvalidStatusUpdate ]

    [Fact]
    public void AgainstInvalidStatusUpdate_ShouldThrowException_ForInvalidStatusTransition()
    {
        // Arrange
        var isAlive = false;
        DateOnly? departure = default;

        // Act
        var act = () => GuardHelper.AgainstInvalidStatusUpdate(departure, isAlive, "test");

        // Assert
        act.Should().Throw<DomainValidationException>().WithMessage("*test*");
    }

    [Fact]
    public void AgainstInvalidStatusUpdate_ShouldNotThrow_ForValidStatusTransition()
    {
        // Arrange
        var isAlive1 = true;
        DateOnly? departure1 = default;

        var isAlive2 = false;
        DateOnly? departure2 = DateOnly.FromDateTime(new DateTime(2022, 1, 1));

        // Act
        var act1 = () => GuardHelper.AgainstInvalidStatusUpdate(departure1, isAlive1, "test");
        var act2 = () => GuardHelper.AgainstInvalidStatusUpdate(departure2, isAlive2, "test");

        // Assert
        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    #endregion [ AgainstInvalidStatusUpdate ]

    #region [ AgainstNonPositiveNumber ]

    [Fact]
    public void AgainstNonPositiveNumber_ShouldThrowException_ForZeroOrNegativeValues()
    {
        // Arrange
        var zero = 0;
        var negative = -5;
        var zeroDecimal = 0m;
        var negativeDouble = -2.5;

        // Act
        var act1 = () => GuardHelper.AgainstNonPositiveNumber(zero, "intValue");
        var act2 = () => GuardHelper.AgainstNonPositiveNumber(negative, "intValue");
        var act3 = () => GuardHelper.AgainstNonPositiveNumber(zeroDecimal, "decimalValue");
        var act4 = () => GuardHelper.AgainstNonPositiveNumber(negativeDouble, "doubleValue");

        // Assert
        act1.Should().Throw<DomainValidationException>().WithMessage("*should be positive*");
        act2.Should().Throw<DomainValidationException>().WithMessage("*should be positive*");
        act3.Should().Throw<DomainValidationException>().WithMessage("*should be positive*");
        act4.Should().Throw<DomainValidationException>().WithMessage("*should be positive*");
    }

    [Fact]
    public void AgainstNonPositiveNumber_ShouldNotThrow_ForPositiveValues()
    {
        // Arrange
        var positiveInt = 10;
        var positiveDecimal = 5.5m;
        var positiveDouble = 0.0001;

        // Act
        var actInt = () => GuardHelper.AgainstNonPositiveNumber(positiveInt, "intValue");
        var actDecimal = () => GuardHelper.AgainstNonPositiveNumber(positiveDecimal, "decimalValue");
        var actDouble = () => GuardHelper.AgainstNonPositiveNumber(positiveDouble, "doubleValue");

        // Assert
        actInt.Should().NotThrow();
        actDecimal.Should().NotThrow();
        actDouble.Should().NotThrow();
    }

    #endregion [ AgainstNonPositiveNumber ]
}
