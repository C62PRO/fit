﻿using System.Net;
using System.Text.Json;
using Dauer.Api.Model;
using SendGrid;
using SendGrid.Helpers.Mail;
using static SendGrid.BaseClient;

namespace Dauer.Api.Services;

public class SendGridEmailService : IEmailService
{
  private readonly SendGridClient client_;
  private readonly ILogger<SendGridEmailService> log_;
  private readonly string customerListId_;

  public SendGridEmailService(SendGridClient client, ILogger<SendGridEmailService> log, string customerListId)
  {
    client_ = client;
    log_ = log;
    customerListId_ = customerListId;
  }

  public async Task<bool> AddContactAsync(User user)
  {
    var obj = new
    {
      list_ids = new[] { customerListId_ },
      contacts = new object[]
      {
        new
        {
          email = user.Email,
          first_name = user.Name,
        }
      }
    };

    var response = await client_.RequestAsync(
        method: Method.PUT,
        urlPath: "marketing/contacts",
        requestBody: JsonSerializer.Serialize(obj)
    );

    bool ok = response.StatusCode == HttpStatusCode.Accepted;
      
    if (ok)
    {
      log_.LogInformation("Added contact {email}", user.Email);
    }
    else
    {
      string respBody = await response.Body.ReadAsStringAsync();
      log_.LogError("Could not add contact {email}: {code} {body}", user.Email, response.StatusCode, respBody);
    }

    return ok;
  }

  public async Task<bool> SendEmailAsync(string to, string subject, string body)
  {

    var fromAddr = new EmailAddress("support@fitedit.io", "FitEdit");
    var toAddr = new EmailAddress(to);
    var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, body, body);
    var response = await client_.SendEmailAsync(msg);

    bool ok = response.StatusCode == HttpStatusCode.Accepted;
      
    if (ok)
    {
      log_.LogInformation("Sent email to {to}: {subject} {body}", to, subject, body);
    }
    else
    {
      string respBody = await response.Body.ReadAsStringAsync();
      log_.LogError("Could not send email to {to}: {code} {body}", to, response.StatusCode, respBody);
    }

    return ok;
  }
}
