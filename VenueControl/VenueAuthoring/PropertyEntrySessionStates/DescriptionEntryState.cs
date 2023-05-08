﻿using System.Threading.Tasks;
using Discord;
using FFXIVVenues.Veni.Infrastructure.Context;
using FFXIVVenues.Veni.Infrastructure.Context.SessionHandling;
using FFXIVVenues.Veni.Utils;
using FFXIVVenues.Veni.VenueControl.VenueAuthoring.PropertyEntrySessionStates.LocationEntry;

namespace FFXIVVenues.Veni.VenueControl.VenueAuthoring.PropertyEntrySessionStates
{
    class DescriptionEntrySessionState : ISessionState
    {
        public Task Enter(VeniInteractionContext c)
        {
            c.Session.RegisterMessageHandler(this.OnMessageReceived);
            return c.Interaction.RespondAsync(MessageRepository.AskForDescriptionMessage.PickRandom(),
                new ComponentBuilder()
                    .WithBackButton(c)
                    .WithSkipButton<LocationTypeEntrySessionState, ConfirmVenueSessionState>(c)
                    .Build());
        }

        public Task OnMessageReceived(MessageVeniInteractionContext c)
        {
            var venue = c.Session.GetVenue();
            venue.Description = c.Interaction.Content.StripMentions().AsListOfParagraphs();
            if (c.Session.GetItem<bool>("modifying"))
                return c.Session.MoveStateAsync<ConfirmVenueSessionState>(c);
            return c.Session.MoveStateAsync<LocationTypeEntrySessionState>(c);
        }

    }
}