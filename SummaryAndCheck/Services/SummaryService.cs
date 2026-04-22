using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SummaryAndCheck.Models;
using System.Transactions;

namespace SummaryAndCheck.Services
{
    class SummaryService(SummaryAndCheckDbContext dbContext, SummaryQueue summaryQueue)
    {
        static readonly TimeSpan FreeUseInterval = TimeSpan.FromMinutes(30);
        public async Task<EnqueueMessage> EnqueueAsync(SocketCommandBase socketCommand, ulong messageId, SummaryRequest request)
        {
            if (summaryQueue.FindFromCache(request, out var cached))
            {
                return new()
                {
                    EnqueueResult = cached,
                    Message = "",
                    QueueNumber = cached!.RequestQueuePosition,
                    Success = true
                };
            }
            var user = socketCommand.User;
            var usrSet = dbContext.Set<DiscordUser>();
            var historySet = dbContext.Set<CreditHistory>();
            using var tran = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);
            var callingUser = await usrSet.Where(x => x.Id == user.Id).Select(x => new { x.Id, x.IsEnabled, x.LastFreeUse }).FirstOrDefaultAsync();
            if (callingUser != null)
            {
                //check free use

                var updated = await usrSet.Where(x => x.Id == user.Id).Where(x => !x.LastFreeUse.HasValue || (DateTimeOffset.UtcNow - x.LastFreeUse.Value) > FreeUseInterval)
                    .ExecuteUpdateAsync(settings => settings.SetProperty(c => c.LastFreeUse, DateTimeOffset.UtcNow).SetProperty(c => c.Name, user.GlobalName));
                if (updated > 0)
                {
                    historySet.Add(new CreditUseHistory
                    {
                        DiscordUser = null!,
                        DiscordUserId = user.Id,
                        HistoryTime = DateTimeOffset.UtcNow,
                        ValueChanged = 0,
                        HistoryType = CreditHistoryType.FreeUse,
                        ChannelId = socketCommand.ChannelId ?? 0,
                        MessageId = messageId,
                        ServerId = socketCommand.GuildId ?? 0,
                        UseType = UseType.VideoSummary
                    });
                    await dbContext.SaveChangesAsync();
                    var queueResult = summaryQueue.Enqueue(request);
                    if (queueResult.IsNewRequest)
                    {
                        tran.Complete();
                    }
                    return new()
                    {
                        EnqueueResult = queueResult,
                        Message = "",
                        QueueNumber = queueResult.RequestQueuePosition,
                        Success = true
                    };
                }
                var changed = await usrSet.Where(x => x.Id == user.Id && x.Credit > 0)
                   .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.Credit, c => c.Credit - 1).SetProperty(c => c.Name, user.GlobalName));
                if (changed > 0)
                {
                    historySet.Add(new CreditUseHistory
                    {
                        DiscordUser = null!,
                        DiscordUserId = user.Id,
                        HistoryTime = DateTimeOffset.UtcNow,
                        ValueChanged = -1,
                        HistoryType = CreditHistoryType.CreditUse,
                        ChannelId = socketCommand.ChannelId ?? 0,
                        MessageId = messageId,
                        ServerId = socketCommand.GuildId ?? 0,
                        UseType = UseType.VideoSummary
                    });
                    await dbContext.SaveChangesAsync();
                    var queueResult = summaryQueue.Enqueue(request);
                    if (queueResult.IsNewRequest)
                    {
                        tran.Complete();
                    }
                    return new()
                    {
                        EnqueueResult = queueResult,
                        Message = "",
                        QueueNumber = queueResult.RequestQueuePosition,
                        Success = true
                    };
                }
                else
                {
                    var min = (FreeUseInterval - (DateTimeOffset.UtcNow - callingUser.LastFreeUse)) ?? TimeSpan.Zero;
                    return new()
                    {
                        Message = "NoCredit",
                        QueueNumber = -1,
                        Success = false,
                        MessageArgs = new { min = Math.Ceiling(min.TotalMinutes) }
                    };
                }
            }
            return new()
            {
                Message = "NoBotAccess",
                QueueNumber = -1,
                Success = false
            };

        }
    }
}
