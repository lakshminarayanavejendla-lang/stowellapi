using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using StowellCoAPI.DTO;
using StowellCoAPI.Models;
using StowellCoAPI.Services;
using System.Data;

namespace StowellCoAPI.Controllers
{
    [ApiController]
    public class SecurityGroupController : ControllerBase
    {
        private readonly ILogger<SecurityGroupController> _logger;
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly IMemoryCache _cache;
        // The directory changes rarely and Graph is slow, so results are kept for a while
        private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(30);
        private const string UserFields = "id,displayName,jobTitle,city,state,zip,businessPhones,mobilePhone,mail,officeLocation,companyName,postalCode,streetAddress,onPremisesExtensionAttributes";

        public SecurityGroupController(ILogger<SecurityGroupController> logger, IConfiguration configuration,GraphServiceClient graphServiceClient, IMemoryCache cache)
        {
            _cache = cache;
            _configuration = configuration;
             _graphServiceClient = graphServiceClient;
            _logger = logger;
        }

        [HttpGet("api/[controller]/GetAllGroups", Name = "GetAllGroups")]
        public async Task<IActionResult> GetAllGroups()
        {
            try
            {
                if (_cache.TryGetValue("contacts:groups", out List<ContactGroups>? cachedGroups) && cachedGroups != null)
                {
                    return Ok(cachedGroups);
                }

                var userList = new List<ContactGroups>();

                // Fetch all users with pagination
                var groups = await _graphServiceClient.Groups
                    .Request()
                    .GetAsync();

                while (groups != null)
                {
                    // Process the current page
                    userList.AddRange(groups.CurrentPage
                        .Where(group => group.GroupTypes == null || !group.GroupTypes.Contains("Unified")) // Only include security groups
                        .Select(group => new ContactGroups
                        {
                            DisplayName = group.DisplayName,
                            Id = group.Id
                        }).OrderBy(group => group.DisplayName));
                    // Get the next page
                    groups = groups.NextPageRequest != null ?
                            await groups.NextPageRequest.GetAsync() :
                            null;
                }
                
                _cache.Set("contacts:groups", userList, CacheFor);
                return Ok(userList);
            }
            catch (SqlException sqlEx)
            {
                // Log SQL-specific errors (e.g., connection or syntax issues)
                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving jobs.",
                    Details = sqlEx.Message
                });
                _logger.LogError(sqlEx, sqlEx.Message);
            }
            catch (Exception ex)
            {
                // Catch all unexpected exceptions
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving jobs.",
                    Details = ex.Message
                });
                _logger.LogError(ex, ex.Message);
            }

        }
        [HttpGet("api/[controller]/GetAllUsers", Name = "GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var groupMembers = new List<ContactGroupMembers>();

                var users = await _graphServiceClient.Users
                    .Request()
                    .Select("id,displayName,jobTitle,city,state,zip,businessPhones,mail,officeLocation,companyName,postalCode,streetAddress,onPremisesExtensionAttributes,accountEnabled,userType")
                    .GetAsync();

                while (users != null)
                {
                    foreach (var fullUser in users.CurrentPage)
                    {
                        // Skip disabled accounts and guest/service accounts - not real employee contacts.
                        if (fullUser.AccountEnabled == false || fullUser.UserType != "Member")
                        {
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(fullUser.DisplayName))
                        {
                            continue;
                        }

                        string? photoBase64 = null;
                        try
                        {
                            var photoStream = await _graphServiceClient.Users[fullUser.Id]
                                .Photo
                                .Content
                                .Request()
                                .GetAsync();

                            using (var ms = new MemoryStream())
                            {
                                await photoStream.CopyToAsync(ms);
                                photoBase64 = Convert.ToBase64String(ms.ToArray());
                            }
                        }
                        catch (ServiceException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            // Photo not found, leave photoBase64 null
                        }

                        groupMembers.Add(new ContactGroupMembers
                        {
                            Id = fullUser.Id,
                            DisplayName = fullUser.DisplayName,
                            JobTitle = fullUser.JobTitle,
                            City = fullUser.City,
                            Mail = fullUser.Mail,
                            StreetAddress = fullUser.StreetAddress,
                            State = fullUser.State,
                            PostalCode = fullUser.PostalCode,
                            Location = fullUser.OfficeLocation,
                            Company = fullUser.CompanyName,
                            Phone = fullUser.BusinessPhones.Any() ? fullUser.BusinessPhones.FirstOrDefault() : fullUser.MobilePhone,
                            AddressLine1 = fullUser.OnPremisesExtensionAttributes?.ExtensionAttribute1,
                            AddressLine2 = fullUser.OnPremisesExtensionAttributes?.ExtensionAttribute2,
                            PhotoBase64 = photoBase64
                        });
                    }

                    users = users.NextPageRequest != null ? await users.NextPageRequest.GetAsync() : null;
                }

                return Ok(groupMembers.OrderBy(m => m.DisplayName));
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, new { Message = "A database error occurred.", Details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred.", Details = ex.Message });
            }
        }

        [HttpGet("api/[controller]/GetGroupUsers/{groupId}", Name = "GetGroupUsers")]
        public async Task<IActionResult> GetUsersInGroup(string groupId)
        {
            try
            {
                if (_cache.TryGetValue($"contacts:group:{groupId}", out List<ContactGroupMembers>? cached) && cached != null)
                {
                    return Ok(cached);
                }

                var users = new List<User>();
                var members = await _graphServiceClient.Groups[groupId].Members
                    .Request()
                    .Select(UserFields)
                    .GetAsync();
                while (members != null)
                {
                    users.AddRange(members.CurrentPage.OfType<User>());
                    members = members.NextPageRequest != null ? await members.NextPageRequest.GetAsync() : null;
                }

                // photos are not part of this reply, so the list shows at once; the page asks for each one (UserPhoto)
                var groupMembers = users.Select(ToContact).OrderBy(m => m.DisplayName).ToList();

                _cache.Set($"contacts:group:{groupId}", groupMembers, CacheFor);
                return Ok(groupMembers);
            }
            catch (SqlException sqlEx)
            {
                // Log SQL-specific errors (e.g., connection or syntax issues)
                return StatusCode(500, new
                {
                    Message = "A database error occurred while retrieving jobs.",
                    Details = sqlEx.Message
                });
                _logger.LogError(sqlEx, sqlEx.Message);
            }
            catch (Exception ex)
            {
                // Catch all unexpected exceptions
                return StatusCode(500, new
                {
                    Message = "An unexpected error occurred while retrieving jobs.",
                    Details = ex.Message
                });
                _logger.LogError(ex, ex.Message);
            }

        }
        private static ContactGroupMembers ToContact(User user)
        {
            return new ContactGroupMembers
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                JobTitle = user.JobTitle,
                City = user.City,
                Mail = user.Mail,
                StreetAddress = user.StreetAddress,
                State = user.State,
                PostalCode = user.PostalCode,
                Location = user.OfficeLocation,
                Company = user.CompanyName,
                Phone = user.BusinessPhones.Any() ? user.BusinessPhones.FirstOrDefault() : user.MobilePhone,
                AddressLine1 = user.OnPremisesExtensionAttributes?.ExtensionAttribute1,
                AddressLine2 = user.OnPremisesExtensionAttributes?.ExtensionAttribute2,
                PhotoBase64 = null
            };
        }

        /// <summary>A small profile photo (96x96), kept for an hour. 404 when the person has none.</summary>
        [HttpGet("api/[controller]/UserPhoto/{userId}", Name = "UserPhoto")]
        public async Task<IActionResult> GetUserPhoto(string userId)
        {
            var key = $"contacts:photo:{userId}";
            if (!_cache.TryGetValue(key, out byte[]? bytes))
            {
                try
                {
                    var stream = await _graphServiceClient.Users[userId].Photos["96x96"].Content.Request().GetAsync();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                catch (ServiceException)
                {
                    bytes = null;
                }
                _cache.Set(key, bytes, TimeSpan.FromHours(1));
            }

            if (bytes == null || bytes.Length == 0) return NotFound();
            Response.Headers["Cache-Control"] = "private, max-age=3600";
            return File(bytes, "image/jpeg");
        }

        [HttpGet("api/[controller]/friend-photo/{email}", Name = "friend-photo")]
        private async Task<IActionResult> GetFriendPhotoAsync([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email ID is required.");

            //var scopes = new[] { "https://graph.microsoft.com/.default" };

            //var tenantId = "8797197d-bd16-4c5d-a882-ef538675a233";
            //var clientId = "0e419b11-0ecb-4266-a183-5818080dd12c";
            //var clientSecret = "<redacted>";

            //var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            //var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            try
            {
                var user = await _graphServiceClient.Users
                    .Request()
                    .Filter($"mail eq '{email}'")
                    .Select("id")
                    .GetAsync();

                var userId = user?.FirstOrDefault()?.Id;
                if (string.IsNullOrEmpty(userId))
                    return NotFound($"User not found with email: {email}");

                var photoStream = await _graphServiceClient.Users[userId].Photo.Content.Request().GetAsync();
                if (photoStream != null)
                    return File(photoStream, "image/jpeg");

                return NotFound($"Photo not found for user with email: {email}");
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound($"User or photo not found for email: {email}");

                Console.WriteLine($"Graph API error: {ex.Message}");
                return StatusCode(500, "Internal server error while fetching user photo.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
