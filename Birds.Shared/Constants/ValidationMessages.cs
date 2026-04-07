using Birds.Shared.Localization;

namespace Birds.Shared.Constants
{
    public static class ValidationMessages
    {
        public static string UnselectedBird => AppText.Get("Validation.UnselectedBird");

        public static string LongDescription => AppText.Get("Validation.LongDescription");

        public static string DateIsNotSpecified => AppText.Get("Validation.DateIsNotSpecified");

        public static string InvalidDateRange => AppText.Get("Validation.InvalidDateRange");

        public static string DateIsNotValid => AppText.Get("Validation.DateIsNotValid");

        public static string DateCannotBeInTheFuture => AppText.Get("Validation.DateCannotBeInTheFuture");

        public static string DepartureLaterThenArrival => AppText.Get("Validation.DepartureLaterThenArrival");
    }
}
