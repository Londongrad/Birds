namespace Birds.Shared.Constants
{
    public class ValidationMessages
    {
        public const string UnselectedBird = "Выберите вид птицы из списка.";

        public const string LongDescription = "Описание слишком длинное. Максимум 200 символов.";

        public const string DateIsNotSpecified = "Укажите дату.";

        public const string InvalidDateRange = "Дата должна быть в диапазоне от {0:dd-MM-yyyy} до {1:dd-MM-yyyy}.";

        public const string DateIsNotValid = "Укажите корректную дату.";

        public const string DateCannotBeInTheFuture = "Дата не может быть из будущего (не позже {0:dd-MM-yyyy}).";

        public const string DepartureLaterThenArrival = "Дата выбытия не может быть раньше даты прибытия ({0:dd-MM-yyyy}).";
    }
}
