namespace Birds.Shared.Constants
{
    public class ValidationMessages
    {
        public const string UnselectedBird = "No bird selected. Please select a bird from the list.";

        public const string LongDescription = "Description is too long. Maximum length is 100 characters.";

        public const string DateIsNotSpecified = "Specify the date.";

        public const string InvalidDateRange = "Date must be between {0:dd-MM-yyyy} and {1:dd-MM-yyyy}";

        public const string DateIsNotValid = "Please specify a valid date.";

        public const string DateCannotBeInTheFuture = "The date cannot be in the future (no later than {0:dd-MM-yyyy}).";

        public const string DepartureLaterThenArrival = "Departure date cannot be earlier than arrival date ({0:dd-MM-yyyy}).";
    }
}
