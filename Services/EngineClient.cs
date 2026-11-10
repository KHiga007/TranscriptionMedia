using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace TranscriptionMedia;

public sealed class RequestRejectedException(string message) : IOException(message);

public sealed class EngineClient : IDisposable
{
    private const int LastAttempt = 2;
    private const long WorkerMemoryLimit = 3L * 1024 * 1024 * 1024;
    private readonly bool forceCpu;
    private readonly SemaphoreSlim sessionLock = new(1, 1);
    private Process? process;
    private IntPtr job;

    public EngineClient(bool forceCpu = false)
    {
        this.forceCpu = forceCpu;
    }

    public string Backend { get; private set; } = "";

    public async Task<string> Run(EngineRequest request, Action<string>? progress, CancellationToken token, Action<double>? percent = null)
    {
        await sessionLock.WaitAsync(token);
        try
        {
            for (int attempt = 0; attempt <= LastAttempt; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    if (process == null || process.HasExited)
                    {
                        Start(forceCpu ? LastAttempt : attempt);
                    }

                    var current = process ?? throw new IOException("El motor no está disponible.");
                    using var registration = token.Register(() => TryKill(current));
                    await current.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request));
                    return await ReadTranscript(current, progress, token, percent);
                }
                catch (RequestRejectedException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    Dispose();
                    throw;
                }
                catch
                {
                    Dispose();
                    token.ThrowIfCancellationRequested();
                    if (attempt == LastAttempt)
                    {
                        throw;
                    }

                    progress?.Invoke("\0RESET");
                    percent?.Invoke(0);
                }
            }
            throw new IOException("Motor no disponible.");
        }
        finally
        {
            sessionLock.Release();
        }
    }

    private async Task<string> ReadTranscript(Process current, Action<string>? progress, CancellationToken token, Action<double>? percent)
    {
        var transcript = new StringBuilder();
        while (true)
        {
            var line = await current.StandardOutput.ReadLineAsync(token).AsTask().WaitAsync(TimeSpan.FromMinutes(5), token);
            if (line == null)
            {
                throw new IOException("El motor se cerró inesperadamente.");
            }

            var reply = JsonSerializer.Deserialize<EngineReply>(line) ?? throw new IOException("Respuesta inválida del motor.");
            switch (reply.Kind)
            {
                case "request-error":
                    throw new RequestRejectedException(reply.Text);
                case "error":
                    throw new InvalidOperationException(reply.Text);
                case "backend":
                    Backend = reply.Text;
                    break;
                case "progress":
                    if (double.TryParse(reply.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                        && double.IsFinite(value))
                    {
                        percent?.Invoke(value);
                    }
                    break;
                case "done":
                    percent?.Invoke(100);
                    return transcript.ToString().Trim();
                case "text":
                    transcript.AppendLine(reply.Text);
                    progress?.Invoke(reply.Text);
                    break;
            }
        }
    }

    private void Start(int attempt)
    {
        Dispose();
        string executable = Environment.ProcessPath ?? throw new IOException("Ruta del ejecutable no disponible.");
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        info.ArgumentList.Add("--worker");
        if (attempt == LastAttempt)
        {
            info.ArgumentList.Add("--cpu");
        }
        else if (attempt == 1)
        {
            info.ArgumentList.Add("--integrated");
        }

        var current = Process.Start(info) ?? throw new IOException("No se pudo iniciar el motor.");
        process = current;
        _ = Task.Run(() => DrainErrors(current));
        try
        {
            ApplyWorkerLimits(current);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void ApplyWorkerLimits(Process current)
    {
        job = Native.CreateJobObject(IntPtr.Zero, null);
        if (job == IntPtr.Zero)
        {
            throw new IOException("No se pudo crear el aislamiento del motor.");
        }

        const uint killOnClose = 0x2000;
        const uint limitProcessMemory = 0x100;
        const uint limitProcessCount = 0x8;
        var limits = new Native.ExtendedLimits();
        limits.Basic.LimitFlags = killOnClose | limitProcessMemory | limitProcessCount;
        limits.Basic.ActiveProcessLimit = 1;
        limits.ProcessMemoryLimit = (UIntPtr)WorkerMemoryLimit;

        int size = Marshal.SizeOf(limits);
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(limits, buffer, false);
            if (!Native.SetInformationJobObject(job, 9, buffer, (uint)size) || !Native.AssignProcessToJobObject(job, current.Handle))
            {
                throw new IOException("No se pudo aplicar el límite de recursos al motor.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static async Task DrainErrors(Process current)
    {
        try
        {
            while (await current.StandardError.ReadLineAsync() != null)
            {
                // Consume stderr so the worker cannot block on a full pipe.
            }
        }
        catch
        {
            // The worker can be disposed while its error stream is being read.
        }
    }

    private static void TryKill(Process current)
    {
        try
        {
            if (!current.HasExited)
            {
                current.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // The worker may have exited between the check and Kill.
        }
    }

    public void Dispose()
    {
        Process? current = process;
        process = null;
        if (current != null)
        {
            TryKill(current);
            current.Dispose();
        }

        if (job != IntPtr.Zero)
        {
            Native.CloseHandle(job);
            job = IntPtr.Zero;
        }
    }
}
