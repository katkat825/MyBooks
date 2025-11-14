using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace MyBooks.AuthService.Services;

public class InvitationService
{
    private readonly AuthDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public InvitationService(AuthDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    private async Task<string> GetAuthsystemTokenAsync()
    {
        // get a system token from AuthService for EmailService calls
        var httpClient = _httpClientFactory.CreateClient();
        var systemHelper = new SystemTokenHelper(httpClient, _config["ServiceUrls:AuthService"]);

        return await systemHelper.GetSystemTokenAsync(
            "AuthService",
            _config["ServiceSecrets:AuthService"] 
        );
    }

    public async Task<Invitation?> CreateAndSendInviteAsync(int userId)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive || !user.IsVisible)
            return null;

        // deactivate old invites
        var oldInvites = await _context.Invitations
            .IgnoreQueryFilters()
            .Where(i => i.UserId == userId && i.IsActive)
            .ToListAsync();

        foreach (var i in oldInvites)
        {
            i.IsActive = false;
            i.DeactivationReason = InvitationDeactivationReason.Replaced;
            _context.Entry(i).State = EntityState.Modified;
        }

        var invitedById = int.TryParse(user.CreatedBy, out var inviterId);
        var inviter = await _context.Users.FirstOrDefaultAsync(u => u.Id == inviterId);
        var invitedBy = inviter != null
            ? $"{inviter.FirstName} {inviter.LastName}"
            : "My Book Catalog";

        var inviteToken = Guid.NewGuid().ToString("N");
        var invite = new Invitation
        {
            UserId = user.Id,
            TenantId = user.TenantId ?? 0,
            Email = user.Email,
            InvitationToken = inviteToken,
            ExpirationDate = DateTime.UtcNow.AddDays(14),
            IsActive = true,
            CreatedBy = invitedById.ToString(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Invitations.Add(invite);
        await _context.SaveChangesAsSystemAsync();

        // send email via EmailService
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetAuthsystemTokenAsync());

        var dto = new InviteDto
        {
            ToEmail = invite.Email,
            InvitedBy = invitedBy,
            InvitationToken = invite.InvitationToken
        };

        var emailUrl = $"{_config["ServiceUrls:EmailService"]}/invite/user";
        var response = await httpClient.PostAsJsonAsync(emailUrl, dto);

        if (!response.IsSuccessStatusCode)
        {
            invite.IsActive = false;
            invite.DeactivationReason = InvitationDeactivationReason.EmailFailed;
            _context.Entry(invite).State = EntityState.Modified;
            await _context.SaveChangesAsSystemAsync();
            return null;
        }

        return invite;
    }

    public async Task<Invitation?> CreateAndSendNewAccountEmailAsync(int userId)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive || !user.IsVisible)
            return null;

        // deactivate old invites
        var oldInvites = await _context.Invitations
            .IgnoreQueryFilters()
            .Where(i => i.UserId == userId && i.IsActive)
            .ToListAsync();

        foreach (var i in oldInvites)
        {
            i.IsActive = false;
            i.DeactivationReason = InvitationDeactivationReason.Replaced;
            _context.Entry(i).State = EntityState.Modified;
        }

        var inviteToken = Guid.NewGuid().ToString("N");
        var invite = new Invitation
        {
            UserId = user.Id,
            TenantId = user.TenantId ?? 0,
            Email = user.Email,
            InvitationToken = inviteToken,
            ExpirationDate = DateTime.UtcNow.AddDays(14),
            IsActive = true,
            CreatedBy = "System: New Account",
            CreatedDate = DateTime.UtcNow
        };

        _context.Invitations.Add(invite);
        await _context.SaveChangesAsSystemAsync();

        // send email via EmailService
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetAuthsystemTokenAsync());

        var dto = new AccountCreatedDto
        {
            ToEmail = user.Email,
            InvitationToken = inviteToken,
            FirstName = user.FirstName
        };

        var emailUrl = $"{_config["ServiceUrls:EmailService"]}/invite/owner";
        var response = await httpClient.PostAsJsonAsync(emailUrl, dto);

        if (!response.IsSuccessStatusCode)
        {
            invite.IsActive = false;
            invite.DeactivationReason = InvitationDeactivationReason.EmailFailed;
            _context.Entry(invite).State = EntityState.Modified;
            await _context.SaveChangesAsSystemAsync();
            return null;
        }

        return invite;
    }

    public async Task<Invitation?> CreateAndSendPasswordResetAsync(int userId)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || !user.IsActive || !user.IsVisible)
            return null;

        // deactivate old invites
        var oldInvites = await _context.Invitations
            .IgnoreQueryFilters()
            .Where(i => i.UserId == userId && i.IsActive)
            .ToListAsync();

        foreach (var i in oldInvites)
        {
            i.IsActive = false;
            i.DeactivationReason = InvitationDeactivationReason.Replaced;
            _context.Entry(i).State = EntityState.Modified;
        }

        var inviteToken = Guid.NewGuid().ToString("N");
        var invite = new Invitation
        {
            UserId = user.Id,
            TenantId = user.TenantId ?? 0,
            Email = user.Email,
            InvitationToken = inviteToken,
            ExpirationDate = DateTime.UtcNow.AddDays(14),
            IsActive = true,
            CreatedBy = user.Id.ToString(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Invitations.Add(invite);
        await _context.SaveChangesAsSystemAsync();

        // send email via EmailService
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetAuthsystemTokenAsync());

        var dto = new PwdResetDto
        {
            ToEmail = invite.Email,
            InvitationToken = invite.InvitationToken
        };

        var emailUrl = $"{_config["ServiceUrls:EmailService"]}/invite/password";
        var response = await httpClient.PostAsJsonAsync(emailUrl, dto);

        if (!response.IsSuccessStatusCode)
        {
            invite.IsActive = false;
            invite.DeactivationReason = InvitationDeactivationReason.EmailFailed;
            _context.Entry(invite).State = EntityState.Modified;
            await _context.SaveChangesAsSystemAsync();
            return null;
        }

        return invite;
    }
}