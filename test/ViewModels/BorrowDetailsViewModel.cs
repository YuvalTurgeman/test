using test.Models;

namespace test.ViewModels
{
    public class BorrowDetailsViewModel
    {
        public BookModel Book { get; set; }
        public int AvailableCopies { get; set; }
        public int? UserWaitingListPosition { get; set; }
        public int WaitingListCount { get; set; }
        public DateTime? EstimatedAvailabilityDate { get; set; }
        public bool CanBorrow { get; set; }
    }

    public class BorrowConfirmViewModel
    {
        public BookModel Book { get; set; }
        public string Action { get; set; }
        public bool CanBorrow { get; set; }
        public int WaitingListPosition { get; set; }
        public decimal? BorrowPrice { get; set; }
    }
}