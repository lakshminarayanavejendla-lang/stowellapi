using Microsoft.Graph;
using StowellCoAPI.Models;

namespace StowellCoAPI.Services
{
    public class UserInfoService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly Dictionary<string, UserInfo> _userCache = new();

        public UserInfoService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public async Task InitializeUserInfo(string userId)
        {
            // Check if user data is already cached
            if (!_userCache.ContainsKey(userId))
            {
                // Fetch user groups
                var userGroups = await _graphServiceClient.Me.MemberOf.Request().GetAsync();
                Dictionary<string,string> groupNames = new Dictionary<string,string>();

                foreach (var directoryObject in userGroups)
                {
                    if (directoryObject is Microsoft.Graph.Group group)
                    {
                        groupNames.Add(group.Id, group.DisplayName);
                    }
                }

                // Fetch user email
                var user = await _graphServiceClient.Me.Request().GetAsync();
                var userEmail = user.UserPrincipalName;

                // Cache the user data in a UserInfo object
                _userCache[userId] = new UserInfo(groupNames, userEmail);
            }
        }

        public UserInfo GetUserInfo(string userId)
        {
            return _userCache.TryGetValue(userId, out var userInfo) ? userInfo : null;
        }
    }
}
