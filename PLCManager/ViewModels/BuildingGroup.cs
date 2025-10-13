using System.Collections.Generic;
using System.Linq;
using PLCManager.Models;

namespace PLCManager.ViewModels;

public class BuildingGroup
{
    public string BuildingName { get; set; } = string.Empty;
    public List<PlcConnection> Connections { get; set; } = new List<PlcConnection>();
}
