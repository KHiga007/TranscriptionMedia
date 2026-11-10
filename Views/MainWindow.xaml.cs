using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

using NAudio.Wave;


namespace TranscriptionMedia;

public partial class MainWindow : Window, IComponentConnector
{
	private readonly Button homeRecord = new Button();

	private readonly Button homeUpload = new Button();

	private readonly Button homeHistory = new Button();

	private readonly Button homeNav = new Button
	{
		Content = "\ue80f",
		ToolTip = "Inicio"
	};

	private readonly Button recordNav = new Button
	{
		Content = IconText("\ue720", "Grabar")
	};

	private readonly Button uploadNav = new Button
	{
		Content = IconText("\ue898", "Subir audio")
	};

	private readonly Button historyNav = new Button
	{
		Content = IconText("\ue81c", "Historial")
	};

	private readonly Border navBar = new Border();

	private bool historyFailed;
    private long? displayedHistoryId;

	private readonly Grid homeView = new Grid();

	private readonly Grid workspaceView = new Grid();

	private readonly Grid uploadView = new Grid();

	private readonly Grid historyView = new Grid();

	private readonly TextBlock workTitle = new TextBlock
	{
		Text = "Transcripción",
		FontSize = 13.5,
		FontWeight = FontWeights.SemiBold,
		TextTrimming = TextTrimming.CharacterEllipsis,
		Margin = new Thickness(0.0, 0.0, 0.0, 2.0)
	};

	private readonly ComboBox micPicker = new ComboBox
	{
		Width = 250.0,
		DisplayMemberPath = "Name"
	};

	private readonly Border statusCard = new Border();

	private readonly ProgressBar statusProgress = new ProgressBar
	{
		Minimum = 0.0,
		Maximum = 100.0,
		Height = 4.0,
		Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
		Visibility = Visibility.Collapsed
	};

	private readonly Button cancelRecording = new Button
	{
		Content = "Cancelar",
		Visibility = Visibility.Collapsed
	};

	private readonly DockPanel micRow = new DockPanel
	{
		Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
	};

	private readonly TextBlock meta = new TextBlock
	{
		Text = "Sin conexión",
		FontSize = 12.0,
		VerticalAlignment = VerticalAlignment.Center,
		TextTrimming = TextTrimming.CharacterEllipsis
	};

	private readonly Border recordingControls = new Border();

	private readonly Border progressPanel = new Border();

