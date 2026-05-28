using System.Linq;
using System.Windows;
using JobTrackerWPF.Data;

namespace JobTrackerWPF.Views
{
    public partial class QuestionLinker : Window
    {
        public string SelectedCompanies { get; private set; } = "";
        public QuestionLinker(string currentCompanies)
        {
            InitializeComponent();
            // load companies from interviews
            var companies = InterviewRepository.GetAll();
            foreach (var c in companies.Select(x => x.CompanyName).Distinct())
            {
                LbCompanies.Items.Add(c);
            }
            // preselect
            var parts = currentCompanies?.Split(',')?.Select(p => p.Trim())?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new System.Collections.Generic.List<string>();
            for (int i = 0; i < LbCompanies.Items.Count; i++)
            {
                if (parts.Contains(LbCompanies.Items[i].ToString())) LbCompanies.SelectedItems.Add(LbCompanies.Items[i]);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            SelectedCompanies = string.Join(", ", LbCompanies.SelectedItems.Cast<string>());
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
