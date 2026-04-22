namespace PriceTrackerAlert.Models;

public class Alert
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public double TargetPrice { get; set; }
    public AlertCondition Condition { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTriggered { get; set; } = false;
    public string SoundFile { get; set; } = "default";
    public string Note { get; set; } = "";
}

public enum AlertCondition { Above, Below }
