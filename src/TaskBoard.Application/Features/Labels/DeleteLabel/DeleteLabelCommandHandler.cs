using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Features.Labels.DeleteLabel;

internal sealed class DeleteLabelCommandHandler(ILabelRepository labels, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteLabelCommand>
{
    public async Task<Result> Handle(DeleteLabelCommand command, CancellationToken cancellationToken)
    {
        var label = await labels.GetByIdAsync(command.LabelId, cancellationToken);

        // Authorization ran against the board in the route, so a label of another board does not exist here.
        if (label is null || label.BoardId != command.BoardId)
        {
            return LabelErrors.NotFound(command.LabelId);
        }

        label.Delete();
        labels.Remove(label);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
