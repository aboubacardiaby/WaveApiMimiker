namespace WaveApiMimiker.Data;

public static class WaveConstants
{
    public static readonly Dictionary<string, string> CountryCurrencies = new()
    {
        ["SN"] = "XOF",
        ["CI"] = "XOF",
        ["ML"] = "XOF",
        ["BF"] = "XOF",
        ["UG"] = "UGX",
        ["GM"] = "GMD",
        ["GH"] = "GHS",
    };

    public static readonly HashSet<string> SupportedCountries = new(CountryCurrencies.Keys);

    // Maps E.164 phone prefix → ISO country code.
    // Longer prefixes must come first so "+2250" matches CI before "+225".
    public static readonly (string Prefix, string CountryCode)[] PhonePrefixes =
    [
        ("+2250", "CI"),  // Côte d'Ivoire (must be before +225 catch-all)
        ("+221",  "SN"),  // Senegal
        ("+223",  "ML"),  // Mali
        ("+224",  "GN"),  // Guinea (not Wave-supported, kept for reference)
        ("+225",  "CI"),  // Côte d'Ivoire
        ("+226",  "BF"),  // Burkina Faso
        ("+220",  "GM"),  // Gambia
        ("+233",  "GH"),  // Ghana
        ("+256",  "UG"),  // Uganda
    ];

    /// <summary>
    /// Returns the ISO country code for a given E.164 phone number, or null if unknown.
    /// </summary>
    public static string? DetectCountry(string phone)
    {
        foreach (var (prefix, code) in PhonePrefixes)
            if (phone.StartsWith(prefix))
                return code;
        return null;
    }
}
