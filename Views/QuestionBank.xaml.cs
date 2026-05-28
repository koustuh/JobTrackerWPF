using System.Windows;
using JobTrackerWPF.Data;
using JobTrackerWPF.Models;
using System.Collections.Generic;

namespace JobTrackerWPF.Views
{
    public partial class QuestionBank : Window
    {
        private readonly string? _companyFilter;
        public QuestionBank(string? companyFilter = null)
        {
            InitializeComponent();
            _companyFilter = companyFilter;
            LoadQuestions();
        }

        private void LoadQuestions()
        {
            if (string.IsNullOrEmpty(_companyFilter)) LvQuestions.ItemsSource = InterviewRepository.GetAllQuestions();
            else LvQuestions.ItemsSource = InterviewRepository.GetQuestionsByCompany(_companyFilter);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var q = new InterviewQuestion { Question = "New question", Round = "", Answer = "", Companies = "", Rating = 0 };
            InterviewRepository.SaveQuestion(q);
            var dlg = new EditQuestion(q) { Owner = this };
            if (dlg.ShowDialog() == true) LoadQuestions();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (LvQuestions.SelectedItem is not InterviewQuestion q) { MessageBox.Show("Select a question to edit."); return; }
            var dlg = new EditQuestion(q) { Owner = this };
            if (dlg.ShowDialog() == true) LoadQuestions();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (LvQuestions.SelectedItem is not InterviewQuestion q) { MessageBox.Show("Select a question to delete."); return; }
            if (MessageBox.Show("Delete this question?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            InterviewRepository.DeleteQuestion(q.Id);
            LoadQuestions();
        }

        private void BtnLink_Click(object sender, RoutedEventArgs e)
        {
            if (LvQuestions.SelectedItem is not InterviewQuestion q) { MessageBox.Show("Select a question first."); return; }
            var linker = new QuestionLinker(q.Companies);
            linker.Owner = this;
            if (linker.ShowDialog() == true)
            {
                q.Companies = linker.SelectedCompanies;
                InterviewRepository.SaveQuestion(q);
                LoadQuestions();
            }
        }

        private void BtnSort_Click(object sender, RoutedEventArgs e)
        {
            var list = InterviewRepository.GetAllQuestions();
            list.Sort((a, b) => b.Rating.CompareTo(a.Rating));
            LvQuestions.ItemsSource = list;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
