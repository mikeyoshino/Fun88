namespace Fun88.Web.Infrastructure.Data.Entities;

using Postgrest.Attributes;
using Postgrest.Models;
using System;

[Table("scraper_schedules")]
public class ScraperSchedule : BaseModel
{
    [PrimaryKey("provider_id", false)]
    public int ProviderId { get; set; }

    [Column("cron_expression")]
    public string CronExpression { get; set; } = string.Empty;

    [Column("is_enabled")]
    public bool IsEnabled { get; set; }
}
