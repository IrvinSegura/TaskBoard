using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Features.Labels.UpdateLabel;

internal sealed class UpdateLabelCommandHandler(ILabelRepository labels, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateLabelCommand>
{
    public async Task<Result> Handle(UpdateLabelCommand command, CancellationToken cancellationToken)
    {
        var label = await labels.GetByIdAsync(command.LabelId, cancellationToken);

        // Authorization ran against the board in the route, so a label of another board does not exist here.
        if (label is null || label.BoardId != command.BoardId)
        {
            return LabelErrors.NotFound(command.LabelId);
        }

        label.Update(command.Name, command.Color);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
