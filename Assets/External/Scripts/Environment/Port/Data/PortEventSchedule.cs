[System.Serializable]
public struct PortEventSchedule
{
    public string eventId;

    [UnityEngine.Tooltip("0 = any year")]
    public int year;
    [UnityEngine.Tooltip("0 = any month")]
    public int month;
    [UnityEngine.Tooltip("0 = any week")]
    public int week;
    [UnityEngine.Tooltip("0 = any day")]
    public int day;

    /// <summary>
    /// 주어진 날짜에 이 이벤트가 활성화되는지 확인.
    /// 0으로 설정된 필드는 와일드카드로 취급하여 항상 일치.
    /// </summary>
    public bool IsActiveOn(int currentYear, int currentMonth, int currentWeek, int currentDay)
    {
        if (year > 0 && year != currentYear) return false;
        if (month > 0 && month != currentMonth) return false;
        if (week > 0 && week != currentWeek) return false;
        if (day > 0 && day != currentDay) return false;
        return true;
    }
}
