using System.Collections.ObjectModel;

namespace JobTrackerWPF.Models
{
    public class Interview
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string PanelLane { get; set; } = "";
        public string HRName { get; set; } = "";
        public string InterviewDate { get; set; } = "";
        public string Status { get; set; } = "Scheduled";
        public string Notes { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public ObservableCollection<InterviewQuestion> Questions { get; set; } = new();
    }

    public class InterviewQuestion
    {
        public int Id { get; set; }
        public int InterviewId { get; set; }
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public string Round { get; set; } = "";
    }
}
