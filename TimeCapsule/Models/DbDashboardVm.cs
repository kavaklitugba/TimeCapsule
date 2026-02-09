namespace TimeCapsule.Models
{
    public class DbDashboardVm
    {
        public PeriodVm Today { get; set; } = new();
        public PeriodVm Week { get; set; } = new();   // son 7 gün
        public PeriodVm Month { get; set; } = new();  // son 30 gün
        public PeriodVm All { get; set; } = new();

        public int TotalWithImage { get; set; }
        public DateTime GeneratedAtLocal { get; set; }
    }

    public class PeriodVm
    {
        public int LettersSaved { get; set; }
        public int LettersSent { get; set; }
    }
}
