using System.Diagnostics;

namespace FormatProjectSpecificOnSave;

public interface IProcessRunner
{
    Task<int> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken);
}
