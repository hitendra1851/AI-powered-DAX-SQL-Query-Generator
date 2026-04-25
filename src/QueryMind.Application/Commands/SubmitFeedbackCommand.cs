using MediatR;
using QueryMind.Application.DTOs;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Application.Commands;

public record SubmitFeedbackCommand(
    Guid TenantId,
    Guid MessageId,
    int Rating,
    string? Comment
) : IRequest<FeedbackDto>;

public class SubmitFeedbackCommandHandler(QueryMindDbContext db) : IRequestHandler<SubmitFeedbackCommand, FeedbackDto>
{
    public async Task<FeedbackDto> Handle(SubmitFeedbackCommand request, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        var message = await db.Messages.FindAsync([request.MessageId], ct)
            ?? throw new InvalidOperationException("Message not found");

        var session = await db.Sessions.FindAsync([message.SessionId], ct)!;
        if (session?.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied");

        var feedback = new Domain.Entities.QueryFeedback
        {
            MessageId = request.MessageId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.Feedback.Add(feedback);
        await db.SaveChangesAsync(ct);

        return new FeedbackDto(feedback.Id, feedback.Rating, feedback.Comment);
    }
}
