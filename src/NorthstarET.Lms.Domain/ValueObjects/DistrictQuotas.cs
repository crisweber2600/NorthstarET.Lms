namespace NorthstarET.Lms.Domain.ValueObjects;

public record DistrictQuotas
{
    private int _maxStudents;
    private int _maxStaff;
    private int _maxAdmins;

    public int MaxStudents 
    { 
        get => _maxStudents;
        init => _maxStudents = value > 0 ? value : throw new ArgumentException("MaxStudents must be positive", nameof(MaxStudents));
    }

    public int MaxStaff 
    { 
        get => _maxStaff;
        init => _maxStaff = value > 0 ? value : throw new ArgumentException("MaxStaff must be positive", nameof(MaxStaff));
    }

    public int MaxAdmins 
    { 
        get => _maxAdmins;
        init => _maxAdmins = value > 0 ? value : throw new ArgumentException("MaxAdmins must be positive", nameof(MaxAdmins));
    }

    public bool IsWithinLimits(int currentStudents, int currentStaff, int currentAdmins)
    {
        return currentStudents <= MaxStudents && 
               currentStaff <= MaxStaff && 
               currentAdmins <= MaxAdmins;
    }

    public QuotaUtilization CalculateUtilization(int currentStudents, int currentStaff, int currentAdmins)
    {
        return new QuotaUtilization
        {
            StudentUtilization = (double)currentStudents / MaxStudents * 100,
            StaffUtilization = (double)currentStaff / MaxStaff * 100,
            AdminUtilization = (double)currentAdmins / MaxAdmins * 100
        };
    }
}

public record QuotaUtilization
{
    public double StudentUtilization { get; init; }
    public double StaffUtilization { get; init; }
    public double AdminUtilization { get; init; }
}