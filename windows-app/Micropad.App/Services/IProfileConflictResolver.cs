using System.Threading.Tasks;
using Micropad.App.Models;
using Micropad.Core.Models;

namespace Micropad.App.Services;

public interface IProfileConflictResolver
{
    /// <summary>Show conflict dialog when PC and device have same profile id but different versions. Returns null if cancelled.</summary>
    Task<ProfileConflictResolution?> ResolveAsync(Profile localProfile, Profile deviceProfile);
}
