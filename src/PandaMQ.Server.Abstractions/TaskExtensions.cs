namespace PandaMQ.Server.Abstractions;

public static class TaskExtensions
{
    public static async void Orphan(this ValueTask task)
    {
        try
        {
            await Task.Yield();
            await task.ConfigureAwait(false);
        }
        catch
        {
            // Ignored
        }
    }
    public static async void Orphan(this Task task)
    {
        try
        {
            await Task.Yield();
            await task.ConfigureAwait(false);
        }
        catch
        {
            // Ignored.
        }
    }
}