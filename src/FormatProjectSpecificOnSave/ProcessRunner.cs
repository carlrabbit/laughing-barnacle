using System.Diagnostics;

namespace FormatProjectSpecificOnSave;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start process '{startInfo.FileName}'.");
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }
}
