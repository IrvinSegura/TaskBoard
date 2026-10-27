using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Features.Labels.CreateLabel;

internal sealed class CreateLabelCommandHandler(ILabelRepository labels, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateLabelCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLabelCommand command, CancellationToken cancellationToken)
    {
        // The authorization decorator already checked that the board exists and is not archived.
        var label = Label.Create(command.BoardId, command.Name, command.Color);
        labels.Add(label);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return label.Id;
    }
}
