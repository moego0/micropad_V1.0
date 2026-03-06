using System.Threading.Tasks;
using System.Windows;
using Micropad.App.Dialogs;
using Micropad.App.Models;
using Micropad.Core.Models;

namespace Micropad.App.Services;

public class ProfileConflictResolverService : IProfileConflictResolver
{
    public Task<ProfileConflictResolution?> ResolveAsync(Profile localProfile, Profile deviceProfile)
    {
        var tcs = new TaskCompletionSource<ProfileConflictResolution?>();
        Application.Current.Dispatcher.Invoke(() =>
        {
            var owner = Application.Current.MainWindow;
            var dialog = new ProfileConflictWindow(
                localProfile.Name,
                localProfile.Version,
                deviceProfile.Version)
            {
                Owner = owner
            };
            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult == true && dialog.Result.HasValue && dialog.Result.Value != ProfileConflictResolution.Cancel)
                tcs.SetResult(dialog.Result.Value);
            else
                tcs.SetResult(null);
        });
        return tcs.Task;
    }
}
