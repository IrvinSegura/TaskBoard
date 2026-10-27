using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;

namespace TaskBoard.Application.Features.Activity.GetTeamActivity;

// The team's boards the user can see, newest first.
public sealed record GetTeamActivityQuery(Guid TeamId, PageRequest Page) : IQuery<PagedResponse<ActivityResponse>>;
