using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookShopByKg;

public class EmailSender : IEmailSender
{
  public Task SendEmailAsync(string email, string subject, string htmlMessage)
  {
    return Task.CompletedTask;
  }
}
