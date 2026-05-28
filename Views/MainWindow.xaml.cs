using JobTrackerWPF.Data;
using JobTrackerWPF.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JobTrackerWPF.Views
{
    public partial class MainWindow : Window
    {
        private int _currentId = 0;
        private bool _isNew = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (TxtDbPath != null)
            {
                TxtDbPath.Text = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JobTracker", "jobtracker.db");
            }
            LoadList();
            UpdateStats();
        }

        private void BtnOpenQuestionBank_Click(object sender, RoutedEventArgs e)
        {
            var qb = new QuestionBank();
            qb.Owner = this;
            qb.ShowDialog();
        }

        private void BtnCompanyQ_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string company)
            {
                var qb = new QuestionBank(company) { Owner = this };
                qb.ShowDialog();
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // Reset the form fields to defaults
            ClearForm();
            MessageBox.Show("Form reset.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadList()
        {
            if (CmbFilter == null || TxtSearch == null || LvInterviews == null) return;
            var status = (CmbFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (status == "All Statuses") status = null;
            var search = TxtSearch.Text.Trim();
            LvInterviews.ItemsSource = InterviewRepository.GetAll(status, string.IsNullOrEmpty(search) ? null : search);
        }

        private void UpdateStats()
        {
            StatsPanel.Children.Clear();
            if (StatsPanel == null) return;
            var stats = InterviewRepository.GetStats();
            var items = new[] {
                ("Total", stats.ContainsKey("Total") ? stats["Total"] : 0, "#555"),
                ("Scheduled", stats.ContainsKey("Scheduled") ? stats["Scheduled"] : 0, "#185FA5"),
                ("Interviewed", stats.ContainsKey("Interviewed") ? stats["Interviewed"] : 0, "#854F0B"),
                ("Next Round", stats.ContainsKey("Next Round") ? stats["Next Round"] : 0, "#3B6D11"),
                ("Offer", stats.ContainsKey("Offer") ? stats["Offer"] : 0, "#085041"),
                ("Rejected", stats.ContainsKey("Rejected") ? stats["Rejected"] : 0, "#A32D2D"),
            };
            var max = 1;
            foreach (var (_, count, _) in items) if (count > max) max = count;
            foreach (var (label, count, color) in items)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,28,0) };
                sp.Children.Add(new TextBlock { Text = count.ToString(), FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!) });
                sp.Children.Add(new TextBlock { Text = " " + label, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(100,100,100)), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0,0,0,3) });
                // small bar
                var bar = new System.Windows.Shapes.Rectangle { Height = 10, Width = 60 * (double)count / max, Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!), Margin = new Thickness(8,0,0,0), VerticalAlignment = VerticalAlignment.Center };
                sp.Children.Add(bar);
                StatsPanel.Children.Add(sp);
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _isNew = true;
            _currentId = 0;
            ClearForm();
            TxtFormTitle.Text = "New interview";
            BtnDelete.Visibility = Visibility.Collapsed;
            QSection.Visibility = Visibility.Collapsed;
            ShowForm();
        }

        private void LvInterviews_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LvInterviews.SelectedItem is not Interview iv) return;
            _isNew = false;
            _currentId = iv.Id;
            TxtFormTitle.Text = $"Edit — {iv.CompanyName}";
            FCompany.Text = iv.CompanyName;
            FRole.Text = iv.RoleName;
            FPanel.Text = iv.PanelLane;
            FHR.Text = iv.HRName;
            FNotes.Text = iv.Notes;
            if (DateTime.TryParse(iv.InterviewDate, out var dt)) FDate.SelectedDate = dt;
            else FDate.SelectedDate = null;
            SetCombo(FStatus, iv.Status);
            BtnDelete.Visibility = Visibility.Visible;
            QSection.Visibility = Visibility.Visible;
            LoadQuestions();
            ShowForm();
        }

        private void SetCombo(ComboBox cb, string value)
        {
            foreach (ComboBoxItem item in cb.Items)
                if (item.Content?.ToString() == value) { cb.SelectedItem = item; return; }
        }

        private void ShowForm()
        {
            TxtSelectHint.Visibility = Visibility.Collapsed;
            FormPanel.Visibility = Visibility.Visible;
        }

        private void ClearForm()
        {
            FCompany.Text = FRole.Text = FPanel.Text = FHR.Text = FNotes.Text = "";
            FDate.SelectedDate = null;
            FStatus.SelectedIndex = 0;
            IcQuestions.ItemsSource = null;
            QTxt.Text = QRound.Text = "";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FCompany.Text) || string.IsNullOrWhiteSpace(FRole.Text))
            {
                MessageBox.Show("Company name and Role are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var iv = new Interview
            {
                Id = _currentId,
                CompanyName = FCompany.Text.Trim(),
                RoleName = FRole.Text.Trim(),
                PanelLane = FPanel.Text.Trim(),
                HRName = FHR.Text.Trim(),
                InterviewDate = FDate.SelectedDate?.ToString("yyyy-MM-dd") ?? "",
                Status = (FStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Scheduled",
                Notes = FNotes.Text.Trim(),
            };
            var newId = InterviewRepository.Save(iv);
            if (_isNew) { _currentId = newId; _isNew = false; BtnDelete.Visibility = Visibility.Visible; QSection.Visibility = Visibility.Visible; LoadQuestions(); }
            LoadList();
            UpdateStats();
            TxtFormTitle.Text = $"Edit — {iv.CompanyName}";
            MessageBox.Show("Saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Delete this interview entry?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            InterviewRepository.Delete(_currentId);
            _currentId = 0;
            ClearForm();
            TxtSelectHint.Visibility = Visibility.Visible;
            FormPanel.Visibility = Visibility.Collapsed;
            LoadList();
            UpdateStats();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            LvInterviews.SelectedItem = null;
            TxtSelectHint.Visibility = Visibility.Visible;
            FormPanel.Visibility = Visibility.Collapsed;
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => LoadList();
        private void Search_Changed(object sender, TextChangedEventArgs e) => LoadList();
        private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadList();

        private void LoadQuestions()
        {
            IcQuestions.ItemsSource = InterviewRepository.GetQuestions(_currentId);
        }

        private void BtnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(QTxt.Text)) { MessageBox.Show("Please enter a question."); return; }
            if (_currentId == 0) { MessageBox.Show("Please save the interview first."); return; }
            InterviewRepository.SaveQuestion(new InterviewQuestion { InterviewId = _currentId, Question = QTxt.Text.Trim(), Round = QRound.Text.Trim() });
            QTxt.Text = QRound.Text = "";
            LoadQuestions();
        }

        private void BtnDeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Delete this question?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                InterviewRepository.DeleteQuestion(id);
                LoadQuestions();
            }
        }
    }
}
