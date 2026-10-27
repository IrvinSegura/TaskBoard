using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;

namespace TaskBoard.Application.Features.Activity.GetMyActivity;

// Every board the user can see, across teams, newest first.
public sealed record GetMyActivityQuery(PageRequest Page) : IQuery<PagedResponse<ActivityResponse>>;
