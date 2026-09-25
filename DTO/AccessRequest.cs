using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StowellCoAPI.DTO
{
    public class AccessRequest
    {
        public string JobNumber { get; set; }
        public string JobName { get; set; }
        public string ProjectManager { get; set; }
        public string AssistantProjectManager { get; set; }
        public int JobId { get; set; }
        public string BranchManagerGroupId { get; set; }
    }

    public class UserGroup
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

}
