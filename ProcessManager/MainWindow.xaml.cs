using ProcessManager.Models;
using ProcessManager.Services;
using ProcessManager.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ProcessManager
{
    public partial class MainWindow : Window
    {
        private readonly ProcessService _service = new ProcessService();
        private List<ProcessInfo> _allProcesses = new List<ProcessInfo>();
        private DispatcherTimer _refreshTimer;
        private DispatcherTimer _clockTimer;
        private bool _autoRefreshOn = false;
        private PerformanceCounter _cpuCounter;
        private readonly int _coreCount;

        // Современная цветовая палитра
        private static SolidColorBrush Clr(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        private static readonly SolidColorBrush TextWhite = Clr("#ffffff");
        private static readonly SolidColorBrush TextGray = Clr("#a0a0c0");
        private static readonly SolidColorBrush TextCyan = Clr("#4facfe");
        private static readonly SolidColorBrush TextPurple = Clr("#667eea");
        private static readonly SolidColorBrush TextPink = Clr("#f093fb");
        private static readonly SolidColorBrush TextOrange = Clr("#fee140");
        private static readonly SolidColorBrush BgDark = Clr("#1a1a2e");
        private static readonly SolidColorBrush BgDarker = Clr("#16162a");

        public MainWindow()
        {
            InitializeComponent();
            _coreCount = _service.GetCoreCount();

            // Включаем перетаскивание окна
            this.MouseLeftButtonDown += (s, e) => this.DragMove();

            try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); }
            catch { }

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += (s, e) => LoadProcesses();

            BuildCpuCheckboxes();
            LoadProcesses();
        }

        private void UpdateClock()
        {
            ClockText.Text = "🕐 " + DateTime.Now.ToString("HH:mm:ss");
            try
            {
                if (_cpuCounter != null)
                    CpuLabel.Text = $"💻 CPU: {_cpuCounter.NextValue():0.0}%";
            }
            catch { }
            RamLabel.Text = $"🧠 RAM: {GC.GetTotalMemory(false) / 1024 / 1024} MB";
        }

        private void LoadProcesses()
        {
            _allProcesses = _service.GetProcesses();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allProcesses.AsEnumerable();

            string search = SearchBox?.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.ToLower().Contains(search));

            if (FilterGuiOnly?.IsChecked == true)
                query = query.Where(p => p.MemoryUsage > 1024 * 1024);

            if (FilterSystemOnly?.IsChecked == true)
            {
                var sysNames = new[] { "system", "svchost", "lsass", "csrss", "wininit", "services", "smss" };
                query = query.Where(p => sysNames.Any(n => p.Name.ToLower().Contains(n)) || p.Id < 10);
            }

            var result = query.OrderBy(p => p.Name).ToList();
            ProcessGrid.ItemsSource = result;

            if (ProcessCountLabel != null)
                ProcessCountLabel.Text = $"{result.Count} / {_allProcesses.Count}";

            SetStatus($"✓ Обновлено: {DateTime.Now:HH:mm:ss}  |  Процессов: {result.Count}");
        }

        private void ProcessGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is ProcessInfo p)
                ShowProcessInfo(p);
        }

        private void ShowProcessInfo(ProcessInfo p)
        {
            ProcessInfoPanel.Children.Clear();

            // Заголовок с иконкой
            var titleBorder = new Border
            {
                Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#667eea"),
                    (Color)ColorConverter.ConvertFromString("#764ba2"),
                    90),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var titleText = new TextBlock
            {
                Text = "📊 " + p.Name,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = TextWhite,
                FontFamily = new FontFamily("Segoe UI")
            };

            titleBorder.Child = titleText;
            ProcessInfoPanel.Children.Add(titleBorder);

            void AddRow(string icon, string label, string value, SolidColorBrush color = null)
            {
                color = color ?? TextWhite;

                var border = new Border
                {
                    Background = BgDarker,
                    BorderBrush = TextPurple,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 4, 0, 4)
                };

                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var iconBlock = new TextBlock
                {
                    Text = icon,
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var lbl = new TextBlock
                {
                    Text = label,
                    Foreground = TextGray,
                    FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var val = new TextBlock
                {
                    Text = value,
                    Foreground = color,
                    FontSize = 13,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(iconBlock, 0);
                Grid.SetColumn(lbl, 1);
                Grid.SetColumn(val, 2);

                g.Children.Add(iconBlock);
                g.Children.Add(lbl);
                g.Children.Add(val);

                border.Child = g;
                ProcessInfoPanel.Children.Add(border);
            }

            AddRow("🆔", "PID", p.Id.ToString(), TextCyan);
            AddRow("📝", "Имя процесса", p.Name, TextWhite);
            AddRow("⚡", "Приоритет", p.Priority.ToString(), p.IsHighPriority ? TextOrange : TextCyan);
            AddRow("🧵", "Потоков", p.ThreadCount.ToString(), TextPink);
            AddRow("💾", "Память", p.MemoryUsageMb, TextOrange);
            AddRow("⏱️", "CPU Time", p.CpuTimeStr, TextCyan);
            AddRow("🚀", "Запуск", p.StartTimeStr, TextWhite);

            try
            {
                var mask = _service.GetAffinity(p.Id);
                AddRow("🎯", "Affinity HEX", AffinityHelper.ToHexString(mask), TextPink);
                AddRow("💻", "Affinity BIN", AffinityHelper.ToBinaryString(mask, _coreCount), TextCyan);
                UpdateAffinityCheckboxes(mask);
            }
            catch { AddRow("⚠️", "CPU Affinity", "Нет доступа", Clr("#ff4757")); }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadProcesses();

        private void AutoRefresh_Click(object sender, RoutedEventArgs e)
        {
            _autoRefreshOn = !_autoRefreshOn;
            if (_autoRefreshOn)
            {
                _refreshTimer.Start();
                AutoRefreshBtn.Content = "⚡ Auto: ON";
                SetStatus("✓ Авто-обновление включено (каждые 5 сек)");
            }
            else
            {
                _refreshTimer.Stop();
                AutoRefreshBtn.Content = "⚡ Auto: OFF";
                SetStatus("✓ Авто-обновление выключено");
            }
        }

        private void Kill_Click(object sender, RoutedEventArgs e)
        {
            if (!(ProcessGrid.SelectedItem is ProcessInfo p)) return;

            if (MessageBox.Show($"Завершить процесс «{p.Name}» (PID {p.Id})?",
                    "⚠️ Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            try
            {
                _service.KillProcess(p.Id);
                SetStatus($"✓ Процесс {p.Name} ({p.Id}) завершён.");
                LoadProcesses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось завершить процесс:\n{ex.Message}", "❌ Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetPriority_Click(object sender, RoutedEventArgs e)
        {
            if (!(ProcessGrid.SelectedItem is ProcessInfo p)) return;
            if (!(PriorityBox.SelectedItem is ComboBoxItem item)) return;

            var priorityStr = item.Content.ToString();

            if (priorityStr == "RealTime" &&
                MessageBox.Show("⚠️ Приоритет RealTime может дестабилизировать систему!\nПродолжить?",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                != MessageBoxResult.Yes) return;

            try
            {
                var priority = (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), priorityStr);
                _service.SetPriority(p.Id, priority);
                SetStatus($"✓ Приоритет процесса {p.Name} изменён на {priorityStr}");
                LoadProcesses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения приоритета:\n{ex.Message}", "❌ Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Threads_Click(object sender, RoutedEventArgs e)
        {
            if (!(ProcessGrid.SelectedItem is ProcessInfo p))
            {
                MessageBox.Show("Сначала выберите процесс в таблице.",
                    "ℹ️ Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var threads = _service.GetThreads(p.Id);
                ThreadGrid.ItemsSource = null;
                ThreadGrid.ItemsSource = threads;
                ThreadCountLabel.Text = $"Потоков: {threads.Count}  —  {p.Name} (PID {p.Id})";
                SetStatus($"✓ Потоки процесса {p.Name} — всего: {threads.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения потоков:\n{ex.Message}", "❌ Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildCpuCheckboxes()
        {
            CpuCoresPanel.Children.Clear();
            for (int i = 0; i < _coreCount; i++)
            {
                var cb = new CheckBox
                {
                    Content = $"Core {i}",
                    IsChecked = true,
                    Margin = new Thickness(8, 6, 8, 6)
                };
                cb.Checked += (s, ev) => UpdateMaskLabels();
                cb.Unchecked += (s, ev) => UpdateMaskLabels();
                CpuCoresPanel.Children.Add(cb);
            }
            UpdateMaskLabels();
        }

        private bool[] GetCurrentCoreMask()
        {
            var cores = new bool[_coreCount];
            int i = 0;
            foreach (UIElement el in CpuCoresPanel.Children)
                if (el is CheckBox cb && i < _coreCount)
                    cores[i++] = cb.IsChecked == true;
            return cores;
        }

        private void UpdateMaskLabels()
        {
            var mask = AffinityHelper.SetCoreMask(GetCurrentCoreMask());
            BinMaskLabel.Text = AffinityHelper.ToBinaryString(mask, _coreCount);
            HexMaskLabel.Text = AffinityHelper.ToHexString(mask);
        }

        private void UpdateAffinityCheckboxes(IntPtr mask)
        {
            int i = 0;
            foreach (UIElement el in CpuCoresPanel.Children)
                if (el is CheckBox cb && i < _coreCount)
                    cb.IsChecked = AffinityHelper.IsCoreEnabled(mask, i++);
            UpdateMaskLabels();
        }

        private void SetAffinity_Click(object sender, RoutedEventArgs e)
        {
            if (!(ProcessGrid.SelectedItem is ProcessInfo p)) return;

            var cores = GetCurrentCoreMask();
            if (!Array.Exists(cores, c => c))
            {
                MessageBox.Show("⚠️ Необходимо выбрать хотя бы одно ядро!",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var mask = AffinityHelper.SetCoreMask(cores);
                _service.SetAffinity(p.Id, mask);
                SetStatus($"✓ CPU Affinity процесса {p.Name} изменён. Маска: {AffinityHelper.ToHexString(mask)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка изменения CPU Affinity:\n{ex.Message}", "❌ Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildTree_Click(object sender, RoutedEventArgs e)
        {
            ProcessTree.Items.Clear();
            SetStatus("🌲 Строим дерево процессов...");

            var allProcs = _service.GetProcesses();
            var map = _service.BuildParentChildMap(allProcs);

            if (map.ContainsKey(0))
                foreach (var root in map[0].OrderBy(p => p.Name))
                    ProcessTree.Items.Add(MakeTreeItem(root, map));

            SetStatus($"✓ Дерево построено. Корневых процессов: {ProcessTree.Items.Count}");
        }

        private TreeViewItem MakeTreeItem(ProcessInfo p, Dictionary<int, List<ProcessInfo>> map)
        {
            int childCount = map.ContainsKey(p.Id) ? map[p.Id].Count : 0;
            string header = childCount > 0
                ? $"🔹 [{p.Id}]  {p.Name}  ({childCount} дочерних)"
                : $"🔸 [{p.Id}]  {p.Name}";

            var item = new TreeViewItem
            {
                Header = header,
                Foreground = p.IsHighPriority ? TextOrange : TextCyan,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };

            if (map.ContainsKey(p.Id))
                foreach (var child in map[p.Id].OrderBy(c => c.Name))
                    item.Items.Add(MakeTreeItem(child, map));

            return item;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
        private void ProcessGrid_Sorting(object sender, DataGridSortingEventArgs e) { }
        private void SetStatus(string msg) => StatusLabel.Text = msg;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}