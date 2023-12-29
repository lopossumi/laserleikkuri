public class TimeSlot : IEquatable<TimeSlot>
{
    public DateTime Begin { get; set; }
    public DateTime End { get; set; }

    public bool Equals(TimeSlot? other)
    {
        if (other == null)
        {
            return false;
        }
        return Begin == other.Begin && End == other.End;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (obj is TimeSlot other)
        {
            return Equals(other);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Begin.GetHashCode() ^ End.GetHashCode();
    }

    public override string ToString()
    {
        return $"{Begin.GetDateString()}: {Begin.GetTimeString()} - {End.GetTimeString()} ({(End - Begin).TotalHours}h)";
    }
}
