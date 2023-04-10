using extraltodeus;

static class Loggr
{
    public static void Error(string error)
    {
        SuperController.LogError($"{nameof(ExpressionRandomizer)}: {error}");
    }

    public static void Message(string message)
    {
        SuperController.LogMessage($"{nameof(ExpressionRandomizer)}: {message}");
    }
}
