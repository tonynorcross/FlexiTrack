using FlexiTrack.Api.Data;
using FlexiTrack.Mediator;
using Microsoft.EntityFrameworkCore;

namespace FlexiTrack.Api.Features.TaskItems;

public static class DeleteTaskItem
{
    public record Command(int Id) : IRequest<Response>;

    public record Response(bool Success, string? Error = null);

    public class Handler : IRequestHandler<Command, Response>
    {
        private readonly AppDbContext _db;

        public Handler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Response> Handle(Command request, CancellationToken cancellationToken = default)
        {
            var taskItem = await _db.TaskItems
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (taskItem == null)
            {
                return new Response(false, Error: "Task not found");
            }

            if (taskItem.Removed != null)
            {
                return new Response(false, Error: "Task already removed");
            }

            taskItem.Removed = DateTime.UtcNow;

            foreach (var subTask in taskItem.SubTasks.Where(st => st.Removed == null))
            {
                subTask.Removed = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new Response(true);
        }
    }
}
