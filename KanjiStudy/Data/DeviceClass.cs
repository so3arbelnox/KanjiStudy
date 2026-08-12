namespace KanjiStudy.Data
{
    /// <summary>
    /// Coarse device category, used only to pick a sensible default orientation
    /// (see Services/OrientationService.cs) when the user hasn't overridden it.
    /// </summary>
    public enum DeviceClass
    {
        Desktop,
        Tablet,
        Phone
    }
}
