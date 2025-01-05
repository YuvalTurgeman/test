using Microsoft.EntityFrameworkCore;
using test.Models;

namespace test.Data
{
    public class WaitingListDAL
    {
        private readonly ApplicationDbContext _context;

        public WaitingListDAL(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WaitingListModel> AddToWaitingListAsync(int userId, int bookId)
        {
            var lastPosition = await _context.WaitingList
                .Where(w => w.BookId == bookId)
                .MaxAsync(w => (int?)w.Position) ?? 0;

            var entry = new WaitingListModel
            {
                UserId = userId,
                BookId = bookId,
                Position = lastPosition + 1,
                JoinDate = DateTime.UtcNow
            };

            await _context.WaitingList.AddAsync(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<List<WaitingListModel>> GetBookWaitingListAsync(int bookId)
        {
            return await _context.WaitingList
                .Where(w => w.BookId == bookId)
                .OrderBy(w => w.Position)
                .Include(w => w.User)
                .ToListAsync();
        }

        public async Task<int?> GetUserPositionAsync(int userId, int bookId)
        {
            var entry = await _context.WaitingList
                .Where(w => w.UserId == userId && w.BookId == bookId)
                .FirstOrDefaultAsync();
            return entry?.Position;
        }

        public async Task<bool> IsUserInWaitingListAsync(int userId, int bookId)
        {
            return await _context.WaitingList
                .AnyAsync(w => w.UserId == userId && w.BookId == bookId);
        }

        public async Task RemoveFromWaitingListAsync(int userId, int bookId)
        {
            var entry = await _context.WaitingList
                .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId);

            if (entry != null)
            {
                _context.WaitingList.Remove(entry);

                // Reorder positions for remaining users
                var remainingEntries = await _context.WaitingList
                    .Where(w => w.BookId == bookId && w.Position > entry.Position)
                    .ToListAsync();

                foreach (var remaining in remainingEntries)
                {
                    remaining.Position--;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<DateTime?> GetEstimatedAvailabilityAsync(int bookId, int position)
        {
            var activeLoans = await _context.Borrows
                .Where(b => b.BookId == bookId && !b.IsReturned)
                .OrderBy(b => b.EndDate)
                .Take(position)
                .ToListAsync();

            return activeLoans.Any() ? activeLoans.Last().EndDate : null;
        }

        public async Task UpdateWaitingListPositionsAsync(int bookId)
        {
            var waitingList = await _context.WaitingList
                .Where(w => w.BookId == bookId)
                .OrderBy(w => w.JoinDate)
                .ToListAsync();

            for (int i = 0; i < waitingList.Count; i++)
            {
                waitingList[i].Position = i + 1;
            }

            await _context.SaveChangesAsync();
        }

        public async Task NotifyUsersAsync(int bookId, int availableCopies)
        {
            var usersToNotify = await _context.WaitingList
                .Where(w => w.BookId == bookId && !w.IsNotified && w.Position <= availableCopies)
                .ToListAsync();

            foreach (var user in usersToNotify)
            {
                user.IsNotified = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<WaitingListModel> GetFirstInLineAsync(int bookId)
        {
            return await _context.WaitingList
                .Include(w => w.User)
                .Include(w => w.Book)
                .Where(w => w.BookId == bookId && !w.IsNotified)
                .OrderBy(w => w.Position)
                .FirstOrDefaultAsync();
        }

        public async Task MarkAsNotifiedAsync(int waitingListId)
        {
            var waitingListEntry = await _context.WaitingList.FindAsync(waitingListId);
            if (waitingListEntry != null)
            {
                waitingListEntry.IsNotified = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromWaitingListAsync(int waitingListId)
        {
            var entry = await _context.WaitingList.FindAsync(waitingListId);
            if (entry != null)
            {
                // Get all waiting list entries for the same book with higher positions
                var higherPositions = await _context.WaitingList
                    .Where(w => w.BookId == entry.BookId && w.Position > entry.Position)
                    .ToListAsync();

                // Decrease their positions by 1
                foreach (var item in higherPositions)
                {
                    item.Position--;
                }

                // Remove the current entry
                _context.WaitingList.Remove(entry);
                await _context.SaveChangesAsync();
            }
        }

// Optional: Method to check waiting list status
        public async Task<int> GetPositionInWaitingListAsync(int userId, int bookId)
        {
            var entry = await _context.WaitingList
                .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId);
            return entry?.Position ?? -1;
        }
    }
}