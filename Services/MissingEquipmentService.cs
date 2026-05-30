using Microsoft.Data.Sqlite;

namespace MABamlai.Services;

public sealed class MissingEquipmentService
{
    private const string DefaultReason = "No reason provided.";
    private const string PreferredTableName = "missingProduct";
    private readonly object syncLock = new();
    private readonly string connectionString =
        "Data Source=C:\\Users\\almak\\Documents\\computer science\\MABamlai\\MABamlai\\Data\\DataBase.db";

    public int AddReport(NewMissingEquipmentReport request)
    {
        lock (syncLock)
        {
            using SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            string tableName = ResolveOrCreateTableName(connection);
            MissingTableColumnMap columnMap = GetTableColumnMap(connection, tableName);

            if (string.IsNullOrWhiteSpace(columnMap.EquipmentNameColumn) || string.IsNullOrWhiteSpace(columnMap.QuantityColumn))
            {
                throw new InvalidOperationException(
                    $"Table '{tableName}' does not include required columns for equipment name and quantity.");
            }

            List<string> columnNames = new List<string>();
            List<string> parameterNames = new List<string>();
            SqliteCommand insertCommand = connection.CreateCommand();

            AddInsertValue(
                insertCommand,
                columnNames,
                parameterNames,
                columnMap.EquipmentNameColumn,
                "$equipmentName",
                request.EquipmentName.Trim());

            AddInsertValue(
                insertCommand,
                columnNames,
                parameterNames,
                columnMap.QuantityColumn,
                "$quantityNeeded",
                request.QuantityNeeded);

            if (!string.IsNullOrWhiteSpace(columnMap.ReasonColumn))
            {
                AddInsertValue(
                    insertCommand,
                    columnNames,
                    parameterNames,
                    columnMap.ReasonColumn,
                    "$whyNeeded",
                    string.IsNullOrWhiteSpace(request.WhyNeeded) ? DefaultReason : request.WhyNeeded.Trim());
            }

            if (!string.IsNullOrWhiteSpace(columnMap.CreatedAtColumn))
            {
                AddInsertValue(
                    insertCommand,
                    columnNames,
                    parameterNames,
                    columnMap.CreatedAtColumn,
                    "$createdAtUtc",
                    DateTime.UtcNow.ToString("O"));
            }

            insertCommand.CommandText =
                $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames.Select(name => $"\"{name}\""))}) " +
                $"VALUES ({string.Join(", ", parameterNames)})";

            int affectedRows = insertCommand.ExecuteNonQuery();
            if (affectedRows <= 0)
            {
                throw new InvalidOperationException("Could not insert missing equipment report.");
            }

            SqliteCommand idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid()";
            long insertedId = (long)(idCommand.ExecuteScalar() ?? 0L);
            return (int)insertedId;
        }
    }

    public IReadOnlyList<MissingEquipmentReport> GetAllReports()
    {
        lock (syncLock)
        {
            using SqliteConnection connection = new SqliteConnection(connectionString);
            connection.Open();

            string tableName = ResolveOrCreateTableName(connection);
            MissingTableColumnMap columnMap = GetTableColumnMap(connection, tableName);

            if (string.IsNullOrWhiteSpace(columnMap.EquipmentNameColumn) || string.IsNullOrWhiteSpace(columnMap.QuantityColumn))
            {
                return Array.Empty<MissingEquipmentReport>();
            }

            SqliteCommand readCommand = connection.CreateCommand();
            readCommand.CommandText = $"SELECT * FROM \"{tableName}\"";

            List<MissingEquipmentReport> loadedReports = new List<MissingEquipmentReport>();
            using SqliteDataReader reader = readCommand.ExecuteReader();
            int fallbackId = 1;

            while (reader.Read())
            {
                int id = TryReadInt(reader, columnMap.IdColumn, fallbackId);
                string equipmentName = TryReadText(reader, columnMap.EquipmentNameColumn, "Unknown equipment");
                int quantity = Math.Max(1, TryReadInt(reader, columnMap.QuantityColumn, 1));
                string whyNeeded = TryReadText(reader, columnMap.ReasonColumn, DefaultReason);
                DateTime createdAtUtc = TryReadDateTime(reader, columnMap.CreatedAtColumn, DateTime.UtcNow);

                loadedReports.Add(new MissingEquipmentReport(id, equipmentName, quantity, whyNeeded, createdAtUtc));
                fallbackId++;
            }

            return loadedReports
                .OrderByDescending(report => report.GetCreatedAtUtc())
                .ToList();
        }
    }

    private static void AddInsertValue(
        SqliteCommand command,
        List<string> columnNames,
        List<string> parameterNames,
        string? columnName,
        string parameterName,
        object value)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return;
        }

        columnNames.Add(columnName);
        parameterNames.Add(parameterName);
        command.Parameters.AddWithValue(parameterName, value);
    }

    private static int TryReadInt(SqliteDataReader reader, string? columnName, int fallback)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return fallback;
        }

        object rawValue = reader[columnName];
        if (rawValue is null || rawValue == DBNull.Value)
        {
            return fallback;
        }

        if (rawValue is int intValue)
        {
            return intValue;
        }

        if (rawValue is long longValue && longValue <= int.MaxValue && longValue >= int.MinValue)
        {
            return (int)longValue;
        }

        if (int.TryParse(rawValue.ToString(), out int parsedValue))
        {
            return parsedValue;
        }

        return fallback;
    }

    private static string TryReadText(SqliteDataReader reader, string? columnName, string fallback)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return fallback;
        }

        object rawValue = reader[columnName];
        if (rawValue is null || rawValue == DBNull.Value)
        {
            return fallback;
        }

        string text = rawValue.ToString() ?? string.Empty;
        return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
    }

    private static DateTime TryReadDateTime(SqliteDataReader reader, string? columnName, DateTime fallback)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return fallback;
        }

        object rawValue = reader[columnName];
        if (rawValue is null || rawValue == DBNull.Value)
        {
            return fallback;
        }

        if (rawValue is DateTime dateTimeValue)
        {
            return dateTimeValue.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc)
                : dateTimeValue.ToUniversalTime();
        }

        string text = rawValue.ToString() ?? string.Empty;
        if (DateTime.TryParse(text, out DateTime parsed))
        {
            return parsed.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : parsed.ToUniversalTime();
        }

        return fallback;
    }

    private static MissingTableColumnMap GetTableColumnMap(SqliteConnection connection, string tableName)
    {
        HashSet<string> columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        SqliteCommand columnsCommand = connection.CreateCommand();
        columnsCommand.CommandText = $"PRAGMA table_info(\"{tableName}\")";

        using SqliteDataReader reader = columnsCommand.ExecuteReader();
        while (reader.Read())
        {
            string columnName = reader["name"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                columnNames.Add(columnName);
            }
        }

        return new MissingTableColumnMap(
            FindColumn(columnNames, "Id", "ID", "id"),
            FindColumn(columnNames, "EquipmentName", "equipmentName", "productName", "ProductName", "name", "Name"),
            FindColumn(columnNames, "QuantityNeeded", "quantityNeeded", "Quantity", "quantity", "Amount", "amount", "boxes", "Boxes"),
            FindColumn(columnNames, "WhyNeeded", "whyNeeded", "Reason", "reason", "Notes", "notes"),
            FindColumn(columnNames, "CreatedAtUtc", "createdAtUtc", "CreatedAt", "createdAt", "CreatedDate", "createdDate"));
    }

    private static string? FindColumn(HashSet<string> columns, params string[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            if (columns.Contains(candidates[i]))
            {
                return candidates[i];
            }
        }

        return null;
    }

    private static string ResolveOrCreateTableName(SqliteConnection connection)
    {
        List<string> existingTables = new List<string>();
        SqliteCommand tablesCommand = connection.CreateCommand();
        tablesCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";

        using (SqliteDataReader reader = tablesCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                string tableName = reader["name"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    existingTables.Add(tableName);
                }
            }
        }

        string[] candidateTableNames =
        [
            "missingProduct",
            "MissingProduct",
            "missing_equipment",
            "MissingEquipment",
            "missingEquipment",
            "MissingEquipmentReports"
        ];

        for (int i = 0; i < candidateTableNames.Length; i++)
        {
            string candidate = candidateTableNames[i];
            for (int j = 0; j < existingTables.Count; j++)
            {
                if (existingTables[j].Equals(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return existingTables[j];
                }
            }
        }

        SqliteCommand createCommand = connection.CreateCommand();
        createCommand.CommandText =
            $"CREATE TABLE IF NOT EXISTS \"{PreferredTableName}\" (" +
            "\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "\"EquipmentName\" TEXT NOT NULL, " +
            "\"QuantityNeeded\" INTEGER NOT NULL, " +
            "\"WhyNeeded\" TEXT NOT NULL DEFAULT 'No reason provided.', " +
            "\"CreatedAtUtc\" TEXT NOT NULL)";
        createCommand.ExecuteNonQuery();
        return PreferredTableName;
    }

    private sealed class MissingTableColumnMap
    {
        public string? IdColumn { get; }
        public string? EquipmentNameColumn { get; }
        public string? QuantityColumn { get; }
        public string? ReasonColumn { get; }
        public string? CreatedAtColumn { get; }

        public MissingTableColumnMap(
            string? idColumn,
            string? equipmentNameColumn,
            string? quantityColumn,
            string? reasonColumn,
            string? createdAtColumn)
        {
            IdColumn = idColumn;
            EquipmentNameColumn = equipmentNameColumn;
            QuantityColumn = quantityColumn;
            ReasonColumn = reasonColumn;
            CreatedAtColumn = createdAtColumn;
        }
    }
}

