namespace StowellCoAPI.Models
{
    public class UserInfo
    {
        public Dictionary<string, string> Groups { get; set; }
        public string Email { get; set; }

        public UserInfo(Dictionary<string, string> groups, string email)
        {
            Groups = groups;
            Email = email;
        }
    }
}
