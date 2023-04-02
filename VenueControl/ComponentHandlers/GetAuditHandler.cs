using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using FFXIVVenues.Veni.Infrastructure.Components;
using FFXIVVenues.Veni.Infrastructure.Context;
using FFXIVVenues.Veni.Infrastructure.Persistence.Abstraction;
using FFXIVVenues.Veni.People;
using FFXIVVenues.Veni.Services.Api;
using FFXIVVenues.Veni.VenueAuditing;
using Microsoft.Extensions.Primitives;
using moment.net;

namespace FFXIVVenues.Veni.VenueControl.ComponentHandlers;

public class GetAuditHandler : IComponentHandler
{

    // Change this key and any existing buttons linked to this will die
    public static string Key => "CONTROL_GET_AUDIT";

    private readonly IStaffService _staffService;
    private readonly IApiService _apiService;
    private readonly IRepository _repository;

    public GetAuditHandler(IStaffService staffService, IApiService apiService, IRepository repository)
    {
        this._staffService = staffService;
        this._apiService = apiService;
        this._repository = repository;
    }
    
    public async Task HandleAsync(MessageComponentVeniInteractionContext context, string[] args)
    {
        var user = context.Interaction.User.Id;
        if (!this._staffService.IsEditor(user))
            return;
        
        _ = context.Interaction.DeleteOriginalResponseAsync();
        
        var auditId = context.Interaction.Data.Values.First();
        var audit = await this._repository.GetByIdAsync<VenueAuditRecord>(auditId);
        var venueTask = this._apiService.GetVenueAsync(audit.VenueId);


        var description = new StringBuilder()
            .Append("**Sent: **")
            .AppendLine(audit.SentTime.ToString("G"))
            .Append("**Requested by: **")
            .AppendLine(MentionUtils.MentionUser(audit.RequestedBy))
            .AppendLine()
            .Append("**Status: **")
            .AppendLine(audit.Status.ToString())
            .Append("**Completed by: **")
            .AppendLine(audit.CompletedBy == 0 ? "Pending" : MentionUtils.MentionUser(audit.CompletedBy))
            .Append("**Completed at: **")
            .AppendLine(audit.CompletedAt?.ToString("G") ?? "Pending")
            .AppendLine()
            .AppendLine("**Messages:** ");

        foreach (var message in audit.Messages)
        {
            description.Append("Message to ").Append(MentionUtils.MentionUser(message.UserId))
                .Append(": ").Append(message.Status).Append(" (").Append(message.Log).AppendLine(")");
        }
        
        description.AppendLine().AppendLine("**Log:**");
        foreach (var log in audit.Logs)
            description.Append(log.Date.ToString("G"))
                .Append(": ")
                .AppendLine(log.Message);
        
        var embed = new EmbedBuilder()
            .WithTitle($"Audit for {(await venueTask).Name}")
            .WithDescription(description.ToString());
        
        await context.Interaction.Channel.SendMessageAsync("Okay, here's the audit! 🥰", embed: embed.Build());
    }
    
}