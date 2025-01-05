namespace test.ViewModels;

public class WaitingListConfirmViewModel
{
    public int BookId { get; set; }
    public string BookTitle { get; set; }
    public int PeopleInQueue { get; set; }
    public int EstimatedWaitDays { get; set; }
    public int TotalCopies { get; set; }
    public DateTime? EstimatedAvailabilityDate { get; set; }
}