	private readonly TextBlock timerText = new TextBlock
	{
		Text = "00:00",
		FontFamily = new FontFamily("Cascadia Mono, Consolas"),
		FontSize = 22.0,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Rectangle[] meterBars = new Rectangle[28];

	private readonly double[] meterLevels = new double[28];

	private readonly DispatcherTimer meterTimer = new DispatcherTimer
	{
		Interval = TimeSpan.FromMilliseconds(80.0)
	};

	private readonly Stopwatch recordWatch = new Stopwatch();

	private volatile float micLevel;

	private bool recording;

	private readonly Stopwatch transcribeWatch = new Stopwatch();

	private readonly Button newAudio = new Button
	{
		Content = IconText("\ue898", "Elegir otro audio"),
		Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
		Visibility = Visibility.Collapsed
	};

	private readonly TextBlock dropHint = new TextBlock
	{
		FontSize = 12.5,
		HorizontalAlignment = HorizontalAlignment.Center,
		Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
	};

	private readonly TextBlock searchPlaceholder = new TextBlock
	{
		Text = "Buscar en el historial",
		FontSize = 14.0,
		IsHitTestVisible = false,
		VerticalAlignment = VerticalAlignment.Center,
		Margin = new Thickness(36.0, 0.0, 0.0, 0.0)
	};

	private readonly TextBlock historyCount = new TextBlock
	{
		FontSize = 12.5,
		VerticalAlignment = VerticalAlignment.Center,
		TextTrimming = TextTrimming.CharacterEllipsis
	};

	private readonly TextBlock historyEmpty = new TextBlock
	{
		FontSize = 13.0,
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center,
		IsHitTestVisible = false
	};

	private readonly Button clearHistory = new Button
	{
		Content = "Borrar todo"
	};

	private static readonly Brush Purple = Palette.Primary;

	private static readonly Brush PanelColor = Palette.Surface;

	private static readonly Brush Muted = Palette.Muted;

	private static readonly Brush Lines = Palette.Border;

	private static readonly FontFamily Icons = new FontFamily("Segoe MDL2 Assets");

	private readonly TextBox transcript = new TextBox
	{
		IsReadOnly = true,
		AcceptsReturn = true,
		TextWrapping = TextWrapping.Wrap,
		VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
		FontSize = 15.0
	};

	private readonly TextBox search = new TextBox
	{
		ToolTip = "Buscar en el historial"
	};

	private readonly ListBox history = new ListBox
	{
		DisplayMemberPath = "Label"
	};

	private readonly TextBlock status = new TextBlock
	{
		Text = "Preparando…",
		TextTrimming = TextTrimming.CharacterEllipsis
	};

	private readonly Button record = new Button
	{
		Content = "\ue720",
		Tag = "Grabar"
	};

	private readonly Button pause = new Button
	{
		Content = "\ue769",
		Tag = "Pausar",
		IsEnabled = false
	};

	private readonly Button stop = new Button
	{
		Content = "\ue71a",
		Tag = "Detener",
		IsEnabled = false
	};

	private readonly Button file = new Button
	{
		Content = "Abrir audio…"
	};

	private readonly Button cancel = new Button
	{
		Content = "Cancelar",
		IsEnabled = false
	};

	private readonly Button confirm = new Button
	{
		Content = "Usar este audio y transcribir",
		IsEnabled = false
	};

	private readonly TextBlock selectedLabel = new TextBlock
	{
		Text = "Ningún audio seleccionado",
		TextTrimming = TextTrimming.CharacterEllipsis,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly ProgressBar progressBar = new ProgressBar
	{
		Minimum = 0.0,
		Maximum = 100.0,
		Height = 12.0,
		Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
	};

	private readonly TextBlock progressLabel = new TextBlock
	{
		Text = "Sin tareas pendientes",
		Foreground = Brushes.LightGray
	};

	private string? selectedAudio;

	private List<Entry> entries = new();

	private readonly TranscriptionController controller = new();

	private readonly object audioLock = new object();

	private readonly List<short[]> recorded = new List<short[]>();

	private long recordedSamples;

	private const long MaxRecordedSamples = 115200000L;

	private WaveInEvent? mic;

	private CancellationTokenSource? cancellation;

	private bool suppressCompletionAlert;

	private Task? activeFileTask;

	private bool busy;

	private volatile bool paused;

	private string backend = "";

	private void ShowSection(Grid view, Button active)
	{
		if (view != homeView && SizeToContent != SizeToContent.Manual)
		{
			UpdateLayout();
			double actualHeight = ActualHeight;
			SizeToContent = SizeToContent.Manual;
			Height = actualHeight;
		}
		homeView.Visibility = ((view != homeView) ? Visibility.Collapsed : Visibility.Visible);
		workspaceView.Visibility = ((view != workspaceView) ? Visibility.Collapsed : Visibility.Visible);
		uploadView.Visibility = ((view != uploadView) ? Visibility.Collapsed : Visibility.Visible);
		historyView.Visibility = ((view != historyView) ? Visibility.Collapsed : Visibility.Visible);
		navBar.Visibility = ((view == homeView) ? Visibility.Collapsed : Visibility.Visible);
		Button[] array = new Button[4] { homeNav, recordNav, uploadNav, historyNav };
		foreach (Button obj in array)
		{
			obj.Tag = ((obj == active) ? "active" : null);
		}
	}

	private void ShowHome()
	{
		if (!busy)
		{
			if (WindowState == WindowState.Maximized)
			{
				WindowState = WindowState.Normal;
			}
			Point? center = CurrentCenter();
			ShowSection(homeView, homeNav);
			MinWidth = 540.0;
			MinHeight = 0.0;
			Width = 580.0;
			SizeToContent = SizeToContent.Height;
			Recenter(center);
		}
	}

	private void ShowRecord()
	{
		if (recording || !busy)
		{
			ShowWorkspace(microphone: true, "Micrófono");
		}
	}

	private void ShowWorkspace(bool microphone, string title, bool showProgress = true)
	{
		MainWindow mainWindow = this;
		Grid view = workspaceView;
		Button active;
		if (microphone)
		{
			active = recordNav;
		}
		else
		{
			active = (showProgress ? uploadNav : historyNav);
		}
		mainWindow.ShowSection(view, active);
		recordingControls.Visibility = ((!microphone) ? Visibility.Collapsed : Visibility.Visible);
		progressPanel.Visibility = ((!(!microphone & showProgress)) ? Visibility.Collapsed : Visibility.Visible);
		micRow.Visibility = ((!microphone) ? Visibility.Collapsed : Visibility.Visible);
		newAudio.Visibility = Visibility.Collapsed;
		workTitle.Text = title;
		if (microphone && !busy)
		{
			recording = false;
			paused = false;
			recordWatch.Reset();
			Array.Clear(meterLevels);
			UpdateMeter();
			UpdateRecordingButtons();
			transcript.Clear();
			RefreshMicrophones();
			status.Text = "Pulsá Grabar para empezar.";
		}
	}

	private void ShowUpload()
	{
		if (!busy)
		{
			ShowSection(uploadView, uploadNav);
			UpdateUploadLabels();
			confirm.IsEnabled = selectedAudio != null;
		}
	}

	public MainWindow()
	{
		InitializeComponent();
		InitializeCompactUi();
		record.Click += Record;
		pause.Click += (object _, RoutedEventArgs _) =>
		{
			paused = !paused;
			UpdateRecordingButtons();
			UpdateMeter();
			status.Text = (paused ? "Grabación pausada" : "Grabando · La transcripción empieza al pulsar Detener.");
		};
		stop.Click += Stop;
		file.Click += OpenFile;
		confirm.Click += ConfirmFile;
		cancel.Click += (object _, RoutedEventArgs _) =>
		{
			cancellation?.Cancel();
		};
		search.TextChanged += (object _, TextChangedEventArgs _) =>
		{
			Filter();
		};
		Closing += OnClosing;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			Rect workArea = SystemParameters.WorkArea;
			Recenter(new Point(workArea.Left + workArea.Width / 2.0, workArea.Top + workArea.Height / 2.0));
			try
			{
				controller.OpenHistory();
				Refresh();
				status.Text = "Listo · Sin conexión · Historial protegido por Windows";
			}
			catch (Exception ex)
			{
				status.Text = "No se puede abrir el historial: " + ex.Message;
				meta.Text = status.Text;
				meta.Foreground = Palette.Danger;
				historyFailed = true;
				homeHistory.IsEnabled = false;
				historyNav.IsEnabled = false;
				homeHistory.ToolTip = status.Text;
				Button button = record;
				Button button2 = file;
				Button button3 = homeRecord;
				bool flag = (homeUpload.IsEnabled = false);
				bool flag3 = flag;
				flag = (button3.IsEnabled = flag3);
				bool flag5 = flag;
				flag = (button2.IsEnabled = flag5);
				bool isEnabled = flag;
				button.IsEnabled = isEnabled;
			}
		};
	}

	private void SetBusy(bool value)
	{
		busy = value;
		Button button = record;
		bool flag = (file.IsEnabled = !value);
		bool isEnabled = flag;
		button.IsEnabled = isEnabled;
		cancel.IsEnabled = value;
		history.IsEnabled = !value;
		confirm.IsEnabled = !value && selectedAudio != null;
		homeRecord.IsEnabled = !value;
		homeUpload.IsEnabled = !value;
		homeHistory.IsEnabled = !value && !historyFailed;
		Button[] array = new Button[4] { homeNav, recordNav, uploadNav, historyNav };
		foreach (Button button2 in array)
		{
			button2.IsEnabled = !value || object.Equals(button2.Tag, "active");
		}
		historyNav.IsEnabled &= !historyFailed;
		UpdateHistoryState();
	}

	private string TranscribedIn()
	{
		transcribeWatch.Stop();
		int num = Math.Max(1, (int)Math.Round(transcribeWatch.Elapsed.TotalSeconds));
		if (num < 60)
		{
			if (num != 1)
			{
				return $"Texto transcrito en {num} segundos";
			}
			return "Texto transcrito en 1 segundo";
		}
		return $"Texto transcrito en {num / 60} min {num % 60} s";
	}

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		if (busy)
		{
			e.Cancel = true;
			status.Text = "Detené o cancelá la transcripción antes de cerrar.";
			if (!recording)
			{
				progressLabel.Text = status.Text;
			}
		}
		else
		{
			controller.Dispose();
		}
	}

}
