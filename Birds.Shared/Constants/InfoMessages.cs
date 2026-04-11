using Birds.Shared.Localization;

namespace Birds.Shared.Constants;

public static class InfoMessages
{
    public static string LoadingBirdData => AppText.Get("Info.LoadingBirdData");

    public static string LoadedSuccessfully => AppText.Get("Info.LoadedSuccessfully");
    public static string NoBirdRecordsYet => AppText.Get("Info.NoBirdRecordsYet");

    public static string ReloadingBirdData => AppText.Get("Info.ReloadingBirdData");

    public static string AddingBird => AppText.Get("Info.AddingBird");

    public static string UpdatingBird => AppText.Get("Info.UpdatingBird");
    public static string UpdatedBird => AppText.Get("Info.UpdatedBird");

    public static string DeletingBird => AppText.Get("Info.DeletingBird");
    public static string DeletedBird => AppText.Get("Info.DeletedBird");

    public static string BirdAdded => AppText.Get("Info.BirdAdded");

    public static string ToThisDay => AppText.Get("Info.ToThisDay");

    public static string LoadFailed => AppText.Get("Info.LoadFailed");

    public static string AutoExportSucceeded => AppText.Get("Info.AutoExportSucceeded");
}