﻿using FFXIVVenues.Veni.Context;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Linq;
using System.Net.Http;
using System.IO;
using Discord;
using Image = SixLabors.ImageSharp.Image;
using FFXIVVenues.Veni.Utils;
using FFXIVVenues.Veni.States.Abstractions;

namespace FFXIVVenues.Veni.States
{
    class BannerInputState : IState
    {
        private readonly HttpClient _httpClient;

        public BannerInputState(HttpClient httpClient)
        {
            this._httpClient = httpClient;
        }

        public Task Init(InteractionContext c)
        {
            c.Session.RegisterMessageHandler(this.OnMessageReceived);
            return c.Interaction.RespondAsync("What cute image would you like to use as a **banner**?\nBanners are usually 600x200; I can do the scaling/crop for you :heart:.",
                new ComponentBuilder()
                    .WithButton("Skip", c.Session.RegisterComponentHandler(cm => {
                        if (cm.Session.GetItem<bool>("modifying"))
                            return cm.Session.ShiftState<ConfirmVenueState>(cm);
                        return cm.Session.ShiftState<ManagerEntryState>(cm);
                    }, ComponentPersistence.ClearRow), ButtonStyle.Secondary)
                .Build());
        }

        public async Task OnMessageReceived(MessageInteractionContext c)
        {
            if (!c.Interaction.Attachments.Any())
            {
                await c.Interaction.Channel.SendMessageAsync(MessageRepository.DontUnderstandResponses.PickRandom() + " Could you send me an image you'd like to use for your banner?");
                return;
            }

            var attachment = c.Interaction.Attachments.First();
            if (!attachment.ContentType.StartsWith("image"))
            {
                await c.Interaction.Channel.SendMessageAsync("Sorry, could you send me an image file like a jpeg or png? :relaxed: ");
                return;
            }

            var stream = await this._httpClient.GetStreamAsync(attachment.ProxyUrl);
            var outStream = new MemoryStream();
            using (var image = await Image.LoadAsync(stream))
            {

                if (image.Height * image.Width > 5_000_000)
                {
                    await c.Interaction.Channel.SendMessageAsync("Aaaah, my desk isn't big enough for this! 😓\n Can you send me that a _little_ smaller?");
                    return;
                }

                if (image.Height < 200 || image.Width < 600)
                {
                    await c.Interaction.Channel.SendMessageAsync("Sorry, could you send me something that's bigger than 600px width and 200px height? :blush:");
                    return;
                }

                image.Mutate(context =>
                {
                    var scale = 600f / image.Width;
                    if (image.Height * scale < 200)
                        scale = 200f / image.Height;
                    context.Resize((int)(image.Width * scale), (int)(image.Height * scale));
                });

                image.Mutate(context =>
                {
                    var xPoint = (image.Width - 600) / 2;
                    var yPoint = (image.Height - 200) / 2;
                    context.Crop(new Rectangle(xPoint, yPoint, 600, 200));
                });

                await image.SaveAsJpegAsync(outStream);
            }

            var component = new ComponentBuilder();
            component.WithButton("Looks good!", c.Session.RegisterComponentHandler(async cm =>
            {
                cm.Session.SetItem("bannerUrl", cm.Interaction.Message.Attachments.First().ProxyUrl);
                if (cm.Session.GetItem<bool>("modifying"))
                    await cm.Session.ShiftState<ConfirmVenueState>(c);
                else
                    await cm.Session.ShiftState<ManagerEntryState>(c);
            }, ComponentPersistence.ClearRow));
            component.WithButton("Let's try another!", c.Session.RegisterComponentHandler(cm => cm.Interaction.RespondAsync("Alrighty, send over another image! :heart:"), ComponentPersistence.ClearRow), ButtonStyle.Secondary);
            var response = await c.Interaction.Channel.SendFileAsync(outStream, "banner.jpg", "How does this look? :heart:", components: component.Build());
        }

    }
}
