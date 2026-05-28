using System.Windows;
using JobTrackerWPF.Models;
using JobTrackerWPF.Data;

namespace JobTrackerWPF.Views
{
    public partial class EditQuestion : Window
    {
        public InterviewQuestion Question { get; private set; }

        public EditQuestion(InterviewQuestion q)
        {
            InitializeComponent();
            Question = q;
            TxtQuestion.Text = q.Question;
            TxtRound.Text = q.Round;
            TxtAnswer.Text = q.Answer;
            TxtCompanies.Text = q.Companies;
            SldRating.Value = q.Rating;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Question.Question = TxtQuestion.Text.Trim();
            Question.Round = TxtRound.Text.Trim();
            Question.Answer = TxtAnswer.Text.Trim();
            Question.Companies = TxtCompanies.Text.Trim();
            Question.Rating = (int)SldRating.Value;
            InterviewRepository.SaveQuestion(Question);
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
