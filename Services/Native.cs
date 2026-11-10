using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace TranscriptionMedia;

internal static class Native
{
	public struct BasicLimits
	{
		public long PerProcessUserTimeLimit;

		public long PerJobUserTimeLimit;

		public uint LimitFlags;

		public nuint MinimumWorkingSetSize;

		public nuint MaximumWorkingSetSize;

		public uint ActiveProcessLimit;

		public nuint Affinity;

		public uint PriorityClass;

		public uint SchedulingClass;
	}

	public struct IoCounters
	{
		public ulong a;

		public ulong b;

		public ulong c;

		public ulong d;

		public ulong e;

		public ulong f;
	}

	public struct ExtendedLimits
	{
		public BasicLimits Basic;

		public IoCounters Io;

		public nuint ProcessMemoryLimit;

		public nuint JobMemoryLimit;

		public nuint PeakProcessMemoryUsed;

		public nuint PeakJobMemoryUsed;
	}

	private struct Label
	{
		public nint Sid;

		public uint Attributes;
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	public static extern nint CreateJobObject(nint attributes, string? name);

	[DllImport("kernel32.dll")]
	public static extern bool SetInformationJobObject(nint job, int type, nint info, uint size);

	[DllImport("kernel32.dll")]
	public static extern bool AssignProcessToJobObject(nint job, nint process);

	[DllImport("kernel32.dll")]
	public static extern bool CloseHandle(nint handle);

	[DllImport("kernel32.dll")]
	private static extern nint LocalFree(nint ptr);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool OpenProcessToken(nint process, uint access, out nint token);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool ConvertStringSidToSid(string text, out nint sid);

	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool SetTokenInformation(nint token, int type, ref Label info, int length);

	[DllImport("advapi32.dll")]
	private static extern int GetLengthSid(nint sid);

	public static void LowIntegrity()
	{
		if (!OpenProcessToken(Process.GetCurrentProcess().Handle, 136u, out var token))
		{
			throw new IOException("No se pudo restringir el motor.");
		}
		nint sid = IntPtr.Zero;
		try
		{
			if (!ConvertStringSidToSid("S-1-16-4096", out sid))
			{
				throw new IOException("No se pudo restringir el motor.");
			}
			Label info = new Label
			{
				Sid = sid,
				Attributes = 32u
			};
			if (!SetTokenInformation(token, 25, ref info, Marshal.SizeOf<Label>() + GetLengthSid(sid)))
			{
				throw new IOException("No se pudo restringir el motor.");
			}
		}
		finally
		{
			if (sid != IntPtr.Zero)
			{
				LocalFree(sid);
			}
			CloseHandle(token);
		}
	}
}
