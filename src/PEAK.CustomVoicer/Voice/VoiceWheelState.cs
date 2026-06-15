namespace PEAK.CustomVoicer.Voice;

internal static class VoiceWheelState
{
    public const int SlicesPerPage = 8;

    public static bool IsActive { get; set; }
    public static bool WheelVisible { get; set; }
    public static bool WindowBlockingInput { get; set; }
    public static bool VanillaWheelActive { get; set; }

    public static void Reset()
    {
        IsActive = false;
        WheelVisible = false;
        WindowBlockingInput = false;
    }
}
