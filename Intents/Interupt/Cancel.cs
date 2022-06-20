﻿using FFXIVVenues.Veni;
using FFXIVVenues.Veni.Context;
using FFXIVVenues.Veni.Intents;
using System.Threading.Tasks;

namespace FFXIVVenues.Veni.Intents.Interupt
{
    internal class Cancel : IIntentHandler
    {

        public Task Handle(MessageContext context)
        {
            if (context.Conversation.ActiveState == null)
                return context.SendMessageAsync("Huh? We're not in the middle of anything. :shrug:");

            context.Conversation.ClearData();
            context.Conversation.ClearState();
            return context.SendMessageAsync(MessageRepository.StoppedMessage.PickRandom());
        }

    }
}
