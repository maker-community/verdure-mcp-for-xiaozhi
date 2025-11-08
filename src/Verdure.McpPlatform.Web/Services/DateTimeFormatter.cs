using System.Globalization;

namespace Verdure.McpPlatform.Web.Services;

/// <summary>
/// 时间格式化服务
/// 负责将 UTC 时间转换为用户本地时间并格式化显示
/// </summary>
public interface IDateTimeFormatter
{
    /// <summary>
    /// 格式化为短日期格式（yyyy-MM-dd）
    /// </summary>
    string FormatShortDate(DateTime utcDateTime);
    
    /// <summary>
    /// 格式化为短日期格式（yyyy-MM-dd）- 可空版本
    /// </summary>
    string FormatShortDate(DateTime? utcDateTime, string defaultValue = "-");
    
    /// <summary>
    /// 格式化为日期时间格式（yyyy-MM-dd HH:mm）
    /// </summary>
    string FormatDateTime(DateTime utcDateTime);
    
    /// <summary>
    /// 格式化为日期时间格式（yyyy-MM-dd HH:mm）- 可空版本
    /// </summary>
    string FormatDateTime(DateTime? utcDateTime, string defaultValue = "-");
    
    /// <summary>
    /// 格式化为友好的日期格式（MMM dd, yyyy）
    /// </summary>
    string FormatFriendlyDate(DateTime utcDateTime);
    
    /// <summary>
    /// 格式化为友好的日期格式（MMM dd, yyyy）- 可空版本
    /// </summary>
    string FormatFriendlyDate(DateTime? utcDateTime, string defaultValue = "-");
    
    /// <summary>
    /// 格式化为友好的日期时间格式（MMM dd, yyyy HH:mm）
    /// </summary>
    string FormatFriendlyDateTime(DateTime utcDateTime);
    
    /// <summary>
    /// 格式化为友好的日期时间格式（MMM dd, yyyy HH:mm）- 可空版本
    /// </summary>
    string FormatFriendlyDateTime(DateTime? utcDateTime, string defaultValue = "-");
    
    /// <summary>
    /// 格式化为相对时间（如：2 hours ago）
    /// </summary>
    string FormatRelativeTime(DateTime utcDateTime, CultureInfo? culture = null);
    
    /// <summary>
    /// 格式化为相对时间（如：2 hours ago）- 可空版本
    /// </summary>
    string FormatRelativeTime(DateTime? utcDateTime, string defaultValue = "-", CultureInfo? culture = null);
    
    /// <summary>
    /// 将 UTC 时间转换为本地时间
    /// </summary>
    DateTime ToLocalTime(DateTime utcDateTime);
    
    /// <summary>
    /// 将 UTC 时间转换为本地时间 - 可空版本
    /// </summary>
    DateTime? ToLocalTime(DateTime? utcDateTime);
}

/// <summary>
/// 时间格式化服务实现
/// 在 Blazor WebAssembly 中，DateTime.ToLocalTime() 会自动使用浏览器的时区
/// </summary>
public class DateTimeFormatter : IDateTimeFormatter
{
    public string FormatShortDate(DateTime utcDateTime)
    {
        var localTime = utcDateTime.ToLocalTime();
        return localTime.ToString("yyyy-MM-dd");
    }
    
    public string FormatShortDate(DateTime? utcDateTime, string defaultValue = "-")
    {
        return utcDateTime.HasValue ? FormatShortDate(utcDateTime.Value) : defaultValue;
    }
    
    public string FormatDateTime(DateTime utcDateTime)
    {
        var localTime = utcDateTime.ToLocalTime();
        return localTime.ToString("yyyy-MM-dd HH:mm");
    }
    
    public string FormatDateTime(DateTime? utcDateTime, string defaultValue = "-")
    {
        return utcDateTime.HasValue ? FormatDateTime(utcDateTime.Value) : defaultValue;
    }
    
    public string FormatFriendlyDate(DateTime utcDateTime)
    {
        var localTime = utcDateTime.ToLocalTime();
        return localTime.ToString("MMM dd, yyyy");
    }
    
    public string FormatFriendlyDate(DateTime? utcDateTime, string defaultValue = "-")
    {
        return utcDateTime.HasValue ? FormatFriendlyDate(utcDateTime.Value) : defaultValue;
    }
    
    public string FormatFriendlyDateTime(DateTime utcDateTime)
    {
        var localTime = utcDateTime.ToLocalTime();
        return localTime.ToString("MMM dd, yyyy HH:mm");
    }
    
    public string FormatFriendlyDateTime(DateTime? utcDateTime, string defaultValue = "-")
    {
        return utcDateTime.HasValue ? FormatFriendlyDateTime(utcDateTime.Value) : defaultValue;
    }
    
    public string FormatRelativeTime(DateTime utcDateTime, CultureInfo? culture = null)
    {
        var localTime = utcDateTime.ToLocalTime();
        var now = DateTime.Now;
        var timeSpan = now - localTime;
        
        // 使用当前文化或指定文化
        var currentCulture = culture ?? CultureInfo.CurrentCulture;
        var isZhCn = currentCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        
        if (timeSpan.TotalMinutes < 1)
        {
            return isZhCn ? "刚刚" : "just now";
        }
        else if (timeSpan.TotalMinutes < 60)
        {
            var minutes = (int)timeSpan.TotalMinutes;
            return isZhCn ? $"{minutes} 分钟前" : $"{minutes} minute{(minutes > 1 ? "s" : "")} ago";
        }
        else if (timeSpan.TotalHours < 24)
        {
            var hours = (int)timeSpan.TotalHours;
            return isZhCn ? $"{hours} 小时前" : $"{hours} hour{(hours > 1 ? "s" : "")} ago";
        }
        else if (timeSpan.TotalDays < 30)
        {
            var days = (int)timeSpan.TotalDays;
            return isZhCn ? $"{days} 天前" : $"{days} day{(days > 1 ? "s" : "")} ago";
        }
        else if (timeSpan.TotalDays < 365)
        {
            var months = (int)(timeSpan.TotalDays / 30);
            return isZhCn ? $"{months} 个月前" : $"{months} month{(months > 1 ? "s" : "")} ago";
        }
        else
        {
            var years = (int)(timeSpan.TotalDays / 365);
            return isZhCn ? $"{years} 年前" : $"{years} year{(years > 1 ? "s" : "")} ago";
        }
    }
    
    public string FormatRelativeTime(DateTime? utcDateTime, string defaultValue = "-", CultureInfo? culture = null)
    {
        return utcDateTime.HasValue ? FormatRelativeTime(utcDateTime.Value, culture) : defaultValue;
    }
    
    public DateTime ToLocalTime(DateTime utcDateTime)
    {
        return utcDateTime.ToLocalTime();
    }
    
    public DateTime? ToLocalTime(DateTime? utcDateTime)
    {
        return utcDateTime?.ToLocalTime();
    }
}