public sealed class MissingEquipmentReport
{
    public int Id { get; }
    private string EquipmentName { get; set; }
    private int QuantityNeeded { get; set; }
    private string WhyNeeded { get; set; }
    private DateTime CreatedAtUtc { get; set; }

    public MissingEquipmentReport(int id, string equipmentName, int quantityNeeded, string whyNeeded, DateTime createdAtUtc)
    {
        Id = id;
        EquipmentName = NormalizeRequiredText(equipmentName, nameof(equipmentName));
        QuantityNeeded = NormalizeQuantity(quantityNeeded);
        WhyNeeded = NormalizeRequiredText(whyNeeded, nameof(whyNeeded));
        CreatedAtUtc = createdAtUtc;
    }

    public string GetEquipmentName() => EquipmentName;
    public int GetQuantityNeeded() => QuantityNeeded;
    public string GetWhyNeeded() => WhyNeeded;
    public DateTime GetCreatedAtUtc() => CreatedAtUtc;

    public MissingEquipmentReport Clone()
    {
        return new MissingEquipmentReport(Id, EquipmentName, QuantityNeeded, WhyNeeded, CreatedAtUtc);
    }

    private static string NormalizeRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} cannot be empty.", fieldName);
        }

        return value.Trim();
    }

    private static int NormalizeQuantity(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Quantity must be at least 1.");
        }

        return value;
    }
}

public sealed class NewMissingEquipmentReport
{
    public string EquipmentName { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; }
    public string WhyNeeded { get; set; } = string.Empty;
}
