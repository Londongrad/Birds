using Birds.Domain.Common;
using Birds.Domain.Common.Exceptions;
using Birds.Domain.Enums;
using FluentAssertions;

namespace Birds.Tests.Domain
{
    public class GuardHelperTests
    {
        #region [ AgainstNullOrEmpty ]

        [Fact]
        public void AgainstNullOrEmpty_ShouldThrowException_ForNullOrEmptyStrings()
        {
            // Arrange
            string? nullString = null;
            string emptyString = "";
            string whitespaceString = "   ";

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstNullOrEmpty(nullString, "testArg"));
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstNullOrEmpty(emptyString, "testArg"));
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstNullOrEmpty(whitespaceString, "testArg"));
        }

        [Fact]
        public void AgainstNullOrEmpty_ShouldNotThrow_ForValidString()
        {
            // Arrange
            string validString = "Parrot";

            // Act
            Action act = () => GuardHelper.AgainstNullOrEmpty(validString, "testArg");

            // Assert
            act.Should().NotThrow();
        }

        #endregion [ AgainstNullOrEmpty ]

        #region [ AgainstInvalidDateOnly ]

        [Fact]
        public void AgainstInvalidDateOnly_ShouldThrowException_ForInvalidDates()
        {
            // Arrange
            DateOnly defaultDate = default;
            DateOnly futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
            DateOnly pastDate = DateOnly.FromDateTime(new DateTime(2019, 12, 31));

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateOnly(defaultDate, "testDate"));
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateOnly(futureDate, "testDate"));
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateOnly(pastDate, "testDate"));
        }

        [Fact]
        public void AgainstInvalidDateOnly_ShouldNotThrow_ForValidDates()
        {
            // Arrange
            DateOnly validPastDate = DateOnly.FromDateTime(new DateTime(2021, 6, 15));
            DateOnly validFutureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
            DateOnly now = DateOnly.FromDateTime(DateTime.UtcNow);

            // Act
            Action actPast = () => GuardHelper.AgainstInvalidDateOnly(validPastDate, "testDate");
            Action actFuture = () => GuardHelper.AgainstInvalidDateOnly(validFutureDate, "testDate", allowFuture: true);
            Action actNow = () => GuardHelper.AgainstInvalidDateOnly(now, "testDate");

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
            DateTime futureDate = DateTime.UtcNow.AddDays(1);
            DateTime pastDate = new(2019, 12, 31);
            DateTime nowNoUTC = DateTime.Now;

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateTime(defaultDate, "testDateTime"));
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateTime(futureDate, "testDateTime"));
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateTime(pastDate, "testDateTime"));
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateTime(nowNoUTC, "testDateTime"));
        }

        [Fact]
        public void AgainstInvalidDateTime_ShouldNotThrow_ForValidDates()
        {
            // Arrange
            DateTime validPastDate = new(2021, 6, 15);
            DateTime validFutureDate = DateTime.UtcNow.AddDays(10);
            DateTime now = DateTime.UtcNow;

            // Act
            Action actPast = () => GuardHelper.AgainstInvalidDateTime(validPastDate, "testDateTime");
            Action actFuture = () => GuardHelper.AgainstInvalidDateTime(validFutureDate, "testDateTime", allowFuture: true);
            Action actNow = () => GuardHelper.AgainstInvalidDateTime(now, "testDateTime");

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
            Guid emptyGuid = Guid.Empty;

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstEmptyGuid(emptyGuid, "testGuid"));
        }

        [Fact]
        public void AgainstEmptyGuid_ShouldNotThrow_ForValidGuid()
        {
            // Arrange
            Guid validGuid = Guid.NewGuid();

            // Act
            Action act = () => GuardHelper.AgainstEmptyGuid(validGuid, "testGuid");

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

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstNull(nullObject, "testObject"));
        }

        [Fact]
        public void AgainstNull_ShouldNotThrow_ForNonNullReference()
        {
            // Arrange
            object validObject = new();

            // Act
            Action act = () => GuardHelper.AgainstNull(validObject, "testObject");

            // Assert
            act.Should().NotThrow();
        }

        #endregion [ AgainstNull ]

        #region [ AgainstInvalidEnum ]

        [Fact]
        public void AgainstInvalidEnum_ShouldThrowException_ForInvalidEnumValue()
        {
            // Arrange
            var invalidEnumValue = (BirdsName)999;

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidEnum(invalidEnumValue, "testEnum"));
        }

        [Fact]
        public void AgainstInvalidEnum_ShouldNotThrow_ForValidEnumValue()
        {
            // Arrange
            var validEnumValue = BirdsName.Воробей;

            // Act
            Action act = () => GuardHelper.AgainstInvalidEnum(validEnumValue, "testEnum");

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

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidStringToEnum<BirdsName>(invalidString, "testEnum"));
        }

        [Fact]
        public void AgainstInvalidStringToEnum_ShouldNotThrow_ForValidString()
        {
            // Arrange
            var validString = "Воробей";

            // Act
            Action act = () => GuardHelper.AgainstInvalidStringToEnum<BirdsName>(validString, "testEnum");

            // Assert
            act.Should().NotThrow();
        }

        #endregion [ AgainstInvalidStringToEnum ]

        #region [ AgainstInvalidDateRange ]

        [Fact]
        public void AgainstInvalidDateRange_ShouldThrowException_ForInvalidDateRange()
        {
            // Arrange
            DateOnly startDate = DateOnly.FromDateTime(new DateTime(2022, 1, 1));
            DateOnly? endDate = DateOnly.FromDateTime(new DateTime(2021, 12, 31));
            DateOnly defaultDate = default;

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidDateRange(startDate, endDate));
            Assert.Throws<DomainValidationException>(() =>
                GuardHelper.AgainstInvalidDateRange(defaultDate, endDate));
        }

        [Fact]
        public void AgainstInvalidDateRange_ShouldNotThrow_ForValidDateRange()
        {
            // Arrange
            DateOnly startDate = DateOnly.FromDateTime(new DateTime(2021, 1, 1));
            DateOnly? endDate = DateOnly.FromDateTime(new DateTime(2022, 1, 1));
            DateOnly? nullDate = null;

            // Act
            Action act1 = () => GuardHelper.AgainstInvalidDateRange(startDate, endDate);
            Action act2 = () => GuardHelper.AgainstInvalidDateRange(startDate, nullDate);

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
            bool isAlive = false;
            DateOnly? departure = default;

            // Act & Assert
            Assert.Throws<DomainValidationException>(() => 
                GuardHelper.AgainstInvalidStatusUpdate(departure, isAlive, "test"));
        }

        [Fact]
        public void AgainstInvalidStatusUpdate_ShouldNotThrow_ForValidStatusTransition()
        {
            // Arrange
            bool isAlive1 = true;
            DateOnly? departure1 = default;

            bool isAlive2 = false;
            DateOnly? departure2 = DateOnly.FromDateTime(new DateTime(2022, 1, 1));

            // Act
            Action act1 = () => GuardHelper.AgainstInvalidStatusUpdate(departure1, isAlive1, "test");
            Action act2 = () => GuardHelper.AgainstInvalidStatusUpdate(departure2, isAlive2, "test");

            // Assert
            act1.Should().NotThrow();
            act2.Should().NotThrow();
        }

        #endregion [ AgainstInvalidStatusUpdate ]
    }
}