using MABamlai.Model;

namespace MABamlai.Services;

public sealed class ElectronicsInventoryService
{
    private readonly object syncLock = new();
    private readonly List<ElectronicProduct> products = new();
    private int nextId = 1;

    public ElectronicsInventoryService()
    {
        SeedDefaults();
    }

    public IReadOnlyList<ElectronicProduct> GetAllProducts()
    {
        lock (syncLock)
        {
            return products
                .Select(product => product.Clone())
                .OrderBy(product => product.GetName(), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public ElectronicProduct? GetProductById(int id)
    {
        lock (syncLock)
        {
            ElectronicProduct? product = products.FirstOrDefault(item => item.Id == id);
            return product is null ? null : product.Clone();
        }
    }

    public int AddProduct(NewElectronicProduct request)
    {
        lock (syncLock)
        {
            DateTime nowUtc = DateTime.UtcNow;
            int id = nextId++;

            ElectronicProduct product = new ElectronicProduct(
                id,
                request.Name,
                request.Category,
                request.SerialNumber);

            string notes = string.IsNullOrWhiteSpace(request.Notes)
                ? "Component added to inventory."
                : request.Notes.Trim();

            product.RecordLocationChange("New", "Closet", notes, nowUtc);
            products.Add(product);
            return id;
        }
    }

    private void SeedDefaults()
    {
        AddSeededProduct(
            "Power Distribution Hub (PDH)",
            "Power",
            "PDH-001",
            "Robot #5951",
            "Closet",
            new DateTime(2025, 9, 1, 10, 30, 0, DateTimeKind.Utc),
            "Removed after off-season maintenance.");

        AddSeededProduct(
            "RoboRIO 2.0",
            "Controller",
            "RIO-014",
            "Robot #5951",
            "Closet",
            new DateTime(2025, 9, 3, 15, 0, 0, DateTimeKind.Utc),
            "Stored after diagnostics.");

        AddSeededProduct(
            "CANivore",
            "CAN Bus",
            "CAN-009",
            "Pit Station",
            "Closet",
            new DateTime(2025, 9, 5, 11, 20, 0, DateTimeKind.Utc),
            "Moved after event prep.");

        AddSeededProduct(
            "Pneumatics Hub (PH)",
            "Pneumatics",
            "PH-006",
            "Robot #5951",
            "Closet",
            new DateTime(2025, 9, 7, 8, 45, 0, DateTimeKind.Utc),
            "Stored as backup unit.");

        AddSeededProduct(
            "SPARK MAX",
            "Motor Controller",
            "SPK-021",
            "Electrical Bench",
            "Closet",
            new DateTime(2025, 9, 7, 8, 45, 0, DateTimeKind.Utc),
            "Returned after motor tests.");
    }

    private void AddSeededProduct(
        string name,
        string category,
        string serialNumber,
        string fromLocation,
        string toLocation,
        DateTime changedAtUtc,
        string notes)
    {
        int id = nextId++;
        ElectronicProduct product = new ElectronicProduct(id, name, category, serialNumber);
        product.RecordLocationChange(fromLocation, toLocation, notes, changedAtUtc);
        products.Add(product);
    }
}

public sealed class ElectronicProduct
{
    private readonly List<ProductHistoryEntry> history = new();

    public int Id { get; }
    private string Name { get; set; }
    private string Category { get; set; }
    private string? SerialNumber { get; set; }
    private string CurrentLocation { get; set; }
    private DateTime LastUpdatedUtc { get; set; }

    public ElectronicProduct(int id, string name, string category, string? serialNumber)
    {
        Id = id;
        Name = NormalizeRequired(name, nameof(name));
        Category = NormalizeRequired(category, nameof(category));
        SerialNumber = NormalizeOptional(serialNumber);
        CurrentLocation = "Unknown";
        LastUpdatedUtc = DateTime.UtcNow;
    }

    public string GetName() => Name;
    public string GetCategory() => Category;
    public string? GetSerialNumber() => SerialNumber;
    public string GetSerialOrFallback() => string.IsNullOrWhiteSpace(SerialNumber) ? "N/A" : SerialNumber!;
    public string GetCurrentLocation() => CurrentLocation;
    public DateTime GetLastUpdatedUtc() => LastUpdatedUtc;

    public IReadOnlyList<ProductHistoryEntry> GetHistory()
    {
        return history
            .Select(item => item.Clone())
            .OrderByDescending(item => item.GetChangedAtUtc())
            .ToList();
    }

    public void MoveTo(string newLocation, string notes, DateTime changedAtUtc)
    {
        string fromLocation = CurrentLocation;
        RecordLocationChange(fromLocation, newLocation, notes, changedAtUtc);
    }

    public void RecordLocationChange(string fromLocation, string toLocation, string notes, DateTime changedAtUtc)
    {
        string normalizedFrom = NormalizeRequired(fromLocation, nameof(fromLocation));
        string normalizedTo = NormalizeRequired(toLocation, nameof(toLocation));

        ProductHistoryEntry entry = new ProductHistoryEntry(
            changedAtUtc,
            normalizedFrom,
            normalizedTo,
            string.IsNullOrWhiteSpace(notes) ? "Location updated." : notes.Trim());

        history.Add(entry);
        CurrentLocation = normalizedTo;
        LastUpdatedUtc = changedAtUtc;
    }

    public ElectronicProduct Clone()
    {
        ElectronicProduct clone = new ElectronicProduct(Id, Name, Category, SerialNumber)
        {
            CurrentLocation = CurrentLocation,
            LastUpdatedUtc = LastUpdatedUtc
        };

        foreach (ProductHistoryEntry item in history)
        {
            clone.history.Add(item.Clone());
        }

        return clone;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} cannot be empty.", fieldName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class ProductHistoryEntry
{
    private readonly DateTime changedAtUtc;
    private readonly string fromLocation;
    private readonly string toLocation;
    private readonly string notes;

    public ProductHistoryEntry(DateTime changedAtUtc, string fromLocation, string toLocation, string notes)
    {
        this.changedAtUtc = changedAtUtc;
        this.fromLocation = fromLocation;
        this.toLocation = toLocation;
        this.notes = notes;
    }

    public DateTime GetChangedAtUtc() => changedAtUtc;
    public string GetFromLocation() => fromLocation;
    public string GetToLocation() => toLocation;
    public string GetNotes() => notes;
    public string GetMoveText() => $"{fromLocation} -> {toLocation}";

    public ProductHistoryEntry Clone()
    {
        return new ProductHistoryEntry(changedAtUtc, fromLocation, toLocation, notes);
    }
}

public sealed class NewElectronicProduct
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}
