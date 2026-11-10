using System.Runtime.InteropServices;

namespace TranscriptionMedia;

internal static class VulkanDevice
{
	private struct CreateInfo
	{
		public int Type;

		public nint Next;

		public uint Flags;

		public nint App;

		public uint Layers;

		public nint LayerNames;

		public uint Extensions;

		public nint ExtensionNames;
	}

	public static string Name = "GPU";

	[DllImport("vulkan-1.dll")]
	private static extern int vkCreateInstance(ref CreateInfo info, nint allocator, out nint instance);

	[DllImport("vulkan-1.dll")]
	private static extern int vkEnumeratePhysicalDevices(nint instance, ref uint count, nint devices);

	[DllImport("vulkan-1.dll")]
	private static extern void vkGetPhysicalDeviceProperties(nint device, nint properties);

	[DllImport("vulkan-1.dll")]
	private static extern void vkDestroyInstance(nint instance, nint allocator);

	public static int Best(bool integrated)
	{
		nint instance = IntPtr.Zero;
		nint num = IntPtr.Zero;
		nint num2 = IntPtr.Zero;
		try
		{
			CreateInfo info = new CreateInfo
			{
				Type = 1
			};
			if (vkCreateInstance(ref info, IntPtr.Zero, out instance) != 0)
			{
				return -1;
			}
			uint count = 0u;
			vkEnumeratePhysicalDevices(instance, ref count, IntPtr.Zero);
			if (count == 0 || count > 32)
			{
				return -1;
			}
			num = Marshal.AllocHGlobal((int)count * IntPtr.Size);
			num2 = Marshal.AllocHGlobal(4096);
			if (vkEnumeratePhysicalDevices(instance, ref count, num) != 0)
			{
				return -1;
			}
			int result = -1;
			int num3 = 0;
			for (int i = 0; i < count; i++)
			{
				vkGetPhysicalDeviceProperties(Marshal.ReadIntPtr(num, i * IntPtr.Size), num2);
				int num4 = Marshal.ReadInt32(num2, 16);
				int num5 = ((num4 == 2) ? 2 : ((num4 == 1) ? 1 : 0));
				if (num5 > num3 && (!integrated || num4 == 1))
				{
					num3 = num5;
					result = i;
					Name = ((num4 == 2) ? "GPU dedicada · " : "GPU integrada · ") + Marshal.PtrToStringUTF8(num2 + 20);
				}
			}
			return result;
		}
		catch
		{
			return -1;
		}
		finally
		{
			if (num2 != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(num2);
			}
			if (num != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(num);
			}
			if (instance != IntPtr.Zero)
			{
				vkDestroyInstance(instance, IntPtr.Zero);
			}
		}
	}
}
