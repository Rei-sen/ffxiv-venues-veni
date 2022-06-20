﻿using FFXIVVenues.Veni;
using FFXIVVenues.Veni.Api.Models;
using FFXIVVenues.Veni.Context;
using FFXIVVenues.Veni.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVVenues.Veni.States
{
    class SelectVenueToModifyState : IState
    {

        private static string[] _messages = new[]
        {
            "Which venue would you like to edit?",
            "Which one would you like to change?",
            "Oki, which one? 🙂"
        };

        private IEnumerable<Venue> _contactsVenues;

        public Task Enter(MessageContext c)
        {
            _contactsVenues = c.Conversation.GetItem<IEnumerable<Venue>>("venues");
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(_messages.PickRandom());
            foreach (var venue in _contactsVenues)
            {
                var isLast = _contactsVenues.Last() == venue;
                if (isLast) stringBuilder.Append("or ");
                stringBuilder.Append(venue.Name);
                if (!isLast) stringBuilder.Append(", ");
                else stringBuilder.Append("?");
            }
            return c.SendMessageAsync(stringBuilder.ToString());
        }

        public Task Handle(MessageContext c)
        {
            var (phrase, score) = c.Message.Content.StripMentions().IsSimilarToAnyPhrase(_contactsVenues.Select(v => v.Name));
            if (score < 0.35)
                return c.SendMessageAsync(MessageRepository.DontUnderstandResponses.PickRandom());

            var venue = _contactsVenues.FirstOrDefault(v => v.Name == phrase);

            c.Conversation.ClearItem("venues");
            c.Conversation.SetItem("venue", venue);
            return c.Conversation.ShiftState<ModifyVenueState>(c);
        }
    }
}
