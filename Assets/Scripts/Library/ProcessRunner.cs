using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//Runs a program with an argument list (no shell). Cancelling kills it. Output is stdout and stderr together.
public static class ProcessRunner
{
    public static async Task<(int ExitCode, string Output)> RunAsync(string file, IReadOnlyList<string> args, CancellationToken ct, int timeoutMs = 0)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach(var a in args) { psi.ArgumentList.Add(a); }

        using(var process = new Process { StartInfo = psi })
        {
            var output = new StringBuilder();
            void Collect(object s, DataReceivedEventArgs e) { if(e.Data != null) { lock(output) { output.AppendLine(e.Data); } } }
            process.OutputDataReceived += Collect;
            process.ErrorDataReceived += Collect;

            process.Start();
            process.StandardInput.Close();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            void Kill() { try { if(!process.HasExited) { process.Kill(); } } catch(InvalidOperationException) { } }
            using(ct.Register(Kill))
            {
                var finished = await Task.Run(() => timeoutMs > 0 ? process.WaitForExit(timeoutMs) : WaitForever(process)).ConfigureAwait(false);
                if(!finished) { Kill(); throw new TimeoutException(file + " did not finish in time."); }
                process.WaitForExit();
            }
            ct.ThrowIfCancellationRequested();
            lock(output) { return (process.ExitCode, output.ToString()); }
        }
    }

    private static bool WaitForever(Process p) { p.WaitForExit(); return true; }
}
