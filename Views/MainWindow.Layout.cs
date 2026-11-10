using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace TranscriptionMedia;

public partial class MainWindow
{
	private void InitializeCompactUi()
	{
		Palette.Apply(Resources);
		Background = Palette.Bg;
		Foreground = Palette.Text;
		FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
		DockPanel dockPanel = new DockPanel();
		FrameContent(this, dockPanel, main: true);
		DockPanel dockPanel2 = new DockPanel
		{
			Margin = new Thickness(12.0, 6.0, 16.0, 6.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(12.0, 0.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(new Ellipse
		{
			Width = 7.0,
			Height = 7.0,
			Fill = Palette.Success,
			Margin = new Thickness(0.0, 1.0, 7.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		});
		meta.Foreground = Muted;
		stackPanel.Children.Add(meta);
		DockPanel.SetDock(stackPanel, Dock.Right);
		dockPanel2.Children.Add(stackPanel);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		Button[] array = new Button[4] { homeNav, recordNav, uploadNav, historyNav };
		foreach (Button button in array)
		{
			button.Style = (Style)FindResource("NavButton");
			button.Margin = new Thickness(0.0, 0.0, 2.0, 0.0);
			stackPanel2.Children.Add(button);
		}
		homeNav.FontFamily = Icons;
		homeNav.FontSize = 14.0;
		homeNav.Padding = new Thickness(10.0, 7.0, 10.0, 7.0);
		AutomationProperties.SetName(homeNav, "Inicio");
		dockPanel2.Children.Add(stackPanel2);
		navBar.BorderBrush = Lines;
		navBar.BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0);
		navBar.Child = dockPanel2;
		DockPanel.SetDock(navBar, Dock.Top);
		dockPanel.Children.Add(navBar);
		Grid grid = new Grid
		{
			Margin = new Thickness(24.0, 18.0, 24.0, 20.0)
		};
		grid.Children.Add(homeView);
		grid.Children.Add(workspaceView);
		grid.Children.Add(uploadView);
		grid.Children.Add(historyView);
		dockPanel.Children.Add(grid);
		BuildHome();
		BuildWorkspace();
		BuildUpload();
		BuildHistory();
		homeNav.Click += (object _, RoutedEventArgs _) =>
		{
			ShowHome();
		};
		homeRecord.Click += (object _, RoutedEventArgs _) =>
		{
			ShowRecord();
		};
		recordNav.Click += (object _, RoutedEventArgs _) =>
		{
			ShowRecord();
		};
		homeUpload.Click += (object _, RoutedEventArgs _) =>
		{
			ShowUpload();
		};
		uploadNav.Click += (object _, RoutedEventArgs _) =>
		{
			ShowUpload();
		};
		newAudio.Click += (object _, RoutedEventArgs _) =>
		{
			ShowUpload();
		};
		homeHistory.Click += (object _, RoutedEventArgs _) =>
		{
			ShowHistory();
		};
		historyNav.Click += (object _, RoutedEventArgs _) =>
		{
			ShowHistory();
		};
		meterTimer.Tick += (object? _, EventArgs _) =>
		{
			UpdateMeter();
		};
		ShowHome();
	}

	private void BuildHome()
	{
		StackPanel stackPanel = new StackPanel();
		homeView.Children.Add(stackPanel);
		stackPanel.Children.Add(new TextBlock
		{
			Text = "¿Cómo querés empezar?",
			FontSize = 22.0,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 4.0, 0.0, 18.0)
		});
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(14.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		ConfigureChoice(homeRecord, "\ue720", "Grabar audio", "Usá el micrófono de tu computadora.");
		ConfigureChoice(homeUpload, "\ue898", "Subir audio", "Seleccioná una grabación existente.");
		Grid.SetColumn(homeUpload, 2);
		grid.Children.Add(homeRecord);
		grid.Children.Add(homeUpload);
		stackPanel.Children.Add(grid);
		Grid grid2 = new Grid
		{
			Margin = new Thickness(0.0, 22.0, 0.0, 22.0)
		};
		grid2.ColumnDefinitions.Add(new ColumnDefinition());
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(2.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition());
		Border element = new Border
		{
			Height = 1.0,
			Background = Lines
		};
		Grid.SetColumn(element, 1);
		grid2.Children.Add(element);
		stackPanel.Children.Add(grid2);
		ConfigureChoice(homeHistory, "\ue81c", "Historial", "Abrí una transcripción guardada.");
		homeHistory.HorizontalAlignment = HorizontalAlignment.Center;
		homeHistory.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
		{
			Source = homeRecord
		});
		stackPanel.Children.Add(homeHistory);
	}

	private void BuildWorkspace()
	{
		DockPanel dockPanel = new DockPanel();
		workspaceView.Children.Add(dockPanel);
		TextBlock element = new TextBlock
		{
			Text = "Micrófono",
			Foreground = Muted,
			FontSize = 12.5,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		};
		DockPanel.SetDock(element, Dock.Left);
		micRow.Children.Add(element);
		AutomationProperties.SetName(micPicker, "Micrófono");
		DockPanel.SetDock(micPicker, Dock.Left);
		micRow.Children.Add(micPicker);
		micRow.Children.Add(new Border());
		DockPanel.SetDock(micRow, Dock.Top);
		dockPanel.Children.Add(micRow);
		DockPanel dockPanel2 = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		Button button2 = new Button
		{
			Content = "\ue8c8",
			ToolTip = "Copiar texto"
		};
		Button button3 = new Button
		{
			Content = "\ue74e",
			ToolTip = "Guardar .txt"
		};
		newAudio.Content = "\ue898";
		newAudio.ToolTip = "Elegir otro audio";
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		Button[] array = new Button[3] { button2, button3, newAudio };
		foreach (Button button4 in array)
		{
			button4.FontFamily = Icons;
			button4.FontSize = 14.0;
			button4.FontWeight = FontWeights.Normal;
			button4.Width = 36.0;
			button4.Height = 36.0;
			button4.Padding = new Thickness(0.0);
			button4.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
			AutomationProperties.SetName(button4, (string)button4.ToolTip);
			stackPanel.Children.Add(button4);
		}
		DockPanel.SetDock(stackPanel, Dock.Right);
		dockPanel2.Children.Add(stackPanel);
		DockPanel dockPanel3 = new DockPanel();
		cancelRecording.Style = (Style)FindResource("DangerButton");
		cancelRecording.Padding = new Thickness(10.0, 3.0, 10.0, 3.0);
		cancelRecording.FontSize = 12.0;
		cancelRecording.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
		cancelRecording.VerticalAlignment = VerticalAlignment.Center;
		cancelRecording.Click += (object _, RoutedEventArgs _) =>
		{
			cancellation?.Cancel();
		};
		DockPanel.SetDock(cancelRecording, Dock.Right);
		dockPanel3.Children.Add(cancelRecording);
		StackPanel stackPanel2 = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center
		};
		status.Foreground = Palette.Text;
		status.FontSize = 12.5;
		stackPanel2.Children.Add(status);
		stackPanel2.Children.Add(statusProgress);
		dockPanel3.Children.Add(stackPanel2);
		statusCard.Child = dockPanel3;
		statusCard.Background = Palette.Surface2;
		statusCard.BorderBrush = Lines;
		statusCard.BorderThickness = new Thickness(1.0);
		statusCard.CornerRadius = new CornerRadius(8.0);
		statusCard.Padding = new Thickness(12.0, 7.0, 10.0, 7.0);
		statusCard.MinHeight = 36.0;
		dockPanel2.Children.Add(statusCard);
		DockPanel.SetDock(dockPanel2, Dock.Top);
		dockPanel.Children.Add(dockPanel2);
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.Children.Add(timerText);
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Height = 32.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(14.0, 0.0, 6.0, 0.0),
			ClipToBounds = true
		};
		for (int num = 0; num < meterBars.Length; num++)
		{
			meterBars[num] = new Rectangle
			{
				Width = 4.0,
				Height = 4.0,
				RadiusX = 2.0,
				RadiusY = 2.0,
				Margin = new Thickness(1.5, 0.0, 1.5, 0.0),
				Fill = Palette.BorderStrong,
				VerticalAlignment = VerticalAlignment.Center
			};
			stackPanel3.Children.Add(meterBars[num]);
		}
		Grid.SetColumn(stackPanel3, 1);
		grid.Children.Add(stackPanel3);
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center
		};
		array = new Button[3] { record, pause, stop };
		foreach (Button button5 in array)
		{
			button5.Style = (Style)FindResource("PillButton");
			AutomationProperties.SetName(button5, (string)button5.Tag);
			stackPanel4.Children.Add(button5);
		}
		array = new Button[2] { record, stop };
		foreach (Button obj in array)
		{
			obj.Background = Palette.Rec;
			obj.Foreground = Brushes.White;
			obj.BorderThickness = new Thickness(0.0);
		}
		Grid.SetColumn(stackPanel4, 2);
		grid.Children.Add(stackPanel4);
		recordingControls.Child = grid;
		recordingControls.Background = Palette.Surface2;
		recordingControls.BorderBrush = Lines;
		recordingControls.BorderThickness = new Thickness(1.0);
		recordingControls.CornerRadius = new CornerRadius(8.0);
		recordingControls.Height = 56.0;
		recordingControls.Padding = new Thickness(16.0, 0.0, 10.0, 0.0);
		recordingControls.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
		DockPanel.SetDock(recordingControls, Dock.Top);
		dockPanel.Children.Add(recordingControls);
		StackPanel stackPanel5 = new StackPanel();
		DockPanel dockPanel4 = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		cancel.Style = (Style)FindResource("DangerButton");
		cancel.Padding = new Thickness(10.0, 4.0, 10.0, 4.0);
		cancel.FontSize = 12.5;
		DockPanel.SetDock(cancel, Dock.Right);
		dockPanel4.Children.Add(cancel);
		progressLabel.Foreground = Muted;
		progressLabel.FontSize = 12.5;
		StackPanel stackPanel6 = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 12.0, 0.0)
		};
		stackPanel6.Children.Add(workTitle);
		stackPanel6.Children.Add(progressLabel);
		dockPanel4.Children.Add(stackPanel6);
		stackPanel5.Children.Add(dockPanel4);
		progressBar.Margin = new Thickness(0.0);
		stackPanel5.Children.Add(progressBar);
		progressPanel.Child = stackPanel5;
		progressPanel.Background = Palette.Surface2;
		progressPanel.BorderBrush = Lines;
		progressPanel.BorderThickness = new Thickness(1.0);
		progressPanel.CornerRadius = new CornerRadius(8.0);
		progressPanel.Padding = new Thickness(16.0, 10.0, 16.0, 14.0);
		progressPanel.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
		DockPanel.SetDock(progressPanel, Dock.Top);
		dockPanel.Children.Add(progressPanel);
		transcript.Background = Palette.Surface;
		transcript.Foreground = Palette.Text;
		transcript.BorderThickness = new Thickness(0.0);
		transcript.Padding = new Thickness(14.0, 10.0, 14.0, 10.0);
		dockPanel.Children.Add(new Border
		{
			Background = Palette.Surface,
			BorderBrush = Lines,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			ClipToBounds = true,
			Child = transcript
		});
		button2.Click += (object _, RoutedEventArgs _) =>
		{
			if (transcript.Text.Length > 0)
			{
				try
				{
					Clipboard.SetText(transcript.Text);
					Flash(button2, "\ue73e");
				}
				catch
				{
					Flash(button2, "\ue783");
				}
			}
		};
		button3.Click += (object _, RoutedEventArgs _) =>
		{
			if (transcript.Text.Length != 0)
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Filter = "Texto|*.txt",
					FileName = "transcripcion.txt"
				};
				if (saveFileDialog.ShowDialog(this) == true)
				{
					try
					{
						File.WriteAllText(saveFileDialog.FileName, transcript.Text, Encoding.UTF8);
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, "No se pudo guardar el archivo: " + ex.Message, "Guardar .txt", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					}
				}
			}
		};
	}

	private void BuildUpload()
	{
		DockPanel dockPanel = new DockPanel();
		uploadView.Children.Add(dockPanel);
		TextBlock element = new TextBlock
		{
			Text = "Elegí el archivo que querés convertir en texto.",
			Foreground = Muted,
			FontSize = 13.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		DockPanel.SetDock(element, Dock.Top);
		dockPanel.Children.Add(element);
		DockPanel dockPanel2 = new DockPanel
		{
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
		};
		confirm.Content = "Usar este audio y transcribir";
		confirm.Style = (Style)FindResource("PrimaryButton");
		confirm.VerticalAlignment = VerticalAlignment.Center;
		DockPanel.SetDock(confirm, Dock.Right);
		dockPanel2.Children.Add(confirm);
		dockPanel2.Children.Add(new TextBlock
		{
			Text = "WAV, MP3, M4A, AAC, WMA, FLAC, OGG, OGA y OPUS\nHasta 2 GB y 12 horas",
			Foreground = Muted,
			FontSize = 11.5,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 12.0, 0.0)
		});
		DockPanel.SetDock(dockPanel2, Dock.Bottom);
		dockPanel.Children.Add(dockPanel2);
		Rectangle dash = new Rectangle
		{
			Stroke = Palette.BorderStrong,
			StrokeThickness = 1.5,
			StrokeDashArray = new DoubleCollection { 5.0, 3.0 },
			RadiusX = 8.0,
			RadiusY = 8.0
		};
		Button button = new Button
		{
			Style = (Style)FindResource("HomeChoiceStyle"),
			Padding = new Thickness(18.0),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		AutomationProperties.SetName(button, "Elegir archivo de audio");
		button.MouseEnter += (object _, MouseEventArgs _) =>
		{
			dash.Stroke = Purple;
		};
		button.MouseLeave += (object _, MouseEventArgs _) =>
		{
			dash.Stroke = Palette.BorderStrong;
		};
		StackPanel stackPanel = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.Children.Add(new Border
		{
			Width = 46.0,
			Height = 46.0,
			CornerRadius = new CornerRadius(23.0),
			Background = Palette.PrimarySubtle,
			Child = new TextBlock
			{
				Text = "\ue898",
				FontFamily = Icons,
				FontSize = 20.0,
				Foreground = Palette.PrimaryText,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		});
		selectedLabel.FontWeight = FontWeights.SemiBold;
		selectedLabel.FontSize = 14.5;
		selectedLabel.TextWrapping = TextWrapping.Wrap;
		selectedLabel.TextTrimming = TextTrimming.CharacterEllipsis;
		selectedLabel.TextAlignment = TextAlignment.Center;
		selectedLabel.HorizontalAlignment = HorizontalAlignment.Center;
		selectedLabel.MaxHeight = 42.0;
		selectedLabel.MaxWidth = 420.0;
		selectedLabel.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		stackPanel.Children.Add(selectedLabel);
		dropHint.Foreground = Muted;
		stackPanel.Children.Add(dropHint);
		button.Content = stackPanel;
		button.Click += (object _, RoutedEventArgs _) =>
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Audio|*.wav;*.mp3;*.m4a;*.aac;*.wma;*.flac;*.ogg;*.oga;*.opus",
				CheckFileExists = true
			};
			if (openFileDialog.ShowDialog(this) == true)
			{
				SelectAudio(openFileDialog.FileName);
				UpdateUploadLabels();
			}
		};
		Grid grid = new Grid();
		grid.Children.Add(dash);
		grid.Children.Add(button);
		dockPanel.Children.Add(grid);
	}

	private void BuildHistory()
	{
		DockPanel dockPanel = new DockPanel();
		historyView.Children.Add(dockPanel);
		search.Margin = new Thickness(0.0);
		search.Padding = new Thickness(34.0, 7.0, 10.0, 7.0);
		searchPlaceholder.Foreground = Muted;
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		grid.Children.Add(search);
		grid.Children.Add(new TextBlock
		{
			Text = "\ue721",
			FontFamily = Icons,
			FontSize = 13.0,
			Foreground = Muted,
			IsHitTestVisible = false,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(12.0, 0.0, 0.0, 0.0)
		});
		grid.Children.Add(searchPlaceholder);
		DockPanel.SetDock(grid, Dock.Top);
		dockPanel.Children.Add(grid);
		DockPanel dockPanel2 = new DockPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		clearHistory.Style = (Style)FindResource("DangerButton");
		clearHistory.Click += (object _, RoutedEventArgs _) =>
		{
			Delete(all: true);
		};
		stackPanel.Children.Add(clearHistory);
		DockPanel.SetDock(stackPanel, Dock.Right);
		dockPanel2.Children.Add(stackPanel);
		historyCount.Foreground = Muted;
		dockPanel2.Children.Add(historyCount);
		DockPanel.SetDock(dockPanel2, Dock.Bottom);
		dockPanel.Children.Add(dockPanel2);
		historyEmpty.Foreground = Muted;
		Grid grid2 = new Grid();
		grid2.Children.Add(history);
		grid2.Children.Add(historyEmpty);
		dockPanel.Children.Add(new Border
		{
			BorderBrush = Lines,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Background = Palette.Surface,
			ClipToBounds = true,
			Child = grid2
		});
		history.DisplayMemberPath = "";
		history.Background = Palette.Surface;
		history.Foreground = Palette.Text;
		history.BorderThickness = new Thickness(0.0);
		FrameworkElementFactory frameworkElementFactory = new FrameworkElementFactory(typeof(DockPanel));
		FrameworkElementFactory frameworkElementFactory2 = new FrameworkElementFactory(typeof(Button));
		frameworkElementFactory2.SetValue(FrameworkElement.StyleProperty, (Style)FindResource("IconDangerButton"));
		frameworkElementFactory2.SetValue(ContentControl.ContentProperty, "\ue74d");
		frameworkElementFactory2.SetValue(FrameworkElement.ToolTipProperty, "Eliminar");
		frameworkElementFactory2.SetValue(AutomationProperties.NameProperty, "Eliminar");
		frameworkElementFactory2.SetValue(DockPanel.DockProperty, Dock.Right);
		frameworkElementFactory2.SetValue(FrameworkElement.MarginProperty, new Thickness(2.0, 0.0, 0.0, 0.0));
		frameworkElementFactory2.SetBinding(FrameworkElement.TagProperty, new Binding());
		frameworkElementFactory2.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(DeleteHistoryRow));
		frameworkElementFactory.AppendChild(frameworkElementFactory2);
		FrameworkElementFactory frameworkElementFactory3 = new FrameworkElementFactory(typeof(Button));
		frameworkElementFactory3.SetValue(FrameworkElement.StyleProperty, (Style)FindResource("IconButton"));
		frameworkElementFactory3.SetValue(ContentControl.ContentProperty, "\ue8a7");
		frameworkElementFactory3.SetValue(FrameworkElement.ToolTipProperty, "Abrir");
		frameworkElementFactory3.SetValue(AutomationProperties.NameProperty, "Abrir");
		frameworkElementFactory3.SetValue(DockPanel.DockProperty, Dock.Right);
		frameworkElementFactory3.SetValue(FrameworkElement.MarginProperty, new Thickness(10.0, 0.0, 0.0, 0.0));
		frameworkElementFactory3.SetBinding(FrameworkElement.TagProperty, new Binding());
		frameworkElementFactory3.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OpenHistoryRow));
		frameworkElementFactory.AppendChild(frameworkElementFactory3);
		FrameworkElementFactory frameworkElementFactory4 = new FrameworkElementFactory(typeof(TextBlock));
		frameworkElementFactory4.SetBinding(TextBlock.TextProperty, new Binding("Date"));
		frameworkElementFactory4.SetValue(DockPanel.DockProperty, Dock.Right);
		frameworkElementFactory4.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Cascadia Mono, Consolas"));
		frameworkElementFactory4.SetValue(TextBlock.FontSizeProperty, 11.0);
		frameworkElementFactory4.SetValue(TextBlock.ForegroundProperty, Muted);
		frameworkElementFactory4.SetValue(FrameworkElement.MarginProperty, new Thickness(10.0, 0.0, 0.0, 0.0));
		frameworkElementFactory4.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		frameworkElementFactory.AppendChild(frameworkElementFactory4);
		FrameworkElementFactory frameworkElementFactory5 = new FrameworkElementFactory(typeof(TextBlock));
		frameworkElementFactory5.SetBinding(TextBlock.TextProperty, new Binding("Source"));
		frameworkElementFactory5.SetValue(DockPanel.DockProperty, Dock.Left);
		frameworkElementFactory5.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
		frameworkElementFactory5.SetValue(TextBlock.FontSizeProperty, 13.0);
		frameworkElementFactory5.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
		frameworkElementFactory5.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
		frameworkElementFactory.AppendChild(frameworkElementFactory5);
		history.ItemTemplate = new DataTemplate
		{
			VisualTree = frameworkElementFactory
		};
		history.MouseDoubleClick += (object _, MouseButtonEventArgs _) =>
		{
			if (history.SelectedItem is Entry entry)
			{
				OpenEntry(entry);
			}
		};
	}

	private static StackPanel IconText(string glyph, string text)
	{
		return new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children =
			{
				(UIElement)new TextBlock
				{
					Text = glyph,
					FontFamily = Icons,
					FontSize = 14.0,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0.0, 0.0, 7.0, 0.0)
				},
				(UIElement)new TextBlock
				{
					Text = text,
					VerticalAlignment = VerticalAlignment.Center
				}
			}
		};
	}

	private static async void Flash(Button button, object content)
	{
		object original = button.Content;
		button.Content = content;
		await Task.Delay(1500);
		button.Content = original;
	}

	private void ConfigureChoice(Button button, string glyph, string title, string description)
	{
		button.Style = (Style)FindResource("HomeChoiceStyle");
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(new Border
		{
			Width = 44.0,
			Height = 44.0,
			CornerRadius = new CornerRadius(22.0),
			Background = Palette.PrimarySubtle,
			HorizontalAlignment = HorizontalAlignment.Left,
			Child = new TextBlock
			{
				Text = glyph,
				FontFamily = Icons,
				Foreground = Palette.PrimaryText,
				FontSize = 20.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = title,
			FontSize = 15.5,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = description,
			Foreground = Muted,
			FontSize = 13.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0),
			TextWrapping = TextWrapping.Wrap
		});
		button.Content = stackPanel;
		AutomationProperties.SetName(button, title);
	}
}
