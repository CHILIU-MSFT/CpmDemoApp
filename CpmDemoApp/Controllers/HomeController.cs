﻿using Azure.Communication.Messages;
using CpmDemoApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Azure;
using Microsoft.Extensions.Options;

namespace CpmDemoApp.Controllers
{
    public class HomeController : Controller
    {
        private static bool _clientInitialized;
        private static NotificationMessagesClient _notificationMessagesClient;
        private static Guid _channelRegistrationId;

        public HomeController(IOptions<ClientOptions> options)
        {
            if (!_clientInitialized)
            {
                _channelRegistrationId = Guid.Parse(options.Value.ChannelRegistrationId);
                _notificationMessagesClient = new NotificationMessagesClient(options.Value.ConnectionString);
                _clientInitialized = true;
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string Phone_Number, string Message, string Image)
        {
            if (string.IsNullOrWhiteSpace(Phone_Number) 
                || (string.IsNullOrWhiteSpace(Message) && string.IsNullOrWhiteSpace(Image)))
            {
                Messages.MessagesListStatic.Add(new Message
                {
                    Text = "Please make sure you have put down phone number and either a text message or an image url.",
                });
                return View();
            }

            var recipientList = new List<string> { Phone_Number };

            try
            {
                if (Image != null)
                {
                    var mediaContext = new MediaNotificationContent(_channelRegistrationId, recipientList, new Uri(Image));
                    await _notificationMessagesClient.SendAsync(mediaContext); 
                    Messages.MessagesListStatic.Add(new Message
                    {
                        Text = $"Sent a image to \"{Phone_Number}\": ",
                        Image = Image
                    });
                }
                else
                {
                    var textContent = new TextNotificationContent(_channelRegistrationId, recipientList, Message);
                    await _notificationMessagesClient.SendAsync(textContent);
                    Messages.MessagesListStatic.Add(new Message
                    {
                        Text = $"Sent a message to \"{Phone_Number}\": \"{Message}\""
                    });
                }
            }
            catch (RequestFailedException e)
            {
                Messages.MessagesListStatic.Add(new Message
                {
                    Text = $"Message \"{Message}\" to \"{Phone_Number}\" failed. Exception: {e.Message}"
                });
            }
                   
            ModelState.Remove(nameof(Message));
            ModelState.Remove(nameof(Image));

            return View();
        }

        [HttpPost]
        public IActionResult MessagesList()
        {
            return PartialView();
        }

        [HttpPost]
        public IActionResult ClearHistory()
        {
            Messages.MessagesListStatic = new List<Message>();
            return RedirectToAction("Index");
        }
    }
}