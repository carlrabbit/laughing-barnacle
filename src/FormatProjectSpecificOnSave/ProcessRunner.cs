using System.Diagnostics;

namespace FormatProjectSpecificOnSave;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
