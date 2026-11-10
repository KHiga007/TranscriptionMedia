using System.Windows.Threading;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace TranscriptionMedia;

public partial class MainWindow
{
	private struct FrameMargins
	{
		public int Left;

		public int Right;

		public int Top;

		public int Bottom;
	}

	private Point? CurrentCenter()
	{
		if (!IsLoaded || WindowState != WindowState.Normal)
		{
			return null;
		}
		return new Point(Left + ActualWidth / 2.0, Top + ActualHeight / 2.0);
	}

	private void Recenter(Point? center)
	{
		if (center.HasValue)
		{
			Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() =>
			{
				Left = center.Value.X - ActualWidth / 2.0;
				Top = Math.Max(SystemParameters.VirtualScreenTop, center.Value.Y - ActualHeight / 2.0);
			}));
		}
	}

	private void ApplyFrame(Window window, double captionHeight)
	{
		window.WindowStyle = WindowStyle.None;
		WindowChrome.SetWindowChrome(window, new WindowChrome
		{
			CaptionHeight = captionHeight,
			ResizeBorderThickness = new Thickness(6.0),
			GlassFrameThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(12.0),
			UseAeroCaptionButtons = false
		});
		window.SourceInitialized += (object? _, EventArgs _) =>
		{
			EnableNativeShadow(window);
		};
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmExtendFrameIntoClientArea(nint hwnd, ref FrameMargins margins);

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

	private static void EnableNativeShadow(Window window)
	{
		nint handle = new WindowInteropHelper(window).Handle;
		int value = 2;
		DwmSetWindowAttribute(handle, 2, ref value, 4);
		FrameMargins margins = new FrameMargins
		{
			Left = 1,
			Right = 1,
			Top = 1,
			Bottom = 1
		};
		DwmExtendFrameIntoClientArea(handle, ref margins);
		int value2 = 2;
		DwmSetWindowAttribute(handle, 33, ref value2, 4);
	}

	private StackPanel WindowControls(Window window, bool main)
	{
		StackPanel controls = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Stretch
		};
		if (main)
		{
			Control("\ue921", "Minimizar").Click += (object _, RoutedEventArgs _) =>
			{
				window.WindowState = WindowState.Minimized;
			};
			Button maximize = Control("\ue922", "Maximizar");
			maximize.Click += (object _, RoutedEventArgs _) =>
			{
				window.WindowState = ((window.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
			};
			window.StateChanged += (object? _, EventArgs _) =>
			{
				bool flag = window.WindowState == WindowState.Maximized;
				maximize.Content = (flag ? "\ue923" : "\ue922");
				maximize.ToolTip = (flag ? "Restaurar" : "Maximizar");
				AutomationProperties.SetName(maximize, flag ? "Restaurar" : "Maximizar");
			};
		}
		Button close = Control("\ue8bb", "Cerrar");
		close.MouseEnter += (object _, MouseEventArgs _) =>
		{
			close.Background = Palette.CloseHover;
			close.Foreground = Brushes.White;
		};
		close.Click += (object _, RoutedEventArgs _) =>
		{
			window.Close();
		};
		return controls;
		Button Control(string glyph, string name)
		{
			Button button = new Button
			{
				Content = glyph,
				Width = 46.0,
				Padding = new Thickness(0.0),
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0.0),
				Foreground = Muted,
				FontFamily = Icons,
				FontSize = 10.0,
				FontWeight = FontWeights.Normal,
				ToolTip = name
			};
			button.MouseEnter += (object _, MouseEventArgs _) =>
			{
				button.Background = Lines;
			};
			button.MouseLeave += (object _, MouseEventArgs _) =>
			{
				button.Background = Brushes.Transparent;
				button.Foreground = Muted;
			};
			AutomationProperties.SetName(button, name);
			WindowChrome.SetIsHitTestVisibleInChrome(button, hitTestVisible: true);
			controls.Children.Add(button);
			return button;
		}
	}

	private void FrameContent(Window window, UIElement content, bool main)
	{
		window.Content = null;
		DockPanel dockPanel = new DockPanel();
		DockPanel dockPanel2 = new DockPanel
		{
			Height = (main ? 40.0 : 48.0),
			Background = (main ? Palette.Chrome : Palette.Surface)
		};
		StackPanel element = WindowControls(window, main);
		DockPanel.SetDock(element, Dock.Right);
		dockPanel2.Children.Add(element);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(main ? 14.0 : 22.0, 0.0, 8.0, 0.0)
		};
		if (main)
		{
			stackPanel.Children.Add(new Border
			{
				Width = 22.0,
				Height = 22.0,
				CornerRadius = new CornerRadius(6.0),
				Background = Purple,
				Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
				Child = new System.Windows.Shapes.Path
				{
					Data = Geometry.Parse("M 2,9 L 2,15 M 7,5 L 7,19 M 12,2 L 12,22 M 17,6 L 17,18 M 22,10 L 22,14"),
					Stroke = Palette.OnPrimary,
					StrokeThickness = 1.7,
					StrokeStartLineCap = PenLineCap.Round,
					StrokeEndLineCap = PenLineCap.Round,
					Width = 14.0,
					Height = 14.0,
					Stretch = Stretch.Uniform
				}
			});
		}
		stackPanel.Children.Add(new TextBlock
		{
			Text = window.Title,
			Foreground = Palette.Text,
			FontSize = (main ? 12.5 : 15.0),
			FontWeight = (main ? FontWeights.SemiBold : FontWeights.SemiBold),
			VerticalAlignment = VerticalAlignment.Center
		});
		dockPanel2.Children.Add(stackPanel);
		Border element2 = new Border
		{
			BorderBrush = Lines,
			BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0),
			Child = dockPanel2
		};
		DockPanel.SetDock(element2, Dock.Top);
		dockPanel.Children.Add(element2);
		dockPanel.Children.Add(content);
		Border edge = new Border
		{
			Background = window.Background,
			BorderBrush = Palette.BorderStrong,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Child = dockPanel,
			ClipToBounds = true
		};
		window.Content = edge;
		window.Activated += (object? _, EventArgs _) =>
		{
			edge.BorderBrush = Palette.BorderStrong;
		};
		window.Deactivated += (object? _, EventArgs _) =>
		{
			edge.BorderBrush = Lines;
		};
		ApplyFrame(window, main ? 40.0 : 48.0);
	}
}
