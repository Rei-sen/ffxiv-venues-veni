using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using FFXIVVenues.Veni.Infrastructure.Context;
using FFXIVVenues.Veni.Infrastructure.Context.SessionHandling;
using FFXIVVenues.Veni.Utils;
using FFXIVVenues.VenueModels;

namespace FFXIVVenues.Veni.SessionStates
{
    class DiscordEntrySessionState : ISessionState
    {

        static HttpClient _discordClient = new HttpClient();
        static Regex _discordPattern = new Regex(@"(https?:\/\/)?(www\.)?((discord(app)?(\.com|\.io)(\/invite)?)|(discord\.gg))\/(\w+)");

        public Task Enter(VeniInteractionContext c)
        {
            c.Session.RegisterMessageHandler(this.OnMessageReceived);
            return c.Interaction.RespondAsync(MessageRepository.AskForDiscordMessage.PickRandom(),
                new ComponentBuilder()
                    .WithBackButton(c)
                    .WithSkipButton<HaveScheduleEntrySessionState, ConfirmVenueSessionState>(c)
                .Build());
        }

        public async Task OnMessageReceived(MessageVeniInteractionContext c)
        {
            var venue = c.Session.GetItem<Venue>("venue");
            string message = c.Interaction.Content.StripMentions();

            var rawDiscordString = c.Interaction.Content.StripMentions();
            if (!new Regex("^https?://").IsMatch(rawDiscordString))
                rawDiscordString = "https://" + rawDiscordString;

            var match = _discordPattern.Match(rawDiscordString);
            if (!match.Success)
            {
                await c.Interaction.Channel.SendMessageAsync("That doesn't look like a valid Discord invite to me. :think:");
                return;
            }

            var inviteCode = match.Groups[9].ToString();
            var responseMessage = await _discordClient.GetAsync($"https://discordapp.com/api/invite/{inviteCode}");

            if (!responseMessage.IsSuccessStatusCode)
            {
                await c.Interaction.Channel.SendMessageAsync("I tried that invite link but it seems to be invalid. :cry:");
                return;
            }

            var response = await responseMessage.Content.ReadAsStreamAsync();
            var invite = await JsonSerializer.DeserializeAsync<DiscordInvite>(response);

            if (invite.expires_at != null)
            {
                await c.Interaction.Channel.SendMessageAsync($"That invite link is not permanent, it'll expire on {invite.expires_at.Value:m}.");
                return;
            }

            venue.Discord = new Uri(rawDiscordString);

            if (c.Session.GetItem<bool>("modifying"))
            {
                await c.Session.MoveStateAsync<ConfirmVenueSessionState>(c);
                return;
            }

            await c.Session.MoveStateAsync<HaveScheduleEntrySessionState>(c);
        }

    }

    public class DiscordInvite
    {
        public DateTime? expires_at { get; set; }
    }
}