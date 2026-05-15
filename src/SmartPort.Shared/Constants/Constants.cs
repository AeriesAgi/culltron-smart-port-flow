namespace SmartPort.Shared.Constants;

public static class Roles
{
    public const string Admin                 = "Admin";
    public const string PortOperationsManager = "PortOperationsManager";
    public const string TerminalStaff         = "TerminalStaff";
    public const string LogisticsPartner      = "LogisticsPartner";
    public const string FleetOwner            = "FleetOwner";
    public const string Driver                = "Driver";
    public const string JudgeDemo             = "JudgeDemo";
    public const string Viewer                = "Viewer";

    public const string AdminOrOps            = "Admin,PortOperationsManager";
    public const string OpsOrStaff           = "Admin,PortOperationsManager,TerminalStaff";
    public const string AllOperational        = "Admin,PortOperationsManager,TerminalStaff,LogisticsPartner,FleetOwner,JudgeDemo";
}

public static class Policies
{
    public const string CanManageUsers        = "CanManageUsers";
    public const string CanManageVessels      = "CanManageVessels";
    public const string CanAcknowledgeAlerts  = "CanAcknowledgeAlerts";
    public const string CanManageIncidents    = "CanManageIncidents";
    public const string CanApproveDocuments   = "CanApproveDocuments";
    public const string CanViewAnalytics      = "CanViewAnalytics";
    public const string CanAccessControlRoom  = "CanAccessControlRoom";
    public const string CanAccessFleet        = "CanAccessFleet";
    public const string CanAccessDriver       = "CanAccessDriver";
    public const string CanAccessGeminiAgent  = "CanAccessGeminiAgent";
    public const string CanAccessReports      = "CanAccessReports";
    public const string CanManageSettings     = "CanManageSettings";
}

public static class MetricTypes
{
    public const string DailyThroughputTEU     = "DailyThroughputTEU";
    public const string AverageTurnaroundHours = "AverageTurnaroundHours";
    public const string BerthUtilisationPercent = "BerthUtilisationPercent";
    public const string CraneProductivity      = "CraneProductivity";
    public const string TruckTurnaround        = "TruckTurnaround";
    public const string YardDensity            = "YardDensity";
    public const string VesselsServed          = "VesselsServed";
    public const string DelayedVessels         = "DelayedVessels";
}
