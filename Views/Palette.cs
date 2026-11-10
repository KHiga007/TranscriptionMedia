using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace TranscriptionMedia;

internal static class Palette
{
	public static readonly bool Dark = ReadDark();

	public static readonly Brush Bg = B("#FFFFFF", "#1A1724");

	public static readonly Brush Chrome = B("#F7F6FB", "#211E2E");

	public static readonly Brush Surface = B("#FFFFFF", "#1A1724");

	public static readonly Brush Surface2 = B("#F7F6FB", "#211E2E");

	public static readonly Brush Border = B("#DFDCEA", "#302C42");

	public static readonly Brush BorderStrong = B("#C9C4DB", "#423C58");

	public static readonly Brush Text = B("#1E1A2C", "#EBE8F4");

	public static readonly Brush Muted = B("#686379", "#A39EB8");

	public static readonly Brush Primary = B("#5A46D4", "#9A89F4");

	public static readonly Brush PrimaryHover = B("#4935BF", "#AB9CF7");

	public static readonly Brush OnPrimary = B("#FFFFFF", "#17132B");

	public static readonly Brush PrimarySubtle = B("#ECE9FC", "#29234A");

	public static readonly Brush PrimaryText = B("#4733BD", "#B8ABFA");

	public static readonly Brush Danger = B("#C4314B", "#F0778C");

	public static readonly Brush DangerSubtle = B("#FBE9EC", "#3A1D26");

	public static readonly Brush Success = B("#17804F", "#5FD09A");

	public static readonly Brush SuccessSubtle = B("#E3F4EC", "#16302A");

	public static readonly Brush Rec = B("#D93448", "#E5566A");

	public static readonly Brush CloseHover = B("#C42B1C", "#C42B1C");

	public static void Apply(ResourceDictionary resources)
	{
		resources["Bg"] = Bg;
		resources["Chrome"] = Chrome;
		resources["Surface"] = Surface;
		resources["Surface2"] = Surface2;
		resources["Border"] = Border;
		resources["BorderStrong"] = BorderStrong;
		resources["Text"] = Text;
		resources["Muted"] = Muted;
		resources["Primary"] = Primary;
		resources["PrimaryHover"] = PrimaryHover;
		resources["OnPrimary"] = OnPrimary;
		resources["PrimarySubtle"] = PrimarySubtle;
		resources["PrimaryText"] = PrimaryText;
		resources["Danger"] = Danger;
		resources["DangerSubtle"] = DangerSubtle;
		resources["Success"] = Success;
		resources["SuccessSubtle"] = SuccessSubtle;
		resources["Rec"] = Rec;
	}

	private static bool ReadDark()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
			return registryKey?.GetValue("AppsUseLightTheme") is int num && num == 0;
		}
		catch
		{
			return false;
		}
	}

	private static Brush B(string light, string dark)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Dark ? dark : light));
		solidColorBrush.Freeze();
		return solidColorBrush;
	}
}
